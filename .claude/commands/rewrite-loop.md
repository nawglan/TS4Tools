# TS4Tools Rewrite Loop

Start an autonomous Ralph Wiggum loop to complete 100% parity with legacy s4pe.

---

## Instructions

Read the prompt file at `/mnt/ai/code/TS4Tools/.claude/prompts/rewrite-loop-prompt.md` for the full loop instructions.

Then execute this slash command with a simple prompt:

```
/ralph-wiggum:ralph-loop "Read .claude/prompts/rewrite-loop-prompt.md and follow those instructions to implement legacy parity features one at a time." --completion-promise "FULL_PARITY_COMPLETE" --max-iterations 30
```

---

## Control Commands

- **Cancel**: `/ralph-wiggum:cancel-ralph`
- **Monitor**: `git log --oneline -20`
- **Check progress**: `ls src/TS4Tools.UI/Views/`
