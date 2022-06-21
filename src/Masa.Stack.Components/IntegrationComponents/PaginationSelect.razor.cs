namespace Masa.Stack.Components;

public partial class PaginationSelect
{
    [Parameter]
    public int Value { get; set; }

    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    [Parameter]
    public List<int> Items { get; set; } = new();

    private string Icon { get; set; } = "mdi-menu-down";

    public async Task SelectAsync(int value)
    {
        Value = value;
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }
        Icon = "mdi-menu-down";
    }

    public void ChangeIconState(bool show)
    {
        if (show) Icon = "mdi-menu-up";
        else Icon = "mdi-menu-down";
    }
}

