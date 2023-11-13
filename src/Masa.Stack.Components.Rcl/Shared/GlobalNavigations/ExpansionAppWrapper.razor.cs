using Masa.Stack.Components.Rcl.Shared.GlobalNavigations;

namespace Masa.Stack.Components.Shared.GlobalNavigations;

public partial class ExpansionAppWrapper
{
    [Parameter]
    public ExpansionMenu Value { get; set; } = default!;
    
    [Parameter] 
    public EventCallback<ExpansionMenu> OnItemClick { get; set; }

    [Parameter] 
    public EventCallback<ExpansionMenu> OnItemOperClick { get; set; }
}
