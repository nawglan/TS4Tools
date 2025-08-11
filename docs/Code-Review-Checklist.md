# Code Review Checklist

**Use this checklist for all pull requests to ensure consistent code quality and architecture adherence.**

---

## ðŸŽ¯ **TS4Tools-Specific Requirements (ADR Compliance)**

### Greenfield Migration Strategy (ADR-004)

- [ ] **Business Logic Extraction**: Extracts domain knowledge without copying old code structures
- [ ] **API Compatibility**: Existing third-party tools work without modification
- [ ] **Legacy Adapter Pattern**: Uses adapter pattern for backward compatibility where needed
- [ ] **Performance Preservation**: Maintains or improves upon legacy performance metrics

### Assembly Loading Modernization (ADR-005)

- [ ] **AssemblyLoadContext Usage**: Uses modern `AssemblyLoadContext` instead of `Assembly.LoadFile()`
- [ ] **Plugin Loading**: Proper plugin assembly loading with context management
- [ ] **Memory Management**: Assembly contexts properly disposed to prevent memory leaks
- [ ] **Type Resolution**: Cross-assembly type resolution works correctly

### Golden Master Testing (ADR-006)

- [ ] **Byte-Perfect Validation**: Changes validated against real Sims 4 package files
- [ ] **Format Preservation**: Binary formats remain identical to legacy implementation
- [ ] **Regression Testing**: Performance and compatibility regression tests included
- [ ] **Test Data Coverage**: Uses diverse real-world package files for validation

### Plugin Architecture (ADR-007)

- [ ] **Legacy Plugin Support**: Existing community plugins work without modification
- [ ] **Modern Plugin Interface**: New plugins can use dependency injection and modern patterns
- [ ] **Resource Type Discovery**: Plugin discovery and registration works correctly
- [ ] **Error Handling**: Plugin loading failures are handled gracefully

### Cross-Platform Compatibility (ADR-008)

- [ ] **Binary Format Consistency**: Package files identical across Windows/Linux/macOS
- [ ] **Compression Algorithms**: Compression produces identical output across platforms
- [ ] **File I/O Operations**: File operations work correctly on all target filesystems
- [ ] **Memory Layout**: Struct packing and endianness handled correctly

---

## ðŸ—ï¸ **Architecture & Design**

### Dependency Injection

- [ ] **Constructor Injection**: All dependencies injected via constructor, not property/method injection
- [ ] **Null Checks**: All injected dependencies have null checks with `ArgumentNullException`
- [ ] **Interface Usage**: Depends on interfaces, not concrete implementations
- [ ] **Service Lifetimes**: Appropriate service lifetime (Singleton/Scoped/Transient) chosen
- [ ] **No Service Locator**: Avoids static service locator pattern (except legacy compatibility)

### SOLID Principles

- [ ] **Single Responsibility**: Each class has one clear purpose and reason to change
- [ ] **Open/Closed**: Classes open for extension, closed for modification
- [ ] **Liskov Substitution**: Derived classes can substitute base classes without breaking functionality
- [ ] **Interface Segregation**: Interfaces are focused and don't force unnecessary dependencies
- [ ] **Dependency Inversion**: Depends on abstractions, not concretions

### Cross-Platform Compatibility

- [ ] **File Paths**: Uses `Path.Combine()` instead of string concatenation
- [ ] **Platform APIs**: Uses cross-platform .NET APIs only
- [ ] **Case Sensitivity**: Considers case-sensitive file systems (Linux/macOS)
- [ ] **Path Separators**: No hardcoded `\` or `/` in paths
- [ ] **Platform-Specific Code**: Properly conditionally compiled (`#if WINDOWS`)

---

## ðŸš€ **Performance & Memory**

### Memory Management

- [ ] **IDisposable**: Implements and calls `Dispose()` for unmanaged resources
- [ ] **Using Statements**: Uses `using` statements or `using var` for disposable objects
- [ ] **Array Pooling**: Uses `ArrayPool<T>` for temporary large arrays
- [ ] **String Builder**: Uses `StringBuilder` for multiple string concatenations
- [ ] **Span Usage**: Uses `Span<T>` and `ReadOnlySpan<T>` for zero-allocation operations

### Async/Await Patterns

- [ ] **ConfigureAwait**: Uses `ConfigureAwait(false)` in library code
- [ ] **Async All The Way**: Async methods call async methods, no `Result` or `Wait()`
- [ ] **CancellationToken**: Accepts and properly handles `CancellationToken` parameters
- [ ] **Task vs ValueTask**: Uses `ValueTask<T>` for frequently-called, fast-completing operations
- [ ] **Async Naming**: Async methods end with `Async` suffix

### Algorithm Efficiency

- [ ] **Big-O Complexity**: Considers algorithmic complexity for large datasets
- [ ] **Collection Choice**: Chooses appropriate collection types (`Dictionary`, `HashSet`, etc.)
- [ ] **LINQ Performance**: Avoids inefficient LINQ operations in hot paths
- [ ] **Early Returns**: Uses early returns to avoid unnecessary processing
- [ ] **Caching Strategy**: Implements appropriate caching where beneficial

---

## ðŸ§ª **Testing & Quality**

### Unit Tests

- [ ] **AAA Pattern**: Tests use Arrange-Act-Assert structure clearly
- [ ] **Test Coverage**: New code has 95%+ unit test coverage
- [ ] **Meaningful Names**: Test names describe the scenario and expected outcome
- [ ] **Single Assert**: Each test verifies one specific behavior
- [ ] **No Logic**: Tests don't contain business logic or complex conditionals

### Test Quality

- [ ] **Mocking**: Uses interfaces and dependency injection for proper mocking
- [ ] **Test Data**: Uses test builders or fixture patterns for complex test data
- [ ] **Isolated Tests**: Tests don't depend on external resources or other tests
- [ ] **Deterministic**: Tests produce consistent results across runs
- [ ] **Performance Tests**: Critical paths have performance benchmark tests

### Integration Tests

- [ ] **Service Integration**: Tests verify services work together correctly
- [ ] **Configuration**: Tests verify configuration binding and validation
- [ ] **Error Handling**: Tests verify error scenarios and edge cases
- [ ] **Cleanup**: Tests properly clean up resources and state

---

## ðŸ“ **Code Style & Documentation**

### Code Formatting

- [ ] **EditorConfig**: Code follows `.editorconfig` formatting rules
- [ ] **Naming Conventions**: Follows C# naming conventions (PascalCase, camelCase, etc.)
- [ ] **File Organization**: Using statements, namespace, classes organized logically
- [ ] **Line Length**: Lines under 120 characters (prefer 80-100)
- [ ] **Consistent Style**: Consistent indentation, spacing, and brace placement

### Documentation

- [ ] **XML Comments**: Public APIs have comprehensive XML documentation
- [ ] **Parameter Docs**: All parameters documented with `<param>` tags
- [ ] **Return Docs**: Return values documented with `<returns>` tags
- [ ] **Example Usage**: Complex APIs include `<example>` sections
- [ ] **Inline Comments**: Complex logic explained with inline comments

### Code Clarity

- [ ] **Descriptive Names**: Variables, methods, classes have clear, descriptive names
- [ ] **Magic Numbers**: No magic numbers; uses named constants or enums
- [ ] **Method Length**: Methods are focused and typically under 50 lines
- [ ] **Cognitive Complexity**: Code is easy to understand and reason about
- [ ] **Error Messages**: Meaningful error messages with context

---

## ðŸ”’ **Security & Reliability**

### Input Validation

- [ ] **Null Checks**: All public method parameters validated for null
- [ ] **Range Validation**: Numeric parameters validated for acceptable ranges
- [ ] **String Validation**: String parameters checked for null/empty/whitespace
- [ ] **File Paths**: File paths validated and sanitized
- [ ] **User Input**: All user input properly validated and sanitized

### Error Handling

- [ ] **Exception Types**: Throws appropriate exception types (`ArgumentException`, etc.)
- [ ] **Exception Messages**: Meaningful messages that help developers diagnose issues
- [ ] **Resource Cleanup**: Resources cleaned up in finally blocks or using statements
- [ ] **Partial Failures**: Handles partial failures gracefully in bulk operations
- [ ] **Logging**: Appropriate logging for errors and important operations

### Thread Safety

- [ ] **Concurrent Access**: Thread-safe for expected usage patterns
- [ ] **Immutable Objects**: Prefers immutable objects where possible
- [ ] **Proper Locking**: Uses appropriate synchronization primitives if needed
- [ ] **Async Safety**: Async operations don't create race conditions
- [ ] **Static State**: Minimizes or eliminates mutable static state

---

## ðŸ·ï¸ **Git & CI/CD**

### Commit Quality

- [ ] **Conventional Commits**: Follows conventional commit format (`feat:`, `fix:`, etc.)
- [ ] **Atomic Commits**: Each commit represents a single logical change
- [ ] **Descriptive Messages**: Commit messages clearly describe what and why
- [ ] **No Merge Commits**: Uses rebase workflow to maintain clean history
- [ ] **Signed Commits**: Commits are signed for security (if required)

### Pull Request

- [ ] **Clear Description**: PR description explains the change and rationale
- [ ] **Breaking Changes**: Breaking changes clearly documented
- [ ] **Migration Guide**: Includes migration instructions if needed
- [ ] **Issue Reference**: References related GitHub issues
- [ ] **Screenshots**: UI changes include before/after screenshots

### CI/CD Pipeline

- [ ] **Build Success**: All CI/CD pipeline checks pass
- [ ] **Test Coverage**: Code coverage meets minimum threshold (95%)
- [ ] **Performance Gates**: Performance benchmarks within acceptable limits
- [ ] **Security Scan**: Security analysis passes without critical issues
- [ ] **Cross-Platform**: Builds and tests pass on all supported platforms

---

## ðŸ“‹ **Reviewer Checklist**

### Before Starting Review

- [ ] **Pull Latest**: Reviewed branch is up-to-date with target branch
- [ ] **Build Locally**: Code builds and tests pass locally
- [ ] **Code Quality Check**: Run `.\scripts\check-quality.ps1` to verify formatting and analyzers pass
- [ ] **Requirements**: Understand the requirements and acceptance criteria
- [ ] **Architecture**: Change aligns with overall architecture decisions

### During Review

- [ ] **Code Logic**: Logic is correct and handles edge cases
- [ ] **Performance**: No obvious performance issues or anti-patterns
- [ ] **Security**: No security vulnerabilities or sensitive data exposure
- [ ] **Maintainability**: Code will be easy to maintain and extend
- [ ] **Documentation**: Changes are properly documented

### Review Feedback

- [ ] **Constructive**: Feedback is specific, actionable, and constructive
- [ ] **Explanatory**: Explains the reasoning behind suggested changes
- [ ] **Prioritized**: Distinguishes between must-fix issues and suggestions
- [ ] **Code Examples**: Provides code examples for suggested improvements
- [ ] **Positive Recognition**: Acknowledges good patterns and solutions

---

## ðŸŽ¯ **Definition of Done**

A pull request is ready to merge when:

- [ ] **All checklist items above are satisfied**
- [ ] **Two approving reviews** from qualified team members
- [ ] **All CI/CD pipeline checks pass** (build, test, security, performance)
- [ ] **Documentation updated** (ADRs, README, API docs as needed)
- [ ] **README.md updated** if changes affect user-facing functionality or setup
- [ ] **MIGRATION_ROADMAP.md updated** if completing phases, milestones, or major implementations
- [ ] **CHANGELOG.md updated** with detailed technical accomplishments and implementation details
- [ ] **Phase completion documentation updated** if work completes a project phase (mark phase as âœ… COMPLETED with date in roadmap and changelog)
- [ ] **Performance impact assessed** and within acceptable limits
- [ ] **Breaking changes documented** with migration guide if applicable

---

## ðŸš¨ **Red Flags - Block the PR**

**The following issues should prevent merging:**

- âŒ **Build Failures**: Code doesn't compile or tests fail
- âŒ **Security Vulnerabilities**: Code introduces security risks
- âŒ **Performance Regressions**: Significant performance degradation
- âŒ **Breaking Changes**: Undocumented breaking changes to public APIs
- âŒ **Test Coverage**: Test coverage drops below 95% threshold
- âŒ **Memory Leaks**: Code introduces memory leaks or resource leaks
- âŒ **Platform Compatibility**: Breaks cross-platform compatibility
- âŒ **Architecture Violations**: Violates established architecture principles

---

## ðŸ’¡ **Review Tips**

### For Authors

- **Self-Review First**: Review your own PR before requesting reviews
- **Run Code Quality Checks**: Execute `.\scripts\check-quality.ps1` before submitting PR
- **Small PRs**: Keep PRs focused and under 400 lines when possible
- **Context**: Provide sufficient context in PR description
- **Address Feedback**: Respond to all review comments promptly
- **Learn from Reviews**: Use feedback as learning opportunities

### For Reviewers

- **Timely Reviews**: Provide feedback within 24 hours when possible
- **Focus on Important Issues**: Don't nitpick minor style issues
- **Ask Questions**: Ask clarifying questions when unsure
- **Suggest Alternatives**: Provide alternative solutions, not just criticism
- **Test the Code**: Pull and test complex changes locally

---

**Remember**: Code reviews are about **learning**, **knowledge sharing**, and **maintaining high quality**. Be respectful, constructive, and focus on the code, not the person.

---

**Last Updated**: August 9, 2025
**Related Documents**: [Developer Onboarding Guide](Developer-Onboarding-Guide.md), [Architecture ADRs](architecture/adr/)

