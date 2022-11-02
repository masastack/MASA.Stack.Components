namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionApp
{
    [CascadingParameter]
    public ExpansionWrapper ExpansionWrapper { get; set; } = default!;

    [CascadingParameter]
    public GlobalNavigation? GlobalNavigation { get; set; }

    [Parameter, EditorRequired]
    public App App { get; set; } = default!;

    [Parameter, EditorRequired]
    public string CategoryCode { get; set; } = null!;

    [Parameter, EditorRequired]
    public List<FavoriteNav> FavoriteNavs
    {
        get
        {
            return _favoriteNavs;
        }
        set
        {
            _favoriteNavs = value;
            UpdateFavorite(App.Navs);

            void UpdateFavorite(List<Nav> navs)
            {
                navs.ForEach(nav =>
                {
                    nav.IsFavorite = _favoriteNavs?.FirstOrDefault(fn => fn.Nav.Code == nav.Code)?.Nav.IsFavorite ?? false;
                    if (nav.Children.Any())
                    {
                        UpdateFavorite(nav.Children);
                    }
                });
            }
        }
    }

    [Parameter]
    public bool Checkable { get; set; }

    [Parameter]
    public bool CheckStrictly { get; set; }

    [Parameter]
    public bool InPreview { get; set; }

    public List<ExpansionAppItem> ExpansionAppItems { get; set; } = new();

    private List<StringNumber> _values = new();
    private List<FavoriteNav> _favoriteNavs = new();

    public bool AppChecked => ExpansionAppItems.All(item => item.IsChecked);

    public List<CategoryAppNav> CategoryAppNavs => ExpansionAppItems.Select(item => item.CategoryAppNav).ToList();

    public List<CategoryAppNav> CheckedCategoryAppNavs => ExpansionAppItems.Where(item => item.IsChecked).Select(item => item.CategoryAppNav).ToList();

    private bool IsCheckable => Checkable && !InPreview;

    internal readonly string ActionCodeFormat = "nav#{0}__action#{1}";

    private async Task AppCheckedChanged(bool v)
    {
        if (!CheckStrictly)
        {
            if (AppChecked) await UpdateValues(new List<CategoryAppNav>());
            else await UpdateValues(CategoryAppNavs);
        }
    }

    public async Task CheckedAllNavs(bool isChecked)
    {
        if (isChecked) await UpdateValues(CategoryAppNavs);
        else await UpdateValues(new());
    }

    public async Task SwitchValue(CategoryAppNav value)
    {
        var values = CheckedCategoryAppNavs;
        if (values.Contains(value))
        {
            values.Remove(value);
            if (value.NavModel!.HasActions)
            {
                foreach (var categoryAppNav in CategoryAppNavs.Where(v => value.NavModel.Actions.Any(action => action.Code == v.Action)))
                {
                    values.Remove(categoryAppNav);
                }
            }
        }
        else
        {
            values.Add(value);
            if (value.NavModel!.IsAction)
            {
                var parent = CategoryAppNavs.First(v => v.Nav == value.NavModel.ParentCode);
                if (values.Contains(parent) is false) values.Add(parent);
            }
            if (value.NavModel.HasActions)
            {
                values.AddRange(CategoryAppNavs.Where(v => value.NavModel.Actions.Any(action => action.Code == v.Action)));
            }
        }
        await UpdateValues(values);
    }

    private async Task UpdateValues(List<CategoryAppNav> values)
    {
        await ExpansionWrapper!.UpdateValues(App.Code, values, CodeType.App);
    }

    private async Task ToggleFavorite(string category, string app, Nav nav)
    {
        var favoriteNav = new FavoriteNav(category, app, nav);
        var item = FavoriteNavs!.FirstOrDefault(f => f.Id == favoriteNav.Id);
        if (item is not null)
        {
            if (GlobalNavigation?.OnFavoriteRemove is not null)
            {
                await GlobalNavigation.OnFavoriteRemove(item.Nav.Code);
            }

            FavoriteNavs!.Remove(item);

            nav.IsFavorite = false;
        }
        else
        {
            if (GlobalNavigation?.OnFavoriteAdd is not null)
            {
                await GlobalNavigation.OnFavoriteAdd(nav.Code);
            }

            FavoriteNavs!.Add(favoriteNav);

            nav.IsFavorite = true;
        }

        // TODO: need to notify parent
        GlobalNavigation?.InvokeStateHasChanged();
    }

    private bool Filter(Nav nav)
    {
        if (InPreview)
        {
            return ExpansionWrapper.Value.Any(value => value.NavModel == nav) || nav.Children.Any(Filter);
        }
        else
        {
            return !nav.Hiden;
        }
    }

    public void Register(ExpansionAppItem expansionAppItem)
    {
        ExpansionAppItems.Add(expansionAppItem);
    }
}
