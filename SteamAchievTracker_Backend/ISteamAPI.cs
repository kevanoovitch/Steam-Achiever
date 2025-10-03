using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Linq;

// GET https://partner.steam-api.com/ISteamUserStats/GetPlayerAchievements/v1/

namespace ISteamAPI
{
    class SteamAPIClient
    {

        private readonly HttpClient _http;
        private readonly string _apiKey;

        public SteamAPIClient(string apiKey, HttpClient? http = null)
        {
            _apiKey = apiKey ?? throw new ArgumentException(nameof(apiKey));
            _http = http ?? new HttpClient { BaseAddress = new Uri("https://api.steampowered.com/") };
        }

        public async Task<IReadOnlyList<Achievement>> GetUnfinishedAchievementsAsync(ulong steamId64, uint appId, string? language = null)
        {
            var url = $"ISteamUserStats/GetPlayerAchievements/v1/?key={_apiKey}&steamid={steamId64}&appid={appId}"
            + (language is null ? "" : $"&l={Uri.EscapeDataString(language)}");

            var resp = await _http.GetFromJsonAsync<PlayerAchievementsResponse>(url);

            if (resp?.PlayerStats?.Success == false)
                throw new InvalidOperationException("Steam API returned success=false for this app/user.");

            var list = resp?.PlayerStats?.Achievements ?? new List<Achievement>();
            return list.Where(a => a.Achieved == 0).ToList();


        }

        // === JSON models ===
        public record PlayerAchievementsResponse([property: JsonPropertyName("playerstats")] PlayerStats? PlayerStats);
        public record PlayerStats([property: JsonPropertyName("steamID")] string SteamID,
                                [property: JsonPropertyName("gameName")] string GameName,
                                [property: JsonPropertyName("achievements")] List<Achievement> Achievements,
                                [property: JsonPropertyName("success")] bool Success);

        public record Achievement([property: JsonPropertyName("apiname")] string ApiName,
                                  [property: JsonPropertyName("achieved")] int Achieved,
                                  [property: JsonPropertyName("unlocktime")] long UnlockTime);

        public record OwnedGamesResponse([property: JsonPropertyName("response")] OwnedGamesInner Response);
        public record OwnedGamesInner([property: JsonPropertyName("game_count")] int GameCount,
                                      [property: JsonPropertyName("games")] List<OwnedGame> Games);
        public record OwnedGame([property: JsonPropertyName("appid")] uint AppId,
                                [property: JsonPropertyName("name")] string? Name);
    }
}