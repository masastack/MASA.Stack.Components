namespace Masa.Stack.Components;

public class DefaultTextField<TValue> : MTextField<TValue>
{
    [Parameter]
    public bool Small { get; set; }

    [Parameter]
    public bool Medium { get; set; }

    [Parameter]
    public bool Large { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter] public Action<DefaultTextfieldAction>? Action { get; set; }

    private DefaultTextfieldAction InternalAction { get; set; } = new();

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Dense = true;
        HideDetails = "auto";
        Outlined = true;

        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Class ??= "";
        if (Large is false && Small is false) Medium = true;
        if (Dense is true)
        {
            if (Large)
            {
                Height = 56;
                if (Class.Contains("m-input--dense-56") is false)
                {
                    Class += " m-input--dense-56";
                }
            }
            else if (Medium)
            {
                Height = 48;
                if (Class.Contains("m-input--dense-48") is false)
                {
                    Class += " m-input--dense-48";
                }
            }
            else if (Small)
            {
                Height = 40;
                if (Class.Contains("m-input--dense-40") is false)
                {
                    Class += " m-input--dense-40";
                }
            }
        }

        if (Action is not null)
        {
            Action.Invoke(InternalAction);

            AppendContent = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "d-flex");
                builder.AddAttribute(2, "style", $"margin-top:-8px;margin-right:-12px;height:{Height}px;");
                builder.AddContent(3, subBuilder =>
                {
                    subBuilder.OpenComponent<MDivider>(0);
                    subBuilder.AddAttribute(1, "Vertical", true);
                    subBuilder.CloseComponent();

                    subBuilder.OpenComponent<AutoLoadingButton>(2);
                    subBuilder.AddAttribute(3, "Text", InternalAction.Text);
                    subBuilder.AddAttribute(4, "Disabled", InternalAction.Disabled);
                    subBuilder.AddAttribute(5, "Color", InternalAction.Color);
                    subBuilder.AddAttribute(6, "Style", "border:none;border-radius: 0 8px 8px 0;;height:100%;");
                    subBuilder.AddAttribute(7, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, InternalAction.OnClick));
                    subBuilder.AddAttribute(8, "ChildContent", (RenderFragment)(cb => cb.AddContent(9, InternalAction.Content)));
                    subBuilder.CloseComponent();
                });
                builder.CloseElement();
            };
        }

        if (Required && PrependInnerContent == default)
        {
            PrependInnerContent = builder =>
            {
                builder.OpenElement(0, "label");
                builder.AddAttribute(1, "class", "red--text");
                builder.AddContent(2, "*");
                builder.CloseElement();
            };
        }
    }
}

public class DefaultTextfieldAction
{
    public string Content { get; set; }

    public string Color { get; set; } = "primary";

    public bool Disabled { get; set; }

    public bool Text { get; set; }

    public Func<MouseEventArgs, Task> OnClick { get; set; }
}
