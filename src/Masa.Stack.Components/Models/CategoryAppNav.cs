﻿namespace Masa.Stack.Components.Models;

public record CategoryAppNav
{
    public string? Category { get; }

    public string? App { get; }

    public string? Nav { get; }

    public string? Action { get; }

    public CategoryAppNav()
    {
    }

    public CategoryAppNav(string category)
    {
        Category = category;
    }

    public CategoryAppNav(string category, string app) : this(category)
    {
        App = app;
    }

    public CategoryAppNav(string category, string app, string nav) : this(category, app)
    {
        Nav = nav;
    }

    public CategoryAppNav(string category, string app, string nav, string action) : this(category, app, nav)
    {
        Action = action;
    }
}
