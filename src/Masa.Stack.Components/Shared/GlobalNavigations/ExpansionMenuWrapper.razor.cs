namespace Masa.Stack.Components.Shared.GlobalNavigations;

public partial class ExpansionMenuWrapper : MasaComponentBase
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter]
    public ExpansionMenu? Value { get; set; }

    [Parameter]
    public bool RenderLayer { get; set; }

    [Parameter]
    public EventCallback<ExpansionMenu> OnItemClick { get; set; }

    [Parameter]
    public EventCallback<ExpansionMenu> OnItemOperClick { get; set; }

    [Parameter]
    public string? CssForScroll { get; set; }

    private readonly string idPrefix = "g" + Guid.NewGuid().ToString();

    private string CssSelectorForScroll => string.IsNullOrWhiteSpace(CssForScroll) ? string.Empty : "." + CssForScroll;

    private bool _shouldUpdateMasonry;
    private int renderedChildrenCount = 0;
    private int totalChildrenCount = 0;
    private string errorMessage = "";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        //if (Value?.Children.Any(x => !x.Hidden) == true && _shouldUpdateMasonry)
        //{
        //    _shouldUpdateMasonry = false;

        //    await InitOrUpdateMasonryAsync();
        //}

        //if (Value?.Children.Any(x => !x.Hidden) == true)
        //{
        //    await InitOrUpdateMasonryAsync();
        //}

        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("MasaStackComponents.mutationObserverMasonry", ".masonry-container", ".category_title_app", ".app-show", 24);
        }
    }

    //protected override async Task OnParametersSetAsync()
    //{
    //    _shouldUpdateMasonry = true;
    //    await base.OnParametersSetAsync();
    //}

    private async Task OnChildRendered()
    {
        //renderedChildrenCount++;
        //Console.WriteLine($"{totalChildrenCount}:{renderedChildrenCount}");
        //if (renderedChildrenCount >= totalChildrenCount)
        //{
        //    renderedChildrenCount = 0;
        //    await InitOrUpdateMasonryAsync();
        //}
    }

    protected virtual async Task InitOrUpdateMasonryAsync()
    {
        if (Value is not null)
        {
            //foreach (var category in Value.Children.Where(x => !x.Hidden))
            //{
            //    await JsRuntime.InvokeVoidAsync("MasaStackComponents.initOrUpdateMasonry", $".{idPrefix}_{category.Id}", ".app-show", 24);
            //}
            await JsRuntime.InvokeVoidAsync("MasaStackComponents.mutationObserverMasonry", ".masonry-container", ".category_title_app", ".app-show", 24);
        }
    }

    protected virtual async Task ItemClick(ExpansionMenu menu)
    {
        if (Value.MetaData.Situation == ExpansionMenuSituation.Authorization)
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
