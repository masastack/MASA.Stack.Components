namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionAppWrapper
{
    [Parameter]
    public ExpansionMenu Value { get; set; } = default!;
    
    [Parameter] 
    public EventCallback<ExpansionMenu> OnItemClick { get; set; }

    [Parameter] 
    public EventCallback<ExpansionMenu> OnItemOperClick { get; set; }

    private Task AppCheckedChanged(bool v)
    {
        return Task.CompletedTask;
        // if (AppChecked) await UpdateValues(new List<CategoryAppNav>());
        // else await UpdateValues(CategoryAppNavs);
    }
}
