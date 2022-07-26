namespace Masa.Stack.Components;

public class SElevationTabItem : ComponentBase, IDisposable
{
    [CascadingParameter]
    private SElevationTab? Tab { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        Tab?.AddTabItem(this);
        base.OnInitialized();
    }

    public void Dispose()
    {
        Tab?.RemoveTabItem(this);
    }
}
