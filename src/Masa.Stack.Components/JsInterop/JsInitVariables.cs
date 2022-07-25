namespace Masa.Stack.Components;

public static class JsInitVariables
{
    public static TimeSpan TimezoneOffset { get; private set; }

    [JSInvokable]
    public static void SetTimezoneOffset(long offset)
    {
        TimezoneOffset = TimeSpan.FromMinutes(-offset);
    }
}
