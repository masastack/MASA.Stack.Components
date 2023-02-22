namespace Masa.Stack.Components.Models;

internal class CategoryAppNavModel
{
    public string CategoryCode { get; set; }

    public string AppCode { get; set; }

    public Nav Nav { get; set; }

    public CategoryAppNavModel(string categoryCode, string appCode, Nav nav)
    {
        CategoryCode = categoryCode;
        AppCode = appCode;
        Nav = nav;
    }
}
