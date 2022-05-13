namespace Masa.Stack.Components;

public class DefaultTextfield<TValue> : MTextField<TValue>
{
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
        if (Action is not null)
        {
            Action.Invoke(InternalAction);

            AppendContent = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "d-flex");
                builder.AddAttribute(2, "style", "margin-top:-7px;margin-right:-12px;height:40px;");
                builder.AddContent(3, subBuilder =>
                {
                    subBuilder.OpenComponent<MDivider>(0);
                    subBuilder.AddAttribute(1, "Vertical", true);
                    subBuilder.CloseComponent();

                    subBuilder.OpenComponent<MButton>(2);
                    subBuilder.AddAttribute(3, "Text", true);
                    subBuilder.AddAttribute(4, "Color", InternalAction.Color);
                    subBuilder.AddAttribute(5, "Style", "border:none;border-bottom-left-radius:0;border-top-left-radius:0;height:100%;");
                    subBuilder.AddAttribute(6, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, InternalAction.OnClick));
                    subBuilder.AddAttribute(7, "ChildContent", (RenderFragment)(cb => cb.AddContent(8, InternalAction.Content)));
                    subBuilder.CloseComponent();
                });
                builder.CloseElement();
            };
        }
    }
}

public class DefaultTextfieldAction
{
    public string Content { get; set; }

    public string Color { get; set; } = "primary";

    public Action<MouseEventArgs> OnClick { get; set; }
}
