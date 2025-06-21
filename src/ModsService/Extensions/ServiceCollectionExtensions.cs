using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModsService.Repositories;
using ModsService.Services.Download;
using ModsService.Services.Security;
using ModsService.Services.Storage;

namespace ModsService.Extensions
{
    /// <summary>
    /// Extensions pour la configuration des services de l'application
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Ajoute les services spécifiques au module Mods
        /// </summary>
        public static IServiceCollection AddModsServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Repositories
            services.AddScoped<IModRepository, ModRepository>();
            services.AddScoped<IDownloadQuotaRepository, DownloadQuotaRepository>();
            services.AddScoped<IDownloadHistoryRepository, DownloadHistoryRepository>();
            
            // Services Storage
            services.AddSingleton<IBlobStorageService, LocalBlobStorageService>();
            services.AddScoped<FileValidationService>();
            
            // Services Security
            services.AddScoped<ISecurityScanService, SecurityScanService>();
            
            // Services Versioning
            services.AddScoped<IVersioningService, VersioningService>();
            services.AddScoped<IVersionHistoryService, VersionHistoryService>();
            
            // Services Download
            services.AddScoped<IDownloadService, DownloadService>();
            
            // Configuration
            services.Configure<BlobStorageSettings>(configuration.GetSection("BlobStorage"));
            
            return services;
        }
    }
}
