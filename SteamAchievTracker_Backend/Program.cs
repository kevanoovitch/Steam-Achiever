using ISteamAPI;

namespace SteamAchiever;

internal class Program
{
    static async Task Main(string[] args)
    {

        string REACT_SRV = "http://localhost:5173";

        DotNetEnv.Env.Load(); 

        var builder = WebApplication.CreateBuilder(args);

        // (optional) if you want typed HttpClient:
        // builder.Services.AddHttpClient();
        builder.Services.AddControllers();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(REACT_SRV)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        app.UseCors("AllowFrontend");

        // read the key from env (or use builder.Configuration["Steam:ApiKey"])
        var apiKey = Environment.GetEnvironmentVariable("STEAM_WEB_API_KEY")
                    ?? throw new Exception("Missing STEAM_WEB_API_KEY");
        var steam = new SteamAPIClient(apiKey); // or inject via DI if you prefer

        app.MapGet("/api/achievements/unfinished", async (string steamid, uint appid, string? l) =>
            {
                if (!ulong.TryParse(steamid, out var sid))
                    return Results.BadRequest("steamid must be a numeric SteamID64 (e.g., 7656119...).");

                var list = await steam.GetUnfinishedAchievementsDetailedAsync(sid, appid, l);
                return Results.Ok(new { appId = appid, count = list.Count, achievements = list });
            });

        app.MapControllers();
        //app.UseHttpsRedirection();
        app.Run();
    }
}
