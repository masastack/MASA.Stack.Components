namespace Masa.Stack.Components;
public partial class SUserAutoComplete
{
    IAutoCompleteClient? _autocompleteClient;

    [Parameter]
    public Guid Value { get; set; }

    [Parameter]
    public EventCallback<Guid> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<UserSelectModel> OnSelectedItemUpdate { get; set; }

    [Parameter]
    public int Page { get; set; } = 1;

    [Parameter]
    public int PageSize { get; set; } = 10;

    [Parameter]
    public string Class { get; set; } = "";

    [Parameter]
    public string Style { get; set; } = "";

    [Parameter]
    public bool FillBackground { get; set; } = true;

    [Parameter]
    public bool Outlined { get; set; }

    [Parameter]
    public bool Solo { get; set; } = true;

    [Parameter]
    public bool Dense { get; set; } = true;

    [Parameter]
    public bool Flat { get; set; } = true;

    [Parameter]
    public string? Label { get; set; }

    public string I18nLabel => Label is null ? T("Search") : Label;

    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    [Parameter]
    public bool PersistentPlaceholder { get; set; }

    [Parameter]
    public bool Clearable { get; set; }

    [Parameter]
    public bool Small { get; set; }

    [Parameter]
    public bool Medium { get; set; }

    [Parameter]
    public bool Large { get; set; }

    public List<UserSelectModel> UserSelect { get; set; } = new();

    public string Search { get; set; } = "";

    [Inject]
    public IAutoCompleteClient AutoCompleteClient
    {
        get => _autocompleteClient ?? throw new Exception("Please inject IAutoCompleteClient");
        set => _autocompleteClient = value;
    }

    private readonly AsyncTaskQueue _asyncTaskQueue;

    public SUserAutoComplete()
    {
        _asyncTaskQueue = new AsyncTaskQueue
        {
            AutoCancelPreviousTask = true,
            UseSingleThread = true
        };
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!UserSelect.Any() && Value != default)
        {
            await InitUserSelect();
        }

        base.OnParametersSet();
    }

    private async Task InitUserSelect()
    {
        var user = await AuthClient.UserService.GetByIdAsync(Value);
        if (user == null)
            return;

        UserSelect = new List<UserSelectModel>() { new UserSelectModel(user.Id, user.Name ?? string.Empty, user.DisplayName, user.Account, user.PhoneNumber ?? string.Empty, user.Email ?? string.Empty, user.Avatar) };

        StateHasChanged();
    }

    public async Task OnSearchChanged(string search)
    {
        search = search.TrimStart(' ').TrimEnd(' ');
        Search = search;
        if (Search == "")
        {
            UserSelect.Clear();
        }
        else
        {
            var result = await _asyncTaskQueue.ExecuteAsync(async () =>
            {
                var response = await AutoCompleteClient.GetBySpecifyDocumentAsync<UserSelectModel>(search, new AutoCompleteOptions
                {
                    Page = Page,
                    PageSize = PageSize,
                });
                return response;
            });
            if (result.IsValid)
            {
                UserSelect = result.result.Data;
            }
        }
    }

    public string TextView(UserSelectModel user)
    {
        if (string.IsNullOrEmpty(user.DisplayName) is false) return user.DisplayName;
        if (string.IsNullOrEmpty(user.Name) is false) return user.Name;
        if (string.IsNullOrEmpty(user.Account) is false) return user.Account;
        if (string.IsNullOrEmpty(user.PhoneNumber) is false) return user.PhoneNumber;
        if (string.IsNullOrEmpty(user.Email) is false) return user.Email;
        return "";
    }
}

