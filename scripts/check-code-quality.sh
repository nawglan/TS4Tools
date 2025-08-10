#!/bin/bash

# TS4Tools Code Quality Checker
# Run code formatting and analyzer checks locally

set -e

SOLUTION_FILE="TS4Tools.sln"
FIX=false
VERBOSE=false

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --fix|-f)
            FIX=true
            shift
            ;;
        --verbose|-v)
            VERBOSE=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --fix, -f      Automatically fix formatting issues"
            echo "  --verbose, -v  Show detailed output"
            echo "  --help, -h     Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0              Run all checks"
            echo "  $0 --fix        Fix formatting and run checks"
            echo "  $0 --verbose    Run checks with verbose output"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}🔍 TS4Tools Code Quality Checker${NC}"
echo -e "${CYAN}=================================${NC}"
echo ""

# Check if solution file exists
if [ ! -f "$SOLUTION_FILE" ]; then
    echo -e "${RED}❌ Solution file '$SOLUTION_FILE' not found!${NC}"
    echo -e "${RED}Please run this script from the repository root.${NC}"
    exit 1
fi

# Step 1: Restore dependencies
echo -e "${CYAN}📦 Restoring NuGet packages...${NC}"
if ! dotnet restore "$SOLUTION_FILE" --verbosity minimal; then
    echo -e "${RED}❌ Failed to restore dependencies${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Dependencies restored successfully${NC}"
echo ""

# Step 2: Code formatting
echo -e "${CYAN}🎨 Checking code formatting...${NC}"

if [ "$FIX" = true ]; then
    echo -e "${YELLOW}🔧 Auto-fixing formatting issues...${NC}"
    if ! dotnet format "$SOLUTION_FILE"; then
        echo -e "${RED}❌ Failed to apply formatting${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Code formatting applied${NC}"
else
    VERBOSITY="minimal"
    if [ "$VERBOSE" = true ]; then
        VERBOSITY="diagnostic"
    fi
    
    if ! dotnet format "$SOLUTION_FILE" --verify-no-changes --verbosity "$VERBOSITY"; then
        echo -e "${RED}❌ Code formatting issues detected!${NC}"
        echo ""
        echo -e "${YELLOW}🔧 To fix automatically:${NC}"
        echo -e "${YELLOW}   $0 --fix${NC}"
        echo -e "${YELLOW}   or: dotnet format $SOLUTION_FILE${NC}"
        echo ""
        exit 1
    fi
    echo -e "${GREEN}✅ All code is properly formatted${NC}"
fi
echo ""

# Step 3: Build with analyzers
echo -e "${CYAN}🔍 Running .NET analyzers...${NC}"
BUILD_VERBOSITY="minimal"
if [ "$VERBOSE" = true ]; then
    BUILD_VERBOSITY="normal"
fi

if ! dotnet build "$SOLUTION_FILE" --no-restore --configuration Release --verbosity "$BUILD_VERBOSITY" --property:TreatWarningsAsErrors=true; then
    echo -e "${RED}❌ Analyzer warnings detected!${NC}"
    echo ""
    echo -e "${YELLOW}💡 Common solutions:${NC}"
    echo -e "${YELLOW}   • Check for unused variables or imports${NC}"
    echo -e "${YELLOW}   • Ensure proper null handling${NC}"
    echo -e "${YELLOW}   • Follow naming conventions${NC}"
    echo -e "${YELLOW}   • Add XML documentation for public APIs${NC}"
    echo ""
    echo -e "${YELLOW}🔍 For detailed output, run with --verbose flag${NC}"
    exit 1
fi
echo -e "${GREEN}✅ All analyzer checks passed${NC}"
echo ""

# Step 4: Run tests
echo -e "${CYAN}🧪 Running unit tests...${NC}"
if ! dotnet test "$SOLUTION_FILE" --no-build --configuration Release --verbosity minimal --logger "console;verbosity=minimal"; then
    echo -e "${RED}❌ Some tests failed!${NC}"
    echo -e "${RED}Please fix failing tests before committing.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ All tests passed${NC}"
echo ""

# Summary
echo -e "${GREEN}🎉 All code quality checks passed!${NC}"
echo ""
echo -e "${GREEN}✅ Code formatting: OK${NC}"
echo -e "${GREEN}✅ .NET analyzers: OK${NC}"
echo -e "${GREEN}✅ Unit tests: OK${NC}"
echo ""
echo -e "${GREEN}🚀 Your code is ready to commit!${NC}"

# Optional: Show git status
if command -v git >/dev/null 2>&1; then
    if [ -n "$(git status --porcelain)" ]; then
        echo ""
        echo -e "${CYAN}📝 Git status:${NC}"
        git status --short
        echo ""
        echo -e "${YELLOW}💡 Don't forget to commit your changes:${NC}"
        echo -e "${YELLOW}   git add -A${NC}"
        echo -e "${YELLOW}   git commit -m 'Your commit message'${NC}"
    fi
fi
