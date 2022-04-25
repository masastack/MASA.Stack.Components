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

    public List<UserSelect> UserSelect { get; set; } = new();

    public string Search { get; set; } = "";

    [Inject]
    public IAutoCompleteClient AutoCompleteClient
    {
        get => _autocompleteClient ?? throw new Exception("Please inject IAutoCompleteClient");
        set => _autocompleteClient = value;
    }

    public async Task OnSearchChanged(string search)
    {
        Search = search;
        await Task.Delay(500);
        if (Search == "")
        {
            UserSelect.Clear();
        }
        else if (Search == search)
        {
            var response = await AutoCompleteClient.GetAsync<UserSelect, Guid>(search, new AutoCompleteOptions
            {
                Page = Page,
                PageSize = PageSize,
            });
            UserSelect = response.Data;
        }
    }

    public string TextView(UserSelect user)
    {
        if (string.IsNullOrEmpty(user.Name) is false) return user.Name;
        if (string.IsNullOrEmpty(user.Account) is false) return user.Account;
        if (string.IsNullOrEmpty(user.PhoneNumber) is false) return user.PhoneNumber;
        if (string.IsNullOrEmpty(user.Email) is false) return user.Email;
        return "";
    }
}

