#!/bin/sh

# Dossier destination
mkdir -p /certs/modhub.ovh

# Générer un certificat auto-signé pour modhub.ovh valable 1 an
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /certs/modhub.ovh/privkey.pem \
  -out /certs/modhub.ovh/fullchain.pem \
  -subj "/CN=modhub.ovh" \
  -addext "subjectAltName = DNS:modhub.ovh,DNS:www.modhub.ovh"

# Afficher des infos pour vérification
echo "Certificat SSL auto-signé généré avec succès:"
openssl x509 -in /certs/modhub.ovh/fullchain.pem -noout -text | grep -E 'Subject:|DNS:'

echo "Le certificat a été enregistré dans le dossier /certs/modhub.ovh/"
