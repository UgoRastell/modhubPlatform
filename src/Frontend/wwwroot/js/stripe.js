/**
 * Module JavaScript pour l'intégration de Stripe
 */

let stripe;
let elements;
let cardElement;
let clientSecret;
let testMode = false;

/**
 * Charge la bibliothèque Stripe.js
 */
window.loadStripe = function () {
    if (document.getElementById('stripe-js')) {
        console.log('Stripe.js déjà chargé');
        return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
        const stripeScript = document.createElement('script');
        stripeScript.id = 'stripe-js';
        stripeScript.src = 'https://js.stripe.com/v3/';
        stripeScript.async = true;
        
        stripeScript.onload = () => {
            console.log('Stripe.js chargé avec succès');
            resolve();
        };
        
        stripeScript.onerror = () => {
            console.error('Erreur lors du chargement de Stripe.js');
            reject(new Error('Impossible de charger Stripe.js'));
        };
        
        document.head.appendChild(stripeScript);
    });
};

/**
 * Initialise le formulaire de carte Stripe
 * @param {string} publishableKey - Clé publique Stripe
 * @param {string} paymentIntentSecret - Client secret du PaymentIntent
 * @returns {Promise<void>}
 */
window.initializeCardForm = function (publishableKey, paymentIntentSecret) {
    if (!window.Stripe) {
        console.error('Stripe.js n\'est pas chargé');
        return Promise.reject(new Error('Stripe.js n\'est pas chargé'));
    }
    
    // Vérifier si l'élément card-element existe
    const cardElement = document.getElementById('card-element');
    if (!cardElement) {
        console.error('L\'\u00e9l\u00e9ment #card-element n\'est pas pr\u00e9sent dans le DOM');
        return Promise.reject(new Error('L\'\u00e9l\u00e9ment #card-element n\'est pas pr\u00e9sent dans le DOM'));
    }

    // Initialiser Stripe avec la clé publique
    stripe = Stripe(publishableKey);
    clientSecret = paymentIntentSecret;
    
    // Créer une instance de Elements
    const options = {
        locale: 'fr',
        appearance: {
            theme: 'stripe',
            variables: {
                colorPrimary: '#6772e5',
                colorBackground: '#ffffff',
                colorText: '#30313d',
                colorDanger: '#df1b41',
                fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
                spacingUnit: '4px',
                borderRadius: '4px'
            }
        }
    };
    elements = stripe.elements(options);
    
    // Créer l'élément de carte
    cardElement = elements.create('card', {
        style: {
            base: {
                color: '#32325d',
                fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
                fontSmoothing: 'antialiased',
                fontSize: '16px',
                '::placeholder': {
                    color: '#aab7c4'
                }
            },
            invalid: {
                color: '#fa755a',
                iconColor: '#fa755a'
            }
        }
    });
    
    // Monter l'élément de carte dans le DOM
    cardElement.mount('#card-element');
    
    // Gérer les erreurs de validation en temps réel
    cardElement.on('change', function(event) {
        const displayError = document.getElementById('card-errors');
        if (displayError) {
            if (event.error) {
                displayError.textContent = event.error.message;
                displayError.style.display = 'block';
            } else {
                displayError.textContent = '';
                displayError.style.display = 'none';
            }
        }
    });

    return Promise.resolve();
};

/**
 * Confirme le paiement avec les informations de carte saisies
 * @param {string} cardholderName - Nom du titulaire de la carte
 * @returns {Promise<Object>} - Résultat du paiement
 */
/**
 * Initialise le formulaire de carte Stripe en mode test sans client secret
 * @param {string} publishableKey - Clé publique Stripe de test
 * @returns {Promise<void>}
 */
window.initializeCardFormTestMode = function (publishableKey) {
    if (!window.Stripe) {
        console.error('Stripe.js n\'est pas chargé');
        return Promise.reject(new Error('Stripe.js n\'est pas chargé'));
    }
    
    // Vérifier si l'élément card-element existe
    const cardElement = document.getElementById('card-element');
    if (!cardElement) {
        console.error('L\'\u00e9l\u00e9ment #card-element n\'est pas pr\u00e9sent dans le DOM');
        return Promise.reject(new Error('L\'\u00e9l\u00e9ment #card-element n\'est pas pr\u00e9sent dans le DOM'));
    }

    // Activer le mode test
    testMode = true;
    
    // Initialiser Stripe avec la clé publique
    stripe = Stripe(publishableKey);
    
    // Créer une instance de Elements
    const options = {
        locale: 'fr',
        appearance: {
            theme: 'stripe',
            variables: {
                colorPrimary: '#6772e5',
                colorBackground: '#ffffff',
                colorText: '#30313d',
                colorDanger: '#df1b41',
                fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
                spacingUnit: '4px',
                borderRadius: '4px'
            }
        }
    };
    elements = stripe.elements(options);
    
    // Créer l'élément de carte
    cardElement = elements.create('card', {
        style: {
            base: {
                color: '#32325d',
                fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
                fontSmoothing: 'antialiased',
                fontSize: '16px',
                '::placeholder': {
                    color: '#aab7c4'
                }
            },
            invalid: {
                color: '#fa755a',
                iconColor: '#fa755a'
            }
        }
    });
    
    // Monter l'élément de carte dans le DOM
    cardElement.mount('#card-element');
    
    // Gérer les erreurs de validation en temps réel
    cardElement.on('change', function(event) {
        const displayError = document.getElementById('card-errors');
        if (displayError) {
            if (event.error) {
                displayError.textContent = event.error.message;
                displayError.style.display = 'block';
            } else {
                displayError.textContent = '';
                displayError.style.display = 'none';
            }
        }
    });

    return Promise.resolve();
};

window.confirmCardPayment = async function (cardholderName) {
    // En mode test, simuler une réponse réussie sans appeler l'API Stripe
    if (testMode) {
        console.log('Mode test: simulation d\'un paiement réussi');
        return {
            success: true,
            paymentIntentId: 'pi_test_' + Math.random().toString(36).substring(2, 15),
            status: 'succeeded'
        };
    }
    
    if (!stripe || !cardElement || !clientSecret) {
        console.error('Stripe n\'est pas correctement initialisé');
        return { success: false, error: 'Stripe n\'est pas correctement initialisé' };
    }

    try {
        const result = await stripe.confirmCardPayment(clientSecret, {
            payment_method: {
                card: cardElement,
                billing_details: {
                    name: cardholderName
                }
            }
        });
        
        if (result.error) {
            console.error('Erreur de paiement:', result.error);
            return { 
                success: false, 
                error: result.error.message 
            };
        } else if (result.paymentIntent.status === 'succeeded') {
            console.log('Paiement réussi:', result.paymentIntent);
            return { 
                success: true, 
                paymentIntentId: result.paymentIntent.id,
                status: result.paymentIntent.status
            };
        } else {
            console.warn('Statut de paiement non géré:', result.paymentIntent.status);
            return { 
                success: false, 
                status: result.paymentIntent.status,
                error: 'Le paiement est en attente ou a échoué'
            };
        }
    } catch (error) {
        console.error('Erreur lors de la confirmation du paiement:', error);
        return { 
            success: false, 
            error: error.message || 'Une erreur est survenue lors du paiement' 
        };
    }
};
