using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ModsService.Models
{
    public class ModUploadRequest
    {
        /// <summary>
        /// Nom du mod
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description du mod
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// ID du jeu associé au mod
        /// </summary>
        public string GameId { get; set; }
        
        /// <summary>
        /// Nom du jeu (optionnel, peut être résolu côté serveur)
        /// </summary>
        public string GameName { get; set; }
        
        /// <summary>
        /// Version du mod
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Tags associés au mod pour faciliter la recherche
        /// </summary>
        public string[] Tags { get; set; }
        
        /// <summary>
        /// Fichier principal du mod (.zip, .rar)
        /// </summary>
        public IFormFile ModFile { get; set; }
        
        /// <summary>
        /// Image miniature du mod (optionnel)
        /// </summary>
        public IFormFile ThumbnailFile { get; set; }
    }
}
