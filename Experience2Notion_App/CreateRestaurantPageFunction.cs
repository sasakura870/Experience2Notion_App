using Experience2Notion.Exceptions;
using Experience2Notion.Models.Braves;
using Experience2Notion.Services;
using Experience2Notion_App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Experience2Notion_App;

public partial class CreateRestaurantPageFunction(ILogger<CreateRestaurantPageFunction> logger, BraveSearchClient braveSearchClient, NotionClient notionClient)
{
    // クエリ1つあたりの最大検索ページ数 (クォータ枯渇防止のための上限)
    private const int MaxSearchPages = 3;

    private readonly ILogger<CreateRestaurantPageFunction> _logger = logger;
    private readonly BraveSearchClient _braveSearchClient = braveSearchClient;
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

        try
        {
            // まず店名のみで検索し、見つからなければ住所を加えたクエリで再検索する
            var targetSearchResult =
                await SearchTabelogPageAsync(data.Name, data.Name)
                ?? await SearchTabelogPageAsync($"{data.Name} {address}", data.Name);
            if (targetSearchResult is null)
            {
                return new BadRequestObjectResult("食べログのリンクが見つかりませんでした。");
            }

            var link = targetSearchResult.Url;
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
        catch (Experience2NotionException ex)
        {
            // Brave Search のAPIエラー (レート制限など) や Notion 関連のエラー
            return new BadRequestObjectResult(ex.Message);
        }
    }

    /// <summary>
    /// Brave Search で食べログの店トップページを検索する。
    /// 最大 <see cref="MaxSearchPages"/> ページまで検索し、見つからない場合は null を返す。
    /// </summary>
    /// <param name="query">検索クエリ。</param>
    /// <param name="name">店名 (検索結果のタイトルとの一致確認に使用)。</param>
    private async Task<BraveSearchResult?> SearchTabelogPageAsync(string query, string name)
    {
        var tabelogRegex = TabelogRegex();
        for (var offset = 0; offset < MaxSearchPages; offset++)
        {
            IReadOnlyList<BraveSearchResult> searchResults;
            try
            {
                searchResults = await _braveSearchClient.SearchAsync(query, offset);
            }
            catch (Experience2NotionException ex) when (ex.Message.Contains("検索結果が見つかりませんでした"))
            {
                // 検索結果0件。これ以上ページを進めても見つからないため、このクエリでの検索を打ち切る
                // (APIエラーの Experience2NotionException はここでは握りつぶさず、呼び出し元に伝播させる)
                _logger.LogInformation("検索結果が見つかりません。クエリ: {Query}, オフセット: {Offset}", query, offset);
                break;
            }

            var targetSearchResult = searchResults.FirstOrDefault(x => tabelogRegex.IsMatch(x.Url) && x.Title.Contains(name));
            if (targetSearchResult is not null)
            {
                return targetSearchResult;
            }
        }
        _logger.LogInformation("食べログのリンクが見つかりませんでした。クエリ: {Query}", query);
        return null;
    }

    // 食べログの店トップページにマッチする正規表現
    [GeneratedRegex(@"^https://tabelog\.com/[^/]+/[^/]+/[^/]+/\d+/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ja-JP")]
    private static partial Regex TabelogRegex();
}