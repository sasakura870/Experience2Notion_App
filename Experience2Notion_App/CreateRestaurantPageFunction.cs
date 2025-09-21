using Experience2Notion.Exceptions;
using Experience2Notion.Services;
using Experience2Notion_App.Models;
using Google.Apis.CustomSearchAPI.v1.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Experience2Notion_App;

public partial class CreateRestaurantPageFunction(ILogger<CreateRestaurantPageFunction> logger, GoogleEngineSearcher googleEngineSearcher, NotionClient notionClient)
{
    private readonly ILogger<CreateRestaurantPageFunction> _logger = logger;
    private readonly GoogleEngineSearcher _googleEngineSearcher = googleEngineSearcher;
    private readonly NotionClient _notionClient = notionClient;

    [Function("CreateRestaurantPage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<CreateRestaurantRequest>(requestBody);

        if (data is null || string.IsNullOrWhiteSpace(data.Name))
        {
            return new BadRequestObjectResult("店名が指定されていません。");
        }
        if (string.IsNullOrWhiteSpace(data.Address))
        {
            return new BadRequestObjectResult("住所が指定されていません。");
        }
        var lines = data.Address.Split('\n');
        // 住所は5行のデータで来ることを想定
        // 1行目: 店名、2行目: 郵便番号 3行目: 市町村、4行目: 番地、5行目: 国
        // 2行目と3行目を結合して、空白とカンマを削除したものを住所とする
        var address = $"{lines[2]}{lines[3]}".Replace(" ", "").Replace("　", "").Replace(",", "");

        Result? targetSearchResult = null;
        var searchCount = 0;
        var isReachNotFound = false;
        while (targetSearchResult is null)
        {
            try
            {
                searchCount++;
                var query = isReachNotFound ? $"{data.Name} {address}" : $"{data.Name}";
                var searchResult = await _googleEngineSearcher.GetSearchResultAsync(query, searchCount);
                var tabelogRegex = TabelogRegex();
                targetSearchResult = searchResult.FirstOrDefault(x => tabelogRegex.IsMatch(x.Link) && x.Title.Contains(data.Name));
            }
            catch (Experience2NotionException)
            {
                _logger.LogInformation("検索結果が見つかりません。");
                if (isReachNotFound)
                {
                    if (targetSearchResult == null)
                    {
                        return new BadRequestObjectResult("食べログのリンクが見つかりませんでした。");
                    }
                }
                else
                {
                    _logger.LogInformation("検索クエリを緩和して再度検索します。");
                    searchCount = 0;
                    isReachNotFound = true;
                }
            }
        }
        if (targetSearchResult == null)
        {
            return new BadRequestObjectResult("食べログのリンクが見つかりませんでした。");
        }
        var link = targetSearchResult.Link;
        // 食べログのタイトルは「店名 - 住所」の形式なので、" - "で分割して最初の要素を店名とする
        var name = targetSearchResult.Title.Split(" - ")[0];
        var visitDate = DateTime.Parse(data.VisitDate).ToString("yyyy-MM-dd");

        var imageIdList = new List<string>();
        foreach (var photo in data.Photos)
        {
            var photoData = Convert.FromBase64String(photo);
            var imageId = await _notionClient.UploadImageAsync($"{data.Name}.jpg", photoData, MediaTypeNames.Image.Jpeg);
            imageIdList.Add(imageId);
        }

        var result = await _notionClient.CreateRestaurantPageAsync(name, address, link, visitDate, imageIdList);
        return new OkObjectResult(result);
    }

    // 食べログの店トップページにマッチする正規表現
    [GeneratedRegex(@"^https://tabelog\.com/[^/]+/[^/]+/[^/]+/\d+/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ja-JP")]
    private static partial Regex TabelogRegex();
}