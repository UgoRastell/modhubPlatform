# ModsGamingPlatform - Plateforme de Mods

Une plateforme SaaS innovante dédiée au partage, à la découverte et à la monétisation de mods de jeux vidéo, construite avec une architecture de microservices .NET.

## Infrastructure Technique (Phase 1)

Cette première phase d'implémentation met en place l'infrastructure de base nécessaire pour supporter l'ensemble de la plateforme.

### Architecture des Microservices

Le projet est composé des services suivants :

- **Gateway API** : Point d'entrée unique pour les clients, routage des requêtes (port 8080)
- **UsersService** : Gestion des utilisateurs et authentification (port 5001)
- **ModsService** : Gestion des mods et des fichiers associés (port 5002)
- **PaymentsService** : Intégration avec Stripe, gestion des paiements (port 5003)
- **CommunityService** : Fonctionnalités communautaires (commentaires, notes) (port 5004)
- **Frontend** : Interface utilisateur en Blazor WebAssembly

### Services d'Infrastructure

- **MongoDB** : Base de données principale (port 27017)
- **RabbitMQ** : Messagerie pour la communication entre services (ports 5672, 15672)
- **Nginx** : Reverse proxy et terminaison SSL (ports 80, 443)
- **ELK Stack** :
  - Elasticsearch : Stockage et indexation des logs (port 9200)
  - Logstash : Collection et traitement des logs (port 5000)
  - Kibana : Visualisation des logs (port 5601)
- **Prometheus & Grafana** : Monitoring et alerting (ports 9090, 3000)
- **ModSecurity** : Web Application Firewall (WAF) pour la sécurité (port 8000)

## Mise en Route

### Prérequis

- Docker et Docker Compose
- Git
- Domaine configuré pour pointer vers votre serveur (pour HTTPS)

### Installation et Démarrage

1. Clonez le dépôt :

```bash
git clone https://github.com/votre-repo/modhub.git
cd modhub
```

2. Exécutez le script d'initialisation :

```bash
chmod +x init-infrastructure.sh
./init-infrastructure.sh
```

Le script effectue automatiquement :
- Création des répertoires nécessaires
- Démarrage des services dans le bon ordre
- Vérification de l'état de santé des services
- Configuration optionnelle de HTTPS avec Let's Encrypt

### Accès aux Services

Une fois le déploiement terminé, vous pouvez accéder à :

- **Application** : http://localhost ou https://votre-domaine.com
- **API Gateway** : http://localhost/api ou https://votre-domaine.com/api
- **Kibana** : http://localhost:5601
- **Grafana** : http://localhost:3000 (identifiants par défaut: admin/admin)
- **RabbitMQ Management** : http://localhost:15672 (identifiants: admin/admin)

## Sécurité

### HTTPS avec Let's Encrypt

Pour configurer HTTPS manuellement :

```bash
./docker/certbot-init.sh
```

N'oubliez pas d'adapter le domaine dans le script en modifiant les variables `DOMAIN` et `EMAIL`.

### Web Application Firewall (WAF)

Le WAF ModSecurity est configuré avec les règles OWASP Core Rule Set et des règles personnalisées pour :
- Protection contre les injections SQL
- Protection contre les attaques XSS
- Limitation des taux de requêtes
- Protection contre les attaques par force brute
- Et plus encore...

### Backups

Un script de sauvegarde est disponible pour MongoDB et les fichiers de configuration :

```bash
./docker/backup.sh
```

Les sauvegardes sont conservées pendant 7 jours par défaut. Ajoutez ce script à votre crontab pour des sauvegardes automatiques :

```
0 2 * * * /opt/modhub/docker/backup.sh
```

## Monitoring et Logs

### Centralisation des Logs (ELK)

Tous les logs des services sont envoyés à Logstash sur le port 5000, puis stockés dans Elasticsearch et visualisables dans Kibana.

### Monitoring (Prometheus & Grafana)

Le monitoring de l'infrastructure est disponible via Grafana. Des dashboards préconfigurés sont disponibles pour :
- Métriques des services .NET
- MongoDB
- RabbitMQ
- Nginx
- Système hôte

## Maintenance

### Health Check

Pour vérifier l'état de santé des services :

```bash
./docker/healthcheck.sh
```

### Renouvellement des Certificats SSL

Les certificats Let's Encrypt expirent après 90 jours. Un script de renouvellement est créé automatiquement :

```bash
./docker/certbot-renew.sh
```

Il est recommandé d'ajouter ce script à votre crontab pour un renouvellement automatique :

```
0 0 1 * * /opt/modhub/docker/certbot-renew.sh
```

## Prochaines Étapes

Après avoir complété la Phase 1 (Infrastructure), voici les prochaines phases de développement :

1. **Phase 2** : Développement des fonctionnalités de base des microservices
2. **Phase 3** : Frontend et expérience utilisateur avec Blazor WebAssembly
3. **Phase 4** : Tests et optimisations
4. **Phase 5** : Déploiement en production
