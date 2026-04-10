using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class MyData_API
{
    // ★ローカル専用のDBファイル
    private static readonly string dbPath = "Data Source=waza_local.db";

    public static void InitializeLocalDatabase()
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();
        var command = connection.CreateCommand();
        // ローカルデータの保存用テーブル（Key-Value形式で何でも保存できるように設計）
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS LocalStorage (
                Key TEXT PRIMARY KEY, 
                Value TEXT NOT NULL,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );
        ";
        command.ExecuteNonQuery();
    }

    public static void SetupRoutes(WebApplication app)
    {
        var photoDataPath = @"C:\Users\sshuser\Desktop\Data_CamPhoto";
        var webDataPath   = @"C:\Users\sshuser\Desktop\Data_WazaWeb";

        // --- ローカルデータの保存・読み込み API ---
        app.MapPost("/api/local/save", (LocalData req) => SaveData(req.Key, req.Value));
        app.MapGet("/api/local/load", (string key) => LoadData(key));

        // --- 静的ファイルの配信 ---
        app.UseStaticFiles(new StaticFileOptions { 
            FileProvider = new PhysicalFileProvider(photoDataPath), RequestPath = "/photos" 
        });
        app.UseDirectoryBrowser(new DirectoryBrowserOptions { 
            FileProvider = new PhysicalFileProvider(photoDataPath), RequestPath = "/photos" 
        });

        app.UseFileServer(new FileServerOptions { 
            FileProvider = new PhysicalFileProvider(webDataPath), 
            RequestPath = "/waza", 
            EnableDefaultFiles = true 
        });
    }

    // --- DB操作ロジック ---
    private static IResult SaveData(string key, string value)
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();
        var command = connection.CreateCommand();
        // 既に同じキーがあれば上書き(UPSERT)
        command.CommandText = @"
            INSERT INTO LocalStorage (Key, Value, UpdatedAt) VALUES ($key, $value, CURRENT_TIMESTAMP)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value, UpdatedAt = CURRENT_TIMESTAMP;
        ";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
        return Results.Ok(new { Message = $"{key} のデータを保存しました。" });
    }

    private static IResult LoadData(string key)
    {
        using var connection = new SqliteConnection(dbPath);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM LocalStorage WHERE Key = $key";
        command.Parameters.AddWithValue("$key", key);
        using var reader = command.ExecuteReader();
        
        if (reader.Read()) {
            return Results.Ok(new { Key = key, Value = reader.GetString(0) });
        }
        return Results.NotFound(new { Message = "データが見つかりません。" });
    }

    // データ受け取り用の型定義
    public record LocalData(string Key, string Value);
}