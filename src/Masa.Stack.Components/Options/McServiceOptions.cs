namespace Masa.Stack.Components.Options;

public class McServiceOptions
{
    public string BaseAddress { get; set; }

    public McServiceOptions(string baseAddress)
    {
        BaseAddress = baseAddress;
    }
}
