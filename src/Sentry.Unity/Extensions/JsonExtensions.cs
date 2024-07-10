using System;
using System.Text.Json;

namespace Sentry.Unity.Extensions;

internal static class JsonExtensions
{
    // From Sentry.Internal.Extensions.JsonExtensions
    public static JsonElement? GetPropertyOrNull(this JsonElement json, string name)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (json.TryGetProperty(name, out var result))
        {
            if (json.ValueKind == JsonValueKind.Undefined ||
                json.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return result;
        }

        return null;
    }

    public static TEnum? GetEnumOrNull<TEnum>(this JsonElement json, string name)
        where TEnum : struct
    {
        var enumString = json.GetPropertyOrNull(name)?.ToString();
        if (string.IsNullOrWhiteSpace(enumString))
        {
            return null;
        }

        if (!Enum.TryParse(enumString, true, out TEnum value))
        {
            return null;
        }

        return value;
    }
}
