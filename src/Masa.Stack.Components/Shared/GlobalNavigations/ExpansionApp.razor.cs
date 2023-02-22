namespace Masa.Stack.Components.GlobalNavigations;

public partial class ExpansionApp
{
    [CascadingParameter]
    public ExpansionWrapper ExpansionWrapper { get; set; } = default!;

    [Parameter, EditorRequired]
    public App App { get; set; } = default!;

    [Parameter, EditorRequired]
    public string CategoryCode { get; set; } = null!;

    public bool Checkable => ExpansionWrapper.Checkable;

    public bool InPreview => ExpansionWrapper.InPreview;

    public List<ExpansionAppItem> ExpansionAppItems { get; set; } = new();

    public bool AppChecked => ExpansionAppItems.All(item => item.IsChecked);

    public List<CategoryAppNav> CategoryAppNavs => ExpansionAppItems.Select(item => item.CategoryAppNav).ToList();

    public List<CategoryAppNav> CheckedCategoryAppNavs => ExpansionAppItems.Where(item => item.IsChecked).Select(item => item.CategoryAppNav).ToList();

    internal readonly string ActionCodeFormat = "nav#{0}__action#{1}";

    private async Task AppCheckedChanged(bool v)
    {
        if (AppChecked) await UpdateValues(new List<CategoryAppNav>());
        else await UpdateValues(CategoryAppNavs);
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
        await ExpansionWrapper!.UpdateValues(App.Code, values);
    }

    public void Register(ExpansionAppItem expansionAppItem)
    {
        ExpansionAppItems.Add(expansionAppItem);
    }
}
