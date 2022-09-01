namespace Masa.Stack.Components.Store;

public class NoticeState : IScopedDependency
{
    public bool IsRead => !Notices.Any(x => !x.IsRead);

    public List<WebsiteMessageModel> Notices
    {
        get => _notices;
        set
        {
            if (_notices != value)
            {
                _notices = value;
                OnNoticeChanged?.Invoke();
            }
        }
    }

    public delegate Task NoticeChanged();

    public event NoticeChanged? OnNoticeChanged;

    private List<WebsiteMessageModel> _notices = new();

    public void SetNotices(List<WebsiteMessageModel> notices)
    {
        Notices = notices;
    }

    public void SetAllRead()
    {
        var notices = Notices.Select(x =>
        {
            x.IsRead = true;
            return x;
        });
        Notices = notices.ToList();
    }
}
