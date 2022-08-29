namespace Masa.Stack.Components;

public class SCheckbox : MCheckbox
{
    [Parameter]
    public string? Tooltip { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        HideDetails = "auto";

        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(Tooltip) && AppendContent == default)
        {
            AppendContent = builder =>
            {
                builder.OpenComponent<SIcon>(0);
                builder.AddAttribute(1, "Tooltip", Tooltip);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(cb => cb.AddContent(3, "mdi-help-circle-outline")));
                builder.AddAttribute(4, "Style", "margin-top: 6px;");
                builder.CloseComponent();
            };
        }
    }
}
