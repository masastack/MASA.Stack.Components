namespace Masa.Stack.Components;

public partial class SColorGroup
{
    [EditorRequired]
    [Parameter]
    public List<string> Colors { get; set; } = new();

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public int Size { get; set; } = 24;

    [Parameter]
    public bool SpaceBetween { get; set; } = false;

    [Parameter]
    public RenderFragment<string>? ItemAppendContent { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (Colors is null)
            {
                Colors = new();
            }
            if (Colors.Any())
            {
                await ValueChanged.InvokeAsync(Colors.First());
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnParametersSet()
    {
        if (Colors is null)
        {
            Colors = new();
        }
        base.OnParametersSet();
    }

    private async Task OnClickHandler(ItemContext context, string color)
    {
        await context.Toggle();
        await ValueChanged.InvokeAsync(color);
    }
}
