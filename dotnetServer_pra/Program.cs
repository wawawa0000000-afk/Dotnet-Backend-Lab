using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.WriteIndented = true);
builder.Services.AddDirectoryBrowser();
var app = builder.Build();

// --- 🌟 ここが「相対パス」の魔法 🌟 ---
// 実行している場所（dotnetServer_pra フォルダ）を取得
var baseDir = Directory.GetCurrentDirectory();

// そこから「一つ上の階層 (..)」にある Data_Photo と Data_WazaWeb のパスを自動計算
var photoDataPath = Path.GetFullPath(Path.Combine(baseDir, "..", "Data_Photo"));
var webDataPath   = Path.GetFullPath(Path.Combine(baseDir, "..", "Data_WazaWeb"));


// --- 🌿 葉（ロジック）の呼び出し ---
app.MapGet("/api/photos", () => Logic_CamPhoto.GetPhotos(photoDataPath));
app.MapGet("/api/info",   () => Logic_WazaWeb.GetInfo(webDataPath));


// --- 🖼️ 写真の配信（人間用：/photos） ---
// ※GitHubから落とした直後など、空フォルダが存在しないとサーバーが落ちるのを防ぐ安全装置
if (Directory.Exists(photoDataPath))
{
    app.UseStaticFiles(new StaticFileOptions { 
        FileProvider = new PhysicalFileProvider(photoDataPath), RequestPath = "/photos" 
    });
    app.UseDirectoryBrowser(new DirectoryBrowserOptions { 
        FileProvider = new PhysicalFileProvider(photoDataPath), RequestPath = "/photos" 
    });
}

// --- 🌐 Webサイトの配信（/waza） ---
if (Directory.Exists(webDataPath))
{
    app.UseFileServer(new FileServerOptions { 
        FileProvider = new PhysicalFileProvider(webDataPath), 
        RequestPath = "/waza", 
        EnableDefaultFiles = true 
    });
}

// 起動時に「どこを参照しているか」画面に出すようにしておくと、テスト時に便利です
app.MapGet("/", () => $"和座製作所サーバー稼働中。\n\n[自動計算された参照パス]\n写真: {photoDataPath}\nWeb : {webDataPath}");

app.Run();