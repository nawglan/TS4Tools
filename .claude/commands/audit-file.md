# TS4Tools Single File Audit

Perform a deep audit on a single file, comparing it thoroughly against its legacy equivalent.

## Arguments

- `$ARGUMENTS` - Path to the file to audit (required)

## Instructions

If `$ARGUMENTS` is empty, display help and exit:

```
Single File Audit Command

Usage: /audit-file <path/to/file.cs>

Performs deep comparison between a modern implementation and its legacy equivalent.

Examples:
  /audit-file src/TS4Tools.Wrappers/CasPartResource/CasPartResource.cs
  /audit-file src/TS4Tools.Core/Package/DbpfPackage.cs

Checks performed:
  1. Locates legacy file via Source: reference
  2. Compares all constants (TypeId, magic numbers, versions)
  3. Compares public API (methods, properties)
  4. Checks algorithm logic for divergence
  5. Validates .NET best practices
  6. Locates and audits corresponding test file
```

---

## STEP 1: Read the Target File

Read the file at `$ARGUMENTS`.

If file doesn't exist, report error and exit.

Extract:
- Legacy source reference (from `// Source:` or `<remarks>Source:`)
- All constants (`const`, `static readonly`)
- All public members (methods, properties)
- TypeId if present

---

## STEP 2: Locate Legacy Equivalent

Parse the Source: reference to find the legacy file path.

**Expected formats:**
- `// Source: legacy_references/Sims4Tools/s4pi Wrappers/FooResource/FooResource.cs`
- `/// <remarks>Source: legacy_references/Sims4Tools/.../FooResource.cs</remarks>`
- `// Source: FooResource.cs lines 100-200`

If no Source: reference found:
```
ERROR: No legacy source reference found in {file}

Expected format:
  // Source: legacy_references/Sims4Tools/...

Add a source reference pointing to the legacy implementation.
```

Read the legacy file.

---

## STEP 3: Compare Constants

Extract all constants from both files and compare:

| Constant | Modern | Legacy | Status |
|----------|--------|--------|--------|
| TypeId | 0x... | 0x... | MATCH/MISMATCH |
| Version | ... | ... | ... |
| Magic | ... | ... | ... |

**Report mismatches:**
```
CONSTANT MISMATCH:
  {constant_name}
  Modern ({file}:{line}): {modern_value}
  Legacy ({legacy_file}:{line}): {legacy_value}

  This is treated as a BUG. The modern value should match legacy unless there's documented justification.
```

---

## STEP 4: Compare Public API

List public members from both:

**Modern file:**
- public methods
- public properties
- public events

**Legacy file:**
- public methods (look for `public` keyword)
- public properties
- public events

**Report differences:**

```
EXTRA IN MODERN (not in legacy):
  - public void NewMethod()     <- WARNING: May be intentional extension
  - public string NewProperty   <- WARNING: Needs justification

MISSING FROM MODERN (in legacy):
  - public void LegacyMethod()  <- INFO: Not yet ported
  - public int LegacyField      <- INFO: May be intentionally omitted
```

---

## STEP 5: Algorithm Comparison

For key methods (Parse, Serialize, etc.), compare the logic:

1. Extract method body from modern file
2. Extract corresponding method from legacy
3. Identify:
   - Same operations in different order
   - Missing operations
   - Extra operations
   - Different control flow

**Report significant differences:**
```
ALGORITHM DIVERGENCE in {method_name}:
  Modern performs: {description}
  Legacy performs: {description}

  Impact: {potential impact}
  Recommendation: {verify with legacy behavior / document justification}
```

---

## STEP 6: .NET Best Practices Check

Check the single file for:

### 6.1 Input Validation
- Does Parse() validate data length before reading?
- Are array indices bounds-checked?
- Are counts validated before loops?

### 6.2 Memory Patterns
- Using Span<T>/Memory<T> appropriately?
- Avoiding unnecessary allocations?
- Using stackalloc for small buffers?

### 6.3 Async Patterns
- Any synchronous I/O that should be async?

### 6.4 Event Handling
- Any lambda subscriptions without tracking?
- Proper cleanup in Dispose/destructor?

### 6.5 Nullability
- Proper nullable annotations?
- Any `!` (null-forgiving) that could be avoided?

---

## STEP 7: Find and Audit Test File

**Locate test file:**
- If auditing `src/.../FooResource.cs`
- Look for `tests/.../FooResourceTests.cs`

If no test file found:
```
ERROR: No test file found for {file}

Expected: tests/TS4Tools.{project}.Tests/{ClassName}Tests.cs
```

If test file exists, check:

### 7.1 Round-Trip Test
Look for tests that:
- Parse data
- Serialize it back
- Parse again and compare

### 7.2 Edge Cases
Look for tests with:
- Empty data
- Null inputs
- Invalid data (wrong magic, truncated, oversized)
- Boundary values

### 7.3 Legacy Behavior Validation
Look for comments referencing legacy behavior or tests against known-good data.

---

## STEP 8: Generate Report

Output structured findings:

```markdown
# Audit: {filename}
**Generated:** {timestamp}

## File Info
- **Modern:** {modern_path}
- **Legacy:** {legacy_path}
- **Test:** {test_path or "MISSING"}

## Summary
- Errors: {count}
- Warnings: {count}
- Info: {count}

## Constants
{constant_comparison_table}

## Public API
### Extra in Modern
{list}

### Missing from Legacy
{list}

## Algorithm Analysis
{findings}

## .NET Best Practices
{findings}

## Test Coverage
{findings}

## Recommendations
1. {prioritized list}
```

---

## Execution Notes

- Be thorough - this is a deep dive into one file
- Include line numbers for all findings
- Quote relevant code snippets for context
- Distinguish between bugs (must fix) and design decisions (may be intentional)
- If legacy code has comments explaining "why", note them
