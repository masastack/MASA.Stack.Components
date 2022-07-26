namespace Masa.Stack.Components;

public partial class UserAutoComplete
{
    IAutoCompleteClient? _autocompleteClient;

    [Parameter]
    public Guid Value { get; set; }

    [Parameter]
    public EventCallback<Guid> ValueChanged { get; set; }

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
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    [Parameter]
    public bool PersistentPlaceholder { get; set; }

    public List<UserSelectModel> UserSelect { get; set; } = new();

    public string Search { get; set; } = "";

    protected override Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            Label = @T("Search");
        }

        return base.OnInitializedAsync();
    }

    [Inject]
    public IAutoCompleteClient AutoCompleteClient
    {
        get => _autocompleteClient ?? throw new Exception("Please inject IAutoCompleteClient");
        set => _autocompleteClient = value;
    }

    public async Task OnSearchChanged(string search)
    {
        search = search.TrimStart(' ').TrimEnd(' ');
        Search = search;
        await Task.Delay(300);
        if (Search == "")
        {
            UserSelect.Clear();
        }
        else if (Search == search)
        {
            var response = await AutoCompleteClient.GetAsync<UserSelectModel, Guid>(search, new AutoCompleteOptions
            {
                Page = Page,
                PageSize = PageSize,
            });
            UserSelect = response.Data;
        }
    }

    public string TextView(UserSelectModel user)
    {
        if (string.IsNullOrEmpty(user.Name) is false) return user.Name;
        if (string.IsNullOrEmpty(user.Account) is false) return user.Account;
        if (string.IsNullOrEmpty(user.PhoneNumber) is false) return user.PhoneNumber;
        if (string.IsNullOrEmpty(user.Email) is false) return user.Email;
        return "";
    }
}

