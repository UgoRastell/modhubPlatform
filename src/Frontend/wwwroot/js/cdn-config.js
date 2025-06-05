// Configuration CDN pour les fichiers statiques
const CDN_CONFIG = {
    // URL de base du CDN
    baseUrl: 'https://cdn.modsgamingplatform.com',
    
    // Configuration des types de ressources
    resourceTypes: {
        images: {
            path: '/images',
            maxAge: 86400 // 24h en secondes
        },
        modImages: {
            path: '/mods/images',
            maxAge: 604800 // 7 jours en secondes
        },
        gameImages: {
            path: '/games/images',
            maxAge: 2592000 // 30 jours en secondes
        },
        scripts: {
            path: '/js',
            maxAge: 86400 // 24h en secondes
        },
        styles: {
            path: '/css',
            maxAge: 86400 // 24h en secondes
        },
        fonts: {
            path: '/fonts',
            maxAge: 2592000 // 30 jours en secondes
        }
    },
    
    // Fonction pour obtenir l'URL CDN d'une ressource
    getUrl: function(resourceType, filename) {
        if (!this.resourceTypes[resourceType]) {
            console.error(`Type de ressource inconnu: ${resourceType}`);
            return filename; // Retourner le nom de fichier original en cas d'erreur
        }
        
        return `${this.baseUrl}${this.resourceTypes[resourceType].path}/${filename}`;
    },
    
    // Fonction pour précharger les ressources critiques
    preloadCriticalResources: function() {
        const criticalResources = [
            { type: 'images', file: 'logo.png' },
            { type: 'styles', file: 'main.css' },
            { type: 'scripts', file: 'app.js' }
        ];
        
        criticalResources.forEach(resource => {
            const link = document.createElement('link');
            link.rel = 'preload';
            link.href = this.getUrl(resource.type, resource.file);
            
            if (resource.type === 'scripts') {
                link.as = 'script';
            } else if (resource.type === 'styles') {
                link.as = 'style';
            } else if (resource.type === 'images') {
                link.as = 'image';
            } else if (resource.type === 'fonts') {
                link.as = 'font';
                link.crossOrigin = 'anonymous';
            }
            
            document.head.appendChild(link);
        });
    },
    
    // Fonction pour charger les scripts via le CDN
    loadScript: function(filename, callback) {
        const script = document.createElement('script');
        script.src = this.getUrl('scripts', filename);
        script.async = true;
        
        if (callback) {
            script.onload = callback;
        }
        
        document.head.appendChild(script);
    },
    
    // Fonction pour charger les styles via le CDN
    loadStyle: function(filename) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = this.getUrl('styles', filename);
        
        document.head.appendChild(link);
    }
};

// Exporter la configuration pour l'utiliser dans d'autres fichiers
window.CDN_CONFIG = CDN_CONFIG;

// Précharger les ressources critiques au chargement de la page
document.addEventListener('DOMContentLoaded', function() {
    CDN_CONFIG.preloadCriticalResources();
});
