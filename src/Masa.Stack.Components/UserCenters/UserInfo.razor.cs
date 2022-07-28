namespace Masa.Stack.Components.UserCenters;

public partial class UserInfo : MasaComponentBase
{
    private EmailValidateModal? _emailValidateModalRef;
    private IdCardValidateModal? _idCardValidateModalRef;
    private PhoneNumberValidateModal? _phoneNumberValidateModalRef;
    private int _windowValue = 0;

    public StaffModel UserDetail { get; set; } = new();

    public UpdateUserBasicInfoModel UpdateUser { get; set; } = new();

    private Dictionary<string, object?>? Items { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            UserDetail = await AuthClient.UserService.GetCurrentUserAsync();
            UpdateUser = UserDetail.Adapt<UpdateUserBasicInfoModel>();
            StateHasChanged();
        }
    }

    private void UpdateDefinitionsItems()
    {
        Items = new Dictionary<string, object?>()
        {
            ["Position"] = ("mdi-briefcase", UserDetail.Position),
            //["Teams"] = ("mdi-account-supervisor", string.Join(" ", Teams.Select(team => team.Name))),
            ["Company"] = ("mdi-office-building", UserDetail.CompanyName),
            //["CountryOrRegion"] = ("mdi-earth", User.Region),
            ["Address"] = ("mdi-map-marker", UserDetail.Address),
            ["Department"] = ("mdi-file-tree", UserDetail.Department),
            //["CreationTime"] = ("mdi-clock-outline", User.CreatedAt.ToString("yyyy-MM-dd")),
        };
    }

    private async Task UpdateBasicInfoAsync()
    {
        await AuthClient.UserService.UpdateBasicInfoAsync(UpdateUser);
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
