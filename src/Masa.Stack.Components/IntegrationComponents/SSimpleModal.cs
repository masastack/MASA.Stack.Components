﻿namespace Masa.Stack.Components;


public class SSimpleModal : PModal
{
    public override Task SetParametersAsync(ParameterView parameters)
    {
        HideActionsDivider = true;
        HideTitleDivider = true;
        HideCancelAction = true;
        Persistent = true;
        Width = 770;

        return base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        Class ??= string.Empty;
        if (!Class.Contains(" rounded-5"))
        {
            Class += " rounded-5";
        }

        HeaderClass ??= string.Empty;
        if (!HeaderClass.Contains(" h6 emphasis--text pa-0"))
        {
            HeaderClass += " h6 emphasis--text pa-0";
        }

        ContentStyle ??= string.Empty;
        if (!ContentStyle.Contains(" padding:36px 17px 36px 42px;"))
        {
            ContentStyle += " padding:36px 17px 36px 42px;";
        }

        BodyClass ??= string.Empty;
        if (!BodyClass.Contains(" pa-0 overflow-y-auto pr-4 mt-6"))
        {
            BodyClass += " pa-0 overflow-y-auto pr-4 mt-6";
        }

        ActionsClass ??= string.Empty;
        if (!ActionsClass.Contains(" pl-0 pb-0 pt-9"))
        {
            ActionsClass += " pl-0 pb-0 pt-9";
        }

        if (TitleContent is null)
        {
            TitleContent = builder =>
            {
                builder.OpenComponent<MIcon>(0);
                builder.AddAttribute(1, "Size", (StringNumber)16);
                builder.AddAttribute(2, "Color", "primary");
                builder.AddAttribute(3, "Class", "pr-2");
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(cb => cb.AddContent(5, "mdi-circle")));
                builder.CloseComponent();
                builder.OpenElement(6, "span");
                builder.AddContent(7, Title);
                builder.CloseElement();
            };
        }

        if (CloseContent is null)
        {
            CloseContent = callback =>
            {
                RenderFragment content = builder =>
                {
                    builder.OpenComponent<MButton>(0);
                    builder.AddAttribute(1, "Fab", true);
                    builder.AddAttribute(2, "Small", true);
                    builder.AddAttribute(3, "Outlined", true);
                    builder.AddAttribute(4, "Color", "emphasis");
                    builder.AddAttribute(5, "Style", "border-color: #E2E7F4;width:32px;height:32px");
                    builder.AddAttribute(6, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, e => callback(e)));
                    builder.AddAttribute(7, "ChildContent", (RenderFragment)(subBuilder =>
                    {
                        subBuilder.OpenComponent<MIcon>(0);
                        subBuilder.AddAttribute(1, "Size", (StringNumber)24);
                        subBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(cb => cb.AddContent(3, "mdi-close")));
                        subBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                };
                return content;
            };
        }

        if (DeleteContent is null)
        {
            DeleteContent = callback =>
            {
                RenderFragment content = builder =>
                {
                    builder.OpenComponent<MIcon>(0);
                    builder.AddAttribute(1, "Size", (StringNumber)24);
                    builder.AddAttribute(2, "Color", "error");
                    builder.AddAttribute(3, "Class", "align-self-center");
                    builder.AddAttribute(4, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, e => callback.Click(e)));
                    builder.AddAttribute(5, "ChildContent", (RenderFragment)(cb => cb.AddContent(6, "mdi-delete")));
                    builder.CloseComponent();
                };
                return content;
            };
        }

        if (SaveContent is null)
        {
            SaveContent = callback =>
            {
                RenderFragment content = builder =>
                {
                    builder.OpenComponent<MButton>(0);
                    builder.AddAttribute(1, "Color", "primary");
                    builder.AddAttribute(2, "Class", "rounded-3");
                    builder.AddAttribute(3, "Style", "min-width: 140px !important;height: 56px !important;");
                    builder.AddAttribute(4, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, e => callback.Click(e)));
                    builder.AddAttribute(5, "ChildContent", (RenderFragment)(cb => cb.AddContent(6, SaveText)));
                    builder.CloseComponent();
                };
                return content;
            };
        }
    }

}