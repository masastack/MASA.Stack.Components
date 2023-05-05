namespace Masa.Stack.Components.GlobalNavigations;

public partial class NavWrapper : MasaComponentBase
{
    [Parameter]
    public Menu Value { get; set; } = null!;

    private string GetClass(bool hover)
    {
        var css = new string[4];
        css[0] = "clear-before-opacity";

        switch (Value.Deep)
        {
            case 1:
                css[1] = "neutral-text-regular-secondary font-14-bold";
                css[2] = "nav-item";
                css[3] = hover ? "font-14-bold neutral-text-hell fill-hover" : "";
                break;
            case 2:
                css[1] = "neutral-text-secondary font-14";
                css[2] = "sub-nav-item";
                css[3] = hover ? "btn neutral-text-emphasis fill-hover" : "";
                break;
            default:
                css[1] = "neutral-text-secondary font-14";
                css[2] = "action-item";
                css[3] = hover ? "btn neutral-text-emphasis fill-hover" : "";
                break;
        }

        return string.Join(" ", css);
    }

    private string GetActiveClass()
    {
        switch (Value.GetNavDeep())
        {
            case 1:
                return "neutral-text-hell";
            case 2:
            case 3:
                return "neutral-text-emphasis";
            default:
                return string.Empty;
        }
    }

    private Task SelectItem()
    {
        return Task.CompletedTask;
        // if (Checkable)
        // {
        //     await ExpansionApp.SwitchValue(CategoryAppNav, IsQueryNav, isIndeterminate: Indeterminate);
        // }
        // if (Favorite && Data.Url is not null && !Data.HasChildren)
        // {
        //     NavigationManager.NavigateTo(Data.Url, true);
        // }
    }

    private Task AddFavorite()
    {
        // await ExpansionApp.SwitchValue(CategoryAppNav);
        return Task.CompletedTask;
    }
}
