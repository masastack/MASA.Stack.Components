namespace Masa.Stack.Components.UserCenters.Models;
public class UpdateUserNickNameModel
{
    public string NickName { get; set; }

    public UpdateUserNickNameModel(string nickName)
    {
        NickName = nickName;
    }
}

