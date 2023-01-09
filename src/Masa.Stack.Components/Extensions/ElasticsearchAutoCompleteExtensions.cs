// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Extensions;

public static class ElasticsearchAutoCompleteExtensions
{
    public static void AddElasticsearchAutoComplete(this IServiceCollection services, Func<UserAutoCompleteOptions> options)
    {
        var esBuilder = services.AddElasticsearchClient(
                "",
                option =>
                {
                    var autoCompleteOptions = options.Invoke();
                    option.UseNodes(autoCompleteOptions.Nodes);
                },
                true
            );

        esBuilder.AddAutoCompleteBySpecifyDocument<UserSelectModel>(option =>
        {
            var autoCompleteOptions = options.Invoke();
            option.UseIndexName(autoCompleteOptions.Index);
            if (string.IsNullOrEmpty(autoCompleteOptions.Alias) is false)
            {
                option.UseAlias(autoCompleteOptions.Alias);
            }
        });
    }
}