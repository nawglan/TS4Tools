# TS4Tools Work Session

Find actionable work that moves the TS4Tools rewrite forward, implement it, and commit.

## Arguments

- `$ARGUMENTS` - Optional focus area or "help"

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
  wrappers   Resource wrapper implementations
  core       Core package/parsing functionality
  tests      Test coverage improvements
  ui         UI/ViewModel work
  docs       Documentation updates

Without a focus, auto-discovers the best next task based on:
  1. Build/test failures (must be fixed first)
  2. Partially implemented features (TODOs, WIP)
  3. Missing functionality (legacy exists, modern doesn't)
  4. Test coverage gaps
  5. Code quality improvements

Examples:
  /work              Auto-discover and implement next task
  /work wrappers     Focus on resource wrapper work
  /work tests        Focus on improving test coverage
```

**STOP HERE if showing help.**

---

## PHASE 1: Health Check

Before looking for work, verify the codebase is healthy:

```bash
# Check build status
dotnet build

# Check test status
dotnet test
```

**If build fails:** The task is to fix the build. Stop discovery.
**If tests fail:** The task is to fix failing tests. Stop discovery.

---

## PHASE 2: Discovery (Find Work)

Search for work across the entire codebase. Look for coherent units of work that make sense as a single commit - this could be small or large.

### 2.1 Check for Incomplete Work

```
# TODOs, FIXMEs, and WIP markers
grep: "TODO|FIXME|HACK|XXX|WIP" in src/

# Partial implementations
grep: "NotImplementedException|NotSupportedException" in src/
```

### 2.2 Compare Legacy vs Modern

Look at ALL areas, not just wrappers:

```
# Core functionality
legacy_references/Sims4Tools/s4pi/Package/
legacy_references/Sims4Tools/s4pi/Interfaces/
→ Compare with src/TS4Tools.Core/

# Resource wrappers
legacy_references/Sims4Tools/s4pi Wrappers/
→ Compare with src/TS4Tools.Wrappers/

# UI features
legacy_references/Sims4Tools/s4pe/
→ Compare with src/TS4Tools.UI/

# Compatibility layer
→ Check src/TS4Tools.Compatibility/ completeness
```

### 2.3 Find Test Gaps

```
# Find implementations without corresponding tests
glob: src/**/*.cs
glob: tests/**/*Tests.cs
```

### 2.4 Look for Enhancement Opportunities

- Performance improvements visible from code review
- API consistency issues
- Missing validation or error handling
- Refactoring that improves maintainability

---

## PHASE 3: Selection (Choose Task)

Select ONE coherent task. The task should:

1. **Make sense as a single commit** - A logical unit of change
2. **Be completable** - Don't select half of a feature
3. **Have clear scope** - Know when you're done

Task examples (any size is valid):
- Fix a single bug
- Complete a TODO
- Port an entire resource wrapper with tests
- Add comprehensive tests for a module
- Refactor a subsystem for better patterns
- Implement a missing core feature

**Output the selected task:**
```
## Selected Task

**Type:** {Fix | Feature | Test | Refactor | Enhancement}
**Target:** {description of what will change}
**Scope:** {files/areas affected}
**Reason:** {why this task was selected}
**Definition of Done:** {how to know the task is complete}
```

---

## PHASE 4: Implementation

Execute the task completely:

1. **Understand the context** - Read relevant code, legacy references if applicable
2. **Make changes** - Follow project patterns from CLAUDE.md
3. **Add source references** - For any code ported from legacy
4. **Write/update tests** - Ensure the change is tested
5. **Validate** - Run `dotnet build` and `dotnet test`

### Source References

When porting from legacy, add references:
```csharp
// Source: legacy_references/Sims4Tools/{path} lines X-Y
```

This applies to ALL ported code, not just wrappers.

### Validation

```bash
dotnet build
dotnet test
```

**Do not proceed to commit if build or tests fail.**

---

## PHASE 5: Commit

### 5.1 Review Changes

```bash
git status
git diff
```

Ensure changes are coherent and complete.

### 5.2 Commit

Use conventional commit format with explanation:

```bash
git add <files>
git commit -m "$(cat <<'EOF'
<type>: <description>

<Why this change - 1-2 sentences>

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

**Type prefixes:**
- `feat:` - New functionality
- `fix:` - Bug fix
- `test:` - Test additions/improvements
- `refactor:` - Code restructuring
- `perf:` - Performance improvement
- `docs:` - Documentation

---

## PHASE 6: Summary

```markdown
## Work Session Complete

**Task:** {description}
**Commit:** {short hash} - {message}

### Changes
- {bulleted list}

### Suggested Next Steps
- {follow-up work if any}
```

---

## Execution Notes

- Use TodoWrite to track progress through phases
- One complete task is better than multiple incomplete ones
- Task size doesn't matter - coherence does
- If blocked, report why and stop
- Always validate before committing
