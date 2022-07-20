namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionApp
{
    //public App? _app;

    [CascadingParameter]
    public ExpansionWrapper? ExpansionWrapper { get; set; } = default!;

    [CascadingParameter]
    public ExpansionCategory ExpansionCategory { get; set; } = default!;

    [CascadingParameter]
    public GlobalNavigation? GlobalNavigation { get; set; }

    //[Parameter, EditorRequired]
    //public App App
    //{
    //    get { return _app ?? throw new Exception("Please set App parameter"); }
    //    set
    //    {
    //        if (_app is null || _app.Equals(value) is false)
    //        {
    //            _app = value;
    //            _categoryAppNavs = FlattenNavs(value.Navs, true).Select(nav => nav.IsAction
    //            ? new CategoryAppNav(CategoryCode, App.Code, nav.ParentCode, nav.Code, nav)
    //            : new CategoryAppNav(CategoryCode, App.Code, nav.Code, default, nav)).ToList();
    //        }
    //    }
    //}

    [Parameter, EditorRequired]
    public App App { get; set; } = default!;

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

    public List<ExpansionAppItem> ExpansionAppItems { get; set; } = new ();

    //public List<CategoryAppNav> CheckedCategoryAppNavs => ExpansionAppItems.Where(item => item.IsChecked)
    //                                                                       .Select(item => _categoryAppNavs.First(ca => ca.Nav==item.Data?.Code))
    //                                                                       .ToList();
    //private bool _initValues;
    //private bool _fromCheckbox;
    private List<StringNumber> _values = new();
    //private List<CategoryAppNav> _categoryAppNavs = new();

    public bool AppChecked => ExpansionAppItems.All(item => item.IsChecked);//_categoryAppNavs.Count == CheckedCategoryAppNavs.Count;

    public List<CategoryAppNav> CategoryAppNavs => ExpansionAppItems.Select(item => item.CategoryAppNav).ToList();

    public List<CategoryAppNav> CheckedCategoryAppNavs => ExpansionAppItems.Where(item => item.IsChecked).Select(item => item.CategoryAppNav).ToList();

    private bool IsCheckable => Checkable && !InPreview;

    internal readonly string ActionCodeFormat = "nav#{0}__action#{1}";

    protected override void OnInitialized()
    {
        ExpansionCategory.Register(this);
    }

    //protected override void OnParametersSet()
    //{
    //    var values = ExpansionWrapper!.Value.Where(v => v.App == App.Code).Select(v => v.Nav);
    //    if (values.Count() > 0)
    //    {
    //        foreach(var item in ExpansionAppItems)
    //        {
    //            if (values.Contains(item.Data!.Code)) item.IsChecked = true;
    //            else item.IsChecked = false;
    //        }
    //    }
    //}
    //protected override void OnParametersSet()
    //{
    //    FavoriteNavs ??= new();

    //    if (Checkable)
    //    {
    //        if (ExpansionWrapper?.Value != null && (!_initValues || !_fromCheckbox))
    //        {
    //            var categoryAppNavs = ExpansionWrapper.Value.Where(v => v.Category == CategoryCode && v.App == App.Code).ToList();

    //            _checkedCategoryAppNavs = categoryAppNavs;
    //            if (!_initValues)
    //            {
    //                _initValues = true;
    //            }

    //            _values = categoryAppNavs.Select(c =>
    //            {
    //                StringNumber val = null;

    //                if (c.Action is not null)
    //                {
    //                    val = string.Format(ActionCodeFormat, c.Nav, c.Action);
    //                }
    //                else if (c.Nav is not null)
    //                {
    //                    val = c.Nav;
    //                }

    //                return val;
    //            }).Where(n => n is not null).ToList();
    //        }

    //        if (_fromCheckbox)
    //        {
    //            _fromCheckbox = false;
    //        }
    //    }
    //}

    //private async Task ValuesChanged(List<StringNumber> v)
    //{
    //    // TODO: select the nav and select the children and actions
    //    // TODO: there is a bug in ItemGroup

    //    _values = v.Where(u => u.Value is not null).ToList();

    //    _checkedCategoryAppNavs = _values
    //                       .Select(u =>
    //                       {
    //                           var val = u.ToString();

    //                           if (val.Contains("__action#"))
    //                           {
    //                               var navActions = val.Split("__action#");
    //                               var navCode = navActions[0].Replace("nav#", "");
    //                               var actionCode = navActions[1];

    //                               return new CategoryAppNav(CategoryCode, App.Code, navCode, actionCode);
    //                           }

    //                           return new CategoryAppNav(CategoryCode, App.Code, val);
    //                       })
    //                       .ToList();

    //    await UpdateValues(App.Code, _checkedCategoryAppNavs);
    //}

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
        }
        else
        {
            values.Add(value);
            if(value.NavModel!.IsAction)
            {
                var parent = CategoryAppNavs.First(v => v.Nav == value.NavModel.ParentCode);
                if(values.Contains(parent) is false) values.Add(parent);
            }
        }
        await UpdateValues(values);
    }

    private async Task UpdateValues(List<CategoryAppNav> values)
    {
        //_fromCheckbox = true;

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

    private bool Filter(Nav nav)
    {
        //if (nav.IsAction)
        //{
        //    return !(InPreview && !_values.Contains(nav.Code));
        //}

        //var flattenNavs = FlattenNavs(new List<Nav>() { nav })
        //    .Select(item => (StringNumber)item.Code);

        //return !(InPreview && !_values.Intersect(flattenNavs).Any());
        if (Checkable) return true;
        return InPreview && (ExpansionWrapper.Value.Any(value => value.NavModel == nav) || nav.Children.Any(Filter));
    }

    public void Register(ExpansionAppItem expansionAppItem)
    {
        ExpansionAppItems.Add(expansionAppItem);
    }
}
