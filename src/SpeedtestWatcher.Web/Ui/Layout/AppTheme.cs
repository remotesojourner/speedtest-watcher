using MudBlazor;

namespace SpeedtestWatcher.Web.Ui.Layout;

public static class AppTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteDark = new PaletteDark()
        {
            AppbarBackground = "#0f1419",
            AppbarText = "#e2e8f0",
            DrawerBackground = "#0f1419",
            Background = "#0b0f14",
            BackgroundGray = "#151b23",
            Surface = "#151b23",
            LinesInputs = "#263238",
            LinesDefault = "#263238",
            ActionDefault = "#94a3b8",
            Primary = "#10b981",
            Secondary = "#06b6d4",
            Success = "#10b981",
            Warning = "#f59e0b",
            Error = "#ef4444",
            Info = "#3b82f6",
            TextPrimary = "#e2e8f0",
            TextSecondary = "#94a3b8"
        },
        PaletteLight = new PaletteLight()
        {
            AppbarBackground = "#ffffff",
            AppbarText = "#1e293b",
            DrawerBackground = "#f8fafc",
            Background = "#f1f5f9",
            Surface = "#ffffff",
            LinesInputs = "#e2e8f0",
            LinesDefault = "#e2e8f0",
            ActionDefault = "#64748b",
            Primary = "#10b981",
            Secondary = "#06b6d4",
            Success = "#10b981",
            Warning = "#f59e0b",
            Error = "#ef4444",
            Info = "#3b82f6",
            TextPrimary = "#1e293b",
            TextSecondary = "#64748b"
        },
        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "10px",
            AppbarHeight = "56px"
        },
        Typography = new Typography()
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif" }
            },
            H4 = new H4Typography { FontWeight = "700" },
            H5 = new H5Typography { FontWeight = "600" },
            H6 = new H6Typography { FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontWeight = "600" },
            Subtitle2 = new Subtitle2Typography { FontWeight = "600" }
        }
    };
}
