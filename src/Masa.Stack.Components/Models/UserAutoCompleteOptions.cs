// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Models;

public class UserAutoCompleteOptions
{
    public string[] Nodes { get; set; } = { };

    public string Alias { get; set; } = "";

    public string Index { get; set; } = "";
}
