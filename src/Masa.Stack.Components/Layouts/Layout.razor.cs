﻿using Masa.Stack.Components.Models;

namespace Masa.Stack.Components;

public partial class Layout
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter, EditorRequired]
    public string? DefaultRoute { get; set; }

    [Parameter, EditorRequired]
    public string? Logo { get; set; }

    [Parameter, EditorRequired]
    public string? MiniLogo { get; set; }

    [Parameter, EditorRequired]
    public List<Nav>? NavItems { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        NavItems ??= new List<Nav>();
        FlattenedNavs = FlattenNavs(NavItems);
    }

    private List<Nav> FlattenedNavs { get; set; } = new();

    private List<Nav> FlattenNavs(List<Nav> tree)
    {
        var res = new List<Nav>();

        foreach (var nav in tree)
        {
            res.Add(nav);
            res.AddRange(FlattenNavs(nav.Children));
        }

        return res;
    }
}
