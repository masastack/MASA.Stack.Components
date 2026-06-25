namespace Masa.Stack.Components.Infrastructure;

internal enum MiniProgramAccessStatus
{
    Unknown,
    Allowed,
    NotFound,
    NotListed,
    Maintenance
}

internal class MiniProgramAccessState
{
    public MiniProgramAccessStatus Status { get; private set; } = MiniProgramAccessStatus.Unknown;

    public ModuleDetailDto? Module { get; private set; }

    public bool IsBlocked => Status is MiniProgramAccessStatus.NotFound
        or MiniProgramAccessStatus.NotListed
        or MiniProgramAccessStatus.Maintenance;

    public void SetFromModule(ModuleDetailDto? module)
    {
        Module = module;

        if (module == null)
        {
            Status = MiniProgramAccessStatus.NotFound;
            return;
        }

        if (module.Status == StatusTypes.Maintenance && module.Maintenance != null)
        {
            Status = MiniProgramAccessStatus.Maintenance;
            return;
        }

        if (!module.Enable)
        {
            Status = MiniProgramAccessStatus.NotListed;
            return;
        }

        Status = MiniProgramAccessStatus.Allowed;
    }
}
