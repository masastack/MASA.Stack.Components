namespace Masa.Stack.Components;

public partial class SDateTimePicker<TValue>
{
    #region MTextField Parameters

    [Parameter] public bool Clearable { get; set; }
    [Parameter] public bool Dense { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public StringBoolean HideDetails { get; set; }
    [Parameter] public string Label { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClearClick { get; set; }
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }
    [Parameter] public bool Outlined { get; set; }
    [Parameter] public string PrependIcon { get; set; }
    [Parameter] public string PrependInnerIcon { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> Attributes { get; set; } = new();

    #endregion

    #region MDatePicker Parameters

    [Parameter] public bool NoTitle { get; set; }
    [Parameter] public TValue Value { get; set; }
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    #endregion

    [Parameter] public DateTime? DefaultSelectedValue { get; set; }
    [Parameter] public string? Format { get; set; }
    [Parameter] public EventCallback OnOk { get; set; }
    [Parameter] public TimeSpan? TimeZoneOffset { get; set; }

    private bool _menuValue;
    private bool _hourFocused;
    private bool _minuteFocused;
    private bool _secondFocused;

    private DateTime? InternalValue { get; set; }

    private DateTime? DisplayValue { get; set; }

    private string TextFieldValue
    {
        get
        {
            var offsetValue = OffsetValue(InternalValue);

            return offsetValue.HasValue ? offsetValue.Value.ToString(Format) : string.Empty;
        }
    }

    private bool TimeFocused => _hourFocused || _minuteFocused || _secondFocused;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        PrependInnerIcon = "mdi-calendar";

        await base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        DateTime? internalValue = null;

        if (Value is DateTime dateTime && dateTime != DateTime.MinValue)
        {
            internalValue = dateTime;
        }

        InternalValue = internalValue ?? DefaultSelectedValue;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        UpdateDisplay(InternalValue ?? DefaultSelectedValue);

        base.OnParametersSet();
    }

    private void HandleOnCancel()
    {
        _menuValue = false;
    }

    private async Task HandleOnClearClick()
    {
        UpdateInternalAndDisplay(null);

        await UpdateValue(InternalValue);

        if (OnClearClick.HasDelegate)
        {
            await OnClearClick.InvokeAsync();
        }
    }

    private async Task HandleOnOk()
    {
        InternalValue = OffsetValue(DisplayValue, true);

        await UpdateValue(InternalValue);

        _menuValue = false;

        if (OnOk.HasDelegate)
        {
            await OnOk.InvokeAsync();
        }
    }

    private DateTime? OffsetValue(DateTime? utcTime, bool reverseOffset = false)
    {
        if (TimeZoneOffset.HasValue && TimeZoneOffset.Value.TotalMinutes > 0 && utcTime.HasValue)
        {
            if (reverseOffset)
            {
                utcTime = utcTime.Value.Add(TimeSpan.FromMinutes(0 - TimeZoneOffset.Value.TotalMinutes));
            }
            else
            {
                utcTime = utcTime.Value.Add(TimeZoneOffset.Value);
            }
        }

        return utcTime;
    }

    private void OnNow()
    {
        UpdateDisplay(DateTime.UtcNow);
    }

    private void UpdateInternalAndDisplay(DateTime? utcTime)
    {
        InternalValue = utcTime;
        UpdateDisplay(utcTime);
    }

    private void UpdateDisplay(DateTime? utcTime)
    {
        DisplayValue = OffsetValue(utcTime);
    }

    private async Task UpdateValue(DateTime? value)
    {
        var v = default(TValue);

        if (value.HasValue)
        {
            v = (TValue)(object)value;
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(v);
        }
        else
        {
            Value = v;
        }
    }
}
