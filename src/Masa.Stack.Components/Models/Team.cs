namespace Masa.Stack.Components.Models;

public class Team
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Logo { get; set; }

    public int MemberCount { get; set; }

    public string Role { get; set; }
}
