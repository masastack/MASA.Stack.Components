namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionAppItem
{
    private CategoryAppNav? _categoryAppNav;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [CascadingParameter]
    public ExpansionWrapper ExpansionWrapper { get; set; } = null!;

    [CascadingParameter]
    public ExpansionApp ExpansionApp { get; set; } = null!;

    [Parameter, EditorRequired]
    public Nav? Data { get; set; }

    public CategoryAppNav CategoryAppNav
    {
        get
        {
            if (_categoryAppNav is null)
            {
                if (Data!.IsAction)
                {
                    Data.ParentCode = NavCode;
                    _categoryAppNav = new CategoryAppNav(ExpansionApp.CategoryCode, ExpansionApp.App.Code, NavCode, Data.Code, Data);
                }
                else _categoryAppNav = new CategoryAppNav(ExpansionApp.CategoryCode, ExpansionApp.App.Code, Data.Code, default, Data);
            }
            return _categoryAppNav;
        }
    }

    [Parameter]
    public bool Checkable { get; set; }

    [Parameter]
    public bool InPreview { get; set; }

    [Parameter, EditorRequired]
    public int Level { get; set; }

    [Parameter]
    public string? NavCode { get; set; }

    [Parameter]
    public EventCallback ToggleFavorite { get; set; }

    public bool IsChecked
    {
        get
        {           
            var value = ExpansionWrapper.Value.Contains(CategoryAppNav);
            //if(Data!.HasActions)
            //{
            //    if (ExpansionApp.ExpansionAppItems.Any(item => item.Data!.ParentCode == Data.Code && item.IsChecked)) Data.IsDisabled = true;
            //    else Data.IsDisabled = false;
            //}
            return value;
        }
    }

    private bool IsDisabled => Data?.IsDisabled is true || Data is { HasChildren: true } || InPreview;

    private string ActiveClass
    {
        get
        {
            switch (Level)
            {
                case 1:
                    return "neutral-text-hell";
                case 2:
                case 3:
                    return "neutral-text-emphasis";
                default:
                    return string.Empty;
            }
        }
    }

    protected override void OnInitialized()
    {
        if(Data!.HasChildren is false)
        {
            ExpansionApp.Register(this);
        }        
    }

    private string GetClass(bool hover)
    {
        if (Data is not null)
        {
            var css = new string[4];

            css[0] = "clear-before-opacity";

            switch (Level)
            {
                case 1:
                    css[1] = "neutral-text-regular-secondary font-14-bold";
                    css[2] = "nav-item";
                    css[3] = hover ? "font-14-bold neutral-text-hell fill-hover" : "";
                    break;
                case 2:
                    css[1] = "neutral-text-secondary font-14";
                    css[2] = "sub-nav-item";
                    css[3] = hover ? "btn neutral-text-emphasis fill-hover" : "";
                    break;
                default:
                    css[1] = "neutral-text-secondary font-14";
                    css[2] = "action-item";
                    css[3] = hover ? "btn neutral-text-emphasis fill-hover" : "";
                    break;
            }

            return string.Join(" ", css);
        }

        return string.Empty;
    }

    private async Task NavigateTo(string? url)
    {
        if (Checkable || url is null)
        {
            await ExpansionApp.SwitchValue(CategoryAppNav);
            return;
        }      

        NavigationManager.NavigateTo(url, true);
    }
}
