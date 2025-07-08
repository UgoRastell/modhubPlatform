using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/diagnostic")]
    public class DiagnosticController : ControllerBase
    {
        private readonly ILogger<DiagnosticController> _logger;

        public DiagnosticController(ILogger<DiagnosticController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// ENDPOINT TEMPORAIRE - Vérifier la présence des fichiers uploads
        /// À SUPPRIMER en production pour des raisons de sécurité
        /// </summary>
        [HttpGet("check-uploads")]
        [AllowAnonymous] // Temporaire pour debug
        public IActionResult CheckUploads()
        {
            try
            {
                string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                var result = new
                {
                    UploadsDirectory = uploadsDir,
                    DirectoryExists = Directory.Exists(uploadsDir),
                    Mods = new List<object>()
                };

                if (!Directory.Exists(uploadsDir))
                {
                    return Ok(new { 
                        Status = "ERROR", 
                        Message = "Dossier uploads introuvable",
                        Data = result 
                    });
                }

                string modsDir = Path.Combine(uploadsDir, "mods");
                if (!Directory.Exists(modsDir))
                {
                    return Ok(new { 
                        Status = "ERROR", 
                        Message = "Dossier uploads/mods introuvable",
                        Data = result 
                    });
                }

                // Lister tous les dossiers de mods
                var modDirectories = Directory.GetDirectories(modsDir);
                var modsList = new List<object>();

                foreach (var modDir in modDirectories)
                {
                    var modId = Path.GetFileName(modDir);
                    var files = Directory.GetFiles(modDir, "*.*", SearchOption.AllDirectories);
                    
                    var modInfo = new
                    {
                        ModId = modId,
                        DirectoryPath = modDir,
                        FilesCount = files.Length,
                        Files = files.Select(f => new {
                            FileName = Path.GetFileName(f),
                            RelativePath = Path.GetRelativePath(uploadsDir, f),
                            FullPath = f,
                            Size = new FileInfo(f).Length,
                            LastModified = new FileInfo(f).LastWriteTime
                        }).ToArray(),
                        HasThumbnail = files.Any(f => f.Contains("thumbnail")),
                        HasZipFile = files.Any(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    };

                    modsList.Add(modInfo);
                }

                return Ok(new { 
                    Status = "SUCCESS", 
                    Message = $"Analyse terminée - {modsList.Count} dossiers de mods trouvés",
                    Data = new {
                        UploadsDirectory = uploadsDir,
                        ModsDirectory = modsDir,
                        TotalModDirectories = modsList.Count,
                        Mods = modsList
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des uploads");
                return StatusCode(500, new { 
                    Status = "ERROR", 
                    Message = $"Erreur: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Vérifier un mod spécifique par son ID
        /// </summary>
        [HttpGet("check-mod/{modId}")]
        [AllowAnonymous]
        public IActionResult CheckSpecificMod(string modId)
        {
            try
            {
                string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                string modDir = Path.Combine(uploadsDir, "mods", modId);

                var result = new
                {
                    ModId = modId,
                    ModDirectory = modDir,
                    DirectoryExists = Directory.Exists(modDir),
                    Files = new List<object>(),
                    ExpectedThumbnailPath = Path.Combine(modDir, "thumbnail.jpg"),
                    ThumbnailExists = false,
                    ZipFiles = new List<string>()
                };

                if (Directory.Exists(modDir))
                {
                    var files = Directory.GetFiles(modDir, "*.*", SearchOption.AllDirectories);
                    
                    var filesList = files.Select(f => new {
                        FileName = Path.GetFileName(f),
                        FullPath = f,
                        RelativePath = Path.GetRelativePath(uploadsDir, f),
                        Size = new FileInfo(f).Length,
                        Extension = Path.GetExtension(f),
                        LastModified = new FileInfo(f).LastWriteTime
                    }).ToList();

                    var zipFiles = files.Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                       .Select(Path.GetFileName)
                                       .ToList();

                    return Ok(new { 
                        Status = "SUCCESS", 
                        Data = new {
                            ModId = modId,
                            ModDirectory = modDir,
                            DirectoryExists = true,
                            FilesCount = files.Length,
                            Files = filesList,
                            ExpectedThumbnailPath = Path.Combine(modDir, "thumbnail.jpg"),
                            ThumbnailExists = System.IO.File.Exists(Path.Combine(modDir, "thumbnail.jpg")),
                            ZipFiles = zipFiles,
                            HasZipFiles = zipFiles.Any()
                        }
                    });
                }

                return Ok(new { 
                    Status = "NOT_FOUND", 
                    Message = $"Dossier du mod {modId} introuvable",
                    Data = result 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du mod {ModId}", modId);
                return StatusCode(500, new { 
                    Status = "ERROR", 
                    Message = $"Erreur: {ex.Message}" 
                });
            }
        }
    }
}
