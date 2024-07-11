namespace Masa.Stack.Components.Shared.GlobalNavigations;

public partial class ExpansionAppWrapper
{
    [Parameter]
    public ExpansionMenu Value { get; set; } = default!;

    [Parameter]
    public EventCallback<ExpansionMenu> OnItemClick { get; set; }

    [Parameter]
    public EventCallback<ExpansionMenu> OnItemOperClick { get; set; }

    [Inject]
    public GlobalConfig GlobalConfig { get; set; } = null!;

    [Inject]
    public ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var result = await ProtectedLocalStore.GetAsync<int>(GlobalConfig.NavLayerStoreKey);
        if (result.Success)
        {
            GlobalConfig.NavLayer = result.Value;
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            GlobalConfig.OnNavLayerChanged += Changed;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task ItemOperClick()
    {
        await OnItemOperClick.InvokeAsync(Value);
    }
    
    private static string GetClass(ExpansionMenu menu)
    {
        var css = new string[3];
        css[0] = "clear-before-opacity";

        switch (menu.GetNavDeep())
        {
            case 0:
                css[1] = "neutral-text-regular-secondary font-14-bold";
                css[2] = "nav-item";
                break;
            case 1:
                css[1] = "neutral-text-secondary font-14";
                css[2] = "sub-nav-item";
                break;
            case 2:
                css[1] = "neutral-text-secondary font-14";
                css[2] = "action-item";
                break;
            default:
                css[1] = "neutral-text-secondary font-14";
                css[2] = "action-item2";
                break;
        }

        return string.Join(" ", css);
    }

    async Task Changed()
    {
        await InvokeAsync(StateHasChanged);
        await ProtectedLocalStore.SetAsync(GlobalConfig.NavLayerStoreKey, GlobalConfig.NavLayer);
    }
}
