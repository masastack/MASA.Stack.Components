namespace Masa.Stack.Components.UserCenters;

public partial class UpdatePhoneNumberModal : MasaComponentBase
{
    private string? _captchaText;

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public EventCallback<string> OnSuccess { get; set; }

    public UpdateUserPhoneNumberModel UpdateUserPhoneNumber { get; set; } = new();

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
        var field = FormRef.EditContext.Field(nameof(UpdateUserPhoneNumber.PhoneNumber));
        FormRef.EditContext.NotifyFieldChanged(field);
        var result = FormRef.EditContext.GetValidationMessages(field);
        if (result.Any() is false)
        {
            await AuthClient.UserService.SendMsgCodeAsync(new SendMsgCodeModel()
            {
                PhoneNumber = UpdateUserPhoneNumber.PhoneNumber,
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
        FormRef.Reset();
        if (VisibleChanged.HasDelegate)
            await VisibleChanged.InvokeAsync(false);
        else Visible = false;
    }

    private async Task HandleOnOk()
    {
        if (FormRef.Validate())
        {
            var success = await AuthClient.UserService.UpdatePhoneNumberAsync(UpdateUserPhoneNumber);
            if (success)
            {
                if (OnSuccess.HasDelegate)
                    await OnSuccess.InvokeAsync(UpdateUserPhoneNumber.PhoneNumber);

                await PopupService.AlertAsync(T("Modify the phone number successfully"), AlertTypes.Success);
                await HandleOnCancel();
            }
            else
            {
                await PopupService.AlertAsync(T("Modify the phone number failed"), AlertTypes.Error);
            }
        }
    }
}
