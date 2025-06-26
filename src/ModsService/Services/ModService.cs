using ModsService.Models;
using ModsService.Repositories;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using Shared.Models;
using System.Linq;

namespace ModsService.Services
{
    public class ModService : IModService
    {
        private readonly IModRepository _modRepository;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _modContainer;
        private readonly string _modImageContainer;
        
        // Cache expiration times
        private readonly TimeSpan _modListCacheExpiration = TimeSpan.FromMinutes(10);
        private readonly TimeSpan _modDetailCacheExpiration = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _searchResultsCacheExpiration = TimeSpan.FromMinutes(5);

        public ModService(
            IModRepository modRepository, 
            IBlobStorageService blobStorageService,
            ICacheService cacheService,
            IConfiguration configuration)
        {
            _modRepository = modRepository;
            _blobStorageService = blobStorageService;
            _cacheService = cacheService;
            _configuration = configuration;
            _modContainer = configuration["Storage:BlobContainerName"];
            _modImageContainer = configuration["Storage:ModsImageContainer"];
        }

        public async Task<ApiResponse<PagedResult<Mod>>> GetAllModsAsync(int page = 1, int pageSize = 50, string sortBy = "recent")
        {
            try
            {
                // Créer une clé de cache basée sur les paramètres de pagination
                string cacheKey = $"ALL_MODS_{page}_{pageSize}_{sortBy}";
                
                // Essayer de récupérer depuis le cache
                var cachedResult = await _cacheService.GetAsync<PagedResult<Mod>>(cacheKey);
                
                if (cachedResult != null)
                {
                    return new ApiResponse<PagedResult<Mod>>
                    {
                        Success = true,
                        Message = "Mods récupérés du cache avec succès",
                        Data = cachedResult
                    };
                }
                
                // Créer un objet ModSearchParams pour réutiliser la fonctionnalité de recherche
                var searchParams = new ModSearchParams
                {
                    PageNumber = page,
                    PageSize = pageSize,
                    SortBy = sortBy
                };
                
                // Réutiliser la logique de recherche pour la pagination et le tri
                var (mods, totalCount) = await _modRepository.SearchModsAsync(
                    null, // gameId
                    null, // categoryId
                    null, // searchTerm
                    sortBy,
                    page,
                    pageSize
                );
                
                // Créer le résultat paginé
                var result = new PagedResult<Mod>
                {
                    Items = mods,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    CurrentPage = page
                };
                
                // Mettre en cache les résultats
                await _cacheService.SetAsync(cacheKey, result, _modListCacheExpiration);
                
                return new ApiResponse<PagedResult<Mod>>
                {
                    Success = true,
                    Message = "Mods récupérés avec succès",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<Mod>>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Mod>> GetModByIdAsync(string id)
        {
            try
            {
                // Essayer de récupérer depuis le cache
                var cacheKey = $"MOD_{id}";
                var cachedMod = await _cacheService.GetAsync<Mod>(cacheKey);
                
                if (cachedMod != null)
                {
                    return new ApiResponse<Mod>
                    {
                        Success = true,
                        Message = "Mod récupéré du cache avec succès",
                        Data = cachedMod
                    };
                }
                
                // Si pas dans le cache, récupérer depuis le repository
                var mod = await _modRepository.GetModByIdAsync(id);
                
                if (mod == null)
                {
                    return new ApiResponse<Mod>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }
                
                // Mettre en cache les résultats
                await _cacheService.SetAsync(cacheKey, mod, _modDetailCacheExpiration);
                
                return new ApiResponse<Mod>
                {
                    Success = true,
                    Message = "Mod récupéré avec succès",
                    Data = mod
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Mod>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Mod>> CreateModAsync(CreateModRequest createModRequest)
        {
            try
            {
                var mod = new Mod
                {
                    Name = createModRequest.Name,
                    Description = createModRequest.Description,
                    GameId = createModRequest.GameId,
                    CategoryId = createModRequest.CategoryId,
                    CreatorId = createModRequest.CreatorId,
                    Version = createModRequest.Version,
                    Tags = createModRequest.Tags,
                    Requirements = createModRequest.Requirements,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdMod = await _modRepository.CreateModAsync(mod);
                
                // Invalider le cache pour la liste de tous les mods
                await _cacheService.RemoveAsync("ALL_MODS");
                
                return new ApiResponse<Mod>
                {
                    Success = true,
                    Message = "Mod créé avec succès",
                    Data = createdMod
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Mod>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Mod>> UpdateModAsync(string id, UpdateModRequest updateModRequest)
        {
            try
            {
                var existingMod = await _modRepository.GetModByIdAsync(id);
                
                if (existingMod == null)
                {
                    return new ApiResponse<Mod>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }

                // Mettre à jour seulement les propriétés non-nulles du request
                if (!string.IsNullOrEmpty(updateModRequest.Name))
                    existingMod.Name = updateModRequest.Name;
                    
                if (!string.IsNullOrEmpty(updateModRequest.Description))
                    existingMod.Description = updateModRequest.Description;
                    
                if (!string.IsNullOrEmpty(updateModRequest.Version))
                    existingMod.Version = updateModRequest.Version;
                    
                if (updateModRequest.Tags != null)
                    existingMod.Tags = updateModRequest.Tags;
                    
                if (updateModRequest.Requirements != null)
                    existingMod.Requirements = updateModRequest.Requirements;
                    
                if (!string.IsNullOrEmpty(updateModRequest.CategoryId))
                    existingMod.CategoryId = updateModRequest.CategoryId;
                
                existingMod.UpdatedAt = DateTime.UtcNow;

                var updatedMod = await _modRepository.UpdateModAsync(existingMod);
                
                // Invalider les caches concernés
                await _cacheService.RemoveAsync($"MOD_{id}");
                await _cacheService.RemoveAsync("ALL_MODS");
                
                // Mettre à jour le cache avec les nouvelles données
                await _cacheService.SetAsync($"MOD_{id}", updatedMod, _modDetailCacheExpiration);
                
                return new ApiResponse<Mod>
                {
                    Success = true,
                    Message = "Mod mis à jour avec succès",
                    Data = updatedMod
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Mod>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteModAsync(string id)
        {
            try
            {
                var mod = await _modRepository.GetModByIdAsync(id);
                
                if (mod == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }

                // Supprimer les fichiers associés du stockage blob si existants
                if (!string.IsNullOrEmpty(mod.FileUrl))
                {
                    await _blobStorageService.DeleteBlobAsync(_modContainer, Path.GetFileName(mod.FileUrl));
                }
                
                if (!string.IsNullOrEmpty(mod.ImageUrl))
                {
                    await _blobStorageService.DeleteBlobAsync(_modImageContainer, Path.GetFileName(mod.ImageUrl));
                }

                // Supprimer le mod de la base de données
                await _modRepository.DeleteModAsync(id);
                
                // Invalider les caches concernés
                await _cacheService.RemoveAsync($"MOD_{id}");
                await _cacheService.RemoveAsync("ALL_MODS");
                
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Mod supprimé avec succès",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Mod>> UploadModFileAsync(string modId, IFormFile file)
        {
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    return new ApiResponse<Mod>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }

                // Générer un nom de fichier unique
                string fileName = $"{modId}-{Path.GetFileName(file.FileName)}";
                
                // Uploader le fichier
                using (var stream = file.OpenReadStream())
                {
                    mod.FileUrl = await _blobStorageService.UploadBlobAsync(_modContainer, fileName, stream);
                }
                
                mod.FileSize = file.Length;
                mod.UpdatedAt = DateTime.UtcNow;

                var updatedMod = await _modRepository.UpdateModAsync(mod);
                
                // Mettre à jour le cache
                await _cacheService.SetAsync($"MOD_{modId}", updatedMod, _modDetailCacheExpiration);
                
                return new ApiResponse<Mod>
                {
                    Success = true,
                    Message = "Fichier mod uploadé avec succès",
                    Data = updatedMod
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Mod>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Mod>> UploadModImageAsync(string modId, IFormFile image)
        {
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    return new ApiResponse<Mod>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }

                // Vérifier que le fichier est bien une image
                if (!image.ContentType.StartsWith("image/"))
                {
                    return new ApiResponse<Mod>
                    {
                        Success = false,
                        Message = "Le fichier doit être une image"
                    };
                }

                // Générer un nom de fichier unique
                string fileName = $"{modId}-{Path.GetFileName(image.FileName)}";
                
                // Supprimer l'ancienne image si elle existe
                if (!string.IsNullOrEmpty(mod.ImageUrl))
                {
                    await _blobStorageService.DeleteBlobAsync(_modImageContainer, Path.GetFileName(mod.ImageUrl));
                }
                
                // Uploader l'image
                using (var stream = image.OpenReadStream())
                {
                    mod.ImageUrl = await _blobStorageService.UploadBlobAsync(_modImageContainer, fileName, stream);
                }
                
                mod.UpdatedAt = DateTime.UtcNow;

                var updatedMod = await _modRepository.UpdateModAsync(mod);
                
                // Mettre à jour le cache
                await _cacheService.SetAsync($"MOD_{modId}", updatedMod, _modDetailCacheExpiration);
                
                return new ApiResponse<Mod>
                {
                    Success = true,
                    Message = "Image mod uploadée avec succès",
                    Data = updatedMod
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Mod>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }
        
        public async Task<ApiResponse<PagedResult<Mod>>> SearchModsAsync(ModSearchParams searchParams)
        {
            try
            {
                // Créer une clé de cache basée sur les paramètres de recherche
                string cacheKey = $"SEARCH_{searchParams.GameId}_{searchParams.CategoryId}_{searchParams.SearchTerm}_{searchParams.SortBy}_{searchParams.PageNumber}_{searchParams.PageSize}";
                
                // Essayer de récupérer depuis le cache
                var cachedResult = await _cacheService.GetAsync<PagedResult<Mod>>(cacheKey);
                
                if (cachedResult != null)
                {
                    return new ApiResponse<PagedResult<Mod>>
                    {
                        Success = true,
                        Message = "Mods récupérés du cache avec succès",
                        Data = cachedResult
                    };
                }
                
                // Si pas dans le cache, effectuer la recherche
                var (mods, totalCount) = await _modRepository.SearchModsAsync(
                    searchParams.GameId,
                    searchParams.CategoryId,
                    searchParams.SearchTerm,
                    searchParams.SortBy,
                    searchParams.PageNumber,
                    searchParams.PageSize
                );
                
                // Créer le résultat paginé
                var result = new PagedResult<Mod>
                {
                    Items = mods,
                    TotalCount = totalCount,
                    PageSize = searchParams.PageSize,
                    CurrentPage = searchParams.PageNumber
                };
                
                // Mettre en cache les résultats
                await _cacheService.SetAsync(cacheKey, result, _searchResultsCacheExpiration);
                
                return new ApiResponse<PagedResult<Mod>>
                {
                    Success = true,
                    Message = "Mods récupérés avec succès",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<Mod>>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }
        
        public async Task<ApiResponse<Mod>> IncrementDownloadCountAsync(string modId)
        {
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    return new ApiResponse<Mod>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }
                
                mod.DownloadCount += 1;
                
                var updatedMod = await _modRepository.UpdateModAsync(mod);
                
                // Mettre à jour le cache
                await _cacheService.SetAsync($"MOD_{modId}", updatedMod, _modDetailCacheExpiration);
                
                return new ApiResponse<Mod>
                {
                    Success = true,
                    Message = "Compteur de téléchargement incrémenté",
                    Data = updatedMod
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Mod>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }
    }
}
