#!/bin/bash
set -e

# Configuration
BACKUP_DIR="/opt/modhub/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="modhub_backup_$TIMESTAMP"
RETENTION_DAYS=7  # Nombre de jours de conservation des backups

# Assurez-vous que le répertoire existe
mkdir -p $BACKUP_DIR

echo "Démarrage du backup - $BACKUP_NAME"

# Création du répertoire temporaire pour ce backup
TEMP_DIR="$BACKUP_DIR/temp_$TIMESTAMP"
mkdir -p $TEMP_DIR

# Backup de MongoDB
echo "Sauvegarde de la base de données MongoDB..."
docker exec modhub_mongodb_1 sh -c 'mongodump --host localhost --port 27017 -u root -p example --authenticationDatabase admin --archive' > "$TEMP_DIR/mongodb_dump.archive"

# Backup des configurations
echo "Sauvegarde des fichiers de configuration..."
mkdir -p "$TEMP_DIR/configs"
cp -r ./docker/nginx.conf "$TEMP_DIR/configs/"
cp -r ./docker/prometheus.yml "$TEMP_DIR/configs/"
cp -r ./docker/logstash "$TEMP_DIR/configs/"

# Backup des certificats SSL
echo "Sauvegarde des certificats SSL..."
mkdir -p "$TEMP_DIR/certs"
cp -r ./docker/certs "$TEMP_DIR/certs/"
cp -r ./docker/letsencrypt/conf "$TEMP_DIR/letsencrypt_conf/"

# Création de l'archive finale
echo "Création de l'archive finale..."
tar -czf "$BACKUP_DIR/$BACKUP_NAME.tar.gz" -C "$TEMP_DIR" .
rm -rf "$TEMP_DIR"

# Nettoyage des anciens backups
echo "Nettoyage des backups datant de plus de $RETENTION_DAYS jours..."
find $BACKUP_DIR -name "modhub_backup_*.tar.gz" -type f -mtime +$RETENTION_DAYS -delete

echo "Backup terminé: $BACKUP_DIR/$BACKUP_NAME.tar.gz"

# Enregistrement dans le log
echo "$TIMESTAMP: Backup créé - $BACKUP_NAME.tar.gz" >> "$BACKUP_DIR/backup_history.log"
