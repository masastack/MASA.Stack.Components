namespace Masa.Stack.Components.Infrastructure;

/// <summary>
/// 记录项目自身多语言文件(<see cref="Options.MasaStackComponentOptions.I18nDirectoryPath"/>)的加载任务。
/// 加载是在 <c>AddMasaStackComponent</c> 中异步启动的,需要在应用启动阶段等待其完成,
/// 否则首屏渲染时 <see cref="Masa.Blazor.I18n"/> 取到的 Locale 快照里没有项目自身的翻译,页面会显示 key。
/// </summary>
internal sealed class I18nLoadingState
{
    public I18nLoadingState(Task loadingTask)
    {
        LoadingTask = loadingTask;
    }

    public Task LoadingTask { get; }
}
