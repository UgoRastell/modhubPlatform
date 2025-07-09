using Frontend.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Frontend.Services
{
    public interface IGameService
    {
        Task<IEnumerable<Game>> GetGamesAsync(string searchQuery = null, string sortOption = "popularity");
        Task<Game> GetGameByIdAsync(string id);
        Task<ApiResponse<GameDto>> CreateGameAsync(GameCreateRequest request);
    }

    public class GameService : IGameService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GameService> _logger;
        private const string BaseApiUrl = "/api/games";

        public GameService(HttpClient httpClient, ILogger<GameService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ApiResponse<GameDto>> CreateGameAsync(GameCreateRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseApiUrl}", request);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<GameDto>();
                    return new ApiResponse<GameDto> { Success = true, Data = created };
                }

                var message = await response.Content.ReadAsStringAsync();
                return new ApiResponse<GameDto> { Success = false, Message = message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du jeu");
                return new ApiResponse<GameDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<IEnumerable<Game>> GetGamesAsync(string searchQuery = null, string sortOption = "popularity")
        {
            try
            {
                var url = "/api/games";
                bool hasQueryParams = false;
                
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    url += $"?search={Uri.EscapeDataString(searchQuery)}";
                    hasQueryParams = true;
                }
                
                if (!string.IsNullOrEmpty(sortOption))
                {
                    url += hasQueryParams ? $"&sort={sortOption}" : $"?sort={sortOption}";
                }

                _logger.LogInformation($"Fetching games with URL: {url}");
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<IEnumerable<Game>>();
                }
                else
                {
                    _logger.LogError($"Error fetching games: {response.StatusCode}");
                    // En cas d'erreur, retourner les données de démonstration
                    return GetFallbackGames();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception fetching games: {ex.Message}");
                return GetFallbackGames();
            }
        }

        public async Task<Game> GetGameByIdAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/games/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Game>();
                }
                else
                {
                    _logger.LogError($"Error fetching game {id}: {response.StatusCode}");
                    // En cas d'erreur, retourner les données de démonstration
                    return GetFallbackGames().FirstOrDefault(g => g.Id == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception fetching game {id}: {ex.Message}");
                return GetFallbackGames().FirstOrDefault(g => g.Id == id);
            }
        }

        // Données de secours au cas où l'API ne serait pas disponible
        private IEnumerable<Game> GetFallbackGames()
        {
            return new List<Game>
            {
                new Game
                {
                    Id = "skyrim",
                    Name = "The Elder Scrolls V: Skyrim",
                    ShortDescription = "RPG de monde ouvert médiéval-fantastique de Bethesda",
                    ImageUrl = "/images/games/skyrim.jpg",
                    ModCount = 8547
                },
                new Game
                {
                    Id = "minecraft",
                    Name = "Minecraft",
                    ShortDescription = "Jeu bac à sable de construction et de survie",
                    ImageUrl = "/images/games/minecraft.jpg",
                    ModCount = 6238
                },
                new Game
                {
                    Id = "fallout4",
                    Name = "Fallout 4",
                    ShortDescription = "RPG post-apocalyptique en monde ouvert",
                    ImageUrl = "/images/games/fallout4.jpg",
                    ModCount = 5921
                },
                new Game
                {
                    Id = "witcher3",
                    Name = "The Witcher 3: Wild Hunt",
                    ShortDescription = "RPG en monde ouvert basé sur la saga du Sorceleur",
                    ImageUrl = "/images/games/witcher3.jpg",
                    ModCount = 2847
                },
                new Game
                {
                    Id = "stardewvalley",
                    Name = "Stardew Valley",
                    ShortDescription = "Simulation de ferme et RPG relaxant",
                    ImageUrl = "/images/games/stardewvalley.jpg",
                    ModCount = 1953
                },
                new Game
                {
                    Id = "cyberpunk2077",
                    Name = "Cyberpunk 2077",
                    ShortDescription = "RPG futuriste en monde ouvert",
                    ImageUrl = "/images/games/cyberpunk2077.jpg",
                    ModCount = 1845
                }
            };
        }
    }
}
