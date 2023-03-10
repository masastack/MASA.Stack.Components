// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Extensions;

public static class ElasticsearchAutoCompleteExtensions
{
    public static void AddElasticsearchAutoComplete(this IServiceCollection services, Func<UserAutoCompleteOptions> options)
    {
        services.AddAutoCompleteBySpecifyDocument<UserSelectModel>("", option =>
        {
            option.UseElasticSearch(esOption =>
            {
                var autoCompleteOptions = options.Invoke();
                esOption.ElasticsearchOptions.UseNodes(autoCompleteOptions.Nodes).UseConnectionSettings(setting => setting.EnableApiVersioningHeader(false));
                esOption.IndexName = autoCompleteOptions.Index;
                if (string.IsNullOrEmpty(autoCompleteOptions.Alias) is false)
                {
                    esOption.Alias = autoCompleteOptions.Alias;
                }
            });

            var autoCompleteFactory = services.BuildServiceProvider().GetRequiredService<IAutoCompleteFactory>();
            var autoCompleteClient = autoCompleteFactory.Create();
            autoCompleteClient.BuildAsync();
        });
    }
}