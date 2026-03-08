using Kakeibo.Api.Common.Abstractions;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Common.Utils;

/// <summary>
/// Centralizes NodaTime ISO 8601 parsing to eliminate the repeated try/parse pattern
/// across handlers. Returns a Result so callers can propagate validation errors uniformly.
/// </summary>
public static class NodaTimeParser
{
    /// <summary>
    /// Parses an ISO 8601 date string (YYYY-MM-DD) into a <see cref="LocalDate"/>.
    /// Returns <see cref="Error.Validation"/> when the format is invalid.
    /// </summary>
    public static Result<LocalDate> ParseLocalDate(string value, string fieldName)
    {
        var result = LocalDatePattern.Iso.Parse(value);
        return result.Success
            ? result.Value
            : Error.Validation($"Invalid {fieldName} format. Expected ISO 8601 (YYYY-MM-DD).");
    }
}
