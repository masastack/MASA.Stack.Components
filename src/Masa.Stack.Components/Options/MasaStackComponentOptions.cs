namespace Masa.Stack.Components.Options;

public sealed class MasaStackComponentOptions
{
    public string I18nDirectoryPath { get; set; } = "wwwroot/i18n";

    public string? AuthHost { get; set; }

    public string? McHost { get; set; }

    public string? PmHost { get; set; }

    public string? SappHost { get; set; }

    public string? DccHost { get; set; }

    public bool IsMicroFrontend { get; set; } = false;

    public MasaStackProject Project { get; set; } = default!;

    public string? ServiceVersion { get; set; }

    public Action<IMasaBlazorBuilder>? MasaBlazorAction { get; set; }

    public Action<MasaBlazorOptions>? MasaBlazorOptionsAction { get; set; }
}
