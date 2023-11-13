namespace Masa.Stack.Components.Rcl.Shared.Layouts.Components.Modals.Model;

public class UpdatePasswordModel
{
    public string? OldPassword { get; set; }

    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmNewPassword { get; set; } = string.Empty;
}
