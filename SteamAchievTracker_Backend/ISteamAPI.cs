using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.VisualBasic;


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

         // ===== Public API (pretty) =====
        public async Task<IReadOnlyList<DetailedAchievement>> GetUnfinishedAchievementsDetailedAsync(
            ulong steamId64, uint appId, string? language = null)
        {
            // raw unfinished list (apiName/achieved)
            var unfinished = await GetPlayerUnfinishedAsync(steamId64, appId, language);
            if (unfinished.Count == 0) return Array.Empty<DetailedAchievement>();

            // schema map (apiName -> metadata)
            var schemaMap = await GetSchemaMapAsync(appId, language);

            // join
            var detailed = new List<DetailedAchievement>(unfinished.Count);
            foreach (var a in unfinished)
            {
                if (schemaMap.TryGetValue(a.ApiName, out var s))
                {
                    detailed.Add(new DetailedAchievement(
                        ApiName: a.ApiName,
                        DisplayName: s.DisplayName,
                        Description: s.Description,
                        Hidden: s.Hidden == 1,
                        Icon: s.Icon,
                        IconGray: s.IconGray
                    ));
                }
                else
                {
                    // fallback when schema entry is missing
                    detailed.Add(new DetailedAchievement(
                        ApiName: a.ApiName,
                        DisplayName: a.ApiName,
                        Description: null,
                        Hidden: false,
                        Icon: "",
                        IconGray: ""
                    ));
                }
            }

            return detailed.OrderBy(x => x.DisplayName).ToList();
        }

         // ===== Private helpers (no duplication) =====

        private async Task<List<Achievement>> GetPlayerUnfinishedAsync(ulong steamId64, uint appId, string? language)
        {
            var url = $"ISteamUserStats/GetPlayerAchievements/v1/"
                    + $"?key={_apiKey}&steamid={steamId64}&appid={appId}"
                    + (language is null ? "" : $"&l={Uri.EscapeDataString(language)}");

            var player = await _http.GetFromJsonAsync<PlayerAchievementsResponse>(url);

            if (player?.PlayerStats?.Success == false)
                throw new InvalidOperationException("Steam API returned success=false for this app/user.");

            var list = player?.PlayerStats?.Achievements ?? new List<Achievement>();
            return list.Where(a => a.Achieved == 0).ToList();
        }

        private async Task<Dictionary<string, SchemaAchievement>> GetSchemaMapAsync(uint appId, string? language)
        {
            var url = $"ISteamUserStats/GetSchemaForGame/v2/"
                    + $"?key={_apiKey}&appid={appId}"
                    + (language is null ? "" : $"&l={Uri.EscapeDataString(language)}");

            var resp = await _http.GetFromJsonAsync<GameSchemaResponse>(url);
            var list = resp?.Game?.AvailableGameStats?.Achievements ?? new List<SchemaAchievement>();
            return list.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
        }
    }


       // ===== DTOs =====

        public record DetailedAchievement(
            string ApiName,
            string DisplayName,
            string? Description,
            bool Hidden,
            string Icon,
            string IconGray
        );

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

        public record GameSchemaResponse([property: JsonPropertyName("game")] Game? Game);
        public record Game(
            [property: JsonPropertyName("gameName")] string GameName,
            [property: JsonPropertyName("availableGameStats")] AvailableGameStats? AvailableGameStats);
        public record AvailableGameStats(
            [property: JsonPropertyName("achievements")] List<SchemaAchievement>? Achievements);

        public record SchemaAchievement(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("displayName")] string DisplayName,
            [property: JsonPropertyName("description")] string? Description,
            [property: JsonPropertyName("hidden")] int Hidden,
            [property: JsonPropertyName("icon")] string Icon,
            [property: JsonPropertyName("icongray")] string IconGray);        
        
    }
