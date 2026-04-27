# CSharpHelpers Demo

A runnable .NET 8 console app that exercises every helper from [csharp-helper-functions.md](csharp-helper-functions.md) — strings, collections, dates, validation, async, parsing, I/O, object cloning, `Result<T>`, and diagnostics — with realistic inputs and verifiable output.

## Quick start

```bash
dotnet run
```

Requires the .NET 8 SDK (or newer). The only NuGet dependency is `Microsoft.Extensions.Logging.Abstractions`, used by `Forget(ILogger?)`.

## Project layout

| File | Purpose |
| --- | --- |
| [Helpers.cs](Helpers.cs) | All helper functions, grouped into static classes by concern. |
| [Program.cs](Program.cs) | A 10-section walkthrough plus an end-to-end signup pipeline. |
| [HelpersDemo.csproj](HelpersDemo.csproj) | .NET 8 console project, nullable + implicit usings enabled. |
| [csharp-helper-functions.md](csharp-helper-functions.md) | Source-of-truth reference for every helper. |

## What the demo covers

Each section maps 1:1 to a section of the reference doc:

1. **Strings** — `HasValue`, `OrDefault`, `Truncate`, `ToSlug`, `MaskEmail`, `SafeSubstring`
2. **Collections** — `OrEmpty`, `Batch`, `ForEach`, `DistinctByKey`, `RandomItem`
3. **DateTime** — `Age`, `StartOfDay`/`EndOfDay`/`StartOfMonth`/`EndOfMonth`, `AddBusinessDays`, `ToRelative`
4. **Validation & Guard** — `IsValidEmail`, `IsValidUrl`, `Guard.NotNull`/`NotNullOrEmpty`/`Positive`
5. **Async** — `RetryAsync` (success + flaky-then-success), `WithTimeout` (in-time + timeout), `Forget`
6. **Parsing** — `ToIntOrNull`, `ToDecimalOrNull`, `ToDateOrNull`, `ToGuidOrNull`, `ToEnumOrNull<T>`
7. **File & I/O** — `ToHumanSize`, `ToSafeFilename`, `EnsureDirectory` (writes a real file under `%TEMP%/HelpersDemo/reports/`)
8. **Objects** — `DeepClone` (proves independence), `Then` pipeline (with and without nulls)
9. **`Result<T>`** — `FindUser(1)` returns `Ok`, `FindUser(99)` returns `Fail`
10. **Diagnostics** — `Trace` (caller-info), `Measure` (stopwatch wrapper)

The final section, **End-to-end signup pipeline**, chains many helpers together: trims raw input, drops blanks with `HasValue`, deduplicates with `DistinctByKey`, validates with `IsValidEmail`, wraps results in `Result<T>`, and prints them in groups via `Batch` + `ForEach`.

## Two renames vs. the reference doc

To avoid clashes with the BCL, the implementation in [Helpers.cs](Helpers.cs) renames two things:

- `DistinctBy` → `DistinctByKey` — LINQ ships its own `DistinctBy` in .NET 6+.
- The async helper class is `AsyncHelpers` rather than `TaskExtensions` — `System.Threading.Tasks.TaskExtensions` already exists and would cause `CS0104` ambiguity.

Everything else matches the doc verbatim.

## Sample output (abridged)

```
=== 1. String helpers ===
Truncate                     = The quick brown fox…
ToSlug                       = renee-jurgenssen
MaskEmail                    = j*******@example.com

=== 5. Async / Task helpers ===
RetryAsync (3rd try)         = ok (attempts=3)
WithTimeout (too slow)       = TimeoutException raised

=== End-to-end: signup pipeline ===
-- batch --
Ok(jane.doe@example.com)
Fail(invalid email: BAD EMAIL)
-- batch --
Ok(john@example.com)
Ok(alex@example.com)
```
