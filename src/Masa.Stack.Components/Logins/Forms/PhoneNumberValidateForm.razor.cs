using System.Timers;

namespace Masa.Stack.Components.Forms;

public partial class PhoneNumberValidateForm : MasaComponentBase, IDisposable
{
    [Inject]
    private IPopupService PopupService { get; set; } = null!;

    [Parameter]
    public EventCallback<string> OnOk { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Captcha { get; set; }

    private static System.Timers.Timer? _timer;

    private bool _valid;
    private bool _isDirty;
    private int _counter = 0;

    private MForm _form = null!;

    private string ActionContent => _counter > 0 ? $"{_counter}" : T("GetCaptcha");
    private bool ActionDisabled => !_valid || _counter > 0;

    private string? TargetCaptcha { get; set; }

    internal Task ResetFields()
    {
        _counter = 0;
        _isDirty = false;
        _valid = false;
        TargetCaptcha = null;

        return _form.ResetAsync();
    }

    private async Task HandleOnValidSubmit()
    {
        if (!_valid) return;

        if (Captcha != null && Captcha != TargetCaptcha)
        {
            await PopupService.ToastErrorAsync("验证码不正确");
            return;
        }

        if (OnOk.HasDelegate)
        {
            await OnOk.InvokeAsync(PhoneNumber);
        }
    }

    private async Task SendCaptcha()
    {
        _valid = IsValid(PhoneNumber);
        if (!_valid) return;

        _isDirty = true;

        // TODO: send captcha
        await Task.Delay(1000);

        TargetCaptcha = await Task.FromResult("123456");

        if (string.IsNullOrWhiteSpace(TargetCaptcha))
        {
            _isDirty = false;

            await PopupService.AlertAsync("验证码发送失败，请稍后重试。", AlertTypes.Error);
        }
        else
        {
            StartCountdownTimer();

            _isDirty = true;

            await PopupService.AlertAsync("验证码发送成功，请查收。", AlertTypes.Success);
        }
    }

    private void ValidatePhoneNumber(string val)
    {
        // TODO: 验证输入的phoneNumber不是旧的phoneNumber

        // TODO: 正则表达式是否不太严谨，或者有其他需求，需要Auth同学确认
        _valid = IsValid(val);
    }

    private static bool IsValid(string? val)
    {
        if (val is null)
        {
            return false;
        }

        var match = Regex.Match(val, @"^[1]([3-9])[0-9]{9}$");
        return match.Success;
    }

    private void StartCountdownTimer()
    {
        _counter = 60;
        if (_timer is null)
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += CountdownTimer;
        }

        _timer.Enabled = true;
    }

    private void CountdownTimer(object? sender, ElapsedEventArgs e)
    {
        if (_counter > 0)
        {
            _counter -= 1;
        }
        else
        {
            _timer!.Enabled = false;
        }

        InvokeAsync(StateHasChanged);
    }

    class PhoneNumberValidator : AbstractValidator<PhoneNumberValidateForm>
    {
        public PhoneNumberValidator(I18n i18n)
        {
            RuleFor(c => c.PhoneNumber)
                .NotEmpty()
                .Must(IsValid).WithMessage(i18n.T("IncorrectFormat"))
                .WithName(i18n.T("PhoneNumber"));
            RuleFor(c => c.Captcha)
                .NotEmpty()
                .WithName(i18n.T("Captcha"));
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
