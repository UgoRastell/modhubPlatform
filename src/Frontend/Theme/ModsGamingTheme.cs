using MudBlazor;
using MudBlazor.Utilities;

namespace Frontend.Theme;

/// <summary>
/// Thème personnalisé pour la plateforme Mods Gaming
/// Conforme à la charte graphique définie
/// </summary>
public static class ModsGamingTheme
{
    /// <summary>
    /// Obtient le thème MudBlazor personnalisé pour ModsGaming
    /// </summary>
    public static MudTheme GetTheme()
    {
        // Dans les versions récentes de MudBlazor, nous initialisons directement un MudTheme avec toutes les propriétés
        return new MudTheme
        {
            PaletteDark = new PaletteDark()
            {
                // Couleurs principales
                Background = "#1b1f27",
                Surface = "#232634",
                Primary = "#00aaff",
                Secondary = "#25e398",
                
                // Texte
                TextPrimary = "#f5f7fa",
                TextSecondary = "#a7b2c2",
                
                // Autres éléments
                Divider = "#2f3441",
                Error = "#ff3b3b",
                Warning = "#ffc857",
                Success = "#25e398",
                Info = "#00aaff",
                
                // Surcharges spécifiques
                AppbarBackground = "#232634",
                DrawerBackground = "#1b1f27",
                DrawerText = "#f5f7fa",
                ActionDefault = "#a7b2c2",
                ActionDisabled = "rgba(167, 178, 194, 0.4)",
                ActionDisabledBackground = "rgba(27, 31, 39, 0.38)",
                Dark = "#1b1f27"
            },
            
            // PaletteLight définit la même palette sombre (le site utilise un thème sombre par défaut)
            PaletteLight = new PaletteLight
            {
                // Couleurs principales
                Background = "#1b1f27",
                Surface = "#232634",
                Primary = "#00aaff",
                Secondary = "#25e398",
                
                // Texte
                TextPrimary = "#f5f7fa",
                TextSecondary = "#a7b2c2",
                
                // Autres éléments
                Divider = "#2f3441",
                Error = "#ff3b3b",
                Warning = "#ffc857",
                Success = "#25e398",
                Info = "#00aaff",
                
                // Surcharges spécifiques
                AppbarBackground = "#232634",
                DrawerBackground = "#1b1f27",
                DrawerText = "#f5f7fa",
                ActionDefault = "#a7b2c2",
                ActionDisabled = "rgba(167, 178, 194, 0.4)",
                ActionDisabledBackground = "rgba(27, 31, 39, 0.38)",
                Dark = "#1b1f27"
            },
            
            // Surcharges de shadow pour avoir les effets "glow" de la charte
            Shadows = new Shadow
            {
                Elevation = new string[]
                {
                    "none",
                    "0 2px 4px 0 rgba(0, 170, 255, 0.05)",
                    "0 4px 8px 0 rgba(0, 170, 255, 0.1)",
                    "0 6px 12px 0 rgba(0, 170, 255, 0.15)",
                    "0 8px 16px 0 rgba(0, 170, 255, 0.2)",
                    "0 12px 24px 0 rgba(0, 170, 255, 0.25)",
                }
            },
            
            // Surcharges des paramètres de design du système
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "8px",
                DrawerWidthLeft = "260px",
                DrawerWidthRight = "300px",
                AppbarHeight = "64px"
            }
        };
    }
}
