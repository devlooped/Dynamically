using Microsoft.CodeAnalysis;

namespace Devlooped.Dynamically;

public static class Diagnostics
{
    /// <summary>
    /// DYN001: Factory method is not accessible
    /// </summary>
    public static DiagnosticDescriptor CreateMethodNotAccessible { get; } = new(
        "DYN001",
        "Factory method is not accessible",
        "Factory method '{0}.{1}' is not accessible within the current compilation to support hierarchical dynamic conversion.",
        "Build",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In order to support automatic hierarchical dynamic conversion for records, the Create method must be accessible within the compilation.");
}