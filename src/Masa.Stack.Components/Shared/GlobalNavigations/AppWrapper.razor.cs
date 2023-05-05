namespace Masa.Stack.Components.GlobalNavigations;

public partial class AppWrapper
{
    [Parameter]
    public Menu Value { get; set; } = default!;

    private Task AppCheckedChanged(bool v)
    {
        return Task.CompletedTask;
        // if (AppChecked) await UpdateValues(new List<CategoryAppNav>());
        // else await UpdateValues(CategoryAppNavs);
    }
}
