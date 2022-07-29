namespace Masa.Stack.Components.UserCenters;

public partial class StaffInfo : MasaComponentBase
{
    private EmailValidateModal? _emailValidateModalRef;
    private IdCardValidateModal? _idCardValidateModalRef;
    private PhoneNumberValidateModal? _phoneNumberValidateModalRef;
    private int _windowValue = 0;

    public UserModel StaffDetail { get; set; } = new();

    public UpdateUserBasicInfoModel UpdateUser { get; set; } = new();

    private Dictionary<string, object?>? Items { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Items = new Dictionary<string, object?>()
            {
                ["Position"] = ("mdi-briefcase", StaffDetail.Position),
                ["Company"] = ("mdi-office-building", StaffDetail.CompanyName),
                ["Address"] = ("mdi-map-marker", StaffDetail.Address),
                ["Department"] = ("mdi-file-tree", StaffDetail.Department),
                //["CreationTime"] = ("mdi-clock-outline", User.CreatedAt.ToString("yyyy-MM-dd")),
            };
            await GetCurrentUserAsync();        
            StateHasChanged();
        }
    }

    private async Task GetCurrentUserAsync()
    {
        StaffDetail = await AuthClient.UserService.GetCurrentUserAsync();
        UpdateUser = StaffDetail.Adapt<UpdateUserBasicInfoModel>();
    }

    private async Task UpdateBasicInfoAsync()
    {
        await AuthClient.UserService.UpdateBasicInfoAsync(UpdateUser);
        await GetCurrentUserAsync();
        _windowValue = default;
    }

    private void Cancel()
    {
        UpdateUser = StaffDetail.Adapt<UpdateUserBasicInfoModel>();
        _windowValue = default;
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
