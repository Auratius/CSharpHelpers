using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HelpersDemo;

public static class StringExtensions
{
    public static bool HasValue(this string? s) => !string.IsNullOrWhiteSpace(s);

    public static string OrDefault(this string? s, string fallback) => s.HasValue() ? s! : fallback;

    public static string Truncate(this string? value, int maxLength, string suffix = "…")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value ?? "";
        return value[..(maxLength - suffix.Length)] + suffix;
    }

    public static string ToSlug(this string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        var clean = Regex.Replace(sb.ToString().ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        return Regex.Replace(clean, @"\s+", "-").Trim('-');
    }

    public static string MaskEmail(this string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return new string('*', email.Length);
        return email[0] + new string('*', at - 1) + email[at..];
    }

    public static string SafeSubstring(this string s, int start, int length) =>
        string.IsNullOrEmpty(s) || start >= s.Length
            ? string.Empty
            : s.Substring(start, Math.Min(length, s.Length - start));
}

public static class CollectionExtensions
{
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source) => source ?? [];

    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        var batch = new List<T>(size);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == size) { yield return batch; batch = new List<T>(size); }
        }
        if (batch.Count > 0) yield return batch;
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source) action(item);
    }

    public static IEnumerable<T> DistinctByKey<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
            if (seen.Add(keySelector(item))) yield return item;
    }

    public static T RandomItem<T>(this IList<T> list) => list[Random.Shared.Next(list.Count)];
}

public static class DateTimeExtensions
{
    public static int Age(this DateTime birthDate, DateTime? on = null)
    {
        var today = on ?? DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    public static DateTime StartOfDay(this DateTime dt) => dt.Date;
    public static DateTime EndOfDay(this DateTime dt) => dt.Date.AddDays(1).AddTicks(-1);
    public static DateTime StartOfMonth(this DateTime dt) => new(dt.Year, dt.Month, 1);
    public static DateTime EndOfMonth(this DateTime dt) => dt.StartOfMonth().AddMonths(1).AddTicks(-1);

    public static DateTime AddBusinessDays(this DateTime date, int days)
    {
        var direction = Math.Sign(days);
        var remaining = Math.Abs(days);
        while (remaining > 0)
        {
            date = date.AddDays(direction);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                remaining--;
        }
        return date;
    }

    public static string ToRelative(this DateTime dt)
    {
        var span = DateTime.UtcNow - dt.ToUniversalTime();
        return span switch
        {
            { TotalSeconds: < 60 } => "just now",
            { TotalMinutes: < 60 } => $"{(int)span.TotalMinutes}m ago",
            { TotalHours: < 24 } => $"{(int)span.TotalHours}h ago",
            { TotalDays: < 30 } => $"{(int)span.TotalDays}d ago",
            _ => dt.ToString("yyyy-MM-dd")
        };
    }
}

public static class Guard
{
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? name = null)
        where T : class => value ?? throw new ArgumentNullException(name);

    public static string NotNullOrEmpty(string? value, [CallerArgumentExpression(nameof(value))] string? name = null)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Required", name) : value;

    public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? name = null)
        => value <= 0 ? throw new ArgumentOutOfRangeException(name, "Must be > 0") : value;
}

public static class ValidationExtensions
{
    public static bool IsValidEmail(this string s) =>
        !string.IsNullOrWhiteSpace(s) &&
        Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public static bool IsValidUrl(this string s) =>
        Uri.TryCreate(s, UriKind.Absolute, out var u) &&
        (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}

public static class AsyncHelpers
{
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed != task) throw new TimeoutException();
        return await task;
    }

    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        int maxAttempts = 3,
        TimeSpan? initialDelay = null)
    {
        var delay = initialDelay ?? TimeSpan.FromMilliseconds(200);
        for (var attempt = 1; ; attempt++)
        {
            try { return await action(); }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(delay);
                delay *= 2;
            }
        }
    }

    public static void Forget(this Task task, ILogger? logger = null)
    {
        task.ContinueWith(t => logger?.LogError(t.Exception, "Background task failed"),
            TaskContinuationOptions.OnlyOnFaulted);
    }
}

public static class ParseExtensions
{
    public static int? ToIntOrNull(this string? s) => int.TryParse(s, out var v) ? v : null;
    public static decimal? ToDecimalOrNull(this string? s) => decimal.TryParse(s, out var v) ? v : null;
    public static DateTime? ToDateOrNull(this string? s) => DateTime.TryParse(s, out var v) ? v : null;
    public static Guid? ToGuidOrNull(this string? s) => Guid.TryParse(s, out var v) ? v : null;
    public static T? ToEnumOrNull<T>(this string? s) where T : struct, Enum
        => Enum.TryParse<T>(s, true, out var v) ? v : null;
}

public static class FileExtensions
{
    public static string ToHumanSize(this long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes; var i = 0;
        while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
        return $"{size:0.##} {units[i]}";
    }

    public static string ToSafeFilename(this string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    public static void EnsureDirectory(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}

public static class ObjectExtensions
{
    public static T? DeepClone<T>(this T source) =>
        source is null ? default :
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source));

    public static TOut? Then<TIn, TOut>(this TIn? source, Func<TIn, TOut> map)
        where TIn : class => source is null ? default : map(source);
}

public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}

public static class Diagnostics
{
    public static void Trace(string message,
        [CallerMemberName] string? member = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
        => Console.WriteLine($"[{Path.GetFileName(file)}:{line} {member}] {message}");

    public static T Measure<T>(string label, Func<T> action)
    {
        var sw = Stopwatch.StartNew();
        var result = action();
        Console.WriteLine($"{label}: {sw.ElapsedMilliseconds}ms");
        return result;
    }
}
