namespace Masa.Stack.Components.Options;

public class McServiceOptions
{
    public string BaseAddress { get; private set; }

    public McServiceOptions(string baseAddress)
    {
        if (baseAddress.EndsWith('/'))
            baseAddress = baseAddress.TrimEnd('/');
        BaseAddress = baseAddress;
    }
}
