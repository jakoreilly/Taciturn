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
}
