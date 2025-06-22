#!/bin/bash
set -e

DOMAIN_NAME="modhub.fr"
EMAIL_LETSENCRYPT="votre@email.fr" # <-- à personnaliser !

echo "=== Déploiement automatisé ModHub ==="

# 1. MAJ système
echo "Mise à jour du système..."
apt update && apt upgrade -y

# 2. Docker
if ! [ -x "$(command -v docker)" ]; then
  echo "Installation de Docker..."
  apt install -y apt-transport-https ca-certificates curl gnupg lsb-release
  curl -fsSL https://download.docker.com/linux/$(. /etc/os-release; echo "$ID")/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/$(. /etc/os-release; echo "$ID") \
    $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
  apt update
  apt install -y docker-ce docker-ce-cli containerd.io
fi

# 3. Docker Compose
if ! [ -x "$(command -v docker-compose)" ]; then
  echo "Installation de Docker Compose..."
  curl -L "https://github.com/docker/compose/releases/download/v2.23.3/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
  chmod +x /usr/local/bin/docker-compose
  ln -sf /usr/local/bin/docker-compose /usr/bin/docker-compose
fi

# 4. Certbot (Let’s Encrypt)
if ! [ -x "$(command -v certbot)" ]; then
  echo "Installation de Certbot (SSL)..."
  apt install -y certbot
fi

# 5. Préparation des dossiers
echo "Préparation des répertoires nécessaires..."
mkdir -p docker/letsencrypt/live/${DOMAIN_NAME}
mkdir -p docker/letsencrypt/www
mkdir -p docker/data/mongo
mkdir -p docker/data/redis
mkdir -p docker/data/mods
mkdir -p docker/data/prometheus

# 6. Génération fichier .env
if [ ! -f ".env" ]; then
  echo "Création du fichier .env (à personnaliser impérativement !)"
  cat > .env << EOL
MONGO_ROOT_USER=root
MONGO_ROOT_PASSWORD=changeme
JWT_SECRET=your_jwt_secret_key
DOMAIN_NAME=${DOMAIN_NAME}
EOL
  echo "IMPORTANT : Modifiez le fichier .env avant de relancer le script !"
  exit 1
fi

# 7. Stop services existants
echo "Arrêt éventuel des anciens conteneurs..."
docker-compose -f docker-compose.prod.yml down || true

# 8. Générer un certificat auto-signé si non présent
if [ ! -f "./docker/ssl/certs/nginx-selfsigned.crt" ]; then
  echo "Génération d’un certificat SSL auto-signé pour tests..."
  mkdir -p ./docker/ssl/certs ./docker/ssl/private
  openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout ./docker/ssl/private/nginx-selfsigned.key \
    -out ./docker/ssl/certs/nginx-selfsigned.crt \
    -subj "/CN=${DOMAIN_NAME}"
  openssl dhparam -out ./docker/ssl/certs/dhparam.pem 2048
fi

# 9. Lancement stack Docker
echo "Démarrage de l'application avec Docker Compose..."
docker-compose -f docker-compose.prod.yml up -d

# 10. Crontab pour renouvellement SSL
echo "Mise en place du cron pour renouvellement SSL automatique..."
CRONLINE="0 3 * * * certbot renew --webroot -w $(pwd)/docker/letsencrypt/www --deploy-hook 'cp -L /etc/letsencrypt/live/${DOMAIN_NAME}/fullchain.pem $(pwd)/docker/letsencrypt/live/${DOMAIN_NAME}/ && cp -L /etc/letsencrypt/live/${DOMAIN_NAME}/privkey.pem $(pwd)/docker/letsencrypt/live/${DOMAIN_NAME}/ && docker-compose -f $(pwd)/docker-compose.prod.yml restart nginx'"
(crontab -l | grep -v 'certbot renew'; echo "$CRONLINE") | crontab -

echo "=== Déploiement terminé ==="
echo "Accédez à la plateforme sur : https://${DOMAIN_NAME}/"
