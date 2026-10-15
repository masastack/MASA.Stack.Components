namespace Masa.Stack.Components.Layouts;

public sealed class AppearanceMenu : MMenu
{
    [Parameter]
    public string? AppSelector { get; set; }

    // Custom Attach uses toolbar offsets; the default path measures against the overlay's parent.
    protected override string? DefaultAttachSelector => AppSelector ?? base.DefaultAttachSelector;

    public async Task UpdatePositionAsync()
    {
        if (!IsActive) return;
        await UpdateDimensionsAsync();
        StateHasChanged();
    }
}
