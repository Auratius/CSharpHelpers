using HelpersDemo;
using Microsoft.Extensions.Logging.Abstractions;

// A walkthrough that exercises every helper from csharp-helper-functions.md.
// Scenario: import a batch of would-be users, validate them, persist a report, and clone
// records into an audit trail — touching strings, collections, dates, validation, async,
// parsing, file I/O, object cloning, Result<T>, and diagnostics along the way.

Section("1. String helpers");

string? blank = "   ";
string? name = "Renée Jürgenssen";
Console.WriteLine($"HasValue(blank)              = {blank.HasValue()}");
Console.WriteLine($"HasValue(name)               = {name.HasValue()}");
Console.WriteLine($"OrDefault(blank, fallback)   = {blank.OrDefault("Anonymous")}");
Console.WriteLine($"Truncate                     = {"The quick brown fox jumps over the lazy dog".Truncate(20)}");
Console.WriteLine($"ToSlug                       = {name!.ToSlug()}");
Console.WriteLine($"MaskEmail                    = {"jane.doe@example.com".MaskEmail()}");
Console.WriteLine($"SafeSubstring (out of range) = '{"hi".SafeSubstring(10, 5)}'");
Console.WriteLine($"SafeSubstring (clipped)      = '{"hello".SafeSubstring(2, 100)}'");

Section("2. Collection & LINQ helpers");

IEnumerable<int>? maybeNull = null;
Console.WriteLine($"OrEmpty(null) count          = {maybeNull.OrEmpty().Count()}");

var ids = Enumerable.Range(1, 7).ToList();
Console.WriteLine("Batch(3):");
foreach (var chunk in ids.Batch(3))
    Console.WriteLine($"  [{string.Join(", ", chunk)}]");

Console.Write("ForEach print: ");
new[] { "a", "b", "c" }.ForEach(x => Console.Write($"{x} "));
Console.WriteLine();

var dupes = new[]
{
    new { Email = "a@x.com", Name = "Al" },
    new { Email = "a@x.com", Name = "Alice" },
    new { Email = "b@x.com", Name = "Bob" },
};
Console.WriteLine("DistinctByKey(Email):");
dupes.DistinctByKey(d => d.Email).ForEach(d => Console.WriteLine($"  {d.Email} -> {d.Name}"));

var picks = new List<string> { "red", "green", "blue", "yellow" };
Console.WriteLine($"RandomItem                   = {picks.RandomItem()}");

Section("3. DateTime helpers");

var birth = new DateTime(1990, 6, 15);
var today = new DateTime(2026, 4, 27);
Console.WriteLine($"Age(birth, today)            = {birth.Age(today)}");
Console.WriteLine($"StartOfDay                   = {today.StartOfDay():o}");
Console.WriteLine($"EndOfDay                     = {today.EndOfDay():o}");
Console.WriteLine($"StartOfMonth                 = {today.StartOfMonth():yyyy-MM-dd}");
Console.WriteLine($"EndOfMonth                   = {today.EndOfMonth():yyyy-MM-dd HH:mm:ss.fffffff}");
Console.WriteLine($"AddBusinessDays(+5) from Fri = {new DateTime(2026, 4, 24).AddBusinessDays(5):yyyy-MM-dd ddd}");
Console.WriteLine($"AddBusinessDays(-3) from Mon = {new DateTime(2026, 4, 27).AddBusinessDays(-3):yyyy-MM-dd ddd}");
Console.WriteLine($"ToRelative(2 min ago)        = {DateTime.UtcNow.AddMinutes(-2).ToRelative()}");
Console.WriteLine($"ToRelative(5 hours ago)      = {DateTime.UtcNow.AddHours(-5).ToRelative()}");
Console.WriteLine($"ToRelative(90 days ago)      = {DateTime.UtcNow.AddDays(-90).ToRelative()}");

Section("4. Validation helpers (and Guard)");

Console.WriteLine($"IsValidEmail('a@b.co')       = {"a@b.co".IsValidEmail()}");
Console.WriteLine($"IsValidEmail('nope')         = {"nope".IsValidEmail()}");
Console.WriteLine($"IsValidUrl('https://x.com')  = {"https://x.com".IsValidUrl()}");
Console.WriteLine($"IsValidUrl('ftp://x.com')    = {"ftp://x.com".IsValidUrl()}");

try { Guard.NotNull<string>(null); }
catch (ArgumentNullException ex) { Console.WriteLine($"Guard.NotNull caught         = {ex.ParamName}"); }

try { Guard.NotNullOrEmpty(""); }
catch (ArgumentException ex) { Console.WriteLine($"Guard.NotNullOrEmpty caught  = {ex.ParamName}"); }

try { Guard.Positive(-1); }
catch (ArgumentOutOfRangeException ex) { Console.WriteLine($"Guard.Positive caught        = {ex.ParamName}"); }

Section("5. Async / Task helpers");

var fast = await AsyncHelpers.RetryAsync(async () =>
{
    await Task.Delay(10);
    return 42;
});
Console.WriteLine($"RetryAsync (succeeds)        = {fast}");

var attempts = 0;
var eventually = await AsyncHelpers.RetryAsync(async () =>
{
    attempts++;
    await Task.Delay(5);
    if (attempts < 3) throw new InvalidOperationException("flaky");
    return "ok";
}, maxAttempts: 5, initialDelay: TimeSpan.FromMilliseconds(10));
Console.WriteLine($"RetryAsync (3rd try)         = {eventually} (attempts={attempts})");

var quick = Task.Run(async () => { await Task.Delay(20); return "done"; });
Console.WriteLine($"WithTimeout (in time)        = {await quick.WithTimeout(TimeSpan.FromSeconds(1))}");

try
{
    var slow = Task.Run(async () => { await Task.Delay(500); return "late"; });
    await slow.WithTimeout(TimeSpan.FromMilliseconds(50));
}
catch (TimeoutException)
{
    Console.WriteLine("WithTimeout (too slow)       = TimeoutException raised");
}

Task.Run(() => throw new InvalidOperationException("background boom"))
    .Forget(NullLogger.Instance);
Console.WriteLine("Forget                       = scheduled (errors swallowed by logger)");

Section("6. Parsing & conversion helpers");

Console.WriteLine($"ToIntOrNull('42')            = {"42".ToIntOrNull()}");
Console.WriteLine($"ToIntOrNull('nope')          = {"nope".ToIntOrNull()?.ToString() ?? "null"}");
var sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
Console.WriteLine($"ToDecimalOrNull('3{sep}14')      = {$"3{sep}14".ToDecimalOrNull()}");
Console.WriteLine($"ToDateOrNull('2026-04-27')   = {"2026-04-27".ToDateOrNull():yyyy-MM-dd}");
Console.WriteLine($"ToGuidOrNull(new())          = {Guid.NewGuid().ToString().ToGuidOrNull()}");
Console.WriteLine($"ToEnumOrNull<DayOfWeek>      = {"friday".ToEnumOrNull<DayOfWeek>()}");

Section("7. File & I/O helpers");

Console.WriteLine($"ToHumanSize(1023)            = {1023L.ToHumanSize()}");
Console.WriteLine($"ToHumanSize(1536)            = {1536L.ToHumanSize()}");
Console.WriteLine($"ToHumanSize(5_500_000_000)   = {5_500_000_000L.ToHumanSize()}");
Console.WriteLine($"ToSafeFilename               = {"report:final?/v2*.txt".ToSafeFilename()}");

var outDir = Path.Combine(Path.GetTempPath(), "HelpersDemo", "reports");
var outPath = Path.Combine(outDir, $"signup-{DateTime.UtcNow:yyyyMMddHHmmss}.txt".ToSafeFilename());
FileExtensions.EnsureDirectory(outPath);
File.WriteAllText(outPath, "report body");
Console.WriteLine($"EnsureDirectory + write to   = {outPath}");
Console.WriteLine($"File size                    = {new FileInfo(outPath).Length.ToHumanSize()}");

Section("8. Object helpers");

var original = new User("Jane", "jane@example.com", new Profile("Ms.", "Engineer"));
var clone = original.DeepClone()!;
clone = clone with { Profile = clone.Profile! with { Title = "Dr." } };
Console.WriteLine($"DeepClone original.Title     = {original.Profile!.Title}");
Console.WriteLine($"DeepClone modified.Title     = {clone.Profile!.Title}");

User? maybeUser = original;
var displayed = maybeUser.Then(u => u.Profile).Then(p => p.Title) ?? "Anonymous";
Console.WriteLine($"Then pipeline                = {displayed}");

User? noUser = null;
var fallback = noUser.Then(u => u.Profile).Then(p => p.Title) ?? "Anonymous";
Console.WriteLine($"Then pipeline (null)         = {fallback}");

Section("9. Result<T>");

Console.WriteLine($"FindUser(1)                  = {Format(FindUser(1))}");
Console.WriteLine($"FindUser(99)                 = {Format(FindUser(99))}");

Section("10. Debugging & logging helpers");

Diagnostics.Trace("hello from main");
var sum = Diagnostics.Measure("sum 1..1_000_000", () =>
{
    long total = 0;
    for (var i = 1; i <= 1_000_000; i++) total += i;
    return total;
});
Console.WriteLine($"Measure result               = {sum}");

Section("End-to-end: signup pipeline");

var raw = new[]
{
    "  jane.doe@example.com  ",
    "BAD EMAIL",
    "john@example.com",
    "jane.doe@example.com",
    "",
    "alex@example.com",
};

var processed = raw
    .Select(r => r?.Trim())
    .Where(r => r.HasValue())
    .Select(r => r!)
    .DistinctByKey(r => r.ToLowerInvariant())
    .Select(email => email.IsValidEmail()
        ? Result<string>.Ok(email)
        : Result<string>.Fail($"invalid email: {email}"))
    .ToList();

foreach (var batch in processed.Batch(2))
{
    Console.WriteLine("-- batch --");
    batch.ForEach(r => Console.WriteLine(Format(r)));
}

Console.WriteLine();
Console.WriteLine("All helper functions exercised.");
return;


// --- helpers used by the demo ---

static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine($"=== {title} ===");
}

static string Format<T>(Result<T> r) =>
    r.IsSuccess ? $"Ok({r.Value})" : $"Fail({r.Error})";

static Result<User> FindUser(int id)
{
    var users = new Dictionary<int, User>
    {
        [1] = new("Jane", "jane@example.com", new Profile("Ms.", "Engineer")),
    };
    return users.TryGetValue(Guard.Positive(id), out var u)
        ? Result<User>.Ok(u)
        : Result<User>.Fail("Not found");
}

public record Profile(string Title, string Role);
public record User(string Name, string Email, Profile? Profile);
