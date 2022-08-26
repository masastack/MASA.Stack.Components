namespace Masa.Stack.Components.UserCenters;

public partial class UserInfo : MasaComponentBase
{
    private EmailValidateModal? _emailValidateModalRef;
    private IdCardValidateModal? _idCardValidateModalRef;
    private PhoneNumberValidateModal? _phoneNumberValidateModalRef;
    private int _windowValue = 0;

    public UserModel UserDetail { get; set; } = new();

    public UpdateUserBasicInfoModel UpdateUser { get; set; } = new();

    public UpdateUserPhoneNumberModel UpdateUserPhoneNumber { get; set; } = new(default, default, default);

    public UpdateUserAvatarModel UpdateUserAvatar { get; set; } = new(default, default);

    private Dictionary<string, object?>? Items { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetCurrentUserAsync();
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
    }

    private async Task UpdateBasicInfoAsync()
    {
        await AuthClient.UserService.UpdateBasicInfoAsync(UpdateUser);
        await GetCurrentUserAsync();
        _windowValue = default;
    }

    private async Task UpdateAvatarAsync(string avatar)
    {
        UpdateUserAvatar.Avatar = avatar;
        await AuthClient.UserService.UpdateUserAvatarAsync(UpdateUserAvatar);
        await GetCurrentUserAsync();
        _windowValue = default;
    }

    private void Cancel()
    {
        UpdateUser = UserDetail.Adapt<UpdateUserBasicInfoModel>();
        _windowValue = default;
    }

    private void PhoneNumberValidateAction(DefaultTextfieldAction action)
    {
        action.Content = UpdateUserPhoneNumber.PhoneNumber is null ? @T("Add") : @T("Change");
        action.Text = true;
        action.OnClick = OpenPhoneNumberValidateModal;
    }

    private void EmailValidateAction(DefaultTextfieldAction action)
    {
        action.Content = UserDetail.Email is null ? @T("Add") : @T("Change");
        action.Text = true;
        action.OnClick = OpenEmailValidateModal;
    }

    private Task OpenPhoneNumberValidateModal(MouseEventArgs _)
    {
        _phoneNumberValidateModalRef?.Open();
        return Task.CompletedTask;
    }

    private Task OpenEmailValidateModal(MouseEventArgs _)
    {
        _emailValidateModalRef?.Open();
        return Task.CompletedTask;
    }
}
