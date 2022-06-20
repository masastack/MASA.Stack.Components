using System.Globalization;
using FluentValidation;
using Masa.Stack.Components.Models;

namespace Masa.Stack.Components;

public partial class Layout
{
    [Inject]
    private I18n I18n { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter, EditorRequired]
    public string? Logo { get; set; }

    [Parameter, EditorRequired]
    public string? MiniLogo { get; set; }

    [Parameter, EditorRequired]
    public List<Nav>? NavItems { get; set; }

    [Parameter, EditorRequired]
    public string? UserCenterRoute { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        NavItems ??= new List<Nav>();
        FlattenedNavs = FlattenNavs(NavItems, true);
    }

    private List<Nav> FlattenedNavs { get; set; } = new();

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
        }

        return res;
    }
}
