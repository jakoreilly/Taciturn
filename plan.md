# Taciturn

A Roslyn incremental source generator: mark a C# `record` `[Taciturn]` and every
member is redacted (`«redacted»`) in the compiler's synthesized `ToString()`/
`PrintMembers()` — unless a member carries `[Plain]` (not yet implemented — see
Phase 2). The polarity is deliberate: **default is silent, opting a member back
into the clear is the thing that shows up in a diff.**

Full design source: `ClaudeScheduler/ideas/2026-08-06-taciturn.md`. This file
tracks *status against that spec*, not a re-derivation of it.

## Why this exists

A `record` with secret fields (API keys, webhook secrets, connection strings)
leaks them the moment someone logs it — `_logger.LogInformation("{Options}",
opts)` — because the compiler synthesizes a `ToString()` that prints every
public property. Nobody wrote that logging bug; the compiler authored it from a
one-line type declaration, and it recurs every time a property is added.
Taciturn closes that hole at the type level instead of relying on code review to
catch it per call site.

## Status

**All four phases done.** The generator now handles every C# record shape with
the correct `PrintMembers` signature, `[Plain]` opt-out, cross-hierarchy
protection warnings, and a debugger view — the full design from
`ClaudeScheduler/ideas/2026-08-06-taciturn.md` is implemented.

**Phase 1.** Sealed record deriving directly from `object`, `TACIT001` (not
partial), `TACIT002` (not a record), `TACIT003` (already declares its own
`PrintMembers`/`ToString`, generator stands down rather than emit a
duplicate-member compile error) — the latter two landed ahead of schedule
since both are cheap and skipping them would have meant either a confusing
silent no-op or a guaranteed compile break.

**Phase 2.** All three distinct `PrintMembers` signatures: `private` (record
struct, or sealed deriving from `object`), `protected virtual` (non-sealed
deriving from `object`), `protected override` with `base.PrintMembers`
chaining (anything deriving from another record, sealed or not). This is
exhaustive over every record shape, so the old `TACIT099` "not supported yet"
diagnostic became unreachable and was deleted rather than left as dead code.

**Phase 3.** `[Plain]` (and `[property: Plain]` on positional-record
parameters, since the attribute targets the synthesized property) opts one
member back into the clear. Verified against the spec's own motivating
example — output now matches exactly:

```
StripeOptions { PublishableKey = «redacted», AccountId = acct_1M2n3, WebhookSecret = «redacted» }
```

**Phase 4.** `TACIT004` warns when an *unmarked* record derives from a
`[Taciturn]`-marked one (protection is per-type, not per-hierarchy — a
derived type's own new members print in the clear unless it's marked too).
Runs as a second, unfiltered incremental pipeline since it has to see
non-marked types, which `ForAttributeWithMetadataName` structurally can't.
`[DebuggerTypeProxy]` is also generated, with a nested debug-view class
showing the identical redacted/plain split as `ToString()` — including
correct unbound-generic `typeof()` syntax for generic records.

**Test harness upgrade, mid-Phase-2.** Started as source-text greps, then
extended (`GeneratorTestHelper.ToStringOf`) to actually emit the compiled
assembly and call the real `ToString()`/debug-view properties via reflection
— genuine end-to-end proof of the comma-chaining and redaction arithmetic,
not an inference from what the generated text looks like. 19/19 tests pass.

## Layout

```
Taciturn.sln
src/Taciturn/                  the generator (netstandard2.0, IsRoslynComponent)
  TaciturnGenerator.cs         IIncrementalGenerator — attribute emission + Phase 1 logic
  Diagnostics.cs                TACIT001 / TACIT002 / TACIT003 / TACIT099 descriptors
  PolyfillIsExternalInit.cs    netstandard2.0 needs this to compile `record` itself
samples/Taciturn.Sample/       net10.0 console app referencing the generator as
                                an Analyzer (OutputItemType="Analyzer",
                                ReferenceOutputAssembly="false") — run it to see
                                real redacted output, not just read the source.
```

## Verified this session

- `dotnet run --project samples/Taciturn.Sample` prints the fully-redacted
  `StripeOptions` line above.
- `TACIT001` fires (as a build **error**) on a `[Taciturn]` record missing
  `partial`.
- `TACIT099` fires (as a build **warning**) on a non-sealed record and on a
  record deriving from another record — both correctly out of Phase 1 scope.
- Sample project builds clean (0 warnings, 0 errors) with only the Phase-1
  supported shape present.

No automated test project yet — verification so far is the sample app plus the
scratch-file diagnostic checks above (not committed; ad hoc for this session).
A `tests/Taciturn.Tests` project using Roslyn's
`CSharpSourceGeneratorVerifier`/`CSharpAnalyzerVerifier` harness is the natural
next addition before Phase 2, so regressions in the four-branch signature
dispatch get caught mechanically rather than by re-running the sample by hand.

## Path to a publishable v1

Scoped 2026-08-22, in response to "is there a realistic income/portfolio angle
here" — the honest framing is that publishing this properly is a real, bounded
project (roughly a weekend of focused work, not an afternoon), not a passive
lever.

**Update, same day: all four code phases + the test project are done** (see
Status above) — 19/19 tests, 0 build warnings, verified end-to-end including a
generic record's DebuggerTypeProxy. What's left, grouped by who does it:

**Code — I can do all of this:**
- `README.md`: what it does, install snippet, a before/after example, the
  four diagnostic IDs with one-line explanations, license badge. This is
  what someone reads in the 15 seconds before deciding whether to add the
  package — worth real effort, not boilerplate.
- NuGet packaging metadata in `Taciturn.csproj`: `PackageId`, `Authors`,
  `Description`, `PackageLicenseExpression` (MIT is the default choice for a
  small dev-tool package unless you want otherwise), `RepositoryUrl`,
  `PackageTags`. The `analyzers/dotnet/cs` packing item is already in place
  from Phase 1.
- A `LICENSE` file and pushing the existing local repo to a GitHub remote
  you create.
- A GitHub Actions workflow: build + test on every push, `dotnet pack` on a
  version tag. Standard, and removes "did I forget a step" from every future
  release.

**Needs you specifically — I can't do these:**
- Creating the GitHub repo itself (or telling me the URL of one you've
  created) and a NuGet.org account if you want it listed there — both are
  identity-bound signups I shouldn't act on with your credentials.
- The actual `dotnet nuget push` with an API key, since that key has to come
  from your NuGet.org account settings.
- Deciding the license (MIT assumed above — say if you want something else)
  and whether this ships under your name/handle or an org.

**Suggested order:** tests → Phase 2 → Phase 3 → Phase 4 → README → packaging
metadata → GitHub push → (your call) NuGet publish. Each phase is independently
useful and already builds clean, so this can pause at any point without leaving
something broken — say when to start and how far to go in one sitting.

## Not doing (per the original spec — repeated here so it isn't re-litigated)

- No `[JsonConverter]`/serialization redaction — this only changes what's
  *printed*, never what's stored or serialized. A secret is still a live
  `string` on the heap.
- No wrapper type (`Secret<string>`) — that closes direct member-access leaks
  properly but costs a call-site edit everywhere; the whole appeal here is
  zero call-site changes.
- No zeroing/`SecureString` — .NET strings can't be reliably wiped, and
  `SecureString` is documented as not recommended for new development.
