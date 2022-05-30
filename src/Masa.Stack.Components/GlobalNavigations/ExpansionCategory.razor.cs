using Masa.Stack.Components.JsInterop;
using Microsoft.JSInterop;

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

    private bool _initCheckbox;
    private bool _fromCheckbox;
    private Dictionary<string, List<CategoryAppNav>> _valuesDic = new();

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

    private async Task CategoryCheckedValueChanged(bool v)
    {
        _fromCheckbox = true;

        CategoryChecked = v;

        var key = $"category_{Category.Code}";

        var categoryAppNavs = new List<CategoryAppNav>();

        if (CategoryChecked)
        {
            categoryAppNavs.Add(new CategoryAppNav(Category.Code));
        }

        await ExpansionWrapper!.UpdateValues(key, categoryAppNavs);

        foreach (var app in Category.Apps)
        {
            var appKey = $"{key}_app_{app.Code}";

            categoryAppNavs = new List<CategoryAppNav>();

            if (!CheckStrictly && CategoryChecked)
            {
                categoryAppNavs.Add(new CategoryAppNav(Category.Code, app.Code));

                categoryAppNavs.AddRange(FlattenNavs(app.Navs, true).Select(nav => nav.IsAction
                    ? new CategoryAppNav(Category.Code, app.Code, nav.ParentCode, nav.Code)
                    : new CategoryAppNav(Category.Code, app.Code, nav.Code)));
            }

            await ExpansionWrapper!.UpdateValues(appKey, categoryAppNavs);
        }
    }

    internal async Task UpdateValues(string key, List<CategoryAppNav> value)
    {
        if (_valuesDic.TryGetValue(key, out _))
        {
            _valuesDic[key] = value;
        }
        else
        {
            _valuesDic.Add(key, value);
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

    private List<Nav> FlattenNavs(List<Nav> tree, bool excludeNavHasChildren = false)
    {
        var res = new List<Nav>();

        foreach (var nav in tree)
        {
            if (!(nav.HasChildren && excludeNavHasChildren))
            {
                res.Add(nav);
            }

            if (nav.HasChildren)
            {
                res.AddRange(FlattenNavs(nav.Children, excludeNavHasChildren));
            }

            if (nav.HasActions)
            {
                nav.Actions.ForEach(a => a.ParentCode ??= nav.Code);
                res.AddRange(nav.Actions);
            }
        }

        return res;
    }
}
