namespace Masa.Stack.Components.GlobalUsers;

public partial class UserInfo
{
    [Parameter]
    public User? Data { get; set; }

    private bool _idCardAuthVisible;

    private PhoneNumberValidateModal? _phoneNumberValidateModal;
    private EmailValidateModal? _emailValidateModal;

    private User? _prevUser = null;
    private StringNumber _windowValue = 0;

    private IdentityCardAuthentication IdentityCardAuth { get; set; } = new();
    private Dictionary<string, object?>? Items { get; set; }

    private int _userGender;
    private string? _userDisplayName;

    protected override void OnParametersSet()
    {
        if (_prevUser != Data)
        {
            UpdateDefinitionsItems();
        }
    }
    
    private void UpdateDefinitionsItems()
    {
        Items = new Dictionary<string, object?>()
        {
            ["职位"] = ("mdi-briefcase", Data?.Position),
            ["团队"] = ("mdi-account-supervisor", string.Join(" ", Data?.Teams ?? Enumerable.Empty<string>())),
            ["公司"] = ("mdi-office-building", Data?.CompanyName),
            ["国家或地区"] = ("mdi-earth", Data?.Region),
            ["地址"] = ("mdi-map-marker", Data?.Address),
            ["组织"] = ("mdi-file-tree", Data?.Department),
            ["入职时间"] = ("mdi-clock-outline", Data?.CreatedAt?.ToString("yyyy-MM-dd")),
        };
    }

    private async Task HandleOnIdCardAuthSave()
    {
    }

    private void ChangeWindowValue(int val)
    {
        if (val == 0)
        {
            _userGender = Data?.Gender ?? 0;
            _userDisplayName = Data?.DisplayName;
        }

        _windowValue = val;
    }

    private async Task SaveUserInfo()
    {
        // TODO: save _userGender and _userDisplayName
        
        // TODO: validate _userDispalyName
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
