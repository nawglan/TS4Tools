#!/bin/bash
# Setup Pre-Commit Hooks for TS4Tools
# Run this script to configure automatic code formatting before commits

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

echo -e "${CYAN}Setting up TS4Tools pre-commit hooks...${NC}"

# Get repository root
REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null)
if [ $? -ne 0 ]; then
    echo -e "${RED}ERROR: Not in a git repository!${NC}"
    exit 1
fi

cd "$REPO_ROOT"

# Check if we're in a git repository
if [ ! -d ".git" ]; then
    echo -e "${RED}ERROR: Not in a git repository!${NC}"
    exit 1
fi

# Configure Git to use our custom hooks directory
echo -e "${YELLOW}Configuring Git hooks path...${NC}"
git config core.hooksPath .githooks

if [ $? -eq 0 ]; then
    echo -e "${GREEN}SUCCESS: Git hooks path configured successfully.${NC}"
else
    echo -e "${RED}ERROR: Failed to configure Git hooks path.${NC}"
    exit 1
fi

# Make the hooks executable
echo -e "${YELLOW}Making hooks executable...${NC}"
if [ -f ".githooks/pre-commit" ]; then
    chmod +x .githooks/pre-commit
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}SUCCESS: Hooks made executable.${NC}"
    else
        echo -e "${RED}ERROR: Failed to make hooks executable.${NC}"
        exit 1
    fi
else
    echo -e "${YELLOW}WARNING: .githooks/pre-commit file not found. Hook will be created when needed.${NC}"
fi

# Also make this script executable
chmod +x "$0"

echo ""
echo -e "${GREEN}SUCCESS: Pre-commit hooks setup complete!${NC}"
echo ""
echo -e "${CYAN}What happens now:${NC}"
echo -e "${WHITE}  * Code will be automatically formatted before each commit${NC}"
echo -e "${WHITE}  * Only staged files will be processed${NC}"
echo -e "${WHITE}  * A quick build check will run to catch compilation errors${NC}"
echo ""
echo -e "${CYAN}Commands:${NC}"
echo -e "${WHITE}  * Normal commit: git commit -m \"your message\"${NC}"
echo -e "${WHITE}  * Skip hooks (not recommended): git commit --no-verify -m \"your message\"${NC}"
echo -e "${WHITE}  * Manual format: dotnet format TS4Tools.sln${NC}"
echo ""
echo -e "${YELLOW}To disable hooks later:${NC}"
echo -e "${WHITE}   git config --unset core.hooksPath${NC}"
