namespace Masa.Stack.Components.Rcl.Configs;

public class ProjectAppOptions
{
    public MasaStackProject Project { get; init; }

    public ProjectAppOptions(MasaStackProject project)
    {
        Project = project;
    }
}
