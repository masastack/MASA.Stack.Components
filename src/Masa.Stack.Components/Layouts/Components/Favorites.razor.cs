using Masa.Stack.Components.Models;
using Microsoft.AspNetCore.Components.Web;

namespace Masa.Stack.Components.Layouts;

public partial class Favorites
{
    [Inject]
    private CookieStorage? CookieStorage { get; set; }

    [Parameter, EditorRequired]
    public List<Nav> FlattenedNavs { get; set; } = new();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        FlattenedNavs ??= new List<Nav>();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ArgumentNullException.ThrowIfNull(CookieStorage);

            var cookieFavorite = await CookieStorage.GetCookieAsync(GlobalConfig_Favorite);

            if (cookieFavorite is not null)
            {
                FavoriteNavCodes = cookieFavorite.Split("|").ToList();
                StateHasChanged();
            }
        }
    }

    private const string GlobalConfig_Favorite = "GlobalConfig_Favorite";

    private bool _menuValue;
    private string? _searchKey;

    private List<string> FavoriteNavCodes { get; set; } = new List<string>();

    private List<Nav> FavoriteMenus => FlattenedNavs.Where(n => FavoriteNavCodes.Contains(n.Code)).ToList();

    private IEnumerable<Nav> SearchedMenus { get; set; } = Enumerable.Empty<Nav>();

    private async Task HandleOnSearchKeyDown(KeyboardEventArgs args)
    {
        if (args.Code == "Enter")
        {
            await Task.Delay(256);
            SearchNavs(_searchKey);
        }
    }

    private void SearchNavs(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            SearchedMenus = FlattenedNavs!.ToList();
            return;
        }

        SearchedMenus = FlattenedNavs!.Where(n => T(n.Name).Contains(key, StringComparison.OrdinalIgnoreCase));
    }

    private void ToggleFavorite(string code)
    {
        if (FavoriteNavCodes.Contains(code))
        {
            FavoriteNavCodes.Remove(code);
        }
        else
        {
            FavoriteNavCodes.Add(code);
        }

        CookieStorage?.SetItemAsync(GlobalConfig_Favorite, string.Join("|", FavoriteNavCodes));
    }

    private void MenuValueChanged(bool value)
    {
        _menuValue = value;

        if (value)
        {
            _searchKey = null;
            SearchNavs(_searchKey);
        }
    }

    private bool IsMenuActive(Nav menu)
    {
        return (menu.IsActive(NavigationManager.ToBaseRelativePath(NavigationManager.Uri)));
    }
}