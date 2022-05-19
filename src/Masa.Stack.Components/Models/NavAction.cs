namespace Masa.Stack.Components.Models;

public class NavAction
{
    public string Code { get; set; }
    
    public string Name { get; set; }

    public NavAction(string code, string name)
    {
        Code = code;
        Name = name;
    }
}
