namespace Masa.Stack.Components.UserCenters;

public partial class UserInfo : MasaComponentBase
{
    private EmailValidateModal? _emailValidateModalRef;
    private IdCardValidateModal? _idCardValidateModalRef;
    private int _windowValue = 0;

    public UserModel UserDetail { get; set; } = new();

    public UpdateUserBasicInfoModel UpdateUser { get; set; } = new();

    public UpdateUserPhoneNumberModel UpdateUserPhoneNumber { get; set; } = new();

    public UpdateUserAvatarModel UpdateUserAvatar { get; set; } = new();

    private Dictionary<string, object?>? Items { get; set; }

    private bool IsStaff { get; set; }

    private bool UpdateUserPhoneNumberDialogVisible { get; set; }

    private bool VerifyUserPhoneNumberDialogVisible { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetCurrentUserAsync();
            IsStaff = (await AuthClient.UserService.GetCurrentStaffAsync()) is not null;
            StateHasChanged();
        }
    }

    private async Task GetCurrentUserAsync()
    {
        UserDetail = await AuthClient.UserService.GetCurrentUserAsync();
        UpdateUser = UserDetail.Adapt<UpdateUserBasicInfoModel>();
        UpdateUserPhoneNumber = new (default, UserDetail.PhoneNumber, "");
        UpdateUserAvatar = new (default, UserDetail.Avatar);

        Items = new Dictionary<string, object?>()
        {
            ["Position"] = ("mdi-briefcase", UserDetail.Position),
            ["Company"] = ("mdi-office-building", UserDetail.CompanyName),
            ["Address"] = ("mdi-map-marker", UserDetail.Address.Address),
            ["Department"] = ("mdi-file-tree", UserDetail.Department),
            ["CreationTime"] = ("mdi-clock-outline", UserDetail.CreationTime.ToString("yyyy-MM-dd")),
        };
        _windowValue = default;
    }

    private async Task UpdateBasicInfoAsync()
    {
        await AuthClient.UserService.UpdateBasicInfoAsync(UpdateUser);
        await GetCurrentUserAsync();
    }

    private async Task UpdateAvatarAsync(string avatar)
    {
        UpdateUserAvatar.Avatar = avatar;
        await AuthClient.UserService.UpdateUserAvatarAsync(UpdateUserAvatar);
        await GetCurrentUserAsync();
    }

    private void Cancel()
    {
        UpdateUser = UserDetail.Adapt<UpdateUserBasicInfoModel>();
        _windowValue = default;
    }

    private void NavigateToStaff()
    {
        NavigationManager.NavigateTo("/user-center/staff");
    }

    private void PhoneNumberValidateAction(DefaultTextfieldAction action)
    {
        action.Content = UpdateUserPhoneNumber.PhoneNumber is null ? @T("Add") : @T("Change");
        action.Text = true;
        action.OnClick = OpenVerifyPhoneNumberModal;
    }

    private void EmailValidateAction(DefaultTextfieldAction action)
    {
        action.Content = UserDetail.Email is null ? @T("Add") : @T("Change");
        action.Text = true;
        action.OnClick = OpenEmailValidateModal;
    }

    private Task OpenVerifyPhoneNumberModal(MouseEventArgs _)
    {
        VerifyUserPhoneNumberDialogVisible = true;
        return Task.CompletedTask;
    }

    private Task OpenEmailValidateModal(MouseEventArgs _)
    {
        _emailValidateModalRef?.Open();
        return Task.CompletedTask;
    }

    private void OnVerifyPhoneNumberSuccess()
    {
        UpdateUserPhoneNumberDialogVisible = true;
    }

    private async Task OnUpdatePhoneNumberSuccess(string phoneNumber)
    {
        await GetCurrentUserAsync();
    }
}
