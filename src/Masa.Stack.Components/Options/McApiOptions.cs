namespace Masa.Stack.Components.Options;

public class McApiOptions
{
    public string McServiceBaseAddress { get; set; }

    public McApiOptions(string mcServiceBaseAddress)
    {
        McServiceBaseAddress = mcServiceBaseAddress;
    }
}
