#!/bin/bash
set -e

# Couleurs pour l'affichage
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Initialisation de l'infrastructure ModsGamingPlatform ===${NC}"

# Vérification des prérequis
echo -e "${YELLOW}Vérification des prérequis...${NC}"

# Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Docker n'est pas installé. Veuillez l'installer avant de continuer.${NC}"
    exit 1
fi

# Docker-compose
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}Docker Compose n'est pas installé. Veuillez l'installer avant de continuer.${NC}"
    exit 1
fi

# Création des répertoires nécessaires
echo -e "${YELLOW}Création des répertoires de données...${NC}"
mkdir -p ./docker/data/mongo
mkdir -p ./docker/data/elasticsearch
mkdir -p ./docker/data/prometheus
mkdir -p ./docker/data/grafana
mkdir -p ./docker/logstash
mkdir -p ./docker/certs
mkdir -p ./docker/letsencrypt/conf
mkdir -p ./docker/letsencrypt/www
mkdir -p ./docker/modsecurity

# Configuration de Logstash si le fichier n'existe pas
if [ ! -f ./docker/logstash/logstash.conf ]; then
    echo -e "${YELLOW}Création de la configuration Logstash par défaut...${NC}"
    cp ./docker/logstash.conf ./docker/logstash/logstash.conf
fi

# Configuration de Prometheus si le fichier n'existe pas
if [ ! -f ./docker/prometheus.yml ]; then
    echo -e "${RED}Le fichier prometheus.yml est manquant. Veuillez le créer avant de continuer.${NC}"
    exit 1
fi

# Démarrage des services
echo -e "${YELLOW}Démarrage des services...${NC}"
docker-compose up -d mongodb rabbitmq elasticsearch logstash kibana

# Attente que les services critiques soient prêts
echo -e "${YELLOW}Attente du démarrage complet de MongoDB...${NC}"
sleep 10

# Vérification de MongoDB
echo -e "${YELLOW}Vérification de MongoDB...${NC}"
if docker-compose exec -T mongodb mongosh --eval "db.adminCommand('ping')" | grep -q "ok"; then
    echo -e "${GREEN}MongoDB est prêt.${NC}"
else
    echo -e "${RED}MongoDB n'est pas disponible. Vérifiez les logs.${NC}"
    docker-compose logs mongodb
    exit 1
fi

# Démarrage du reste des services
echo -e "${YELLOW}Démarrage des services principaux...${NC}"
docker-compose up -d gateway usersservice modsservice paymentsservice communityservice frontend nginx

# Démarrage du monitoring
echo -e "${YELLOW}Démarrage des services de monitoring...${NC}"
docker-compose up -d prometheus grafana

# Configuration HTTPS avec Let's Encrypt (optionnelle)
read -p "Souhaitez-vous configurer HTTPS avec Let's Encrypt maintenant? (o/n): " configure_https
if [[ $configure_https == "o" || $configure_https == "O" || $configure_https == "oui" ]]; then
    echo -e "${YELLOW}Lancement de la configuration HTTPS...${NC}"
    bash ./docker/certbot-init.sh
else
    echo -e "${YELLOW}Configuration HTTPS ignorée. Vous pourrez la configurer ultérieurement avec ./docker/certbot-init.sh${NC}"
fi

# Configuration du WAF
echo -e "${YELLOW}Démarrage du WAF (ModSecurity)...${NC}"
docker-compose up -d modsec-crs

# Vérification finale
echo -e "${YELLOW}Vérification finale de l'infrastructure...${NC}"
if bash ./docker/healthcheck.sh; then
    echo -e "${GREEN}=== Infrastructure démarrée avec succès! ===${NC}"
    echo -e "${YELLOW}Dashboard Grafana: ${NC}http://localhost:3000 (admin/admin)"
    echo -e "${YELLOW}Kibana: ${NC}http://localhost:5601"
    echo -e "${YELLOW}Application: ${NC}http://localhost"
    echo -e "${YELLOW}API Gateway: ${NC}http://localhost/api"
else
    echo -e "${RED}=== Des problèmes ont été détectés dans l'infrastructure. ===${NC}"
    echo -e "${YELLOW}Consultez les logs pour plus de détails: ${NC}docker-compose logs"
fi
