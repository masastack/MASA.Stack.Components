namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionApp
{
    [CascadingParameter]
    public ExpansionWrapper? ExpansionWrapper { get; set; }

    [CascadingParameter]
    public GlobalNavigation? GlobalNavigation { get; set; }

    [Parameter, EditorRequired]
    public App App { get; set; } = null!;

    [Parameter, EditorRequired]
    public string CategoryCode { get; set; } = null!;

    [Parameter, EditorRequired]
    public List<FavoriteNav>? FavoriteNavs { get; set; }

    [Parameter]
    public bool Checkable { get; set; }

    [Parameter]
    public bool CheckStrictly { get; set; }

    [Parameter]
    public bool InPreview { get; set; }

    private bool _initValues;
    private bool _fromCheckbox;
    private List<StringNumber> _values = new();
    private List<CategoryAppNav> _categoryAppNavs = new();

    private bool AppChecked { get; set; }

    private bool IsCheckable => Checkable && !InPreview;

    internal readonly string ActionCodeFormat = "nav#{0}__action#{1}";

    protected override void OnParametersSet()
    {
        FavoriteNavs ??= new();

        if (Checkable)
        {
            if (ExpansionWrapper?.Value != null && (!_initValues || !_fromCheckbox))
            {
                var categoryAppNavs = ExpansionWrapper.Value.Where(v => v.Category == CategoryCode && v.App == App.Code).ToList();

                _categoryAppNavs = categoryAppNavs;
                if (!_initValues)
                {
                    _initValues = true;
                }

                _values = categoryAppNavs.Select(c =>
                {
                    StringNumber val = null;

                    if (c.Action is not null)
                    {
                        val = string.Format(ActionCodeFormat, c.Nav, c.Action);
                    }
                    else if (c.Nav is not null)
                    {
                        val = c.Nav;
                    }

                    return val;
                }).Where(n => n is not null).ToList();

                AppChecked = categoryAppNavs.Any(c => c.Nav is null);
            }

            if (_fromCheckbox)
            {
                _fromCheckbox = false;
            }
        }
    }

    private async Task ValuesChanged(List<StringNumber> v)
    {
        // TODO: select the nav and select the children and actions
        // TODO: there is a bug in ItemGroup

        _values = v.Where(u => u.Value is not null).ToList();

        _categoryAppNavs = _values
                           .Select(u =>
                           {
                               var val = u.ToString();

                               if (val.Contains("__action#"))
                               {
                                   var navActions = val.Split("__action#");
                                   var navCode = navActions[0].Replace("nav#", "");
                                   var actionCode = navActions[1];

                                   return new CategoryAppNav(CategoryCode, App.Code, navCode, actionCode);
                               }

                               return new CategoryAppNav(CategoryCode, App.Code, val);
                           })
                           .ToList();

        if (AppChecked)
        {
            _categoryAppNavs.Add(new CategoryAppNav(CategoryCode, App.Code));
        }

        await UpdateValues(App.Code, _categoryAppNavs);
    }

    private async Task AppCheckedChanged(bool v)
    {
        AppChecked = v;

        List<CategoryAppNav> categoryAppNavs = new();

        if (!CheckStrictly)
        {
            var flattenedNavs = FlattenNavs(App.Navs, true).Select(nav => nav.IsAction
                ? new CategoryAppNav(CategoryCode, App.Code, nav.ParentCode, nav.Code)
                : new CategoryAppNav(CategoryCode, App.Code, nav.Code));
            categoryAppNavs.AddRange(flattenedNavs);
        }

        categoryAppNavs.Add(new CategoryAppNav(CategoryCode, App.Code));

        foreach (var categoryAppNav in categoryAppNavs)
        {
            if (_categoryAppNavs.Contains(categoryAppNav))
            {
                if (!AppChecked)
                {
                    _categoryAppNavs.Remove(categoryAppNav);
                }
            }
            else if (AppChecked)
            {
                _categoryAppNavs.Add(categoryAppNav);
            }
        }

        await UpdateValues(App.Code, _categoryAppNavs);
    }

    private async Task UpdateValues(string appCode, List<CategoryAppNav> values)
    {
        _fromCheckbox = true;

        var key = $"category_{CategoryCode}_app_{App.Code}";

        await ExpansionWrapper!.UpdateValues(key, values);
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

    private bool IsInPreview(Nav nav)
    {
        if (nav.IsAction)
        {
            return InPreview && !_values.Contains(nav.Code);
        }

        var flattenNavs = FlattenNavs(new List<Nav>() { nav })
            .Select(item => (StringNumber)item.Code);

        return InPreview && !_values.Intersect(flattenNavs).Any();
    }
}
