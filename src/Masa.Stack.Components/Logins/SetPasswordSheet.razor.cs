using FluentValidation;

namespace Masa.Stack.Components;

public partial class SetPasswordSheet : MasaComponentBase
{
    [Inject]
    private IPopupService PopupService { get; set; } = null!;

    [Parameter]
    public string? Account { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    private MForm _form = null!;

    private async Task HandleOnClick()
    {
        await _form.ResetAsync();

        if (OnBack.HasDelegate)
        {
            await OnBack.InvokeAsync();
        }
    }

    private async Task HandleOnValidSubmit()
    {
        if (await _form.ValidateAsync())
        {
            // TODO: 更新密码
            await Task.Delay(1000);

            if (true)
            {
                await PopupService.ToastSuccessAsync("密码更新成功");
                // TODO: 已登录修改密码后需要自动退出，重新登录
            }
        }
    }

    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }

    class SetPasswordValidator : AbstractValidator<SetPasswordSheet>
    {
        public SetPasswordValidator(I18n i18N)
        {
            // TODO: 密码规则补全

            RuleFor(p => p.NewPassword)
                .NotEmpty()
                .WithName(i18N.T("NewPassword"));
            RuleFor(p => p.ConfirmNewPassword)
                .NotEmpty()
                .Equal(p => p.NewPassword).WithMessage(i18N.T("FailToConfirmNewPassword"))
                .WithName(i18N.T("ConfirmNewPassword"));
        }
    }
}
