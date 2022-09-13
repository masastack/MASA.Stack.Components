namespace Masa.Stack.Components.UserCenters;

public partial class VerifyEmailModal : MasaComponentBase
{
    private string? _captchaText;

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public EventCallback OnSuccess { get; set; }

    public VerifyMsgCodeModel VerifyMsgCode { get; set; } = new(default, "");

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
        await AuthClient.UserService.SendMsgCodeAsync(new SendMsgCodeModel()
        {
            SendMsgCodeType = SendMsgCodeTypes.VerifiyPhoneNumber
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
            var success = await AuthClient.UserService.VerifyMsgCodeAsync(VerifyMsgCode);
            if (success)
            {
                if (OnSuccess.HasDelegate)
                    await OnSuccess.InvokeAsync();

                await PopupService.AlertAsync(T("Verify the phone number successfully"), AlertTypes.Success);
                await HandleOnCancel();
            }
            else
            {
                await PopupService.AlertAsync(T("Verify the phone number failed"), AlertTypes.Error);
            }
        }
    }
}
