using MudBlazor;

namespace Frontend.Theme;

public class ModHubTheme
{
    public static MudTheme DefaultTheme => new MudTheme
    {
        // Removed incompatible Palette property for current MudBlazor version
        
        PaletteDark = new PaletteDark
        {
            // Main Colors
            Primary = "#00aaff",
            PrimaryDarken = "#0088cc",
            PrimaryLighten = "#33bbff",
            Secondary = "#25e398",
            SecondaryDarken = "#1fc587",
            SecondaryLighten = "#4fe8ac",
            Tertiary = "#7d6eff",
            
            // Surface Colors
            Surface = "#232634",
            Background = "#1b1f27",
            AppbarBackground = "#1b1f27",
            DrawerBackground = "#232634",
            
            // Text Colors
            TextPrimary = "#ffffff",
            TextSecondary = "#ffffff",
            DrawerText = "#f5f7fa",
            AppbarText = "#f5f7fa",
            
            // Status Colors
            Error = "#ff3b3b",
            Warning = "#ffc857",
            Success = "#25e398",
            Info = "#00aaff",
            
            // Dark Mode Settings - Always Dark
            Black = "#000000",
            White = "#ffffff",
            DarkDarken = "#141414",
            DarkLighten = "#1e1e1e",
            Dark = "#171717",
            
            // Other Colors
            ActionDefault = "#ffffff",
            ActionDisabled = "#2f3441",
            ActionDisabledBackground = "#2a2f3c",
            DrawerIcon = "#ffffff",
            
            // Hover States
            PrimaryContrastText = "#ffffff",
            SecondaryContrastText = "#ffffff",
            TertiaryContrastText = "#ffffff",
            InfoContrastText = "#ffffff",
            SuccessContrastText = "#ffffff",
            WarningContrastText = "#ffffff",
            ErrorContrastText = "#ffffff",
        },
        
        Shadows = new Shadow(),
        
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerMiniWidthLeft = "60px",
            DrawerWidthLeft = "250px",
            AppbarHeight = "60px"
        },
        
        ZIndex = new ZIndex()
    };
}
