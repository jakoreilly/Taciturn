using Microsoft.CodeAnalysis;

namespace Taciturn;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "TACIT001",
        title: "Taciturn type must be partial",
        messageFormat: "'{0}' is marked [Taciturn] but is not 'partial' — no PrintMembers override can be generated for it",
        category: "Taciturn",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotRecord = new(
        id: "TACIT002",
        title: "Taciturn only applies to records",
        messageFormat: "'{0}' is marked [Taciturn] but is not a record — there is no synthesized PrintMembers to redact",
        category: "Taciturn",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AlreadyDeclared = new(
        id: "TACIT003",
        title: "Taciturn standing down: PrintMembers or ToString already declared",
        messageFormat: "'{0}' already declares its own PrintMembers or ToString — Taciturn will not emit a duplicate member, so this type's redaction is whatever that hand-written member does",
        category: "Taciturn",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Temporary, version-scoped limitation — not part of the permanent four-diagnostic
    // design (see plan.md). Sealed-derives-from-record, non-sealed, and record struct
    // shapes get their own correct PrintMembers signature in a later phase; until then
    // this reports loudly rather than silently emitting nothing.
    public static readonly DiagnosticDescriptor UnsupportedShapeThisVersion = new(
        id: "TACIT099",
        title: "Record shape not yet supported by this Taciturn version",
        messageFormat: "'{0}' is marked [Taciturn] but is not a sealed record deriving directly from object — this shape (record struct, non-sealed, or derived-record PrintMembers chaining) is not implemented yet, so nothing was generated and this type is NOT currently redacted",
        category: "Taciturn",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
