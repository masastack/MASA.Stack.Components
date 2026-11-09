namespace Masa.Stack.Components.Options;

/// <summary>Options for the <c>Masa.Skins</c> catalog. See <see cref="MasaStackComponentOptions.UseSkinCatalog"/>.</summary>
public sealed class SkinCatalogOptions
{
    /// <summary>The catalog of a host that runs on its own: the <c>Masa.Skins</c> package it references, relative to the asset root.</summary>
    public const string HostCatalogPath = "_content/Masa.Skins/skins.json";

    /// <summary>
    /// The catalog of a micro frontend: the app-shell serves a copy at the origin root, so the micro frontend does not
    /// have to ship the package. A path that starts with <c>/</c> is resolved against the origin root.
    /// </summary>
    public const string ShellCatalogPath = "/_content/Masa.Skins/skins.json";

    /// <summary>
    /// Where the catalog is served. <c>null</c> (the default) follows the mode of the host: a micro frontend
    /// (<see cref="MasaStackComponentOptions.IsMicroFrontend"/>) reads the app-shell's copy (<see cref="ShellCatalogPath"/>) and
    /// its build can leave the <c>Masa.Skins</c> package out; a host that runs on its own reads the package it references
    /// (<see cref="HostCatalogPath"/>). Set a path to override, for example <see cref="HostCatalogPath"/> for a micro frontend
    /// that ships its own copy.
    /// </summary>
    public string? CatalogPath { get; set; }

    internal string ResolveCatalogPath(bool isMicroFrontend) =>
        CatalogPath ?? (isMicroFrontend ? ShellCatalogPath : HostCatalogPath);

    /// <summary>
    /// The skin used until the user picks one. <c>null</c>, empty or the legacy value <c>"default"</c> mean the
    /// catalog's default (<see cref="MasaStackSkin.DefaultId"/>).
    /// </summary>
    public string? DefaultSkinId { get; set; }

    /// <summary>
    /// Whether the optional component layer of a skin (its component-level tweaks: font stacks, link colour, picker contrast, ...)
    /// is on until the user or host code decides otherwise (<see cref="Configs.SkinManager.SetComponentLayerAsync"/>; the choice is
    /// remembered and wins over this value). On by default; set <c>false</c> to start without it. The layer is requested only for
    /// the active skin and only if that skin has one, so the default skin loads nothing extra.
    /// </summary>
    public bool ComponentLayer { get; set; } = true;

    /// <summary>Limits the skins offered to the user. <c>null</c> offers the whole catalog; the default skin is always offered.</summary>
    public ISet<string>? Include { get; set; }
}
