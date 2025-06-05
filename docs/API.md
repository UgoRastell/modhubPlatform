# Documentation de l'API - ModsGamingPlatform

Cette documentation décrit les API RESTful disponibles pour interagir avec la plateforme ModsGamingPlatform.

## Base URL

```
Production: https://api.votre-domaine.com
Développement: http://localhost:5000
```

## Authentification

L'API utilise une authentification JWT (JSON Web Token). Pour obtenir un token :

### Authentification

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "utilisateur@exemple.com",
  "password": "motdepasse"
}
```

Réponse :

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 86400,
  "userId": "507f1f77bcf86cd799439011"
}
```

Pour les appels API suivants, incluez le token dans l'en-tête HTTP :

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## API pour les Mods

### Récupérer tous les mods avec pagination

```http
GET /api/mods?page=1&pageSize=20&sortBy=downloads&sortOrder=desc&gameId=123&category=weapons
```

Paramètres optionnels :
- `page` : Numéro de page (défaut: 1)
- `pageSize` : Nombre d'éléments par page (défaut: 20, max: 100)
- `sortBy` : Champ de tri (name, downloads, rating, createdAt)
- `sortOrder` : Ordre de tri (asc, desc)
- `gameId` : Filtrer par jeu
- `category` : Filtrer par catégorie
- `search` : Terme de recherche

Réponse :

```json
{
  "items": [
    {
      "id": "507f1f77bcf86cd799439011",
      "name": "Awesome Mod",
      "description": "This is an awesome mod that enhances gameplay",
      "version": "1.2.3",
      "gameId": "123",
      "category": "weapons",
      "author": {
        "id": "507f1f77bcf86cd799439012",
        "username": "ModCreator"
      },
      "rating": 4.8,
      "downloads": 15000,
      "thumbnailUrl": "https://cdn.votre-domaine.com/mods/507f1f77bcf86cd799439011/thumbnail.jpg",
      "createdAt": "2023-01-15T14:30:00Z",
      "updatedAt": "2023-02-01T10:15:00Z"
    },
    // ... autres mods
  ],
  "totalItems": 150,
  "totalPages": 8,
  "currentPage": 1
}
```

### Récupérer un mod spécifique

```http
GET /api/mods/{modId}
```

Réponse :

```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "Awesome Mod",
  "description": "This is an awesome mod that enhances gameplay",
  "longDescription": "Detailed description with formatting...",
  "version": "1.2.3",
  "gameId": "123",
  "gameName": "Super Game",
  "category": "weapons",
  "author": {
    "id": "507f1f77bcf86cd799439012",
    "username": "ModCreator",
    "profileUrl": "/users/507f1f77bcf86cd799439012"
  },
  "rating": 4.8,
  "downloads": 15000,
  "fileSize": 1024000,
  "thumbnailUrl": "https://cdn.votre-domaine.com/mods/507f1f77bcf86cd799439011/thumbnail.jpg",
  "screenShots": [
    "https://cdn.votre-domaine.com/mods/507f1f77bcf86cd799439011/screenshot1.jpg",
    "https://cdn.votre-domaine.com/mods/507f1f77bcf86cd799439011/screenshot2.jpg"
  ],
  "requirements": {
    "minGameVersion": "1.0.0",
    "dependencies": ["507f1f77bcf86cd799439013"]
  },
  "tags": ["multiplayer", "weapon", "enhancement"],
  "createdAt": "2023-01-15T14:30:00Z",
  "updatedAt": "2023-02-01T10:15:00Z"
}
```

### Créer un nouveau mod

```http
POST /api/mods
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "name": "My New Mod",
  "description": "Short description",
  "longDescription": "Long description with details",
  "version": "1.0.0",
  "gameId": "123",
  "category": "weapons",
  "tags": ["multiplayer", "weapon"]
}
```

Réponse (201 Created) :

```json
{
  "id": "507f1f77bcf86cd799439014",
  "name": "My New Mod",
  "uploadUrl": "https://upload.votre-domaine.com/mods/507f1f77bcf86cd799439014?signature=...",
  "message": "Mod created successfully. Use the uploadUrl to upload the mod file."
}
```

### Télécharger un fichier de mod

```http
POST /api/mods/{modId}/upload
Content-Type: multipart/form-data
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

file: [binary data]
```

Réponse :

```json
{
  "success": true,
  "fileId": "507f1f77bcf86cd799439015",
  "fileSize": 1024000,
  "message": "File uploaded successfully"
}
```

### Mettre à jour un mod

```http
PUT /api/mods/{modId}
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "name": "Updated Mod Name",
  "description": "Updated description",
  "version": "1.0.1"
}
```

Réponse :

```json
{
  "success": true,
  "message": "Mod updated successfully"
}
```

### Supprimer un mod

```http
DELETE /api/mods/{modId}
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

Réponse :

```json
{
  "success": true,
  "message": "Mod deleted successfully"
}
```

### Télécharger un mod

```http
GET /api/mods/{modId}/download
```

Cette requête incrémente le compteur de téléchargements et renvoie une redirection 302 vers l'URL de téléchargement.

## API pour les Utilisateurs

### Récupérer le profil utilisateur

```http
GET /api/users/{userId}
```

Réponse :

```json
{
  "id": "507f1f77bcf86cd799439012",
  "username": "ModCreator",
  "profilePicture": "https://cdn.votre-domaine.com/users/507f1f77bcf86cd799439012/profile.jpg",
  "bio": "I create awesome mods!",
  "registeredAt": "2022-12-01T10:00:00Z",
  "modsCount": 15,
  "followersCount": 120,
  "websiteUrl": "https://modcreator.com"
}
```

### Mettre à jour le profil utilisateur

```http
PUT /api/users/profile
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "username": "NewUsername",
  "bio": "Updated bio information",
  "websiteUrl": "https://newwebsite.com"
}
```

Réponse :

```json
{
  "success": true,
  "message": "Profile updated successfully"
}
```

### Récupérer les mods d'un utilisateur

```http
GET /api/users/{userId}/mods?page=1&pageSize=20
```

Réponse : similaire à la liste des mods

## API pour les Paiements

### Obtenir les plans d'abonnement disponibles

```http
GET /api/payments/plans
```

Réponse :

```json
{
  "plans": [
    {
      "id": "basic",
      "name": "Basic",
      "price": 4.99,
      "currency": "USD",
      "features": ["Download standard mods", "Basic support"],
      "stripeProductId": "prod_abc123"
    },
    {
      "id": "premium",
      "name": "Premium",
      "price": 9.99,
      "currency": "USD",
      "features": ["Download all mods", "Priority support", "Early access"],
      "stripeProductId": "prod_def456"
    }
  ]
}
```

### Créer un abonnement

```http
POST /api/payments/subscribe
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "planId": "premium",
  "paymentMethodId": "pm_card_visa"
}
```

Réponse :

```json
{
  "success": true,
  "subscriptionId": "sub_1234",
  "clientSecret": "pi_1234_secret_5678",
  "message": "Subscription created successfully"
}
```

## API pour la Communauté

### Récupérer les discussions d'un mod

```http
GET /api/community/mods/{modId}/discussions?page=1&pageSize=20
```

Réponse :

```json
{
  "items": [
    {
      "id": "507f1f77bcf86cd799439016",
      "title": "How to install this mod?",
      "content": "I'm having trouble installing this mod. Can someone help?",
      "author": {
        "id": "507f1f77bcf86cd799439017",
        "username": "ModUser"
      },
      "createdAt": "2023-02-05T16:45:00Z",
      "repliesCount": 3,
      "lastReplyAt": "2023-02-05T18:30:00Z"
    },
    // ... autres discussions
  ],
  "totalItems": 25,
  "totalPages": 2,
  "currentPage": 1
}
```

### Créer une nouvelle discussion

```http
POST /api/community/mods/{modId}/discussions
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "title": "Question about compatibility",
  "content": "Is this mod compatible with version 2.0 of the game?"
}
```

Réponse (201 Created) :

```json
{
  "id": "507f1f77bcf86cd799439018",
  "title": "Question about compatibility",
  "message": "Discussion created successfully"
}
```

## Limites de l'API

- Rate limit: 100 requêtes par minute par IP
- Taille maximale du fichier pour les uploads: 100 MB
- Nombre maximal de mods par utilisateur: 50

## Codes d'erreur

| Code | Description |
|------|-------------|
| 400 | Requête invalide (vérifiez les paramètres) |
| 401 | Non authentifié (token manquant ou invalide) |
| 403 | Non autorisé (permissions insuffisantes) |
| 404 | Ressource non trouvée |
| 409 | Conflit (ex: nom de mod déjà existant) |
| 429 | Trop de requêtes (rate limit dépassé) |
| 500 | Erreur serveur interne |

## Versionnement de l'API

L'API actuelle est la v1. Pour utiliser une version spécifique, préfixez vos requêtes avec `/v1/`. Par exemple :

```
https://api.votre-domaine.com/v1/mods
```

## Support

Pour toute question relative à l'API, contactez notre équipe de développement à api-support@modhub.example.com ou consultez notre [forum pour développeurs](https://developers.modhub.example.com).
