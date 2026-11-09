namespace Masa.Stack.Components.Options;

public sealed class MasaStackComponentOptions
{
    public MasaStackComponentOptions(MasaStackProject project)
    {
        Project = project;
    }

    public string I18nDirectoryPath { get; set; } = "wwwroot/i18n";

    public string? AuthHost { get; set; }

    public string? McHost { get; set; }

    public string? PmHost { get; set; }

    public string? SappHost { get; set; }

    public string? DccHost { get; set; }

    public bool IsMicroFrontend { get; set; } = false;

    public MasaStackProject Project { get; set; }

    public string? ServiceVersion { get; set; }

    public Action<IMasaBlazorBuilder>? MasaBlazorAction { get; set; }

    public Action<MasaBlazorOptions>? MasaBlazorOptionsAction { get; set; }

    /// <summary>
    /// Per-skin packages are no longer supported: <see cref="Configs.SkinManager"/> loads skins from the
    /// <c>Masa.Skins</c> catalog. Call <see cref="UseSkinCatalog"/> instead.
    /// </summary>
    [Obsolete("Per-skin packages are no longer supported. Reference Masa.Skins and call UseSkinCatalog().", error: true)]
    public List<MasaStackSkin> Skins { get; } = new();

    /// <summary>Same as <see cref="SkinCatalogOptions.DefaultSkinId"/>; kept so existing hosts keep compiling.</summary>
    public string? DefaultSkinId
    {
        get => SkinCatalog?.DefaultSkinId ?? _defaultSkinId;
        set
        {
            _defaultSkinId = value;
            if (SkinCatalog is not null) SkinCatalog.DefaultSkinId = value;
        }
    }

    private string? _defaultSkinId;

    /// <summary>Set once <see cref="UseSkinCatalog"/> has been called; <c>null</c> means only the default skin exists.</summary>
    public SkinCatalogOptions? SkinCatalog { get; private set; }

    /// <summary>
    /// Offers the skins of the <c>Masa.Skins</c> package. The host must reference that package (its
    /// <c>skins.json</c> and <c>skins/**</c> are static web assets), except a micro frontend (<see cref="IsMicroFrontend"/>),
    /// which reads the app-shell's copy by default (<see cref="SkinCatalogOptions.CatalogPath"/>), and, to avoid a flash of the default look on
    /// reload, include <c>_content/Masa.Stack.Components/js/skin-boot.js</c> in the page head.
    /// </summary>
    public MasaStackComponentOptions UseSkinCatalog(Action<SkinCatalogOptions>? configure = null)
    {
        SkinCatalog ??= new SkinCatalogOptions { DefaultSkinId = _defaultSkinId };
        configure?.Invoke(SkinCatalog);
        return this;
    }
}
