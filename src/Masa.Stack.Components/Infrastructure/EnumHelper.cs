namespace Masa.Stack.Components.Infrastructure;

public static class EnumHelper
{
    public static bool TryParse<TEnum>(string? rawValue, out TEnum enumValue) where TEnum : struct, Enum
    {
        enumValue = default;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (Enum.TryParse(rawValue, true, out enumValue))
        {
            return true;
        }

        if (int.TryParse(rawValue, out var intValue) && Enum.IsDefined(typeof(TEnum), intValue))
        {
            enumValue = (TEnum)Enum.ToObject(typeof(TEnum), intValue);
            return true;
        }

        return false;
    }
}
