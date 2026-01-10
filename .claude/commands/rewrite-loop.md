# TS4Tools Rewrite Loop

Start an autonomous Ralph Wiggum loop to port remaining legacy s4pi/s4pe functionality.

---

## Usage

```
/rewrite-loop
```

No arguments required. The loop will automatically discover remaining work.

---

## Execute

Run the following Ralph loop:

```
/ralph-wiggum:ralph-loop "## TS4Tools Rewrite Loop

You are porting legacy s4pi/s4pe to modern TS4Tools.

### Critical Files
- Guidelines: /mnt/ai/code/TS4Tools/CLAUDE.md
- Legacy: /mnt/ai/code/TS4Tools/legacy_references/Sims4Tools/s4pi Wrappers/
- Modern: /mnt/ai/code/TS4Tools/src/TS4Tools.Wrappers/

### Each Iteration

1. **DISCOVER**: Compare legacy vs modern. Find the NEXT unported resource from s4pi Wrappers/.
   Priority: DefaultResource > DataResource > AnimationResources > JazzResource > others.
   If NO unported resources remain, skip to COMPLETION CHECK.

2. **ANALYZE**: For chosen resource: read legacy file, understand binary format, note type IDs.

3. **IMPLEMENT**: Create wrapper with [ResourceHandler(TypeId)] attribute, Factory class,
   Source comments (file + line numbers), C# 12+ patterns, BinaryPrimitives, input validation.

4. **TEST**: Create tests in tests/TS4Tools.Wrappers.Tests/ with FluentAssertions.

5. **VERIFY**: Run 'dotnet build' and 'dotnet test'. Fix any errors.

6. **COMMIT**: Stage and commit: 'feat: add XyzResource wrapper implementation'.

### Completion Check

After each iteration, check if ALL are true:
- All s4pi Wrappers/ resource types have modern equivalents
- 'dotnet test' passes
- Each wrapper has a factory class

If ALL conditions are met, output exactly: <promise>REWRITE_COMPLETE</promise>

### Important
- Do NOT re-implement existing wrappers (check git log)
- If stuck on complex wrapper, skip it and note in TODO.md
- One resource per iteration maximum
" --completion-promise "REWRITE_COMPLETE" --max-iterations 50
```

---

## Control Commands

- **Cancel**: `/ralph-wiggum:cancel-ralph`
- **Monitor**: `git log --oneline -20`
- **Check progress**: `ls src/TS4Tools.Wrappers/`
