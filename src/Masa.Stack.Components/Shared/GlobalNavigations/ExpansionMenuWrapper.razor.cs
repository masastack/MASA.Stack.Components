namespace Masa.Stack.Components.Shared.GlobalNavigations;

public partial class ExpansionMenuWrapper : MasaComponentBase
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;
    
    [Parameter]
    public ExpansionMenu? Value { get; set; }

    [Parameter] 
    public EventCallback<ExpansionMenu> OnItemClick { get; set; }

    [Parameter] 
    public EventCallback<ExpansionMenu> OnItemOperClick { get; set; }
    
    [Parameter]
    public string SideStyle { get; set; } = "";

    [Parameter]
    public string SideClass { get; set; } = "";

    protected virtual async Task ItemClick(ExpansionMenu menu)
    {
        if (Value.Metadata.Situation == ExpansionMenuSituation.Authorization)
        {
            await menu.ChangeStateAsync();
        }

        if (OnItemClick.HasDelegate)
        {
            await OnItemClick.InvokeAsync(menu);
        }
    }

    protected virtual async Task ItemOperClick(ExpansionMenu menu)
    {
        await menu.ChangeStateAsync();

        if (OnItemOperClick.HasDelegate)
        {
            await OnItemOperClick.InvokeAsync(menu);
        }
    }

    private async Task ScrollTo(string tagId, string insideSelector)
    {
        await JsRuntime.InvokeVoidAsync("MasaStackComponents.scrollTo", $"#{tagId}", insideSelector);
    }
}
