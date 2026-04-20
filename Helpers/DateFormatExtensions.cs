namespace EquipmentRental.Helpers;

/// <summary>
/// Uniform date/time formatting helpers — use across all views to replace ad-hoc
/// ToString("yyyy-MM-dd") / ToString("MM-dd") / ToString("yyyy-MM-dd HH:mm") calls.
/// </summary>
public static class DateFormatExtensions
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm";
    public const string DateRangeSeparator = " ~ ";

    public static string ToDateStr(this DateTime dt) =>
        dt.ToLocalTime().ToString(DateFormat);

    public static string ToDateStr(this DateTime? dt) =>
        dt.HasValue ? dt.Value.ToLocalTime().ToString(DateFormat) : "-";

    public static string ToDateStr(this DateOnly d) =>
        d.ToString(DateFormat);

    public static string ToDateStr(this DateOnly? d) =>
        d.HasValue ? d.Value.ToString(DateFormat) : "-";

    public static string ToDateTimeStr(this DateTime dt) =>
        dt.ToLocalTime().ToString(DateTimeFormat);

    public static string ToDateTimeStr(this DateTime? dt) =>
        dt.HasValue ? dt.Value.ToLocalTime().ToString(DateTimeFormat) : "-";

    public static string ToDateRangeStr(this (DateTime Start, DateTime End) r) =>
        $"{r.Start.ToDateStr()}{DateRangeSeparator}{r.End.ToDateStr()}";

    public static string ToDateRangeStr(DateOnly start, DateOnly end) =>
        $"{start.ToDateStr()}{DateRangeSeparator}{end.ToDateStr()}";
}
