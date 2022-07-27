namespace Masa.Stack.Components.IntegrationComponents;

public class SSimpleModal : SModal
{
    protected override ModalButtonProps GetDefaultCancelButtonProps()
    {
        var props = base.GetDefaultCancelButtonProps();
        props.Text = false;
        props.Rounded = true;
        props.Style = "width:100px";

        return props;
    }
}
