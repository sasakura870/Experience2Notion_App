# AGENTS.md

## リポジトリの目的

Experience2Notion_App は、個人の Notion データベースに書籍、音楽アルバム、飲食店の感想ページを作成するための Azure Functions API です。

外部サービス検索、Notion API ペイロード作成、画像アップロード、Notion ページ作成のロジックは関連リポジトリ `Experience2Notion` のクラスライブラリが担当し、このリポジトリは HTTP トリガーの受付、リクエストの検証、ライブラリ呼び出しのオーケストレーションを担当します。

主な利用フローは以下です。

1. iPhone ショートカットで書籍、音楽アルバム、飲食店の情報を取得する。
2. ショートカットがこの Azure Functions API を呼び出す。
3. API が `Experience2Notion` ライブラリを呼び出す。
4. ライブラリが必要に応じて外部サービスを検索し、Notion ページを作成する。
5. API レスポンスをもとに、iPhone 側で作成された Notion ページを開く。

## 技術スタック

- 言語: C#
- ターゲットフレームワーク: .NET 9
- Azure Functions v4 (isolated worker、`Microsoft.Azure.Functions.Worker`)
- HTTP トリガーは ASP.NET Core 統合 (`HttpTrigger` + `IActionResult`)
- テレメトリ: Application Insights
- `Experience2Notion` ライブラリはプロジェクト参照 (`..\..\Experience2Notion\Experience2Notion\Experience2Notion.csproj`)

## ディレクトリ構成

- `Experience2Notion_App/CreateBookPageFunction.cs`: 書籍ページ作成の Function。ISBN 検索、表紙取得、ページ作成を行う。
- `Experience2Notion_App/CreateMusicAlbumPageFunction.cs`: 音楽アルバムページ作成の Function。アルバム検索、ジャケット取得、ページ作成を行う。
- `Experience2Notion_App/CreateRestaurantPageFunction.cs`: 飲食店ページ作成の Function。食べログのリンク検索、写真アップロード、ページ作成を行う。
- `Experience2Notion_App/Models/`: 各エンドポイントのリクエストモデル (`System.Text.Json` の `JsonPropertyName` で snake_case にマッピング)。
- `Experience2Notion_App/Program.cs`: DI 登録 (`Experience2Notion` のサービスクラスをシングルトンで登録)。

## 実装時の注意

- リクエスト JSON のプロパティ名 (`isbn`、`title`、`artist`、`name`、`address`、`visit_date`、`photos`) は iPhone ショートカットとの契約。変更する場合はショートカット側も更新が必要。
- すべての Function は `AuthorizationLevel.Function` の POST。レスポンスは成功時に Notion のページ作成レスポンス、失敗時に 400 とエラーメッセージを返す。
- 飲食店の `address` は 5 行形式 (1行目: 店名、2行目: 郵便番号、3行目: 市町村、4行目: 番地、5行目: 国) を前提にパースする。
- 飲食店の食べログ検索は Brave Search API (`BraveSearchClient`) を利用する。まず店名のみで検索し、見つからない場合は店名 + 住所で再検索する。クエリ1つあたりの検索ページ数には上限 (`MaxSearchPages`) があり、クォータ枯渇を防ぐ。店舗トップページの URL 形式は正規表現 (`TabelogRegex`) で判定する。
- `BraveSearchClient` は「検索結果0件」と「APIエラー (レート制限など)」の両方で `Experience2NotionException` を投げる。0件はメッセージで判別してクエリ緩和・打ち切りに使い、APIエラーは呼び出し元に伝播させて 400 を返す。
- ソースファイルのエンコーディングは UTF-8 (BOM付き) に統一する。Shift-JIS で保存すると日本語文字列が文字化けする (issue #19)。
- `Experience2Notion` ライブラリの公開クラスや公開メソッドの形に依存している。ライブラリ側との契約変更は両リポジトリ合わせて行う。
- 新しいサービスクラスを利用する場合は `Program.cs` に DI 登録を追加する。
- シークレット、API キー、データベース ID、`local.settings.json` はコミットしない。

## 検証

- コード変更後はリポジトリルートで `dotnet build` を実行する。ビルドには `Experience2Notion` リポジトリが同じ親ディレクトリに存在する必要がある。
- 現時点ではテストプロジェクトは存在しない。今後テストを追加する場合は、外部 HTTP/API 呼び出しをライブサービスではなくモック化する。
- リクエスト契約に影響する変更は iPhone ショートカット側、ライブラリ契約に影響する変更は `Experience2Notion` リポジトリと合わせて確認する。
