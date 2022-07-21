namespace Masa.Stack.Components;

public partial class ExpansionWrapperUnique
{
    public List<Category> _categories = new();
    public List<UniqueModel> _values = new();

    [Parameter]
    public string Style { get; set; } = "";

    [Parameter]
    public string Class { get; set; } = "";

    [Parameter]
    public bool Checkable { get; set; }

    [Parameter]
    public bool CheckStrictly { get; set; }

    [Parameter]
    public bool InPreview { get; set; }

    [Parameter]
    public List<FavoriteNav>? FavoriteNavs { get; set; }

    [Parameter]
    public List<UniqueModel> Value
    {
        get => _values;
        set
        {
            if (_values.Count != value.Count || _values.Except(value).Count() > 0)
            {
                _values = value;
                SetCheckedCategoryAppNavs();
            }
        }
    }

    [Parameter]
    public EventCallback<List<UniqueModel>> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public List<Category> Categories
    {
        get => _categories;
        set
        {
            if (_categories.Count != value.Count || _categories.Except(value).Count() > 0)
            {
                _categories = value;
                var navs = value.SelectMany(v => v.Apps.Select(app => new { CategoryCode = v.Code, app }))
                                .SelectMany(ca => ca.app.Navs.Select(nav => new CategoryAppNavModel { CategoryCode = ca.CategoryCode, AppCode = ca.app.Code, Nav = nav }))
                                .ToList();
                CategoryAppNavs = BuilderCategoryAppNavs(navs);
                SetCheckedCategoryAppNavs();
            }
        }
    }

    public List<CategoryAppNav> CategoryAppNavs { get; set; } = new();

    private List<CategoryAppNav> CheckedCategoryAppNavs { get; set; } = new();

    private async Task CategoryAppNavsUpdateAsync(List<CategoryAppNav> categoryAppNavs)
    {
        var value = new List<UniqueModel>();
        foreach (var categoryAppNav in categoryAppNavs)
        {
            if (string.IsNullOrEmpty(categoryAppNav.Action) is false) value.Add(new UniqueModel(categoryAppNav.Action, categoryAppNav.NavModel?.IsDisabled));
            else if (string.IsNullOrEmpty(categoryAppNav.Nav) is false) value.Add(new UniqueModel(categoryAppNav.Nav, categoryAppNav.NavModel?.IsDisabled));
        }
        await UpdateValueAsync(value);
    }

    private async Task UpdateValueAsync(List<UniqueModel> value)
    {
        if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(value);
        else Value = value;
    }

    private void SetCheckedCategoryAppNavs()
    {
        CheckedCategoryAppNavs.Clear();
        foreach (var categoryAppNav in CategoryAppNavs)
        {
            categoryAppNav.NavModel!.IsDisabled = false;
            if (categoryAppNav.Action is not null)
            {
                var value = Value.FirstOrDefault(value => value.Code.Contains(categoryAppNav.Action));
                if (value is not null)
                {
                    categoryAppNav.NavModel.IsDisabled = value.IsDisabled;
                    CheckedCategoryAppNavs.Add(categoryAppNav);
                }
            }
            else if (categoryAppNav.Nav is not null)
            {
                var value = Value.FirstOrDefault(value => value.Code.Contains(categoryAppNav.Nav));
                if (value is not null)
                {
                    categoryAppNav.NavModel.IsDisabled = value.IsDisabled;
                    CheckedCategoryAppNavs.Add(categoryAppNav);
                }
            }
        }
    }

    private List<CategoryAppNav> BuilderCategoryAppNavs(List<CategoryAppNavModel> categoryAppNavs, string? parentNavCode = null)
    {
        var all = new List<CategoryAppNav>();
        foreach (var categoryAppNav in categoryAppNavs)
        {
            if (categoryAppNav.Nav.IsAction)
            {
                all.Add(new CategoryAppNav(categoryAppNav.CategoryCode, categoryAppNav.AppCode, parentNavCode, categoryAppNav.Nav.Code, categoryAppNav.Nav));
            }
            else
            {
                all.Add(new CategoryAppNav(categoryAppNav.CategoryCode, categoryAppNav.AppCode, categoryAppNav.Nav.Code, default, categoryAppNav.Nav));
            }

            if (categoryAppNav.Nav.Children.Count > 0)
                all.AddRange(BuilderCategoryAppNavs(categoryAppNav.Nav.Children.Select(nav => new CategoryAppNavModel
                {
                    CategoryCode = categoryAppNav.CategoryCode,
                    AppCode = categoryAppNav.AppCode,
                    Nav = nav,
                }).ToList(), categoryAppNav.Nav.Code));
        }
        return all;
    }
}
