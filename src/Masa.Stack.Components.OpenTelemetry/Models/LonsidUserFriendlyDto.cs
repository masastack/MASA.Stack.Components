namespace Masa.Stack.Components.OpenTelemetry.Models;

internal class LonsidUserFriendlyDto
{
    public string TargetUrl { get; set; } = string.Empty;

    public bool Success { get; set; } = false;

    public LonsidResponseErrorDto Error { get; set; } = default!;
}

internal class LonsidResponseErrorDto
{
    public int Code { get; set; }

    public string Message { get; set; } = string.Empty;
}