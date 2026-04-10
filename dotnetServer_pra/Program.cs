using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.WriteIndented = true);
builder.Services.AddDirectoryBrowser();

var app = builder.Build();

// 1. データベースの初期化（ローカル用とニュース用をそれぞれ独立して初期化）
MyData_API.InitializeLocalDatabase();
NewsAPI.InitializeNewsDatabase();

// 2. 各API・ファイル配信のURL設定（ルーティング）
MyData_API.SetupRoutes(app);
NewsAPI.SetupRoutes(app);

// トップページ
app.MapGet("/", () => "和座製作所サーバー稼働中。\n /waza (Web) \n /photos (写真) \n /api/local/... (ローカルデータ)\n /api/news/... (ニュースデータ)");

app.Run();