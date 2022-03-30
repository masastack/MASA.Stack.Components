using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Web;

namespace Masa.Stack.Components;

public class AutoLoadingButton : MButton
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        
        var originalOnClick  = OnClick;
        
        if (OnClick.HasDelegate)
        {
            OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async (args) =>
            {
                Loading = true;
                
                await originalOnClick.InvokeAsync(args);

                Loading = false;
                
                StateHasChanged();
            });
        }
    }
}