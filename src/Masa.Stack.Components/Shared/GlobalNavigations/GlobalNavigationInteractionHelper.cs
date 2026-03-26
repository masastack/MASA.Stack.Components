namespace Masa.Stack.Components;

internal static class GlobalNavigationInteractionHelper
{
    public static async Task<List<string>> FetchFavoritesAsync(IAuthClient authClient)
    {
        return (await authClient.PermissionService.GetFavoriteMenuListAsync())
            .Select(item => item.Value.ToString())
            .ToList();
    }

    public static async Task<List<(string name, string url)>> FetchRecentVisitsAsync(IAuthClient authClient)
    {
        var visitedList = await authClient.UserService.GetVisitedListAsync();
        return visitedList.Select(item => (item.Name, item.Url)).ToList();
    }

    public static async Task RemoveFavoriteAsync(List<ExpansionMenu> favorites, ExpansionMenu nav, Func<string, Task>? onFavoriteRemove)
    {
        var favoriteNav = favorites.FirstOrDefault(item => item.Id == nav.Id);
        if (favoriteNav is null)
        {
            return;
        }

        favorites.Remove(favoriteNav);
        if (onFavoriteRemove is not null)
        {
            await onFavoriteRemove.Invoke(favoriteNav.Id);
        }
    }

    public static async Task AddFavoriteAsync(List<ExpansionMenu> favorites, ExpansionMenu nav, Func<string, Task>? onFavoriteAdd)
    {
        if (favorites.Any(item => item.Id == nav.Id))
        {
            return;
        }

        favorites.Add(nav);
        if (onFavoriteAdd is not null)
        {
            await onFavoriteAdd.Invoke(nav.Id);
        }
    }
}
