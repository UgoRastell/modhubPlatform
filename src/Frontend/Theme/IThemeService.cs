using System;

namespace Frontend.Theme;

/// <summary>
/// Interface for theme service that manages dark/light mode
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets whether dark mode is currently active
    /// </summary>
    bool IsDarkMode { get; }
    
    /// <summary>
    /// Sets the dark mode state
    /// </summary>
    void SetDarkMode(bool isDarkMode);
    
    /// <summary>
    /// Event triggered when theme mode changes
    /// </summary>
    event EventHandler<bool> OnThemeChanged;
}
