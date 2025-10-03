using ISteamAPI;

namespace SteamAchiever;

internal class Program
{
    static async Task Main(string[] args)
    {

        DotNetEnv.Env.Load(); 

        var builder = WebApplication.CreateBuilder(args);

        // (optional) if you want typed HttpClient:
        // builder.Services.AddHttpClient();

        var app = builder.Build();

        // read the key from env (or use builder.Configuration["Steam:ApiKey"])
        var apiKey = Environment.GetEnvironmentVariable("STEAM_WEB_API_KEY")
                    ?? throw new Exception("Missing STEAM_WEB_API_KEY");
        var steam = new SteamAPIClient(apiKey); // or inject via DI if you prefer

        app.MapGet("/api/achievements/unfinished", async (ulong steamid, uint appid, string? l) =>
        {
            var list = await steam.GetUnfinishedAchievementsAsync(steamid, appid, l);
            return Results.Ok(new { appId = appid, count = list.Count, achievements = list });
        });

        app.Run();
    }
}
