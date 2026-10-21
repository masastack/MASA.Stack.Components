namespace Masa.Stack.Components;

internal static class DenseSize
{
    /// <summary>
    /// Height and css class of a dense input. Precedence: Large, Medium, Small; Medium when none is set.
    /// </summary>
    public static (int Height, string CssClass) Resolve(bool large, bool medium, bool small)
    {
        if (large) return (56, "m-input--dense-56");

        return medium || !small ? (48, "m-input--dense-48") : (40, "m-input--dense-40");
    }
}
