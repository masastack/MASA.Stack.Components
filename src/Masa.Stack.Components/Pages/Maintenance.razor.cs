namespace Masa.Stack.Components;

public partial class Maintenance
{
    [Inject]
    private JsInitVariables JsInitVariables { get; set; } = default!;

    private bool HasMaintenanceWindow =>
        TryParseMaintenanceTime(Start, out _) && TryParseMaintenanceTime(End, out _);

    private string StartTimeDisplay => FormatMaintenanceTime(Start);

    private string EndTimeDisplay => FormatMaintenanceTime(End);

    private string FormatMaintenanceTime(string? value)
    {
        if (!TryParseMaintenanceTime(value, out var dateTime))
            return string.Empty;

        return dateTime.ToString(I18n.T("$DateTimeFormat"));
    }

    private bool TryParseMaintenanceTime(string? value, out DateTime dateTime)
    {
        dateTime = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffset))
            return false;

        dateTime = dateTimeOffset.UtcDateTime.Add(JsInitVariables.TimezoneOffset);
        return dateTime != default;
    }
}
