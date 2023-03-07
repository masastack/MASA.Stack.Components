using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Masa.Stack.Components.Shared.IntegrationComponents
{
    public class SDeleteButton : SButton
    {
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            Color = "#FF5252";
            if (Class.IsNullOrEmpty())
            {
                Class = " delete-btn";
            }
            Icon = true;
            ChildContent = build =>
            {
                build.OpenComponent<MIcon>(0);
                build.AddAttribute(1, "ChildContent", (RenderFragment)delegate (RenderTreeBuilder builder1)
                {
                    builder1.AddContent(0, "mdi-delete");

                });
                build.CloseComponent();
            };
        }
    }
}
