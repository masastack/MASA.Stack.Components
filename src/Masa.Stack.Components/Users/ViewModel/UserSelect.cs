namespace Masa.Stack.Components;

public class UserSelect : AutoCompleteDocument<Guid>
{
    public new Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string Account { get; set; } = "";

    public string PhoneNumber { get; set; } = "";

    public string Email { get; set; } = "";

    public string Avatar { get; set; } = "";

    public UserSelect(Guid id, string name, string account, string phoneNumber, string email, string avatar)
    {
        Id = id;
        Name = name;
        Account = account;
        PhoneNumber = phoneNumber;
        Email = email;
        Avatar = avatar;
        Value = Id;
        Text = $"{Name},{Account},{PhoneNumber},{Email}";
    }
}

