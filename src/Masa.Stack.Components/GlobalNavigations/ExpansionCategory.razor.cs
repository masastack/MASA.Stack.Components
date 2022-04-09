using Microsoft.JSInterop;

namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionCategory
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [CascadingParameter]
    public ExpansionWrapper? ExpansionWrapper { get; set; }

    [Parameter, EditorRequired]
    public Category Category { get; set; } = null!;

    [Parameter]
    public bool Checkable { get; set; }

    [Parameter]
    public RenderFragment<App>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> Attributes { get; set; } = new();

    private bool _initCheckbox;
    private bool _fromCheckbox;

    private bool _hasResizeNav;
    private bool _refreshWaterFull;

    private bool CategoryChecked { get; set; }

    protected override void OnParametersSet()
    {
        if (Checkable)
        {
            if (ExpansionWrapper is not null && ExpansionWrapper.Value.Any() && (!_initCheckbox || !_fromCheckbox))
            {
                _initCheckbox = true;

                var exists = ExpansionWrapper.Value.Any(v => v.Category == Category.Code && v.App is null && v.Nav is null);

                CategoryChecked = exists;
            }

            if (_fromCheckbox)
            {
                _fromCheckbox = false;
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_hasResizeNav || _refreshWaterFull)
        {
            _hasResizeNav = true;
            _refreshWaterFull = false;

            // TODO: How to wait for elements to be rendered
            await Task.Delay(256);

            await ResizeNav(Category);

            StateHasChanged();
        }
    }

    private void ValuesChanged(List<StringNumber> values)
    {
        Category.BindValues = values;

        _refreshWaterFull = true;
    }

    private async Task CategoryCheckedValueChanged(bool v)
    {
        _fromCheckbox = true;

        CategoryChecked = v;

        CategoryAppNav categoryAppNav = new(Category.Code);

        var key = $"category_{Category.Code}";
        var values = new List<CategoryAppNav>();

        if (CategoryChecked)
        {
            values.Add(categoryAppNav);
            await ExpansionWrapper!.UpdateValues(key, values);
        }
        else
        {
            await ExpansionWrapper!.UpdateValues(key, values);
        }
    }

    private async Task ResizeNav(Category category)
    {
        var height = await JsRuntime.InvokeAsync<double>(
            "MasaStackComponents.waterFull",
            $"#{category.TagId()} .apps",
            ".app");

        category.TagStyle = $"position:relative; height:{height}px;";
    }
}