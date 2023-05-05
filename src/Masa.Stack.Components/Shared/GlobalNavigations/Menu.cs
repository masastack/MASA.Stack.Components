namespace Masa.Stack.Components.GlobalNavigations;

public enum MenuType
{
    Root = 0,
    Category = 1,
    App = 2,
    Nav = 3,
    Element = 4,
    Api = 5,
}

public enum MenuState
{
    Normal = 0,
    Selected = 1,
    Indeterminate = 2,
    Impersonal = 3,
    Favorite = 4
}

public enum MenuSituation
{
    Preview = 1,
    Favorite = 2,
    Authorization = 3,
}

public enum MenuStatePopDirection
{
    UnSet = 0,
    Parent = 1,
    Children = 2,
}

public record MenuMetadata
{
    public MenuMetadata()
    {
        TypeDeepStartDict = new Dictionary<MenuType, int>();
        TypeDeepStartDict.Add(MenuType.Root, -1);
        TypeDeepStartDict.Add(MenuType.Category, -1);
        TypeDeepStartDict.Add(MenuType.App, -1);
        TypeDeepStartDict.Add(MenuType.Nav, -1);
        TypeDeepStartDict.Add(MenuType.Element, -1);
        TypeDeepStartDict.Add(MenuType.Api, -1);
    }
    
    public MenuMetadata(MenuSituation situation) : this()
    {
        Situation = situation;
    }
    
    public IDictionary<MenuType, int> TypeDeepStartDict { get; set; }

    public MenuSituation Situation { get; set; } = MenuSituation.Preview;
}

public class Menu
{
    public Menu(string id, string name, MenuType type, MenuState state, MenuMetadata? metadata = null, bool impersonal = false, bool disabled = false, Menu? parent = null, List<Menu>? childrens = null, Func<Task>? stateChangedAsync = null)
    {
        ID = id;
        Name = name;
        Type = type;
        State = state;
        Metadata = metadata ?? new MenuMetadata();
        Impersonal = impersonal;
        Deep = parent != null ? parent.Deep + 1 : 0;
        Parent = parent;
        Childrens = childrens ?? new List<Menu>();
        StateChangedAsync = stateChangedAsync;
        Data = new Dictionary<string, string>();

        ChildrenHasElement = Childrens.Any(children => children.Type == MenuType.Element);
        if (ChildrenHasElement)
        {
            Childrens.Add(CreateViewElement());
        }
        
        SetTypeDeepRange();
    }

    public Menu CreateViewElement()
    {
        return new Menu(Guid.NewGuid().ToString(), "view", MenuType.Element, State, Metadata, Impersonal, Disabled,this);
    }

    public string ID { get; private set; }

    public string Name { get; private set; }

    public MenuType Type { get; private set; }

    public MenuState State { get; private set; }

    public MenuMetadata Metadata { get; private set; }

    public IDictionary<string, string> Data { get; set; }

    public bool Disabled { get; set; }

    public bool Impersonal { get; private set; }

    public int Deep { get; private set; }

    public Menu? Parent { get; private set; }

    public bool ChildrenHasElement { get; private set; }

    public List<Menu> Childrens { get; set; }

    public Func<Task>? StateChangedAsync { get; private set; }

    public Menu AddData(string key, string value)
    {
        if (Data.ContainsKey(key))
        {
            Data[key] = value;
        }
        else
        {
            Data.Add(key, value);
        }

        return this;
    }

    public string GetData(string key)
    {
        if (!Data.TryGetValue(key, out string? data))
        {
            return string.Empty;
        }

        return data;
    }

    public void SetTypeDeepRange()
    {
        if (Metadata.TypeDeepStartDict.ContainsKey(Type)&&Parent != null && Parent.Type != Type)
        {
            Metadata.TypeDeepStartDict[Type] = Deep;
        }
    }

    public int GetNavDeep()
    {
        if (!Metadata.TypeDeepStartDict.TryGetValue(MenuType.Nav, out int start))
        {
            return Deep;
        }

        return Deep - start;
    }

    public List<Menu> GetMenusByState(MenuState state)
    {
        return GetMenusByStateInternal(this, new List<Menu>());
        
        List<Menu> GetMenusByStateInternal(Menu menu, List<Menu> stateMenus)
        {
            if (menu.State == state)
            {
                stateMenus.Add(menu);
            }
            
            foreach (var stateMenu in menu.Childrens)
            {
                stateMenus = GetMenusByStateInternal(stateMenu, stateMenus);
            }

            return stateMenus;
        }
    }

    public Task ChangeStateAsync()
    {
        var newState = State;
        switch (Metadata.Situation)
        {
            case MenuSituation.Preview:
                newState = CalcNewStateForPreview();
                break;
            case MenuSituation.Favorite:
                newState = CalcNewStateForFavorite();
                break;
            case MenuSituation.Authorization:
                newState = CalcNewStateForAuthorization();
                break;
        }

        return PopStateAsync(newState);
    }

    private MenuState CalcNewStateForPreview()
    {
        throw new UserFriendlyException("Preview situation cannot change state");
    }

    private MenuState CalcNewStateForFavorite()
    {
        return State == MenuState.Normal ? MenuState.Favorite : MenuState.Normal;
    }

    private MenuState CalcNewStateForAuthorization()
    {
        if (State == MenuState.Normal || State == MenuState.Impersonal)
        {
            return MenuState.Selected;
        }
        if (State == MenuState.Selected)
        {
            return Impersonal ? MenuState.Impersonal : MenuState.Normal;
        }
        return State;
    }

    private async Task PopStateAsync(MenuState newState, MenuStatePopDirection popDirection = MenuStatePopDirection.UnSet)
    {
        var oldState = State;
        State = newState;

        if (oldState != newState && StateChangedAsync != null)
        {
            await StateChangedAsync.Invoke();
        }

        if (popDirection == MenuStatePopDirection.UnSet || popDirection == MenuStatePopDirection.Parent)
        {
            await PopParentStateAsync();
        }
        if (popDirection == MenuStatePopDirection.UnSet || popDirection == MenuStatePopDirection.Children)
        {
            await PopChildrenStateAsync();
        }
    }

    private Task PopParentStateAsync()
    {
        if (Parent == null)
        {
            return Task.CompletedTask;
        }

        var childrenAllSelected = Parent.Childrens.All(children => children.State == MenuState.Selected);
        if (childrenAllSelected)
        {
            return Parent.PopStateAsync(MenuState.Selected, MenuStatePopDirection.Parent);
        }

        var childrenPartialSelected = Parent.Childrens.Any(
        children => children.State == MenuState.Selected ||
        children.State == MenuState.Indeterminate);
        if (childrenPartialSelected)
        {
            return Parent.PopStateAsync(MenuState.Indeterminate, MenuStatePopDirection.Parent);
        }

        var childrenAllNormal = Parent.Childrens.All(children => children.State == MenuState.Normal);
        if (childrenAllNormal)
        {
            return Parent.PopStateAsync(MenuState.Normal, MenuStatePopDirection.Parent);
        }

        var childrenAllFavorite = Parent.Childrens.All(children => children.State == MenuState.Favorite);
        if (childrenAllFavorite)
        {
            return Parent.PopStateAsync(MenuState.Favorite, MenuStatePopDirection.Parent);
        }

        return Task.CompletedTask;
    }

    private async Task PopChildrenStateAsync()
    {
        if (Childrens.Count == 0 || State == MenuState.Impersonal || State == MenuState.Indeterminate)
        {
            return;
        }

        foreach (var children in Childrens)
        {
            await PopStateAsync(State, MenuStatePopDirection.Children);
        }
    }
}
