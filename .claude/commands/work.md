# TS4Tools Work Session

Find actionable work that moves the TS4Tools rewrite forward, implement it with tests, and commit.

## Arguments

- `$ARGUMENTS` - Optional focus area (e.g., "wrappers", "core", "ui") or "help"

---

## Decision Logic

**FIRST**, check `$ARGUMENTS`:

1. **If `help` or `?`** → Display help message and STOP
2. **If a specific focus** → Prioritize that area when finding work
3. **If empty** → Auto-discover the best next task

---

## Default: HELP (when `help` or `?`)

Display this help message and **exit immediately**:

```
TS4Tools Work Session

Usage: /work [focus]

Focus areas (optional):
  wrappers   Prioritize resource wrapper implementations
  core       Prioritize core package/parsing functionality
  tests      Prioritize test coverage gaps
  ui         Prioritize UI/ViewModel work

Without a focus, auto-discovers the best next task based on:
  1. Partially implemented features needing completion
  2. Missing wrapper implementations (legacy exists, modern doesn't)
  3. Test coverage gaps
  4. TODOs/FIXMEs in the codebase

Examples:
  /work              Auto-discover and implement next task
  /work wrappers     Focus on resource wrapper work
  /work tests        Focus on improving test coverage
```

**STOP HERE if showing help.**

---

## PHASE 1: Discovery (Find Work)

### 1.1 Check for In-Progress Work

First, look for existing incomplete work:

```
# Check for TODOs and FIXMEs in modern code
grep: "TODO|FIXME|HACK|XXX" in src/

# Check for partial implementations (NotImplementedException)
grep: "NotImplementedException|throw new NotSupportedException" in src/

# Check for WIP markers
grep: "WIP|Work.in.Progress" in src/
```

If found, these are HIGH PRIORITY - complete existing work before starting new.

### 1.2 Identify Missing Wrappers

Compare legacy wrappers to modern implementations:

```
# Legacy resource files (the source of truth)
glob: legacy_references/Sims4Tools/s4pi Wrappers/**/*Resource.cs
glob: legacy_references/Sims4Tools/s4pi Wrappers/**/*Wrapper.cs

# Modern implementations
glob: src/TS4Tools.Wrappers/**/*.cs

# Find resources in legacy without modern equivalent
# A legacy FooResource.cs should have a modern FooResource.cs
```

### 1.3 Find Test Coverage Gaps

```
# Modern resource files
glob: src/TS4Tools.Wrappers/**/*Resource.cs
glob: src/TS4Tools.Core/**/*.cs

# Test files
glob: tests/**/*Tests.cs

# Resources without tests are HIGH PRIORITY
```

### 1.4 Check for Missing Core Features

```
# Core legacy features
glob: legacy_references/Sims4Tools/s4pi/Package/*.cs
glob: legacy_references/Sims4Tools/s4pi/Interfaces/*.cs

# Modern core
glob: src/TS4Tools.Core/**/*.cs
```

---

## PHASE 2: Selection (Choose Task)

Based on discovery, select ONE task using this priority:

1. **HIGHEST**: Complete partially-implemented features (TODOs, NotImplementedException)
2. **HIGH**: Add tests for existing implementations without test coverage
3. **MEDIUM**: Port a new wrapper from legacy (prefer smaller, self-contained ones)
4. **LOWER**: Add edge case tests for existing test files

**Selection criteria for new wrappers:**
- Prefer wrappers with fewer dependencies
- Prefer smaller files (< 500 lines in legacy)
- Prefer wrappers with clear, documented structure

**Output the selected task:**
```
## Selected Task

**Type:** [Complete WIP | Add Tests | New Wrapper | Edge Cases]
**Target:** {file or feature name}
**Reason:** {why this was selected}
**Legacy Reference:** {path to legacy file if applicable}
```

---

## PHASE 3: Implementation

### 3.1 For "Complete WIP" Tasks

1. Read the file with the TODO/NotImplementedException
2. Find the corresponding legacy implementation
3. Understand what's missing
4. Implement it following legacy logic
5. Add `// Source: {legacy_file} lines X-Y` reference

### 3.2 For "Add Tests" Tasks

1. Read the implementation being tested
2. Read the legacy equivalent for test cases
3. Create tests covering:
   - Basic functionality (parse/serialize round-trip)
   - Edge cases (empty, null, boundary values)
   - Error cases (invalid data)
4. Follow existing test patterns in the project

### 3.3 For "New Wrapper" Tasks

1. Read the legacy implementation thoroughly
2. Create the modern file in `src/TS4Tools.Wrappers/`
3. Port the logic using modern C# patterns:
   - Primary constructors
   - Span<T> for binary parsing
   - Nullable reference types
   - Collection expressions
4. Add `// Source:` reference
5. Create corresponding test file

### 3.4 Validation

After implementation:

```bash
# Build to catch errors
dotnet build

# Run tests
dotnet test
```

**If build fails:** Fix errors before proceeding
**If tests fail:** Fix tests before proceeding

---

## PHASE 4: Commit

### 4.1 Stage Changes

```bash
git status
git diff
git add <relevant files>
```

### 4.2 Craft Commit Message

Use conventional commit format:

```
feat: {what was added}

{Why this change was made - 1-2 sentences}

Source: {legacy file reference}

Generated with Claude Code
Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
```

**Type prefixes:**
- `feat:` - New wrapper or feature
- `test:` - New or improved tests
- `fix:` - Bug fix or completing WIP

### 4.3 Commit

```bash
git commit -m "$(cat <<'EOF'
{commit message}
EOF
)"
```

---

## PHASE 5: Summary

Output work session summary:

```markdown
## Work Session Complete

**Task:** {description}
**Files Changed:** {count}
**Tests Added/Modified:** {count}
**Commit:** {short hash} - {first line of message}

### Changes Made
- {bulleted list of changes}

### Next Steps
- {suggested follow-up work if any}
```

---

## Execution Notes

- Use TodoWrite to track progress through phases
- If any phase fails, report the failure and stop
- Always validate with `dotnet build` and `dotnet test`
- Reference CLAUDE.md patterns for code style
- Be thorough but focused - one complete task is better than multiple incomplete ones
- If the codebase is in a broken state (build fails), prioritize fixing that first
