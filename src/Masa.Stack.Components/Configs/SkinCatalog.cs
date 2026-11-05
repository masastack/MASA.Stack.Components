namespace Masa.Stack.Components.Configs;

/// <summary>The parsed <c>skins.json</c> of the <c>Masa.Stack.Skins</c> package (schema 1).</summary>
internal sealed class SkinCatalog
{
    public const int SupportedSchema = 1;

    [JsonPropertyName("schema")]
    public int Schema { get; set; }

    [JsonPropertyName("default")]
    public string Default { get; set; } = MasaStackSkin.DefaultId;

    /// <summary>The shared geometry stylesheet, relative to the asset root of the catalog package.</summary>
    [JsonPropertyName("base")]
    public string? Base { get; set; }

    [JsonPropertyName("skins")]
    public List<SkinCatalogEntry> Skins { get; set; } = new();

    public static SkinCatalog Parse(string json)
    {
        var catalog = JsonSerializer.Deserialize(json, SkinCatalogJsonContext.Default.SkinCatalog)
                      ?? throw new InvalidOperationException("skins.json is empty.");
        if (catalog.Schema != SupportedSchema)
        {
            throw new InvalidOperationException(
                $"skins.json has schema {catalog.Schema}, this version of Masa.Stack.Components supports {SupportedSchema}.");
        }

        if (catalog.Skins.All(s => s.Id != catalog.Default))
        {
            throw new InvalidOperationException($"skins.json: default skin '{catalog.Default}' is not in the catalog.");
        }

        return catalog;
    }
}

internal sealed class SkinCatalogEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("profile")]
    public string Profile { get; set; } = "themed";

    /// <summary>The skin stylesheet, relative to the asset root of the catalog package.</summary>
    [JsonPropertyName("css")]
    public string Css { get; set; } = "";

    /// <summary>The optional component layer of the skin (<c>components.css</c>); <c>null</c> when the skin has none.</summary>
    [JsonPropertyName("components")]
    public SkinCatalogComponents? Components { get; set; }

    [JsonPropertyName("themes")]
    public Dictionary<string, SkinCatalogTheme> Themes { get; set; } = new();
}

/// <summary>Where the optional component layer of a skin lives, relative to the asset root of the catalog package.</summary>
internal sealed class SkinCatalogComponents
{
    [JsonPropertyName("css")]
    public string Css { get; set; } = "";

    [JsonPropertyName("bytes")]
    public int Bytes { get; set; }
}

internal sealed class SkinCatalogTheme
{
    /// <summary>The 27 MASA role colours as <c>#RRGGBB</c>, keyed by CSS role name (<c>surface-container-low</c>).</summary>
    [JsonPropertyName("roles")]
    public Dictionary<string, string> Roles { get; set; } = new();

    /// <summary>The ten MASA opacity variables, keyed by CSS name without prefix (<c>hover-opacity</c>).</summary>
    [JsonPropertyName("opacities")]
    public Dictionary<string, double> Opacities { get; set; } = new();
}

[JsonSerializable(typeof(SkinCatalog))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
internal sealed partial class SkinCatalogJsonContext : JsonSerializerContext
{
}

/// <summary>Copies a catalog theme into a MASA <see cref="ThemeOptions"/>. Everything that is not in the catalog is left alone.</summary>
internal static class SkinThemeMapper
{
    private static readonly Dictionary<string, Action<ThemeOptions, string>> Roles = new()
    {
        ["primary"] = (t, v) => t.Primary = v,
        ["on-primary"] = (t, v) => t.OnPrimary = v,
        ["secondary"] = (t, v) => t.Secondary = v,
        ["on-secondary"] = (t, v) => t.OnSecondary = v,
        ["accent"] = (t, v) => t.Accent = v,
        ["on-accent"] = (t, v) => t.OnAccent = v,
        ["error"] = (t, v) => t.Error = v,
        ["on-error"] = (t, v) => t.OnError = v,
        ["info"] = (t, v) => t.Info = v,
        ["on-info"] = (t, v) => t.OnInfo = v,
        ["success"] = (t, v) => t.Success = v,
        ["on-success"] = (t, v) => t.OnSuccess = v,
        ["warning"] = (t, v) => t.Warning = v,
        ["on-warning"] = (t, v) => t.OnWarning = v,
        ["surface-dim"] = (t, v) => t.SurfaceDim = v,
        ["surface"] = (t, v) => t.Surface = v,
        ["surface-bright"] = (t, v) => t.SurfaceBright = v,
        ["surface-container-lowest"] = (t, v) => t.SurfaceContainerLowest = v,
        ["surface-container-low"] = (t, v) => t.SurfaceContainerLow = v,
        ["surface-container"] = (t, v) => t.SurfaceContainer = v,
        ["surface-container-high"] = (t, v) => t.SurfaceContainerHigh = v,
        ["surface-container-highest"] = (t, v) => t.SurfaceContainerHighest = v,
        ["on-surface"] = (t, v) => t.OnSurface = v,
        ["inverse-surface"] = (t, v) => t.InverseSurface = v,
        ["inverse-on-surface"] = (t, v) => t.InverseOnSurface = v,
        ["inverse-primary"] = (t, v) => t.InversePrimary = v,
        // "stripe" is not mapped: ThemeOptions only has it from MASA Blazor releases after 1.12.0-alpha.2. Where MASA
        // reads --m-theme-stripe, the catalog's static mirror (tokens.css) already supplies the skin's value.
    };

    private static readonly Dictionary<string, Action<ThemeVariables, float>> Opacities = new()
    {
        ["border-opacity"] = (v, x) => v.BorderOpacity = x,
        ["idle-opacity"] = (v, x) => v.IdleOpacity = x,
        ["hover-opacity"] = (v, x) => v.HoverOpacity = x,
        ["focus-opacity"] = (v, x) => v.FocusOpacity = x,
        ["disabled-opacity"] = (v, x) => v.DisabledOpacity = x,
        ["activated-opacity"] = (v, x) => v.ActivatedOpacity = x,
        ["highlight-opacity"] = (v, x) => v.HighlightOpacity = x,
        ["high-emphasis-opacity"] = (v, x) => v.HighEmphasisOpacity = x,
        ["medium-emphasis-opacity"] = (v, x) => v.MediumEmphasisOpacity = x,
        ["low-emphasis-opacity"] = (v, x) => v.LowEmphasisOpacity = x,
    };

    public static void Apply(ThemeOptions target, SkinCatalogTheme source)
    {
        foreach (var (name, value) in source.Roles)
        {
            // Unknown names are ignored on purpose: a newer catalog may add roles an older MASA Blazor does not know.
            if (Roles.TryGetValue(name, out var set)) set(target, value);
        }

        foreach (var (name, value) in source.Opacities)
        {
            if (Opacities.TryGetValue(name, out var set)) set(target.Variables, (float)value);
        }
    }
}
