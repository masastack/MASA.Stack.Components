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

    public UpdateUserBasicInfoModel UpdateUserBasicInfo { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitUserAsync();
            StateHasChanged();
        }
    }

    private async Task InitUserAsync()
    {
        var userDetail = await AuthClient.UserService.GetCurrentUserAsync();
        UpdateUserBasicInfo = userDetail.Adapt<UpdateUserBasicInfoModel>();
    }

    private async Task<bool> HandleOnOk()
    {
        var field = FormRef.EditContext.Field(nameof(UpdateUserBasicInfoModel.DisplayName));
        FormRef.EditContext.NotifyFieldChanged(field);
        var result = FormRef.EditContext.GetValidationMessages(field);
        if (!result.Any())
        {
            await AuthClient.UserService.UpdateBasicInfoAsync(UpdateUserBasicInfo);
            if (OnSuccess.HasDelegate)
                await OnSuccess.InvokeAsync();
        }
        return !result.Any();
    }

    private async Task HandleOnCancel()
    {
        await InitUserAsync();
        if (VisibleChanged.HasDelegate)
            await VisibleChanged.InvokeAsync(false);
        else Visible = false;
    }

}

