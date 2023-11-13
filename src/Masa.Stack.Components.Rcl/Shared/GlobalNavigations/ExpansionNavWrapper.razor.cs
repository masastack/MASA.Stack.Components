using Masa.Stack.Components.Rcl.Shared.GlobalNavigations;

namespace Masa.Stack.Components.Shared.GlobalNavigations;

public partial class ExpansionNavWrapper : MasaComponentBase
{
    [Parameter]
    public ExpansionMenu Value { get; set; } = null!;

    [Parameter] 
    public EventCallback<ExpansionMenu> OnItemClick { get; set; }

    [Parameter] 
    public EventCallback<ExpansionMenu> OnItemOperClick { get; set; }

    private string GetClass(bool hover)
    {
        var css = new string[4];
        css[0] = "clear-before-opacity";

        switch (Value.GetNavDeep())
        {
            case 0:
                css[1] = "neutral-text-regular-secondary font-14-bold";
                css[2] = "nav-item";
                css[3] = hover ? "font-14-bold neutral-text-hell fill-hover" : "";
                break;
            case 1:
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

    private async Task ItemClick()
    {
        if (OnItemClick.HasDelegate)
        {
            await OnItemClick.InvokeAsync(Value);
        }
    }

    private async Task ItemOperClick()
    {
        if (OnItemOperClick.HasDelegate)
        {
            await OnItemOperClick.InvokeAsync(Value);
        }
    }
}
