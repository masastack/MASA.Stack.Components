namespace Masa.Stack.Components.OpenTelemetry.Models;

public class AppModuleDto
{
    public string Name { get; set; } = default!;

    public string Code { get; set; } = default!;

    public string Version { get; set; } = default!;

    public string Router { get; set; } = default!;

    public string PmIdentity {  get; set; } = default!;

    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 客户端自己标识处理
    /// </summary>
    public bool IsAuthenticated { get; set; } = false;

    public string Url { get; set; } = string.Empty;
}
