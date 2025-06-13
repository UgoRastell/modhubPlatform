# Guide Technique - ModsGamingPlatform

Ce document est destiné aux développeurs et opérateurs qui maintiennent la plateforme ModsGamingPlatform.

## Architecture du Système

ModsGamingPlatform est une application basée sur une architecture de microservices .NET 8.0 comprenant :

### Microservices

| Service | Port | Description |
|---------|------|-------------|
| Gateway | 8080 | API Gateway avec Ocelot, point d'entrée pour toutes les requêtes API |
| UsersService | 80 | Gestion des utilisateurs, authentification et profils |
| ModsService | 80 | Gestion des mods, métadonnées, recherche et téléchargements |
| PaymentsService | 80 | Gestion des paiements et abonnements via Stripe |
| CommunityService | 80 | Forums, commentaires et interactions sociales |
| Frontend | 80 | Interface utilisateur Blazor WebAssembly |

### Composants d'Infrastructure

| Composant | Description |
|-----------|-------------|
| MongoDB | Base de données NoSQL principale |
| Redis | Cache pour améliorer les performances |
| Nginx | Serveur web et reverse proxy |
| Prometheus | Collecte de métriques |
| Grafana | Visualisation des métriques |
| ELK Stack | Elasticsearch, Logstash, Kibana pour la gestion des logs |

## Structure du Projet

```
modhub/
├── src/
│   ├── Gateway/
│   ├── UsersService/
│   ├── ModsService/
│   ├── PaymentsService/
│   ├── CommunityService/
│   └── Frontend/
├── tests/
│   ├── Gateway.Tests/
│   ├── UsersService.Tests/
│   ├── ModsService.Tests/
│   ├── PaymentsService.Tests/
│   └── CommunityService.Tests/
├── docker/
│   ├── nginx.conf
│   ├── certbot-init.sh
│   ├── backup.sh
│   ├── prometheus.yml
│   ├── alert.rules
│   ├── alertmanager.yml
│   ├── logstash/
│   ├── elasticsearch/
│   ├── kibana/
│   └── grafana/
├── docs/
│   ├── API.md
│   ├── DEPLOYMENT.md
│   ├── USER_GUIDE.md
│   └── TECHNICAL_GUIDE.md
├── docker-compose.yml
├── docker-compose.prod.yml
└── README.md
```

## Technologies Utilisées

- **Backend**: .NET 8.0, ASP.NET Core, MongoDB.Driver, StackExchange.Redis
- **Frontend**: Blazor WebAssembly, Bootstrap, JS Interop
- **Infrastructure**: Docker, Docker Compose, Nginx, Certbot
- **Monitoring**: Prometheus, Grafana, ELK Stack (Elasticsearch, Logstash, Kibana)
- **CI/CD**: GitHub Actions (ou autre outil selon votre configuration)
- **Services Cloud**: Azure Blob Storage (pour le stockage des mods)
- **Paiements**: Stripe API

## Développement Local

### Prérequis

- .NET SDK 8.0
- Docker et Docker Compose
- MongoDB (local ou conteneur)
- Redis (local ou conteneur)

### Configuration de l'environnement de développement

1. Clonez le dépôt :

```bash
git clone https://github.com/votre-organisation/modhub.git
cd modhub
```

2. Créez un fichier `.env.dev` à la racine du projet avec les variables suivantes :

```
# Informations Générales
DOMAIN_NAME=localhost
ENV=development

# MongoDB
MONGO_ROOT_USER=admin
MONGO_ROOT_PASSWORD=adminpassword
MONGO_DATABASE=modhub

# JWT
JWT_SECRET=dev_secret_key_change_in_production
JWT_EXPIRATION=86400

# Azure Storage (pour ModsService)
AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
AZURE_CONTAINER_NAME=mods-dev

# Stripe (pour PaymentsService)
STRIPE_API_KEY=sk_test_your_test_key
STRIPE_WEBHOOK_SECRET=whsec_your_test_webhook_secret
```

3. Lancez l'environnement de développement :

```bash
docker-compose up -d
```

4. Exécutez les migrations de base de données (si nécessaire) :

```bash
dotnet ef database update --project src/UsersService
dotnet ef database update --project src/ModsService
dotnet ef database update --project src/PaymentsService
dotnet ef database update --project src/CommunityService
```

5. Lancez les services individuellement pour le développement :

```bash
cd src/UsersService
dotnet run
```

### Debug via Visual Studio ou VS Code

1. Ouvrez la solution `ModsGamingPlatform.sln` dans Visual Studio.
2. Configurez le projet de démarrage avec plusieurs projets de démarrage.
3. Définissez les projets Gateway et Frontend comme projets de démarrage.
4. Appuyez sur F5 pour démarrer le débogage.

## Intégration Continue et Déploiement Continu (CI/CD)

### Flux de travail du développement

1. Développez sur une branche de fonctionnalité : `feature/nom-de-la-fonctionnalité`
2. Soumettez une Pull Request vers la branche `develop`
3. Les tests automatisés sont exécutés
4. Après approbation, la PR est fusionnée dans `develop`
5. Pour les releases, `develop` est fusionné dans `main`
6. Les déploiements en production sont déclenchés à partir de `main`

### Tests Automatisés

Exécutez les tests unitaires :

```bash
dotnet test
```

Exécutez les tests d'intégration :

```bash
dotnet test --filter Category=Integration
```

## Meilleures Pratiques de Codage

### Conventions de code

- Suivez les conventions de nommage Microsoft pour C#
- Utilisez PascalCase pour les classes, méthodes et propriétés
- Utilisez camelCase pour les variables locales
- Préfixez les interfaces par un "I"
- Utilisez var uniquement lorsque le type est évident

### Modèle de Conception

- Suivez les principes SOLID
- Injectez les dépendances via le constructeur
- Utilisez les interfaces pour les abstractions
- Préférez les classes immuables lorsque possible

### Documentation du Code

- Documentez les interfaces et méthodes publiques avec XML Comments
- Incluez des exemples d'utilisation pour les API complexes
- Gardez les commentaires synchronisés avec le code

## Monitoring et Alertes

### Metrics et Dashboards

- Prometheus : http://localhost:9090 (dev) ou https://votre-domaine.com/prometheus (prod)
- Grafana : http://localhost:3000 (dev) ou https://votre-domaine.com/grafana (prod)
  - Login par défaut : admin / admin
  - Changer le mot de passe à la première connexion
  - Des dashboards préconfigurés sont disponibles

### Logs

- Kibana : http://localhost:5601 (dev) ou https://votre-domaine.com/kibana (prod)
  - Index pattern : `logstash-*`
  - Les logs sont structurés selon le format :
    ```
    {
      "timestamp": "ISO8601",
      "level": "INFO|WARN|ERROR",
      "service": "service_name",
      "message": "log message",
      "trace_id": "correlation_id",
      "additional_data": { ... }
    }
    ```

### Alertes

Les alertes sont configurées pour :
- Indisponibilité de service (> 1 min)
- Utilisation CPU élevée (> 80% pendant 5 min)
- Utilisation mémoire élevée (> 1.5GB pendant 5 min)
- Taux d'erreurs HTTP élevé (> 5% pendant 2 min)
- Latence élevée (> 1s pour p95 pendant 5 min)

## Gestion des Performances

### Caching avec Redis

Le ModsService utilise Redis pour le cache avec les stratégies suivantes :

- Cache de liste de mods : 10 minutes
- Cache de détails d'un mod : 1 heure
- Cache de recherche : 5 minutes
- Invalider le cache lors des mises à jour de mods

Exemple de code :
```csharp
// Récupérer depuis le cache ou la DB
var cacheKey = $"mod:{modId}";
var mod = await _cache.GetAsync<ModDto>(cacheKey);
if (mod == null) 
{
    mod = await _repository.GetModByIdAsync(modId);
    await _cache.SetAsync(cacheKey, mod, TimeSpan.FromHours(1));
}
```

### Optimisations MongoDB

Les index suivants ont été configurés dans MongoDB :

#### Collection `mods`
- Index composé : `{ "gameId": 1, "category": 1 }`
- Index pour le tri : `{ "downloads": -1 }`
- Index de texte : `{ "$**": "text" }`

#### Collection `users`
- Index : `{ "email": 1 }`, unique
- Index : `{ "username": 1 }`, unique

## Procédures de Maintenance

### Backup et Restauration

Les backups sont automatisés et exécutés quotidiennement. Pour restaurer manuellement :

```bash
# Restauration de MongoDB
docker run --rm -v /path/to/backup:/backup -v modhub_mongodb_data:/data/db mongo \
  bash -c "mongorestore /backup/mongodb-backup-YYYY-MM-DD/dump"
```

### Mise à jour des Certificats SSL

Les certificats Let's Encrypt sont automatiquement renouvelés, mais pour forcer un renouvellement :

```bash
docker-compose -f docker-compose.prod.yml exec nginx certbot renew --force-renewal
docker-compose -f docker-compose.prod.yml restart nginx
```

### Gestion des Versions

Pour déployer une nouvelle version :

1. Mettre à jour les images Docker :
```bash
docker-compose -f docker-compose.prod.yml build
```

2. Déployer sans temps d'arrêt :
```bash
docker-compose -f docker-compose.prod.yml up -d
```

## Résolution de Problèmes

### Vérification des Journaux

```bash
# Vérifier les logs d'un service spécifique
docker-compose -f docker-compose.prod.yml logs --tail=100 usersservice

# Suivre les logs en temps réel
docker-compose -f docker-compose.prod.yml logs -f gateway
```

### Services Qui Crashent

1. Vérifiez l'état des conteneurs :
```bash
docker-compose -f docker-compose.prod.yml ps
```

2. Si un service est down, vérifiez les logs et redémarrez-le :
```bash
docker-compose -f docker-compose.prod.yml restart <service>
```

### Problèmes de Performance

1. Vérifiez l'utilisation des ressources :
```bash
docker stats
```

2. Identifiez les requêtes lentes dans Kibana ou les logs

3. Optimisez les requêtes ou ajoutez des index MongoDB si nécessaire

## Contacts et Support

Pour toute assistance technique interne :

- **Équipe DevOps** : devops@modhub.example.com
- **Équipe Backend** : backend@modhub.example.com
- **Équipe Frontend** : frontend@modhub.example.com
- **Canal Slack** : #tech-support

## Ressources Additionnelles

- [Documentation ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Documentation MongoDB](https://docs.mongodb.com/)
- [Documentation Redis](https://redis.io/documentation)
- [Documentation Docker](https://docs.docker.com/)
- [Documentation Prometheus](https://prometheus.io/docs/introduction/overview/)
- [Documentation ELK Stack](https://www.elastic.co/guide/index.html)
