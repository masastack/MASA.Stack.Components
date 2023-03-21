﻿namespace Masa.Stack.Components.UserCenters;

public partial class UserInfo : MasaComponentBase
{
    private int _windowValue = 0;

    public UserModel UserDetail { get; set; } = new();

    public UpdateUserBasicInfoModel UpdateUser { get; set; } = new();

    public UpdateUserAvatarModel UpdateUserAvatar { get; set; } = new();

    private Dictionary<string, object?>? Items { get; set; }

    private Dictionary<string, object?>? PreviewItems { get; set; }

    private bool IsStaff => UserDetail.StaffId is not null;

    private bool UpdateUserPhoneNumberDialogVisible { get; set; }

    private bool VerifyUserPhoneNumberDialogVisible { get; set; }

    private bool UpdateUserEmailDialogVisible { get; set; }

    private bool VerifyUserEmailDialogVisible { get; set; }

    private SLabeledRadioGroup<GenderTypes> RefLabeledRadioGroup { get; set; }

    private string _i18nName = null!;

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
        UpdateUserAvatar = new(default, UserDetail.Avatar);

        Items = new Dictionary<string, object?>()
        {
            ["CreationTime"] = ("mdi-clock-outline", UserDetail.CreationTime.ToString("yyyy-MM-dd")),
        };

        PreviewItems = new Dictionary<string, object?>()
        {
            ["PhoneNumber"] = ("mdi-phone", UserDetail.PhoneNumber),
            ["Email"] = ("mdi-email", UserDetail.Email),
            ["Position"] = ("mdi-briefcase", UserDetail.Position),
            ["Company"] = ("mdi-office-building", UserDetail.CompanyName),
            ["Address"] = ("mdi-map-marker", UserDetail.Address.ToString()),
            ["Department"] = ("mdi-file-tree", UserDetail.Department),
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
        action.Content = string.IsNullOrEmpty(UserDetail.PhoneNumber) ? @T("Add") : @T("Change");
        action.Text = true;
        action.OnClick = string.IsNullOrEmpty(UserDetail.PhoneNumber) ? OpenUpdatePhoneNumberModal : OpenVerifyPhoneNumberModal;
    }

    private void EmailValidateAction(DefaultTextfieldAction action)
    {
        action.Content = string.IsNullOrEmpty(UserDetail.Email) ? @T("Add") : @T("Change");
        action.Text = true;
        action.Disabled = true;//todo
        action.OnClick = string.IsNullOrEmpty(UserDetail.Email) ? OpenUpdateEmailModal : OpenVerifyEmailModal;
    }

    private Task OpenVerifyPhoneNumberModal(MouseEventArgs _)
    {
        VerifyUserPhoneNumberDialogVisible = true;
        return Task.CompletedTask;
    }

    private Task OpenUpdatePhoneNumberModal(MouseEventArgs _)
    {
        UpdateUserPhoneNumberDialogVisible = true;
        return Task.CompletedTask;
    }

    private Task OpenVerifyEmailModal(MouseEventArgs _)
    {
        VerifyUserEmailDialogVisible = true;
        return Task.CompletedTask;
    }

    private Task OpenUpdateEmailModal(MouseEventArgs _)
    {
        UpdateUserEmailDialogVisible = true;
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

    private void OnVerifyEmailSuccess()
    {
        UpdateUserEmailDialogVisible = true;
    }

    private async Task OnUpdateEmailSuccess(string email)
    {
        await GetCurrentUserAsync();
    }

    protected override Task OnParametersSetAsync()
    {
        _i18nName ??= LanguageProvider.Culture.Name;

        if(_i18nName != LanguageProvider.Culture.Name)
        {
            _i18nName = LanguageProvider.Culture.Name;
            RefLabeledRadioGroup?.CallSlider();
        }
        
        return base.OnParametersSetAsync();
    }
}
