#!/bin/bash
set -e

# Script de déploiement automatisé pour ModHub
echo "=== Démarrage du déploiement ModHub ==="

# Mise à jour du système
echo "Mise à jour du système..."
apt update && apt upgrade -y

# Installation de Docker si nécessaire
if ! [ -x "$(command -v docker)" ]; then
  echo "Installation de Docker..."
  apt install -y apt-transport-https ca-certificates curl gnupg lsb-release
  curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
  echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
  apt update
  apt install -y docker-ce docker-ce-cli containerd.io
fi

# Installation de Docker Compose si nécessaire
if ! [ -x "$(command -v docker-compose)" ]; then
  echo "Installation de Docker Compose..."
  curl -L "https://github.com/docker/compose/releases/download/v2.23.3/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
  chmod +x /usr/local/bin/docker-compose
fi

# Création des répertoires requis
echo "Création des répertoires..."
mkdir -p docker/letsencrypt/live/modhub.fr
mkdir -p docker/letsencrypt/www
mkdir -p docker/data/mongo
mkdir -p docker/data/elasticsearch
mkdir -p docker/logstash/pipeline
mkdir -p docker/logstash/config

# Vérification du certificat SSL
if [ ! -f "docker/letsencrypt/live/modhub.fr/fullchain.pem" ]; then
  echo "Aucun certificat SSL trouvé. Configuration de Certbot..."
  apt install -y certbot
  mkdir -p /tmp/certbot
  certbot certonly --standalone -d modhub.fr -d www.modhub.fr --email admin@modhub.fr --agree-tos --no-eff-email --preferred-challenges http
  # Copie des certificats vers le répertoire Docker
  cp -L /etc/letsencrypt/live/modhub.fr/fullchain.pem docker/letsencrypt/live/modhub.fr/
  cp -L /etc/letsencrypt/live/modhub.fr/privkey.pem docker/letsencrypt/live/modhub.fr/
fi

# Création du .env file si nécessaire
if [ ! -f ".env" ]; then
  echo "Création du fichier .env..."
  cat > .env << EOL
MONGO_ROOT_USER=root
MONGO_ROOT_PASSWORD=changeme
JWT_SECRET=your_jwt_secret_key
STRIPE_SECRET_KEY=your_stripe_secret_key
STRIPE_WEBHOOK_SECRET=your_stripe_webhook_secret
GRAFANA_ADMIN_PASSWORD=admin
DOMAIN_NAME=modhub.fr
AZURE_STORAGE_ACCOUNT=your_storage_account
AZURE_STORAGE_KEY=your_storage_key
EOL
  echo "IMPORTANT: Veuillez éditer le fichier .env avec vos informations confidentielles!"
  exit 1
fi

# Arrêt des conteneurs existants
echo "Arrêt des conteneurs existants..."
docker-compose -f docker-compose.prod.yml down

# Démarrage des services
echo "Démarrage des services..."
docker-compose -f docker-compose.prod.yml up -d

# Vérification de l'état des conteneurs
echo "Vérification de l'état des conteneurs..."
docker-compose -f docker-compose.prod.yml ps

# Configuration du renouvellement automatique des certificats SSL
if ! grep -q "certbot renew" /etc/crontab; then
  echo "Configuration du renouvellement automatique des certificats SSL..."
  echo "0 3 * * * root certbot renew --quiet && cp -L /etc/letsencrypt/live/modhub.fr/fullchain.pem /etc/letsencrypt/live/modhub.fr/privkey.pem $(pwd)/docker/letsencrypt/live/modhub.fr/ && docker-compose -f $(pwd)/docker-compose.prod.yml restart nginx" >> /etc/crontab
fi

echo "=== Déploiement terminé avec succès ==="
echo "Vous pouvez maintenant accéder à ModHub sur https://modhub.fr"
