namespace Masa.Stack.Components.Extensions;

public static class StyleExtensions
{
    /// <summary>
    /// Prepends a default <c>border-radius</c> to an inline style unless the style already sets one.
    /// </summary>
    public static string WithDefaultBorderRadius(this string? style, int radius = 8)
    {
        return style?.Contains("border-radius") is true ? style : $"border-radius:{radius}px;{style}";
    }
}
