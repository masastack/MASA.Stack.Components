namespace Masa.Stack.Components.GlobalNavigations;

public enum ExpansionMenuType
{
    Root = 0,
    Category = 1,
    App = 2,
    Nav = 3,
    Element = 4,
    Api = 5,
}

public enum ExpansionMenuState
{
    Normal = 0,
    Selected = 1,
    Indeterminate = 2,
    Impersonal = 3,
    Favorite = 4,
    Hidden = 5,
}

public enum ExpansionMenuSituation
{
    Preview = 1,
    Favorite = 2,
    Authorization = 3,
}

public enum ExpansionMenuStatePopDirection
{
    UnSet = 0,
    Parent = 1,
    Children = 2,
}

public record ExpansionMenuMetadata
{
    public ExpansionMenuMetadata()
    {
        TypeDeepStartDict = new Dictionary<ExpansionMenuType, int>();
        TypeDeepStartDict.Add(ExpansionMenuType.Root, -1);
        TypeDeepStartDict.Add(ExpansionMenuType.Category, -1);
        TypeDeepStartDict.Add(ExpansionMenuType.App, -1);
        TypeDeepStartDict.Add(ExpansionMenuType.Nav, -1);
        TypeDeepStartDict.Add(ExpansionMenuType.Element, -1);
        TypeDeepStartDict.Add(ExpansionMenuType.Api, -1);
    }
    
    public ExpansionMenuMetadata(ExpansionMenuSituation situation) : this()
    {
        Situation = situation;
    }
    
    public IDictionary<ExpansionMenuType, int> TypeDeepStartDict { get; set; }

    public ExpansionMenuSituation Situation { get; set; } = ExpansionMenuSituation.Preview;
}

public class ExpansionMenu
{
    public ExpansionMenu(string id, string name, ExpansionMenuType type, ExpansionMenuState state, ExpansionMenuMetadata? metadata = null, bool impersonal = false, bool disabled = false, ExpansionMenu? parent = null, List<ExpansionMenu>? childrens = null, Func<Task>? stateChangedAsync = null)
    {
        Id = id;
        Name = name;
        Type = type;
        State = state;
        Metadata = metadata ?? new ExpansionMenuMetadata();
        Impersonal = impersonal;
        Deep = parent != null ? parent.Deep + 1 : 0;
        Parent = parent;
        Childrens = childrens ?? new List<ExpansionMenu>();
        StateChangedAsync = stateChangedAsync;
        Data = new Dictionary<string, string>();

        ChildrenHasElement = Childrens.Any(children => children.Type == ExpansionMenuType.Element);
        if (ChildrenHasElement)
        {
            Childrens.Add(CreateViewElement());
        }
        
        SetTypeDeepRange();
    }

    public ExpansionMenu CreateViewElement()
    {
        return new ExpansionMenu(Guid.NewGuid().ToString(), "view", ExpansionMenuType.Element, State, Metadata, Impersonal, Disabled,this);
    }

    public string Id { get; private set; }

    public string Name { get; private set; }

    public ExpansionMenuType Type { get; private set; }

    public ExpansionMenuState State { get; private set; }

    public ExpansionMenuMetadata Metadata { get; private set; }

    public IDictionary<string, string> Data { get; set; }

    public bool Disabled { get; set; }

    public bool Impersonal { get; private set; }

    public int Deep { get; private set; }

    public bool Hidden { get; private set; } = false;

    public ExpansionMenu? Parent { get; private set; }

    public bool ChildrenHasElement { get; private set; }

    public List<ExpansionMenu> Childrens { get; set; }

    public Func<Task>? StateChangedAsync { get; set; }

    public string GetHiddenStyle() => Hidden ? "display: none;" : "";

    public void SetHiddenBySearch(string search, DynamicTranslateProvider translateProvider)
    {
        Hidden = false;
        
        if (Childrens.Count == 0 && Parent != null)
        {
            var searchChildren= Parent.Childrens.Where(children => children.Filter(translateProvider, search));
            var hiddenChildren = Parent.Childrens.ExceptBy(searchChildren.Select(child => child.Id), child => child.Id);
            foreach (var child in hiddenChildren)
            {
                child.Hidden = true;
            }
            return;
        }

        foreach (var child in Childrens)
        {
            child.SetHiddenBySearch(search, translateProvider);
        }

        if (Childrens.All(child => child.Hidden))
        {
            Hidden = true;
        }
    }

    private bool Filter(DynamicTranslateProvider translateProvider, string? search) => string.IsNullOrEmpty(search) ? true : translateProvider.DT(Name).Contains(search, StringComparison.OrdinalIgnoreCase);

    public ExpansionMenu AddData(string key, string value)
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
        if (!Metadata.TypeDeepStartDict.TryGetValue(ExpansionMenuType.Nav, out int start))
        {
            return Deep;
        }

        return Deep - start;
    }

    public List<ExpansionMenu> GetMenusByState(ExpansionMenuState state)
    {
        return GetMenusByStateInternal(this, new List<ExpansionMenu>());
        
        List<ExpansionMenu> GetMenusByStateInternal(ExpansionMenu menu, List<ExpansionMenu> stateMenus)
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
            case ExpansionMenuSituation.Preview:
                newState = CalcNewStateForPreview();
                break;
            case ExpansionMenuSituation.Favorite:
                newState = CalcNewStateForFavorite();
                break;
            case ExpansionMenuSituation.Authorization:
                newState = CalcNewStateForAuthorization();
                break;
        }

        return PopStateAsync(newState);
    }

    private ExpansionMenuState CalcNewStateForPreview()
    {
        throw new UserFriendlyException("Preview situation cannot change state");
    }

    private ExpansionMenuState CalcNewStateForFavorite()
    {
        return State == ExpansionMenuState.Normal ? ExpansionMenuState.Favorite : ExpansionMenuState.Normal;
    }

    private ExpansionMenuState CalcNewStateForAuthorization()
    {
        if (State == ExpansionMenuState.Normal || State == ExpansionMenuState.Impersonal)
        {
            return ExpansionMenuState.Selected;
        }
        if (State == ExpansionMenuState.Selected)
        {
            return Impersonal ? ExpansionMenuState.Impersonal : ExpansionMenuState.Normal;
        }
        return State;
    }

    private async Task PopStateAsync(ExpansionMenuState newState, ExpansionMenuStatePopDirection popDirection = ExpansionMenuStatePopDirection.UnSet)
    {
        var oldState = State;
        State = newState;

        if (oldState != newState && StateChangedAsync != null)
        {
            await StateChangedAsync.Invoke();
        }

        if (popDirection == ExpansionMenuStatePopDirection.UnSet || popDirection == ExpansionMenuStatePopDirection.Parent)
        {
            await PopParentStateAsync();
        }
        if (popDirection == ExpansionMenuStatePopDirection.UnSet || popDirection == ExpansionMenuStatePopDirection.Children)
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

        var childrenAllSelected = Parent.Childrens.All(children => children.State == ExpansionMenuState.Selected);
        if (childrenAllSelected)
        {
            return Parent.PopStateAsync(ExpansionMenuState.Selected, ExpansionMenuStatePopDirection.Parent);
        }

        var childrenPartialSelected = Parent.Childrens.Any(
        children => children.State == ExpansionMenuState.Selected ||
        children.State == ExpansionMenuState.Indeterminate);
        if (childrenPartialSelected)
        {
            return Parent.PopStateAsync(ExpansionMenuState.Indeterminate, ExpansionMenuStatePopDirection.Parent);
        }
        
        var childrenPartialFavorite = Parent.Childrens.Any(children => children.State == ExpansionMenuState.Normal) &&
                                      Parent.Childrens.Any(children => children.State == ExpansionMenuState.Favorite);
        if (childrenPartialFavorite)
        {
            return Parent.PopStateAsync(ExpansionMenuState.Normal, ExpansionMenuStatePopDirection.Parent);
        }

        var childrenAllFavorite = Parent.Childrens.All(children => children.State == ExpansionMenuState.Favorite);
        if (childrenAllFavorite)
        {
            return Parent.PopStateAsync(ExpansionMenuState.Favorite, ExpansionMenuStatePopDirection.Parent);
        }

        return Task.CompletedTask;
    }

    private async Task PopChildrenStateAsync()
    {
        if (Childrens.Count == 0 || State == ExpansionMenuState.Impersonal || State == ExpansionMenuState.Indeterminate)
        {
            return;
        }

        foreach (var children in Childrens)
        {
            await children.PopStateAsync(State, ExpansionMenuStatePopDirection.Children);
        }
    }

    private ExpansionMenu Clone()
    {
        return new ExpansionMenu(Id, Name, Type, State, Metadata, Impersonal, Disabled);
    }
}
