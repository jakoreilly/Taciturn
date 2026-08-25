# Taciturn

A Roslyn incremental source generator for C#: mark a `record` `[Taciturn]` and
every member is redacted in the compiler's synthesized `ToString()` — until you
explicitly opt one back into the clear with `[Plain]`.

```csharp
[Taciturn]
public sealed partial record StripeOptions(
    string PublishableKey,
    [property: Plain] string AccountId,
    string WebhookSecret);

Console.WriteLine(new StripeOptions("pk_live_abc123", "acct_1M2n3", "whsec_9f2a"));
// StripeOptions { PublishableKey = «redacted», AccountId = acct_1M2n3, WebhookSecret = «redacted» }
```

## The problem

A `record` with secret fields — API keys, webhook secrets, connection strings —
leaks them the moment someone logs it:

```csharp
_logger.LogInformation("starting with {Options}", stripeOptions);
// StripeOptions { PublishableKey = pk_live_abc123, WebhookSecret = whsec_9f2a }
```

Nobody wrote that logging bug. The compiler synthesizes a `ToString()` that
prints every public property, from a one-line type declaration — and it
recurs every time someone adds a property to the type, because the whole
point of a record's synthesized members is that nobody has to think about
them again. Taciturn closes that hole at the type level instead of relying on
code review to catch it at every call site, forever.

## Install

```powershell
dotnet add package Taciturn
```

Mark the type `partial` and add `[Taciturn]`:

```csharp
[Taciturn]
public sealed partial record ApiCredentials(string ClientId, string ClientSecret);
```

That's it — every member is redacted by default. A property added next month
is redacted the day it's added, by nobody, because the redaction is
regenerated from the type rather than maintained alongside it.

## Opting a member back into the clear

Use `[Plain]` on the member you want to keep visible. On a positional record
parameter, apply it as `[property: Plain]` — the attribute targets the
synthesized property, not the constructor parameter:

```csharp
[Taciturn]
public sealed partial record ApiCredentials(
    [property: Plain] string ClientId,   // not secret, fine to log
    string ClientSecret);                // stays redacted
```

## Supported shapes

Every C# record shape works, with the signature the compiler actually
expects for that shape:

| Shape | Notes |
|---|---|
| `sealed record` deriving from `object` | |
| non-sealed `record` deriving from `object` | |
| `record` deriving from another record | chains `base.PrintMembers`, so a `[Taciturn]` base and a `[Taciturn]` derived type compose correctly |
| `record struct` | |

## Diagnostics

| ID | Severity | Meaning |
|---|---|---|
| `TACIT001` | Error | The type is marked `[Taciturn]` but isn't `partial` — nothing can be generated into it. |
| `TACIT002` | Error | The type is marked `[Taciturn]` but isn't a record — there's no synthesized `PrintMembers` to redact. |
| `TACIT003` | Warning | The type already declares its own `PrintMembers`/`ToString` — Taciturn stands down rather than emit a duplicate member. Redaction is whatever that hand-written member does. |
| `TACIT004` | Warning | An *unmarked* record derives from a `[Taciturn]`-marked one. The base's members stay redacted, but the derived type's own new members print in the clear — protection is per-type, not per-hierarchy. |

## Debugger view

A `[Taciturn]` type also gets a generated `[DebuggerTypeProxy]`, so hovering a
value in the debugger shows the same redacted/plain split as `ToString()` —
a breakpoint can't bypass what a log line can't.

## What this doesn't do

- **It never changes what's stored or serialized** — only what's *printed*.
  A secret is still a live `string` on the managed heap, and
  `JsonSerializer.Serialize(options)` still serializes it in full; Taciturn
  only closes the `ToString()`/debugger path.
- **No wrapper type** (`Secret<string>`). That closes direct member-access
  leaks properly, but costs an edit at every call site that reads the value —
  the whole appeal here is zero call-site changes beyond the attribute.
- **No zeroing / `SecureString`.** .NET strings can't be reliably wiped, and
  `SecureString` is documented as not recommended for new development.

## Building from source

```powershell
dotnet build
dotnet test
dotnet run --project samples/Taciturn.Sample
```

## License

MIT — see [LICENSE](LICENSE).
