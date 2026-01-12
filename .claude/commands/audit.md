# TS4Tools Code Audit

Perform a comprehensive code audit for the TS4Tools project. This audit ensures legacy compliance, modern .NET best practices, and test quality.

## Arguments

- `$ARGUMENTS` - Scope of audit (optional, defaults to help)

---

## Decision Logic

**FIRST**, check `$ARGUMENTS`:

1. **If empty, `help`, or `?`** → Display help message and STOP
2. **If `full`** → Run all three audit categories
3. **If `legacy`** → Run Category 1 only
4. **If `dotnet`** → Run Category 2 only
5. **If `tests`** → Run Category 3 only

---

## Default: HELP (no arguments, `help`, or `?`)

When `$ARGUMENTS` is empty or equals `help` or `?`, display this help message and **exit immediately without running any audit**:

```
TS4Tools Audit Command

Usage: /audit [scope]

Scopes:
  help     Show this help message (default)
  full     Run all audit checks
  legacy   Legacy compliance only (source refs, constants, API parity)
  dotnet   .NET best practices only (validation, async, allocations)
  tests    Test quality only (coverage, round-trips, edge cases)

Examples:
  /audit full      Run complete audit
  /audit legacy    Check legacy compliance only
  /audit tests     Check test quality only

Findings are saved to: .claude/audit-reports/
ERROR items are added to the todo list for tracking.

Severity Levels:
  ERROR   - Must fix before merge
  WARNING - Should review and consider fixing
  INFO    - Nice to have, track for future
```

**STOP HERE if showing help. Do not proceed to audit categories.**

---

## Scope: `full`

Run all three categories below in sequence (Category 1, 2, and 3).

---

## Category 1: Legacy Compliance (scope: `legacy` or `full`)

### 1.1 Missing Source References [ERROR]

Find all resource files in `src/TS4Tools.Wrappers/` that lack legacy source documentation.

**Check for files missing BOTH patterns:**
- `// Source:` (inline comment)
- `<remarks>Source:` or `/// Source:` (XML doc comment)

**Commands:**
```
# Get all .cs files in Wrappers
glob: src/TS4Tools.Wrappers/**/*.cs

# For each file, grep for Source: pattern
# Flag files with NO matches
```

**Report format:** `- [ ] ERROR: {file} - Missing legacy source reference`

### 1.2 Constant Verification [ERROR]

For each wrapper with a `TypeId` constant, verify it matches the legacy code.

**Steps:**
1. Grep for `const.*TypeId\s*=\s*0x[0-9A-Fa-f]+` in modern code
2. Locate the corresponding legacy file via the Source: reference
3. Compare TypeId values

**Report format:** `- [ ] ERROR: {file}:{line} - TypeId 0x{modern} differs from legacy 0x{legacy}`

### 1.3 Extra Public API [WARNING]

Compare public methods/properties between modern and legacy implementations.

**For each wrapper file:**
1. Read the modern file and list public members
2. Read the legacy file (from Source: reference)
3. Flag public members in modern that don't exist in legacy

**Report format:** `- [ ] WARNING: {file}:{line} - Extra method `{name}()` not in legacy`

---

## Category 2: .NET Best Practices (scope: `dotnet` or `full`)

### 2.1 Missing Input Validation [ERROR]

Binary parsing without bounds checking is a security risk.

**Grep patterns:**
```
# Look for ReadInt32/ReadUInt32 without size validation
grep: "Read(U)?Int(16|32|64)"
# Check if file has corresponding size validation
grep: "(Length|Count)\s*<|>\s*\d+|throw.*Exception"
```

**Flag files that read binary data but lack validation like:**
- `if (data.Length < MinimumSize)`
- `if (count < 0 || count > MaxCount)`

**Report format:** `- [ ] ERROR: {file}:{line} - Binary read without bounds validation`

### 2.2 Event Subscription Leaks [ERROR]

Lambda event subscriptions in loops/dynamic contexts leak memory.

**Grep pattern:**
```
grep: "\+=\s*\([^)]*\)\s*=>"
# Focus on files in ViewModels/
```

**Check for tracking pattern (good):**
```csharp
private readonly List<(...Handler)> _subscriptions = [];
```

**Report format:** `- [ ] ERROR: {file}:{line} - Lambda event subscription without tracking`

### 2.3 Synchronous I/O [WARNING]

All file I/O should be async.

**Grep patterns:**
```
grep: "File\.(Read|Write|Open|Create)[^A]" (not Async)
grep: "\.Read\(\)|\.Write\(" in Stream contexts
```

**Report format:** `- [ ] WARNING: {file}:{line} - Synchronous I/O, use async variant`

### 2.4 Global Using Violations [ERROR]

Using directives belong in .csproj, not .cs files.

**Grep pattern:**
```
grep: "^using System" in *.cs files (excluding generated)
```

**Exclude:** `obj/`, `bin/`, `*.g.cs`, `*.Designer.cs`

**Report format:** `- [ ] ERROR: {file}:{line} - Using directive in source file, move to .csproj`

### 2.5 Allocation-Heavy Patterns [WARNING]

Prefer Span<T> over new byte[] in hot paths.

**Grep pattern:**
```
grep: "new byte\[\d+\]" in Parse/Serialize methods
```

**Report format:** `- [ ] WARNING: {file}:{line} - Consider Span<T> instead of byte[] allocation`

---

## Category 3: Test Quality (scope: `tests` or `full`)

### 3.1 Coverage Gaps [ERROR]

Every wrapper should have a corresponding test file.

**Commands:**
```
# List all wrapper resource files
glob: src/TS4Tools.Wrappers/**/*Resource.cs

# List all test files
glob: tests/**/*Tests.cs

# Find wrappers without matching tests
# FooResource.cs should have FooResourceTests.cs
```

**Report format:** `- [ ] ERROR: {wrapper} - No corresponding test file found`

### 3.2 Missing Round-Trip Tests [ERROR]

Resources must have Parse→Serialize→Parse tests.

**Grep patterns in test files:**
```
grep: "RoundTrip|Serialize.*Parse|Parse.*Serialize"
```

**For each wrapper, check its test file contains round-trip testing.**

**Report format:** `- [ ] ERROR: {testfile} - Missing round-trip test for {resource}`

### 3.3 Missing Edge Case Tests [WARNING]

Tests should cover empty, null, and invalid inputs.

**Grep patterns:**
```
grep: "Empty|Null|Invalid|Overflow|Underflow|Negative|Zero"
```

**Flag test files that lack these keywords.**

**Report format:** `- [ ] WARNING: {testfile} - Limited edge case coverage`

### 3.4 Testability Code Smells [WARNING]

Production code that's hard to test indicates coupling issues.

**Grep patterns in src/ (not tests/):**
```
grep: "new [A-Z][a-zA-Z]+\(" - Direct instantiation of dependencies
grep: "static.*void|static.*[A-Z]" - Static methods that prevent mocking
grep: "File\.|Directory\.|Path\." - Direct file system access
```

**Exclude:**
- Factory classes (legitimate use of `new`)
- Static extension methods
- Test helpers

**Report format:** `- [ ] WARNING: {file}:{line} - Tight coupling: direct instantiation of {class}`

### 3.5 Meaningless Tests [ERROR]

Tests with no assertions or always-true conditions.

**Grep patterns:**
```
grep: "\[Fact\]|\[Theory\]" - Find test methods
# Check method body for .Should() or Assert calls
# Flag methods without assertions
```

**Report format:** `- [ ] ERROR: {testfile}:{line} - Test method lacks assertions`

---

## Output Generation

### 1. Write Report

Create `.claude/audit-reports/audit-{YYYY-MM-DD}-{HHMMSS}.md`:

**Format**: Use full timestamp with date AND time to allow multiple audits per day.
- Example: `audit-2026-01-10-143052.md` (January 10, 2026 at 14:30:52)

```markdown
# Audit Results: {scope}
**Generated:** {timestamp}
**Project:** TS4Tools

## Summary
- Errors: {error_count} (must fix)
- Warnings: {warning_count} (should fix)
- Info: {info_count} (consider)

## Legacy Compliance
{legacy_findings}

## .NET Best Practices
{dotnet_findings}

## Test Quality
{test_findings}

## Recommended Actions
1. Fix all ERROR items before merging
2. Review WARNING items with team
3. Track INFO items for future improvement
```

### 2. Update Todo List

Use TodoWrite to add ERROR items as pending tasks:

```
For each ERROR finding:
  - content: "Audit fix: {short description}"
  - status: "pending"
  - activeForm: "Fixing {file}"
```

### 3. Display Summary

Output to conversation:
```
## Audit Complete

- Errors: X
- Warnings: Y
- Info: Z

Report saved to: .claude/audit-reports/audit-{YYYY-MM-DD}-{HHMMSS}.md

{If errors > 0: "Run /audit again after fixing errors to verify."}
```

---

## Execution Notes

- Use Grep with `output_mode: "content"` to get line numbers
- Use Glob to find files by pattern
- Use Read to examine file contents for context
- Use Task agents for parallel exploration when checking many files
- Be thorough but efficient - skip obvious false positives
- Include file:line references for all findings
