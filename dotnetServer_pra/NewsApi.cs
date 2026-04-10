using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

public static class NewsAPI
{
    private static readonly string dbPath = "Data Source=external_news.db";
    private static readonly string ApiKey = File.ReadAllText("news_key.txt").Trim();

    public static void InitializeNewsDatabase()
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            -- IsEnabled (1:オン, 0:オフ) を追加
            CREATE TABLE IF NOT EXISTS Keywords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                Word TEXT NOT NULL UNIQUE,
                IsEnabled INTEGER DEFAULT 1
            );
            CREATE TABLE IF NOT EXISTS News (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                Title TEXT, 
                Url TEXT UNIQUE, 
                PublishedAt TEXT
            );
        ";
        command.ExecuteNonQuery();
    }

    public static void SetupRoutes(WebApplication app)
    {
        app.MapGet("/api/news/fetch", FetchNewsFromInternet);
        app.MapGet("/api/news/list", GetSavedNewsHtml);
        
        // 操作系：すべて実行後は list へ戻る
        app.MapGet("/api/news/keywords/add", AddKeyword);
        app.MapGet("/api/news/keywords/delete", DeleteKeyword);
        app.MapGet("/api/news/keywords/toggle", ToggleKeyword); // オンオフ切り替え
    }

    // --- キーワード操作 (すべてリダイレクト) ---
    private static IResult AddKeyword(string word)
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Keywords (Word, IsEnabled) VALUES ($w, 1)";
        cmd.Parameters.AddWithValue("$w", word);
        cmd.ExecuteNonQuery();
        return Results.Redirect("/api/news/list");
    }

    private static IResult DeleteKeyword(string word)
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Keywords WHERE Word = $w";
        cmd.Parameters.AddWithValue("$w", word);
        cmd.ExecuteNonQuery();
        return Results.Redirect("/api/news/list");
    }

    private static IResult ToggleKeyword(string word)
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();
        var cmd = connection.CreateCommand();
        // 1なら0に、0なら1に反転させる
        cmd.CommandText = "UPDATE Keywords SET IsEnabled = 1 - IsEnabled WHERE Word = $w";
        cmd.Parameters.AddWithValue("$w", word);
        cmd.ExecuteNonQuery();
        return Results.Redirect("/api/news/list");
    }

    // --- 取得ロジック (オンのワードのみ使用) ---
    private static async Task<IResult> FetchNewsFromInternet()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "WazaServerApp");

        // 有効なワードのみ取得
        var keywords = GetActiveKeywords();
        string query = keywords.Count > 0 ? string.Join(" OR ", keywords) : "AI";
        
        var url = $"https://newsapi.org/v2/everything?q={Uri.EscapeDataString(query)}&language=jp&sortBy=publishedAt&apiKey={ApiKey}";

        try {
            var response = await http.GetFromJsonAsync<NewsApiResponse>(url);
            if (response?.Articles != null) {
                using var connection = new SqliteConnection(dbPath);
                connection.Open();
                foreach (var article in response.Articles) {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "INSERT OR IGNORE INTO News (Title, Url, PublishedAt) VALUES ($t, $u, $p)";
                    cmd.Parameters.AddWithValue("$t", article.Title ?? "");
                    cmd.Parameters.AddWithValue("$u", article.Url ?? "");
                    cmd.Parameters.AddWithValue("$p", article.PublishedAt.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
            return Results.Redirect("/api/news/list");
        } catch { return Results.Redirect("/api/news/list"); }
    }

    // --- 見た目 (管理パネル付き) ---
    private static IResult GetSavedNewsHtml()
    {
        var allKeywords = GetAllKeywordStates(); // (Word, IsEnabled) のリスト

        var css = @"
<style>
    body { font-family: sans-serif; background: #f4f7f6; padding: 20px; }
    .panel { background: white; padding: 20px; border-radius: 10px; max-width: 800px; margin: 0 auto 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
    .keyword-tag { display: inline-block; padding: 5px 12px; margin: 5px; border-radius: 20px; text-decoration: none; font-size: 0.9em; }
    .tag-on { background: #e1f5fe; color: #0288d1; border: 1px solid #0288d1; }
    .tag-off { background: #eee; color: #888; border: 1px solid #ccc; text-decoration: line-through; }
    .del-btn { color: #ff5252; margin-left: 8px; font-weight: bold; text-decoration: none; }
    .add-form { margin-top: 15px; border-top: 1px solid #eee; padding-top: 15px; }
    
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); grid-gap: 15px; max-width: 1200px; margin: 0 auto; list-style: none; padding: 0; }
    .card { background: white; padding: 15px; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.05); }
    .card a { text-decoration: none; color: #333; font-weight: bold; font-size: 0.95em; }
</style>";

        var html = $"<html><head><meta charset='utf-8'><title>Waza Admin</title>{css}</head><body>";
        
        // 管理パネル
        html += "<div class='panel'><h2>キーワード管理</h2><div>";
        foreach (var k in allKeywords) {
            var styleClass = k.IsEnabled ? "tag-on" : "tag-off";
            var statusLabel = k.IsEnabled ? "ON" : "OFF";
            html += $"<span class='keyword-tag {styleClass}'>" +
                    $"<a href='/api/news/keywords/toggle?word={k.Word}' title='オンオフ切替'>{k.Word} ({statusLabel})</a>" +
                    $"<a href='/api/news/keywords/delete?word={k.Word}' class='del-btn' title='削除'>×</a></span>";
        }
        html += "</div><div class='add-form'>" +
                "<form action='/api/news/keywords/add' method='get'>" +
                "<input type='text' name='word' placeholder='新しい単語' required>" +
                "<button type='submit'>追加</button></form></div>" +
                "<a href='/api/news/fetch' style='display:block; margin-top:10px; color:#3498db;'>[ この設定でニュースを取得 ]</a></div>";

        // ニュース一覧
        html += "<ul class='grid'>";
        using var connection = new SqliteConnection(dbPath);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Title, Url FROM News ORDER BY Id DESC LIMIT 30";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            html += $"<li class='card'><a href='{reader.GetString(1)}' target='_blank'>{reader.GetString(0)}</a></li>";
        }
        html += "</ul></body></html>";

        return Results.Content(html, "text/html", System.Text.Encoding.UTF8);
    }

    private static List<string> GetActiveKeywords() {
        var list = new List<string>();
        using var conn = new SqliteConnection(dbPath);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Word FROM Keywords WHERE IsEnabled = 1";
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString(0));
        return list;
    }

    private static List<(string Word, bool IsEnabled)> GetAllKeywordStates() {
        var list = new List<(string, bool)>();
        using var conn = new SqliteConnection(dbPath);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Word, IsEnabled FROM Keywords";
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add((r.GetString(0), r.GetInt32(1) == 1));
        return list;
    }

    public class NewsApiResponse { public List<Article>? Articles { get; set; } }
    public class Article { public string? Title { get; set; } public string? Url { get; set; } public DateTime PublishedAt { get; set; } }
}