# TS4Tools Work Session

Find meaningful work that moves the TS4Tools rewrite forward, implement it, and commit.

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

Without a focus, auto-discovers meaningful work based on priority:
  1. Build/test failures (must be fixed first)
  2. Missing core functionality (Package API, compression)
  3. Unimplemented resource wrappers (legacy exists, modern doesn't)
  4. Partially implemented features (complete the work)
  5. Test coverage for untested resources

This command focuses on SIGNIFICANT work that advances the project.
It will NOT select trivial tasks like typo fixes or comment updates.

Examples:
  /work              Auto-discover and implement next task
  /work wrappers     Focus on resource wrapper work
  /work core         Focus on core package functionality
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

Search for work across the entire codebase, categorizing by impact tier.

### 2.1 Tier 1: Critical (Must Fix)

Build failures or test failures. Already handled in Phase 1.

### 2.2 Tier 2: High Impact (New Functionality)

```
# Find resource wrappers that exist in legacy but NOT in modern
# Compare:
legacy_references/Sims4Tools/s4pi Wrappers/
→ vs src/TS4Tools.Wrappers/

# Find core functionality gaps
legacy_references/Sims4Tools/s4pi/Package/
→ vs src/TS4Tools.Core/Package/
```

Look for entire resource types or major features that are unimplemented.

### 2.3 Tier 3: Medium Impact (Completing Features)

```
# NotImplementedException - features started but not finished
grep: "NotImplementedException|NotSupportedException" in src/

# TODO markers indicating incomplete work
grep: "TODO|FIXME|WIP" in src/

# Resources with no tests
# Compare wrapper files to test files
```

Only consider if the incomplete work represents a coherent feature, not a single minor TODO.

### 2.4 Tier 4: Low Impact (Minor Improvements)

- Code quality improvements
- Additional edge case handling
- Performance micro-optimizations
- Documentation

**Do NOT automatically select Tier 4 work.** See Phase 3.

---

## PHASE 2.5: Significance Evaluation

Before proceeding to selection, verify the discovered work is significant.

### Significant Work Criteria

Work is significant if it:

1. **Implements new functionality** - A resource type, API, or feature from legacy that doesn't exist in modern
2. **Fixes blocking issues** - Build failures, test failures, broken functionality
3. **Completes a coherent unit** - An entire resource wrapper with factory + tests, not just one method
4. **Enables other work** - Core functionality that unblocks future features
5. **Requires legacy analysis** - Following CLAUDE.md's mandate to port from legacy

### NOT Significant Work

Do **NOT** select:

- Typo fixes or comment updates
- Adding docstrings to working code
- Renaming without functional change
- Single-line "improvements"
- Formatting or style changes
- Work that doesn't require understanding the legacy codebase

If you find yourself wanting to select work that doesn't meet the significance criteria, that work should be rejected.

---

## PHASE 3: Selection (Choose Task)

### 3.1 Priority Selection

Select work from the highest available tier:

1. **Tier 1 (Critical):** Always select if present
2. **Tier 2 (High Impact):** Select the most impactful unimplemented feature
3. **Tier 3 (Medium Impact):** Select the most complete partial implementation
4. **Tier 4 (Low Impact):** See 3.2

### 3.2 Low Impact Fallback

If only Tier 4 work is found, **DO NOT automatically proceed**.

Instead, present the options to the user:

```
## No Significant Work Found

Only low-impact work was discovered:
- {list of Tier 4 items}

Would you like to proceed with one of these, or skip this session?
```

Wait for user confirmation before implementing Tier 4 work.

### 3.3 Task Requirements

The selected task must:

1. **Be completable as a coherent unit** - Don't select half a feature
2. **Require legacy analysis** - Reference s4pi/s4pe code
3. **Include tests** - New functionality needs test coverage
4. **Have clear scope** - Know exactly when you're done

### 3.4 Task Output

```
## Selected Task

**Tier:** {1-Critical | 2-High | 3-Medium}
**Type:** {Fix | Feature | Test | Refactor}
**Target:** {description of what will change}
**Legacy Source:** {path to legacy code being ported}
**Scope:** {files/areas affected}
**Why Significant:** {how this advances the project}
**Definition of Done:** {how to know the task is complete}
```

---

## PHASE 4: Implementation

Execute the task completely:

1. **Study the legacy code** - Read and understand the s4pi/s4pe implementation
2. **Make changes** - Follow project patterns from CLAUDE.md
3. **Add source references** - `// Source: legacy_references/Sims4Tools/{path} lines X-Y`
4. **Write tests** - Comprehensive tests, not just happy path
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

---

## PHASE 6: Summary

```markdown
## Work Session Complete

**Task:** {description}
**Tier:** {impact tier}
**Commit:** {short hash} - {message}

### Changes
- {bulleted list}

### Suggested Next Steps
- {follow-up work if any}
```

---

## Execution Notes

- Use TodoWrite to track progress through phases
- One complete significant task is the goal
- Always reference legacy code when implementing
- If only trivial work exists, ask the user before proceeding
- Always validate before committing
