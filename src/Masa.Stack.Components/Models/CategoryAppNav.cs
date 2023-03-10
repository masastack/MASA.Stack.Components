namespace Masa.Stack.Components.Models;

public class CategoryAppNav
{
    public string? Category { get; }

    public string? App { get; }

    public string? Nav { get; }

    public string? Action { get; }

    public Nav? NavModel { get; }

    public CategoryAppNav()
    {
    }

    public CategoryAppNav(string? category)
    {
        Category = category;
    }

    public CategoryAppNav(string? category, string? app) : this(category)
    {
        App = app;
    }

    public CategoryAppNav(string? category, string? app, string? nav) : this(category, app)
    {
        Nav = nav;
    }

    public CategoryAppNav(string? category, string? app, string? nav, string? action) : this(category, app, nav)
    {
        Action = action;
    }

    public CategoryAppNav(string? category, string? app, string? nav, string? action, Nav? navModel) : this(category, app, nav, action)
    {
        NavModel = navModel;
    }

    public override bool Equals(object? obj)
    {
        return obj is CategoryAppNav categoryAppNav &&
                (categoryAppNav.Category, categoryAppNav.App, categoryAppNav.Nav, categoryAppNav.Action)
                == (Category, App, Nav, Action);
    }

    public override int GetHashCode()
    {
        return 1;
    }
}
