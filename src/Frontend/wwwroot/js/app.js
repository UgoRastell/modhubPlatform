// Main application script file
document.addEventListener('DOMContentLoaded', function() {
    // Initialize any custom JS functionality here
    console.log('ModsGamingPlatform app initialized');

    // Handle theme switching if needed
    setupThemeSwitch();
});

// Example function for theme switching
function setupThemeSwitch() {
    // Will be implemented when theme switching is needed
    // Can interact with MudBlazor's theme provider through JSInterop
}

// Function that can be called from Blazor via JSInterop
window.appFunctions = {
    scrollToTop: function() {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    },
    
    setPageTitle: function(title) {
        document.title = title ? `${title} - ModsGamingPlatform` : 'ModsGamingPlatform';
    }
};
