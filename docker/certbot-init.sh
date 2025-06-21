#!/bin/bash
set -e

# Configuration
DOMAIN="modhub.example.com"  # Remplacez par votre domaine réel
EMAIL="admin@modhub.example.com"  # Remplacez par votre email réel
STAGING=0  # Mettez à 1 pour tester (pas de limite de rate)

# Création du répertoire pour certbot
mkdir -p ./docker/letsencrypt/conf
mkdir -p ./docker/letsencrypt/www

# Paramètres certbot
CERTBOT_PARAMS="--non-interactive --agree-tos -m $EMAIL"

# Mode staging ou production
if [ $STAGING -eq 1 ]; then
  CERTBOT_PARAMS="$CERTBOT_PARAMS --staging"
fi

# Obtention du certificat
echo "Obtention du certificat pour $DOMAIN..."
docker run --rm -v $(pwd)/docker/letsencrypt/conf:/etc/letsencrypt \
  -v $(pwd)/docker/letsencrypt/www:/var/www/certbot \
  certbot/certbot certonly --webroot -w /var/www/certbot \
  $CERTBOT_PARAMS -d $DOMAIN

# Copie des certificats vers le répertoire des certs nginx
echo "Copie des certificats vers le répertoire nginx..."
mkdir -p ./docker/certs
cp ./docker/letsencrypt/conf/live/$DOMAIN/fullchain.pem ./docker/certs/
cp ./docker/letsencrypt/conf/live/$DOMAIN/privkey.pem ./docker/certs/

echo "Configuration HTTPS terminée avec succès!"
echo "N'oubliez pas d'ajouter le renouvellement automatique dans cron:"
echo "0 0 1 * * cd $(pwd) && ./docker/certbot-renew.sh"

# Création du script de renouvellement
cat > ./docker/certbot-renew.sh << EOF
#!/bin/bash
set -e

# Renouvellement du certificat
docker run --rm -v $(pwd)/docker/letsencrypt/conf:/etc/letsencrypt \\
  -v $(pwd)/docker/letsencrypt/www:/var/www/certbot \\
  certbot/certbot renew

# Copie des nouveaux certificats
cp ./docker/letsencrypt/conf/live/$DOMAIN/fullchain.pem ./docker/certs/
cp ./docker/letsencrypt/conf/live/$DOMAIN/privkey.pem ./docker/certs/

# Rechargement de Nginx
docker exec modhub_nginx_1 nginx -s reload
EOF

chmod +x ./docker/certbot-renew.sh
echo "Script de renouvellement créé: ./docker/certbot-renew.sh"
