namespace Masa.Stack.Components.Options;

public class McServiceOptions
{
    private Func<string> _baseAddress;

    public string BaseAddress
    {
        get
        {
            return _baseAddress.Invoke();
        }
    }

    public McServiceOptions(Func<string> baseAddress)
    {
        _baseAddress = baseAddress;
    }
}
