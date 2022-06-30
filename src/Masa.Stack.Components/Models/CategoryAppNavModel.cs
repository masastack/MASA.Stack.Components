namespace Masa.Stack.Components.Models;

internal class CategoryAppNavModel
{
    public string? CategoryCode { get; set; }

    public string? AppCode { get; set; }

    public Nav Nav { get; set; } = new();
}
