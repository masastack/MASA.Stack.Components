namespace Masa.Stack.Components.OpenTelemetry.Options;

public class MasaBlazorWasmObservableOptions
{
    public string ProjectName { get; set; } = default!;

    public string ServiceName { get; set; } = default!;

    public string ServiceNameSpace { get; set; } = default!;

    public string? ServiceInstanceId { get; set; }

    public string? ServiceVersion { get; set; }

    public string SsoClientId { get; set; } = default!;

    public DeviceInfoDto? Device { get; set; }
}

public class DeviceInfoDto
{
    public string? Type { get; set; }

    public string? Platform { get; set; }

    public string? Version { get; set; }

    public string? Model { get; set; }

    public string? Manufacturer { get; set; }

    public string? Name { get; set; }

    public string? Idiom { get; set; }
}
