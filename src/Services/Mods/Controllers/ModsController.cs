using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModsService.Models;
using ModsService.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/mods")]
    public class ModsController : ControllerBase
    {
        private readonly IModRepository _modRepository;
        private readonly ILogger<ModsController> _logger;

        public ModsController(IModRepository modRepository, ILogger<ModsController> logger)
        {
            _modRepository = modRepository;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMods([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string sortBy = "recent")
        {
            try
            {
                _logger.LogInformation("Récupération des mods avec les paramètres: page={Page}, pageSize={PageSize}, sortBy={SortBy}", page, pageSize, sortBy);
                
                var mods = await _modRepository.GetAllAsync(page, pageSize, sortBy);
                var totalCount = await _modRepository.GetTotalCountAsync();
                
                _logger.LogInformation("Récupération de {Count} mods sur un total de {TotalCount}", mods.Count, totalCount);
                
                // Convertir les mods en format compatible avec le frontend ModDto
                var modDtos = mods.Select(mod => new
                {
                    Id = mod.Id,
                    Name = mod.Name,
                    Slug = mod.Name.ToLowerInvariant().Replace(" ", "-"),  // Générer un slug basique
                    Description = mod.Description,
                    ShortDescription = mod.Description.Length > 100 ? mod.Description.Substring(0, 97) + "..." : mod.Description,
                    CreatorId = mod.CreatorId,
                    CreatorName = mod.Author,
                    GameId = mod.GameId,
                    GameName = mod.GameName,
                    CategoryId = string.Empty,
                    ThumbnailUrl = mod.ThumbnailUrl,
                    ScreenshotUrls = new List<string>(),
                    Version = "1.0",
                    DownloadUrl = string.Empty,
                    DocumentationUrl = string.Empty,
                    DownloadCount = mod.DownloadCount,
                    AverageRating = mod.Rating,
                    RatingCount = mod.ReviewCount,
                    CreatedAt = mod.CreatedAt,
                    UpdatedAt = mod.UpdatedAt,
                    Tags = mod.Tags ?? new List<string>(),
                    Categories = new List<string>(),
                    IsFeatured = false,
                    IsApproved = true,
                    Versions = new List<object>()
                }).ToList();

                return Ok(new { 
                    Success = true, 
                    Message = "Mods récupérés avec succès", 
                    Data = new { 
                        Items = modDtos, 
                        TotalCount = totalCount,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des mods");
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Erreur lors de la récupération des mods", 
                    Data = new { 
                        Items = Array.Empty<Mod>(), 
                        TotalCount = 0,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                });
            }
        }
        
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetModById(string id)
        {
            try
            {
                var mod = await _modRepository.GetByIdAsync(id);
                
                if (mod == null)
                {
                    return NotFound(new { Success = false, Message = "Mod non trouvé" });
                }
                
                return Ok(new { Success = true, Message = "Mod récupéré avec succès", Data = mod });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du mod {ModId}", id);
                return StatusCode(500, new { Success = false, Message = "Erreur lors de la récupération du mod" });
            }
        }
    }
}
