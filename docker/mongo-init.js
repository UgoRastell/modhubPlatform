// Script d'initialisation MongoDB pour ModHub
print('Démarrage du script d\'initialisation MongoDB...');

// Authentification en tant que root
db.auth(process.env.MONGO_INITDB_ROOT_USERNAME, process.env.MONGO_INITDB_ROOT_PASSWORD);

// Passage à la base de données de l'application
db = db.getSiblingDB('ModsGamingPlatform');

// Création d'une collection pour s'assurer que la BD existe
db.createCollection('users');
db.createCollection('mods');
db.createCollection('payments');
db.createCollection('comments');
db.createCollection('ratings');

// Pour s'assurer que les collections sont correctement créées
print('Collections créées avec succès:');
db.getCollectionNames().forEach(function(collection) {
  print('- ' + collection);
});

print('Initialisation MongoDB terminée avec succès.');
