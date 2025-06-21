using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Frontend.Theme;

/// <summary>
/// Implementation of IThemeService that manages theme state and persistence
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode = true; // Default to dark mode

    public bool IsDarkMode => _isDarkMode;

    public event EventHandler<bool>? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        // Note: In a real implementation, we'd load the theme preference from localStorage
        // But that requires async initialization which is complicated in a service constructor
    }

    /// <summary>
    /// Initialize theme from saved preferences
    /// Should be called from App.razor OnInitialized
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var storedTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "theme-preference");
            if (!string.IsNullOrEmpty(storedTheme))
            {
                _isDarkMode = storedTheme == "dark";
            }
            else
            {
                // Try to detect system preference
                var prefersDark = await _jsRuntime.InvokeAsync<bool>("matchMedia", "(prefers-color-scheme: dark)");
                _isDarkMode = prefersDark;
            }
        }
        catch (Exception)
        {
            // Default to dark mode if something goes wrong
            _isDarkMode = true;
        }
    }

    public void SetDarkMode(bool isDarkMode)
    {
        if (_isDarkMode == isDarkMode)
            return;

        _isDarkMode = isDarkMode;
        
        // Save preference
        _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme-preference", isDarkMode ? "dark" : "light");
        
        // Notify listeners
        OnThemeChanged?.Invoke(this, isDarkMode);
    }
}
