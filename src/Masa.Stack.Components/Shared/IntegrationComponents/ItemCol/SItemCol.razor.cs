namespace Masa.Stack.Components;

public partial class SItemCol : MasaComponentBase
{
    [Parameter] public Func<bool, string>? BoolFormatter { get; set; }

    [Parameter] public bool ChippedEnum { get; set; }

    [Parameter] public bool SmallChip { get; set; }

    [Parameter] public Func<Enum, string>? EnumColorFormatter { get; set; }

    [Parameter] public Func<DateTime, bool>? DateTimeValidator { get; set; }

    [Parameter] public bool IgnoreTime { get; set; }

    [Parameter] public string? DateFormat { get; set; }

    [Parameter] public string? TimeFormat { get; set; }

    [Parameter, EditorRequired] public object? Value { get; set; }

    protected object InternalValue { get; set; } = null!;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ArgumentNullException.ThrowIfNull(Value);

        BoolFormatter ??= b => b ? T("Yes") : T("No");
        DateTimeValidator ??= dateTime => dateTime == default;

        InternalValue = Value;
    }

    public string GetColor(Enum @enum)
    {
        if (EnumColorFormatter is not null)
        {
            return EnumColorFormatter.Invoke(@enum);
        }

        return ColorHelper.GetColor((int)(object)@enum);
    }
}
