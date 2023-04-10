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
                Data.ParentCode = ParentCode;
                if (Data.IsAction)
                {
                    _categoryAppNav = new CategoryAppNav(ExpansionApp.CategoryCode, ExpansionApp.App.Code, ParentCode,
                        Data.Code, Data);
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
    public string? ParentCode { get; set; }

    public bool IsQueryNav => Level == 3 && ParentCode is null;

    public string Name => IsQueryNav ? T("View") : TranslateProvider.DT(Data.Name);

    public bool IsChecked
    {
        get
        {
            if (Data.HasChildren && Level == 1)
            {
                return Data.Children.All(children => children.HasActions ? children.Actions.All(grandson => ExpansionWrapper.Value.Any(can => can.NavModel == grandson)) : ExpansionWrapper.Value.Any(can => can.NavModel == children));
            }
            else if (Data.HasActions && Level != 3)
            {
                return ExpansionWrapper.Value.Any(children => children.NavModel == Data) && Data.Children.All(children => ExpansionWrapper.Value.Any(can => can.NavModel == children));
            }
            else if (IsQueryNav)
            {
                return ExpansionWrapper.Value.Any(children => children.NavModel == Data);
            }
            else
            {
                return ExpansionWrapper.Value.Contains(CategoryAppNav);
            }
        }
    }

    private bool IsDisabled => InPreview || Data.Disabled;

    public bool Indeterminate => !IsQueryNav && (Data.HasActions || Data.HasChildren) && ExpansionApp.ExpansionAppItems.Any(item => item.IsChecked && (Data.Code == item.Data.ParentCode || Data.Code == item.Data.Code)) && ExpansionApp.ExpansionAppItems.Any(item => !item.IsChecked && item.Data.ParentCode == Data.Code);

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
        if (!Data.HasActions || IsQueryNav)
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
            await ExpansionApp.SwitchValue(CategoryAppNav, IsQueryNav, isIndeterminate: Indeterminate);
        }
        if (Favorite && Data.Url is not null && !Data.HasChildren)
        {
            NavigationManager.NavigateTo(Data.Url, true);
        }
    }

    private async Task AddFavorite()
    {
        await ExpansionApp.SwitchValue(CategoryAppNav);
    }
}
