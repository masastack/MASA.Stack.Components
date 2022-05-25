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
    public bool InPreview { get; set; }

    private bool _initValues;
    private bool _fromCheckbox;
    private List<StringNumber> _values = new();
    private List<CategoryAppNav> _categoryAppNavs = new();

    private bool AppChecked { get; set; }

    private bool IsCheckable => Checkable && !InPreview;

    protected override void OnParametersSet()
    {
        FavoriteNavs ??= new();

        if (Checkable)
        {
            if (ExpansionWrapper is not null && ExpansionWrapper.Value.Any() && (!_initValues || !_fromCheckbox))
            {
                _initValues = true;

                var categoryAppNavs = ExpansionWrapper.Value.Where(v => v.Category == CategoryCode && v.App == App.Code).ToList();
                _values = categoryAppNavs.Select(c => (StringNumber)c.Nav).Where(n => n is not null).ToList();
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
        _values = v.Where(u => u.Value is not null).ToList();

        _categoryAppNavs = _values
                           .Select(u => new CategoryAppNav(CategoryCode, App.Code, u.ToString()))
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

        var categoryAppNav = new CategoryAppNav(CategoryCode, App.Code);

        if (AppChecked)
        {
            _categoryAppNavs.Add(categoryAppNav);
        }
        else
        {
            _categoryAppNavs.Remove(categoryAppNav);
        }

        await UpdateValues(App.Code, _categoryAppNavs);
    }

    private async Task UpdateValues(string appCode, List<CategoryAppNav> values)
    {
        _fromCheckbox = true;

        var key = $"app_{App.Code}";

        await ExpansionWrapper!.UpdateValues(key, values);
    }

    private async Task ToggleFavorite(string category, string app, Nav nav)
    {
        var favoriteNav = new FavoriteNav(category, app, nav);
        var item = FavoriteNavs!.FirstOrDefault(f => f.Id == favoriteNav.Id);
        if (item is not null)
        {
            // TODO: remove favorite
            await Task.Delay(1000);

            FavoriteNavs!.Remove(item);

            nav.IsFavorite = false;
        }
        else
        {
            // TODO: add favorite
            await Task.Delay(1000);

            FavoriteNavs!.Add(favoriteNav);

            nav.IsFavorite = true;
        }

        // TODO: need to notify parent
        GlobalNavigation?.InvokeStateHasChanged();
    }

    private List<Nav> FlattenNavs(List<Nav> tree)
    {
        var res = new List<Nav>();

        foreach (var nav in tree)
        {
            res.Add(nav);

            if (nav.HasChildren)
            {
                res.AddRange(FlattenNavs(nav.Children));
            }

            if (nav.HasActions)
            {
                res.AddRange(nav.Actions!.Select(a => new Nav()
                {
                    Code = a.Code,
                    Name = a.Name
                }));
            }
        }

        return res;
    }

    private bool IsInPreview(Nav nav)
    {
        var flattenNavs = FlattenNavs(new List<Nav>() { nav })
            .Select(item => (StringNumber)item.Code);

        return InPreview && !_values.Intersect(flattenNavs).Any();
    }


    private bool IsInPreview(NavAction action)
    {
        return InPreview && !_values.Contains(action.Code);
    }
}
