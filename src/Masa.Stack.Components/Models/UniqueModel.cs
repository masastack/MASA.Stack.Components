namespace Masa.Stack.Components.Models;

public class UniqueModel
{
    public string Code { get; set; }

    public bool IsDisabled { get; set; }

    public bool IsClose { get; set; }

    public bool IsChecked { get; set; }

    public UniqueModel(string key, bool? isDisabled = null, bool? isClose = null, bool? isChecked = null)
    {
        Code = key;
        IsDisabled = isDisabled ?? false;
        IsClose = isClose ?? false;
        IsChecked = isChecked ?? true;
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
