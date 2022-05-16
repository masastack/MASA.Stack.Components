namespace Masa.Stack.Components.UserCenters;

public partial class UserInfo : MasaComponentBase
{
    [Parameter]
    public User? Data { get; set; }

    private EmailValidateModal? _emailValidateModal;
    private IdCardValidateModal? _idCardValidateModal;
    private PhoneNumberValidateModal? _phoneNumberValidateModal;

    private User? _prevUser = null;
    private StringNumber _windowValue = 0;

    private Dictionary<string, object?>? Items { get; set; }

    private int _userGender;
    private string? _userDisplayName;

    protected override void OnParametersSet()
    {
        if (_prevUser != Data)
        {
            _prevUser = Data;
            UpdateDefinitionsItems();
        }
    }

    private void UpdateDefinitionsItems()
    {
        // TODO: no change after i18n changed 
        Items = new Dictionary<string, object?>()
        {
            [T("Position")] = ("mdi-briefcase", Data?.Position),
            [T("Teams")] = ("mdi-account-supervisor", string.Join(" ", Data?.Teams ?? Enumerable.Empty<string>())),
            [T("Company")] = ("mdi-office-building", Data?.CompanyName),
            [T("CountryOrRegion")] = ("mdi-earth", Data?.Region),
            [T("Address")] = ("mdi-map-marker", Data?.Address),
            [T("Department")] = ("mdi-file-tree", Data?.Department),
            [T("CreationTime")] = ("mdi-clock-outline", Data?.CreatedAt?.ToString("yyyy-MM-dd")),
        };
    }

    private void ChangeWindowValue(int val)
    {
        if (val == 1)
        {
            _userGender = Data?.Gender ?? 0;
            _userDisplayName = Data?.DisplayName;
        }

        _windowValue = val;
    }

    private async Task SaveUserInfo()
    {
        // TODO: save _userGender and _userDisplayName

        // TODO: validate _userDisplayName
    }

    private Task OpenPhoneNumberValidateModal(MouseEventArgs _)
    {
        _phoneNumberValidateModal?.Open();
        return Task.CompletedTask;
    }

    private Task OpenEmailValidateModal(MouseEventArgs _)
    {
        _emailValidateModal?.Open();
        return Task.CompletedTask;
    }
}
