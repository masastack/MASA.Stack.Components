namespace Masa.Stack.Components;

public class UserSelectModel : AutoCompleteDocument<Guid>
{
    public Guid UserId { get; set; }

    public string? DisplayName { get; set; }

    public string? Name { get; set; }

    public string DefaultName => DisplayName ?? Name ?? "";

    public string Account { get; set; } = "";

    public string PhoneNumber { get; set; } = "";

    public string Email { get; set; } = "";

    public string Avatar { get; set; } = "";

    public UserSelectModel(Guid userId, string name, string account, string phoneNumber, string email, string avatar)
    {
        Id = userId.ToString();
        Name = name;
        Account = account;
        PhoneNumber = phoneNumber;
        Email = email;
        Avatar = avatar;
        Value = userId;
        Text = $"{Name},{Account},{PhoneNumber},{Email}";
    }
}

