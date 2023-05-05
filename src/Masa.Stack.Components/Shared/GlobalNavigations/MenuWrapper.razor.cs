namespace Masa.Stack.Components.GlobalNavigations;

public partial class MenuWrapper : MasaComponentBase
{
    [Parameter]
    public Menu Value { get; set; } = null!;

    // private Menu ConvertForNav(NavModel navModel, int deep, Menu? parent = null)
    // {
    //     var menu = new Menu(navModel.Code, navModel.Name, MenuType.Nav, MenuState.Normal, MenuSituation.Preview, false, false, deep, parent);
        
    //     foreach(var childrenNav in navModel.Children)
    //     {
    //         menu.Childrens.Add(ConvertForNav(childrenNav, deep++, menu));
    //     }
    //     return menu;
    // }

    // private Menu ConvertForPermission( navModel, int deep, Menu? parent = null)
    // {
    //     var menu = new Menu(navModel.Code, navModel.Name, MenuType.Nav, MenuState.Normal, Situation, false, false, deep, parent);
        
    //     foreach(var childrenNav in navModel.Children)
    //     {
    //         menu.Childrens.Add(ConvertForNav(childrenNav, deep++, menu));
    //     }
    //     return menu;
    // }

    // private async Task GetMenus()
    // {
    //     var menus = new List<Menu>();
    //     var deep = 0;
    //     foreach(var category in Categories)
    //     {   
    //         foreach(var app in category.Apps)
    //         {
    //             foreach(var a in app.Menus)
    //         }
    //         menus.Add(new Menu(category.Code, category.Name, MenuType.Category, MenuState.Normal, Situation, false, false, deep, null,,));
    //     }

    // }

}