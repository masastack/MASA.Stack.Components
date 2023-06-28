namespace Masa.Stack.Components.Configs;

internal class ProjectAppOptions
{
    public MasaStackProject Project { get; init; }

    public ProjectAppOptions(MasaStackProject project)
    {
        Project = project;
    }
}
