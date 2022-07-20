namespace Masa.Stack.Components.Models;

public class UniqueModel
{
    public string Code { get; set; }

    public bool IsDisabled { get; set; }

    public UniqueModel(string key, bool? isDisabled = null)
    {
        Code = key;
        IsDisabled = isDisabled ?? false;
    }

    public override bool Equals(object? obj)
    {
        return obj is UniqueModel unique && unique.Code == Code;
    }

    public override int GetHashCode()
    {
        return 1;
    }
}
