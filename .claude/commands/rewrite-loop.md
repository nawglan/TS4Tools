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
/ralph-wiggum:ralph-loop "## Project: TS4Tools Rewrite -- You are working on TS4Tools, a greenfield rewrite of legacy Sims4Tools (s4pe/s4pi). Your mission is to port ALL remaining legacy functionality to the modern codebase. -- CRITICAL DOCUMENTATION: Read /mnt/ai/code/TS4Tools/CLAUDE.md for development guidelines. Legacy code is at /mnt/ai/code/TS4Tools/legacy_references/Sims4Tools/. Modern wrappers go in /mnt/ai/code/TS4Tools/src/TS4Tools.Wrappers/. -- ITERATION PATTERN: Each iteration you must: (1) DISCOVER - Compare legacy vs modern implementations. Legacy is in legacy_references/Sims4Tools/s4pi Wrappers/, modern is in src/TS4Tools.Wrappers/. Identify the next unported resource type (prioritize: DefaultResource, DataResource, AnimationResources, then JazzResource). (2) ANALYZE - For the chosen resource: find the legacy implementation file(s), understand the binary format and parsing algorithm, note the resource type IDs (hex values), document version handling and edge cases. (3) IMPLEMENT - Create the modern wrapper with [ResourceHandler(0xXXXXXXXX)] attribute, Factory class implementing IResourceFactory, Source comments referencing legacy file and line numbers, C# 12+ patterns (primary constructors, collection expressions), BinaryPrimitives for endian-aware reads, validate all untrusted values (array sizes, string lengths, offsets). (4) TEST - Create unit tests in tests/TS4Tools.Wrappers.Tests/, test parsing, serialization round-trip, use FluentAssertions. (5) VERIFY - Run dotnet build and dotnet test, fix any errors before proceeding. (6) AUDIT - Run /audit full and fix issues: check legacy compliance (source references, constants match legacy), check .NET best practices (input validation, no event leaks, async I/O), check test quality (coverage, round-trip tests, edge cases), fix all ERROR-level findings before proceeding, address WARNING-level findings if quick to fix. (7) COMMIT - If tests pass and audit is clean: stage changes with git add, commit with conventional format like feat: add XyzResource wrapper implementation, include source reference in commit body. -- COMPLETION CRITERIA: When ALL of these are true, output the promise REWRITE_COMPLETE: All legacy wrappers in s4pi Wrappers/ have modern equivalents, all tests pass (dotnet test succeeds), /audit full reports zero ERRORs, no TODO comments remain in wrapper files, each wrapper has a corresponding factory class. -- CURRENT STATUS: Check git log and file comparison to determine what is already done. Do NOT re-implement existing wrappers. -- PRIORITY ORDER: (1) DefaultResource - Simple fallback, enables handling unknown types, (2) DataResource - Simulation data commonly used, (3) AnimationResources - Animation support, (4) JazzResource - Complex but important for full parity. -- SAFETY RULES: Always run tests AND /audit full before committing. Never commit code with failing tests or audit ERRORs. If stuck on a complex wrapper, document the issue in a TODO.md and move to the next one. If audit finds issues in existing code (not your changes), note them but continue. Maximum 100 iterations." --completion-promise "REWRITE_COMPLETE" --max-iterations 100
```

---

## Control Commands

- **Cancel**: `/ralph-wiggum:cancel-ralph`
- **Monitor**: `git log --oneline -20`
- **Audit reports**: `ls .claude/audit-reports/`
