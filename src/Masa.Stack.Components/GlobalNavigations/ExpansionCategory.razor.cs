using Masa.Stack.Components.JsInterop;

namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionCategory
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private JsDotNetInvoker JsDotNetInvoker { get; set; } = null!;

    [CascadingParameter]
    public ExpansionWrapper? ExpansionWrapper { get; set; }

    [Parameter, EditorRequired]
    public Category Category { get; set; } = null!;

    [Parameter]
    public bool Checkable { get; set; }

    [Parameter]
    public bool CheckStrictly { get; set; }

    [Parameter]
    public bool InPreview { get; set; }

    [Parameter]
    public RenderFragment<App>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> Attributes { get; set; } = new();

    public List<ExpansionApp> ExpansionApps { get; set; } = new();

    private bool CategoryChecked => ExpansionApps.All(expansionApp => expansionApp.AppChecked is true);

    private async Task CategoryCheckedValueChanged(bool v)
    {
        var isChecked = !CategoryChecked;
        foreach (var expansionApp in ExpansionApps)
        {
            await expansionApp.CheckedAllNavs(isChecked);
        }
    }

    public void Register(ExpansionApp expansionApp)
    {
        ExpansionApps.Add(expansionApp);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsDotNetInvoker.ResizeObserver(
                $"#{Category.TagId(ExpansionWrapper?.TagIdPrefix)}",
                async () => { await ResizeNav(Category); }
            );

            foreach (var app in Category.Apps)
            {
                await JsDotNetInvoker.ResizeObserver(
                    $"#{app.TagId(Category.Code, ExpansionWrapper?.TagIdPrefix)}",
                    async () => { await ResizeNav(Category); }
                );
            }
        }
    }

    private async Task ResizeNav(Category category)
    {
        var height = await JsRuntime.InvokeAsync<double>(
            "MasaStackComponents.waterFull",
            $"#{category.TagId(ExpansionWrapper?.TagIdPrefix)} .apps",
            ".app");

        category.TagStyle = $"position:relative; height:{height}px;";

        StateHasChanged();
    }
}
