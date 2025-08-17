using System.Diagnostics.CodeAnalysis;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TS4Tools.Resources.Scripts.Tests")]

// Global suppressions for known analyzer warnings that don't apply to this context

// CA1308: Normalize strings to uppercase - Not applicable for resource type identifiers
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
    Justification = "Resource type identifiers follow established patterns")]

// CA1062: Validate arguments of public methods - Handled by ArgumentNullException.ThrowIfNull
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods",
    Justification = "Arguments are validated using ArgumentNullException.ThrowIfNull")]

// CA2000: Dispose objects before losing scope - Handled by proper using statements
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
    Justification = "Memory streams and other disposables are properly managed")]

// CA1031: Do not catch general exception types - Appropriate for factory error handling
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Scope = "member", Target = "~M:TS4Tools.Resources.Scripts.ScriptResourceFactory.CreateResourceAsync(TS4Tools.Core.Interfaces.Resources.ResourceKey,System.IO.Stream,System.Threading.CancellationToken)~System.Threading.Tasks.Task{TS4Tools.Resources.Scripts.IScriptResource}",
    Justification = "Factory methods need to catch all exceptions to provide consistent error handling")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Scope = "member", Target = "~M:TS4Tools.Resources.Scripts.ScriptResourceFactory.CreateResource(TS4Tools.Core.Interfaces.Resources.ResourceKey,System.IO.Stream)~TS4Tools.Resources.Scripts.IScriptResource",
    Justification = "Factory methods need to catch all exceptions to provide consistent error handling")]

// CA1819: Properties should not return arrays - ReadOnlyMemory<T> provides safe array access
[assembly: SuppressMessage("Performance", "CA1819:Properties should not return arrays",
    Justification = "Using ReadOnlyMemory<T> for safe array access patterns")]

// CA5351: Do not use broken cryptographic algorithms - Legacy compatibility requirement
[assembly: SuppressMessage("Security", "CA5351:Do not use broken cryptographic algorithms",
    Scope = "member", Target = "~M:TS4Tools.Resources.Scripts.ScriptResource.DecryptData~System.Byte[]",
    Justification = "Legacy Sims 4 encryption scheme requires compatibility with existing format")]

[assembly: SuppressMessage("Security", "CA5351:Do not use broken cryptographic algorithms",
    Scope = "member", Target = "~M:TS4Tools.Resources.Scripts.ScriptResource.EncryptData~System.Byte[]",
    Justification = "Legacy Sims 4 encryption scheme requires compatibility with existing format")]
