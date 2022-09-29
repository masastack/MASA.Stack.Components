
namespace Masa.Stack.Components;

public abstract class MasaComponentBase : ComponentBase
{
    [CascadingParameter]
    public I18n LanguageProvider
    {
        get => _languageProvider ?? throw new Exception("please inject I18n!");
        set => _languageProvider = value;
    }

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public IAuthClient AuthClient { get; set; } = null!;

    [Inject]
    public IPmClient PmClient { get; set; } = null!;

    [Inject]
    public IPopupService PopupService { get; set; } = default!;

    [Inject]
    public DynamicTranslateProvider TranslateProvider { get; set; } = default!;

    private I18n? _languageProvider;

    protected string T(string key)
    {
        return LanguageProvider.T(key);
    }

    protected string DT(string key)
    {
        return TranslateProvider.DT(key);
    }
}