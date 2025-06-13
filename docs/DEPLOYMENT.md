# Guide de déploiement - ModsGamingPlatform

Ce document décrit le processus de déploiement complet de la plateforme ModsGamingPlatform en environnement de production.

## Prérequis

- Serveur Linux avec Docker et Docker Compose installés (version 20.10+ recommandée)
- Minimum 8 Go de RAM, 4 vCPU et 50 Go d'espace disque
- Nom de domaine configuré avec les enregistrements DNS pointant vers l'IP du serveur
- Accès SSH au serveur
- Ports ouverts : 80, 443, 22

## Variables d'environnement requises

Créez un fichier `.env` à la racine du projet avec les variables suivantes :

```
# Informations Générales
DOMAIN_NAME=votre-domaine.com
ENV=production

# MongoDB
MONGO_ROOT_USER=admin
MONGO_ROOT_PASSWORD=votre_mot_de_passe_mongo_ici
MONGO_DATABASE=modhub

# JWT
JWT_SECRET=votre_cle_secrete_jwt_ici
JWT_EXPIRATION=86400

# Azure Storage (pour ModsService)
AZURE_STORAGE_CONNECTION_STRING=votre_chaine_de_connexion_azure_ici
AZURE_CONTAINER_NAME=mods

# Stripe (pour PaymentsService)
STRIPE_API_KEY=votre_cle_api_stripe_ici
STRIPE_WEBHOOK_SECRET=votre_cle_secrete_webhook_stripe_ici

# Grafana
GRAFANA_ADMIN_PASSWORD=votre_mot_de_passe_admin_grafana_ici

# Alerting
SMTP_PASSWORD=votre_mot_de_passe_smtp_ici
SLACK_WEBHOOK=votre_url_webhook_slack_ici
```

## Déploiement Initial

1. Clonez le dépôt sur votre serveur :

```bash
git clone https://github.com/votre-organisation/modhub.git
cd modhub
```

2. Créez le fichier `.env` avec vos variables comme indiqué ci-dessus.

3. Exécutez le script d'initialisation des certificats SSL :

```bash
chmod +x ./docker/certbot-init.sh
./docker/certbot-init.sh votre-domaine.com votre@email.com
```

4. Lancez la pile Docker en mode production :

```bash
docker compose -f docker-compose.prod.yml up -d
```

## Vérification du déploiement

1. Vérifiez que tous les conteneurs sont en cours d'exécution :

```bash
docker compose -f docker-compose.prod.yml ps
```

2. Accédez aux différentes interfaces :
   - Frontend : https://votre-domaine.com
   - Grafana : https://votre-domaine.com/grafana (identifiant : admin, mot de passe : celui défini dans .env)
   - Kibana : https://votre-domaine.com/kibana
   - Prometheus : https://votre-domaine.com/prometheus

## Backups

Les backups sont automatiquement configurés via un container dédié, qui exécute le script `backup.sh` quotidiennement à 2h du matin. Les backups sont stockés dans le répertoire `/backups` sur l'hôte.

Pour restaurer un backup :

```bash
# Arrêtez les services
docker compose -f docker-compose.prod.yml down

# Restaurez les données MongoDB
docker run --rm -v $(pwd)/backups:/backups -v modhub_mongodb_data:/data/db mongo \
  bash -c "tar -xzf /backups/mongodb-backup-YYYY-MM-DD.tar.gz -C /tmp && mongorestore /tmp/dump"

# Redémarrez les services
docker compose -f docker-compose.prod.yml up -d
```

## Mise à jour de la plateforme

Pour mettre à jour la plateforme vers une nouvelle version :

```bash
# Récupérez les dernières modifications
git pull

# Reconstruisez et redémarrez les conteneurs
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
```

## Monitoring et alertes

Le système de monitoring est configuré pour envoyer des alertes via email et Slack en cas de problème. Vous pouvez ajuster les seuils et les destinataires dans les fichiers suivants :

- Alertes Prometheus : `docker/alert.rules`
- Configuration AlertManager : `docker/alertmanager.yml`

## Résolution de problèmes courants

### Erreur lors de l'obtention des certificats SSL

Si vous rencontrez des erreurs avec Let's Encrypt, vérifiez que :
- Votre domaine est correctement configuré avec les DNS
- Les ports 80 et 443 sont ouverts
- Vous n'avez pas dépassé les limites de rate de Let's Encrypt (5 certificats par domaine par semaine)

### Erreur de connexion à MongoDB

Vérifiez les logs de MongoDB :
```bash
docker compose -f docker-compose.prod.yml logs mongodb
```

### Performances lentes

1. Vérifiez l'utilisation des ressources via Grafana
2. Augmentez les ressources allouées aux conteneurs dans `docker-compose.prod.yml`
3. Vérifiez les index MongoDB et optimisez si nécessaire

## Support

Pour toute assistance, contactez l'équipe de support à support@modhub.example.com
