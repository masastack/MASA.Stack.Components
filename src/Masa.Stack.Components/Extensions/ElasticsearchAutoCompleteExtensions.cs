// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Extensions;

public static class ElasticsearchAutoCompleteExtensions
{
    public static void AddElasticsearchAutoComplete(this IServiceCollection services)
    {
        var options = services.BuildServiceProvider()
                              .GetRequiredService<IOptions<UserAutoCompleteOptions>>()
                              .Value;

        var esBuilder = services.AddElasticsearchClient(
                options.Name,
                option => option.UseNodes(options.Nodes).UseDefault()
            );

        esBuilder.AddAutoCompleteBySpecifyDocument<UserSelectModel>(option =>
        {
            option.UseIndexName(options.Index);
            if (string.IsNullOrEmpty(options.Alias) is false) option.UseAlias(options.Alias);
        });
    }
}