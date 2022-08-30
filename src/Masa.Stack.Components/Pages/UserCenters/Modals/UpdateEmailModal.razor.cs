namespace Masa.Stack.Components.UserCenters;

public partial class UpdateEmailModal : MasaComponentBase
{
    private string? _captchaText;

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public EventCallback<string> OnSuccess { get; set; }

    public UpdateUserEmailModel UpdateUserEmail { get; set; } = new();

    public MForm FormRef { get; set; } = default!;

    [NotNull]
    private string? CaptchaText
    {
        get => (_captchaText == "0" || _captchaText == null) ? T("Captcha") : _captchaText;
        set => _captchaText = value;
    }

    private void CaptchaValidateAction(DefaultTextfieldAction action)
    {
        action.Content = CaptchaText;
        action.Text = true;
        action.DisableLoding = true;
        action.OnClick = SendCaptcha;
    }

    private async Task SendCaptcha(MouseEventArgs _)
    {
        if (CaptchaText != T("Captcha")) return;
        var field = FormRef.EditContext.Field(nameof(UpdateUserEmail.Email));
        FormRef.EditContext.NotifyFieldChanged(field);
        var result = FormRef.EditContext.GetValidationMessages(field);
        if(result.Any() is false)
        {
            await AuthClient.UserService.SendMsgCodeAsync(new SendMsgCodeModel()
            {
                PhoneNumber = UpdateUserEmail.Email,
                SendMsgCodeType = SendMsgCodeTypes.UpdatePhoneNumber
            });
            await PopupService.AlertAsync(T("The verification code is sent successfully, please enter the verification code within 60 seconds"), AlertTypes.Success);
            int second = 60;
            while (second >= 0)
            {
                CaptchaText = second.ToString();
                StateHasChanged();               
                second--;
                await Task.Delay(1000);
            }
        }
    }

    private async Task HandleOnCancel()
    {
        await FormRef.ResetAsync();
        if (VisibleChanged.HasDelegate)
            await VisibleChanged.InvokeAsync(false);
        else Visible = false;       
    }

    private async Task HandleOnOk()
    {
    }
}
