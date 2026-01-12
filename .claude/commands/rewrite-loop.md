# TS4Tools Rewrite Loop

Start an autonomous Ralph Wiggum loop to complete 100% parity with legacy s4pe.

## Step 1: Activate the Ralph Loop

**USE THE BASH TOOL** to run this command immediately (do not just display it):

```bash
/home/dez/.claude/plugins/cache/claude-code-plugins/ralph-wiggum/1.0.0/scripts/setup-ralph-loop.sh "Read .claude/prompts/rewrite-loop-prompt.md and follow those instructions to implement legacy parity features one at a time." --completion-promise "FULL_PARITY_COMPLETE" --max-iterations 30
```

## Step 2: Begin Implementation

After the loop is activated, read `/mnt/ai/code/TS4Tools/.claude/prompts/rewrite-loop-prompt.md` and start implementing features following the priority queue.

## Control Commands

- **Cancel**: `/ralph-wiggum:cancel-ralph`
- **Monitor**: `git log --oneline -20`
- **Check progress**: `ls src/TS4Tools.UI/Views/`
