namespace Masa.Stack.Components
{
    public class SDefaultAutoComplete<TItem, TItemValue, TValue> : MAutocomplete<TItem, TItemValue, TValue>
    {
        [Parameter]
        public bool Required { get; set; }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            Dense = true;
            HideDetails = true;
            Outlined = true;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (Required && PrependInnerContent == default)
            {
                PrependInnerContent = builder =>
                {
                    builder.OpenElement(0, "label");
                    builder.AddAttribute(1, "class", "red--text");
                    builder.AddContent(2, "*");
                    builder.CloseElement();
                };
            }
        }
    }
}
