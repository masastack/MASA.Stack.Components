namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionAppItem
{
    private CategoryAppNav? _categoryAppNav;

    [CascadingParameter]
    public ExpansionApp ExpansionApp { get; set; } = null!;

    ExpansionWrapper ExpansionWrapper => ExpansionApp.ExpansionWrapper;

    [Parameter, EditorRequired]
    public Nav Data { get; set; } = default!;

    public CategoryAppNav CategoryAppNav
    {
        get
        {
            if (_categoryAppNav is null)
            {
                if (Data.IsAction)
                {
                    Data.ParentCode = NavCode;
                    _categoryAppNav = new CategoryAppNav(ExpansionApp.CategoryCode, ExpansionApp.App.Code, NavCode, Data.Code, Data);
                }
                else _categoryAppNav = new CategoryAppNav(ExpansionApp.CategoryCode, ExpansionApp.App.Code, Data.Code, default, Data);
            }
            return _categoryAppNav;
        }
    }

    public bool Checkable => ExpansionWrapper.Checkable;

    public bool InPreview => ExpansionWrapper.InPreview;

    public bool Favorite => ExpansionWrapper.Favorite;

    [Parameter, EditorRequired]
    public int Level { get; set; }

    [Parameter]
    public string? NavCode { get; set; }

    public bool IsChecked
    {
        get
        {
            var value = ExpansionWrapper.Value.Contains(CategoryAppNav);
            return value;
        }
    }

    private bool IsDisabled => InPreview || Data.Disabled || Data.HasChildren;

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
        if (Data.HasChildren is false)
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

    private async Task SelectItem()
    {
        if (Checkable)
        {
            await ExpansionApp.SwitchValue(CategoryAppNav);
        }
        if(Favorite && Data.Url is not null)
        {
            NavigationManager.NavigateTo(Data.Url, true);
        }
    }

    private async Task AddFavorite()
    {
        await ExpansionApp.SwitchValue(CategoryAppNav);
    }
}
