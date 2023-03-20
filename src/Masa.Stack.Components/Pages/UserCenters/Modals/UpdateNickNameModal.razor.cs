namespace Masa.Stack.Components.UserCenters;

public partial class UpdateNickNameModal : MasaComponentBase
{
    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public EventCallback<string> OnSuccess { get; set; }

    public MForm FormRef { get; set; } = default!;

   // public UpdateUserNickNameModel UpdateUserNickName { get; set; } = new();
    private async Task HandleOnOk()
    {
        if (FormRef.Validate())
        {

        }
    }

    private async Task HandleOnCancel()
    {
        FormRef.Reset();
        if (VisibleChanged.HasDelegate)
            await VisibleChanged.InvokeAsync(false);
        else Visible = false;
    }

}

