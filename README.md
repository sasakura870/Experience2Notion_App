# Experience2Notion_App

Experience2Notion_App は、個人の Notion データベースに感想ページを作成するための Azure Functions API です。iPhone ショートカットから呼び出され、クラスライブラリ `Experience2Notion` を利用して書籍、音楽アルバム、飲食店のページ作成を行います。

## 全体の流れ

1. iPhone で「Notionページを作成」ショートカットを起動する。
2. ショートカットがページ種別に応じた情報を取得する。
3. ショートカットがこの Azure Functions API を呼び出す。
4. API が `Experience2Notion` ライブラリを利用し、外部サービス検索や Notion ページ作成を行う。
5. API のレスポンスをもとに、iPhone 側で作成された Notion ページを開く。

## エンドポイント

すべて HTTP トリガー (POST) で、認可レベルは `Function` です。リクエストボディは JSON です。

### CreateBookPage

ISBN をもとに Google Books で書籍情報を検索し、表紙画像を取得して Notion に書籍ページを作成します。

```json
{
    "isbn": "9784041026221"
}
```

### CreateMusicAlbumPage

アルバム名とアーティスト名をもとに Spotify または MusicBrainz でアルバム情報を検索し、ジャケット画像を取得して Notion に音楽アルバムページを作成します。

```json
{
    "title": "アルバム名",
    "artist": "アーティスト名"
}
```

### CreateRestaurantPage

店名と住所をもとに Brave Search API で食べログの店舗ページを検索し、渡された写真をアップロードして Notion に飲食店ページを作成します。

```json
{
    "name": "店名",
    "address": "店名\n郵便番号\n市町村\n番地\n国",
    "visit_date": "2026-07-09",
    "photos": ["<Base64エンコードされた画像>"]
}
```

- `address` は iPhone ショートカットが取得する 5 行形式 (1行目: 店名、2行目: 郵便番号、3行目: 市町村、4行目: 番地、5行目: 国) を想定しています。
- `photos` は Base64 エンコードされた JPEG 画像の配列です。

### レスポンス

成功時は Notion API の ページ作成レスポンス (作成されたページの URL を含む) をそのまま返します。失敗時は 400 とエラーメッセージを返します。

## 環境変数

Azure Functions のアプリケーション設定 (ローカルでは `local.settings.json`) に以下を設定します。

- `NOTION_API_KEY`
- `NOTION_DB_ID`
- `GOOGLE_BOOKS_API_KEY`
- `SPOTIFY_CLIENT_ID`
- `SPOTIFY_CLIENT_SECRET`
- `BRAVE_SEARCH_API_KEY`

`local.settings.json` はコミットしないでください。

## 技術スタック

- .NET 9 / Azure Functions v4 (isolated worker)
- Application Insights
- `Experience2Notion` クラスライブラリ (プロジェクト参照)

## ビルド

`Experience2Notion` リポジトリを本リポジトリと同じ親ディレクトリに配置した上で、リポジトリルートで以下を実行します。

```powershell
dotnet build
```

## 関連リポジトリ

- [Experience2Notion](https://github.com/sasakura870/Experience2Notion): このアプリが利用するクラスライブラリ。
