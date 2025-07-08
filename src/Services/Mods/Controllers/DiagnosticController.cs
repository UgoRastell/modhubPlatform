using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using ModsService.Repositories;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/diagnostic")]
    public class DiagnosticController : ControllerBase
    {
        private readonly ILogger<DiagnosticController> _logger;
        private readonly IModRepository _modRepository;

        public DiagnosticController(ILogger<DiagnosticController> logger, IModRepository modRepository)
        {
            _logger = logger;
            _modRepository = modRepository;
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

        /// <summary>
        /// ENDPOINT CRITIQUE - Comparer les données DB avec les fichiers physiques
        /// </summary>
        [HttpGet("db-vs-files")]
        [AllowAnonymous]
        public async Task<IActionResult> CompareDbVsFiles()
        {
            try
            {
                // Récupérer tous les mods de la base
                var allMods = await _modRepository.GetAllAsync();
                string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                string modsDir = Path.Combine(uploadsDir, "mods");

                var comparison = new List<object>();

                foreach (var mod in allMods)
                {
                    var modDir = Path.Combine(modsDir, mod.Id);
                    var physicalFiles = Directory.Exists(modDir) 
                        ? Directory.GetFiles(modDir, "*.*", SearchOption.AllDirectories)
                        : new string[0];

                    var modAnalysis = new
                    {
                        // Données de la base
                        ModId = mod.Id,
                        ModName = mod.Name,
                        DbFileName = mod.FileName,
                        DbFileLocation = mod.FileLocation,
                        DbThumbnailUrl = mod.ThumbnailUrl,
                        DbMimeType = mod.MimeType,
                        
                        // Fichiers physiques
                        PhysicalDirectory = modDir,
                        DirectoryExists = Directory.Exists(modDir),
                        PhysicalFiles = physicalFiles.Select(f => new {
                            FileName = Path.GetFileName(f),
                            FullPath = f,
                            RelativePath = Path.GetRelativePath(uploadsDir, f),
                            Size = new FileInfo(f).Length
                        }).ToArray(),
                        
                        // Analyse des écarts
                        Issues = new List<string>(),
                        
                        // Vérifications spécifiques
                        ExpectedZipPath = !string.IsNullOrEmpty(mod.FileLocation) ? 
                            Path.Combine(Directory.GetCurrentDirectory(), mod.FileLocation.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)) : null,
                        ZipFileExists = false,
                        ThumbnailExists = physicalFiles.Any(f => f.Contains("thumbnail")),
                        ZipFiles = physicalFiles.Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToArray()
                    };

                    // Analyser les problèmes
                    var issues = new List<string>();
                    
                    if (!Directory.Exists(modDir))
                        issues.Add("Dossier mod introuvable");
                    
                    if (!physicalFiles.Any(f => f.Contains("thumbnail")))
                        issues.Add("Thumbnail manquant");
                    
                    if (!physicalFiles.Any(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                        issues.Add("Fichier ZIP manquant");
                    
                    if (!string.IsNullOrEmpty(mod.FileLocation))
                    {
                        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), 
                            mod.FileLocation.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (!System.IO.File.Exists(expectedPath))
                            issues.Add($"Fichier attendu inexistant: {expectedPath}");
                    }
                    
                    if (string.IsNullOrEmpty(mod.FileLocation))
                        issues.Add("FileLocation vide dans la DB");

                    comparison.Add(new {
                        modAnalysis.ModId,
                        modAnalysis.ModName,
                        modAnalysis.DbFileName,
                        modAnalysis.DbFileLocation,
                        modAnalysis.DbThumbnailUrl,
                        modAnalysis.PhysicalDirectory,
                        modAnalysis.DirectoryExists,
                        modAnalysis.PhysicalFiles,
                        modAnalysis.ThumbnailExists,
                        modAnalysis.ZipFiles,
                        Issues = issues,
                        HasIssues = issues.Any()
                    });
                }

                var summary = new
                {
                    TotalMods = allMods.Count,
                    ModsWithIssues = comparison.Count(c => ((List<string>)((dynamic)c).Issues).Any()),
                    ModsWithoutDirectory = comparison.Count(c => !((bool)((dynamic)c).DirectoryExists)),
                    ModsWithoutThumbnail = comparison.Count(c => !((bool)((dynamic)c).ThumbnailExists)),
                    ModsWithoutZip = comparison.Count(c => ((Array)((dynamic)c).ZipFiles).Length == 0)
                };

                return Ok(new { 
                    Status = "SUCCESS", 
                    Message = "Comparaison DB vs fichiers terminée",
                    Summary = summary,
                    Details = comparison
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la comparaison DB vs fichiers");
                return StatusCode(500, new { 
                    Status = "ERROR", 
                    Message = $"Erreur: {ex.Message}" 
                });
            }
        }
    }
}
