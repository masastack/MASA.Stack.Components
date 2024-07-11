namespace Masa.Stack.Components.Store;

public class GlobalNavigationState : IScopedDependency
{
    private int _layer = 2;

    public int Layer
    {
        get => _layer;
        set
        {
            if (_layer != value)
            {
                _layer = value;
                OnLayerChanged?.Invoke();
            }
        }
    }

    public List<int> LayerItems = new List<int> { 1, 2, 3 };

    public delegate Task LayerChanged();

    public event LayerChanged? OnLayerChanged;
}
