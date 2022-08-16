// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class SCopyPassword : STextField<string>
{
    private const string LETTERS = "ABCDEFGHIJKMLNOPQRSTUVWXYZabcdefghigklmnopqrstuvwxyz";
    private const string LETTERNUMBERS = "ABCDEFGHIJKMLNOPQRSTUVWXYZabcdefghigklmnopqrstuvwxyz0123456789";
   
    [Inject]
    public I18n I18n { get; set; } = default!;

    public bool Visible { get; set; }

    public new string Type { get; set; } = "password";

    public override Task SetParametersAsync(ParameterView parameters)
    {
        Label = I18n!.T("Password");
        Readonly = true;
        base.Type = Type;
        AppendContent = builder =>
        {
            if (Type == "text")
            {
                builder.OpenComponent<PCopyableText>(0);
                builder.AddAttribute(1, "Class", "ml-n9");
                builder.AddAttribute(2, "Text", Value);
                builder.CloseComponent();
            }
        };
        return base.SetParametersAsync(parameters);
    }

    public async Task ResetPasswordAsync()
    {
        Type = "text";
        var value = GenerateSpecifiedString(8, true);
        if (ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync(value);
        else Value = value;
    }

    private static string GenerateSpecifiedString(int length, bool includeNumbers = false)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            if (includeNumbers)
            {
                var index = Random.Shared.Next(LETTERNUMBERS.Length);
                sb.Append(LETTERNUMBERS[index]);
            }
            else
            {
                var index = Random.Shared.Next(LETTERS.Length);
                sb.Append(LETTERS[index]);
            }
        }
        return sb.ToString();
    }
}
