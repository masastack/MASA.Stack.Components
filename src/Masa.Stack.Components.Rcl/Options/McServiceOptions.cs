namespace Masa.Stack.Components.Rcl.Options;

public class McServiceOptions
{
    private string _baseAddress;

    public string BaseAddress => _baseAddress;

    public McServiceOptions(string baseAddress)
    {
        _baseAddress = baseAddress;
    }
}
