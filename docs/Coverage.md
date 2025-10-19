# Code Coverage Guide

This guide explains how to measure, analyze, and improve code coverage in ProbotSharp, including both line and branch coverage metrics.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Coverage Goals](#coverage-goals)
- [Running Coverage Locally](#running-coverage-locally)
- [Understanding Coverage Reports](#understanding-coverage-reports)
- [CI/CD Integration](#cicd-integration)
- [Improving Coverage](#improving-coverage)
- [Advanced Analysis](#advanced-analysis)
- [Troubleshooting](#troubleshooting)

## Overview

ProbotSharp uses a comprehensive coverage suite that provides:

- **Line Coverage**: Percentage of code lines executed by tests
- **Branch Coverage**: Percentage of decision branches (if/else, switch) covered
- **Method Coverage**: Percentage of methods called at least once
- **Per-layer Analysis**: Separate metrics for Domain, Application, Infrastructure layers
- **Historical Tracking**: Coverage trends over time
- **CI/CD Integration**: Automated coverage in GitHub Actions with PR comments

### Technology Stack

- **Coverlet**: .NET code coverage collection
- **ReportGenerator**: HTML report generation and analysis
- **GitHub Actions**: CI/CD automation
- **Codecov** (optional): Cloud-based coverage tracking

## Quick Start

### Using Make Commands (Recommended)

```bash
# Run full coverage analysis
make coverage

# Run per-layer coverage
make coverage-layer

# Watch mode for development
make coverage-watch

# Open HTML report in browser
make coverage-report
```

### Using Scripts Directly

```bash
# Run full coverage
./scripts/coverage/run-coverage-all.sh

# Run per-layer analysis
./scripts/coverage/run-coverage-by-layer.sh

# Analyze coverage gaps
./scripts/coverage/analyze-coverage-gaps.sh
```

## Coverage Goals

ProbotSharp follows a tiered coverage strategy based on architectural layers:

| Layer | Line Coverage Target | Branch Coverage Target | Rationale |
|-------|---------------------|------------------------|-----------|
| **Domain** | 90% | 85% | Core business logic, must be thoroughly tested |
| **Application** | 80% | 75% | Use cases and orchestration logic |
| **Infrastructure** | 70% | 65% | External integrations, often tested via integration tests |
| **Adapters** | 70% | 65% | HTTP/CLI/Worker adapters |
| **Shared** | 85% | 80% | Common utilities and extensions |
| **Bootstrap** | 60% | 50% | Composition root, mainly configuration |
| **Overall** | 80% | 75% | Project-wide minimum target |

## Running Coverage Locally

### Prerequisites

1. Install .NET tools:
   ```bash
   dotnet tool restore
   ```

2. Build the solution:
   ```bash
   dotnet build --configuration Release
   ```

### Full Coverage Analysis

Run complete coverage for all projects:

```bash
make coverage
```

This will:
1. Run all tests with coverage collection
2. Generate HTML reports with branch coverage
3. Display summary in terminal
4. Open report in browser (if available)

Output locations:
- HTML Report: `coverage-report/index.html`
- Cobertura XML: `coverage-report/Cobertura.xml`
- Coverage Badge: `coverage-report/badge_combined.svg`

### Per-Layer Coverage

Analyze coverage for each architectural layer separately:

```bash
make coverage-layer
```

This generates individual reports:
- Domain: `coverage-report-Domain/index.html`
- Application: `coverage-report-Application/index.html`
- Infrastructure: `coverage-report-Infrastructure/index.html`
- And more...

Each layer is evaluated against its specific thresholds, and the script will fail if any layer is below its target.

### Watch Mode

For continuous feedback during development:

```bash
make coverage-watch
# Or specify a target:
./scripts/coverage/run-coverage-watch.sh domain
```

Watch mode:
- Monitors file changes
- Re-runs coverage automatically
- Updates HTML report in real-time
- Shows coverage metrics in terminal

### Coverage Comparison

Compare coverage between branches:

```bash
# Save current coverage snapshot
make coverage-snapshot

# Compare with main branch
make coverage-compare

# Show coverage trend over time
make coverage-trend
```

## Understanding Coverage Reports

### HTML Report Navigation

The HTML report (`coverage-report/index.html`) provides:

1. **Summary Page**
   - Overall coverage percentages
   - Coverage by assembly
   - Risk hotspots (low coverage + high complexity)

2. **Assembly View**
   - Class-level coverage
   - Namespace breakdown
   - Coverage distribution chart

3. **Class View**
   - Method-level coverage
   - Line-by-line coverage visualization
   - Branch coverage indicators

4. **Source View**
   - Green lines: Covered code
   - Red lines: Uncovered code
   - Yellow lines: Partially covered branches
   - Numbers: Hit count for each line

### Coverage Metrics Explained

#### Line Coverage
Percentage of executable lines that were executed during tests.

```text
public class Calculator
{
    public int Calculate(int a, int b)  // ✓ Covered if method is called
    {
        if (a > 0)                       // ✓ Covered if reached
        {
            return a + b;                // ✓ Covered if a > 0
        }
        return b;                        // ✗ Not covered if a always > 0 in tests
    }
}
```

#### Branch Coverage
Percentage of decision branches taken during tests.

```text
public class StatusChecker
{
    public string GetStatus(int value)
    {
        // Branch coverage requires testing both true AND false paths
        if (value > 100)      // Need tests for value > 100 AND value <= 100
        {
            return "High";
        }
        else if (value > 50)  // Need tests for value > 50 AND value <= 50
        {
            return "Medium";
        }
        return "Low";
    }
}
```

#### Method Coverage
Percentage of methods that were invoked at least once.

### Coverage Gaps Analysis

Run gap analysis to identify untested code:

```bash
make coverage-gaps
```

This generates `coverage-gaps.md` with:
- Files with low coverage (<50%)
- Uncovered methods list
- Priority recommendations
- Action items for improvement

## CI/CD Integration

### GitHub Actions Workflow

Coverage runs automatically on:
- Every push to main branch
- All pull requests
- When source code changes

#### Main CI Pipeline (.github/workflows/dotnet.yml)

- Runs tests with coverage collection
- Generates coverage report
- Uploads to Codecov (if configured)
- Creates coverage artifacts
- Displays summary in workflow

#### PR Coverage Report (.github/workflows/coverage-report.yml)

For pull requests, provides:
- Coverage comparison with base branch
- Automatic PR comment with metrics
- Pass/fail status based on thresholds
- Coverage delta visualization

Example PR comment:
```markdown
## 📊 Coverage Report

✅ Coverage improved!

| Metric | Base Branch | This PR | Delta |
|--------|-------------|---------|-------|
| Line Coverage | 75.3% | 78.1% | 📈 +2.8% |
| Branch Coverage | 70.2% | 73.5% | 📈 +3.3% |
```

### Coverage Badges

Add coverage badges to README.md (after running coverage analysis):

```markdown
![Coverage Status](../coverage-report/badges/coverage-status.svg)
![Line Coverage](../coverage-report/badges/coverage-line.svg)
![Branch Coverage](../coverage-report/badges/coverage-branch.svg)
```

**Note:** These badges are generated automatically when running `make coverage-badges` after coverage collection. The badge files will be placed in the `coverage-report/badges/` directory.

## Improving Coverage

### Strategic Approach

1. **Focus on Domain Layer First**
   - Highest value for testing
   - Core business logic
   - Target: >90% coverage

2. **Test Critical Paths**
   - Authentication/Authorization
   - Data validation
   - Error handling
   - External API interactions

3. **Use Appropriate Test Types**
   - Unit tests for Domain/Application
   - Integration tests for Infrastructure
   - Contract tests for APIs

### Writing Effective Tests

#### Test All Branches

```text
using ProbotSharp.Shared.Abstractions;
using NUnit.Framework;

// Code under test
public class Calculator
{
    public Result<int> Divide(int a, int b)
    {
        if (b == 0)
            return Result<int>.Failure("DivisionByZero", "Cannot divide by zero");

        return Result<int>.Success(a / b);
    }
}

// Tests needed for full branch coverage
[TestFixture]
public class CalculatorTests
{
    private Calculator calculator = new Calculator();

    [Test]
    public void Divide_WithNonZeroDivisor_ReturnsSuccess()
    {
        var result = calculator.Divide(10, 2);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(5, result.Value);
    }

    [Test]
    public void Divide_WithZeroDivisor_ReturnsFailure()
    {
        var result = calculator.Divide(10, 0);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("DivisionByZero", result.Error.Code);
    }
}
```

#### Test Edge Cases

```text
using NUnit.Framework;

[TestFixture]
public class EdgeCaseTests
{
    [TestCase(int.MaxValue, 1)]      // Boundary values
    [TestCase(int.MinValue, -1)]     // Overflow scenarios
    [TestCase(0, 1)]                 // Zero handling
    [TestCase(-10, -2)]              // Negative numbers
    public void Calculate_EdgeCases(int a, int b)
    {
        // Test implementation
    }
}
```

#### Use Property-Based Testing

For value objects and specifications:

```text
using FsCheck;
using FsCheck.NUnit;

public class EmailAddress
{
    public string Value { get; }
    public EmailAddress(string value) => Value = value;
    public override bool Equals(object obj) => obj is EmailAddress other && Value == other.Value;
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
}

public class PropertyTests
{
    [Property]
    public Property EmailAddress_Equality_IsSymmetric()
    {
        return Prop.ForAll<string, string>((email1, email2) =>
        {
            var addr1 = new EmailAddress(email1);
            var addr2 = new EmailAddress(email2);
            return (addr1.Equals(addr2)) == (addr2.Equals(addr1));
        });
    }
}
```

### Excluding Code from Coverage

Some code should be excluded from coverage metrics:

#### In Source Code

```text
using System.Diagnostics.CodeAnalysis;

// Exclude entire class
[ExcludeFromCodeCoverage]
public class MigrationConfiguration { }

// Exclude specific method
public class Service
{
    [ExcludeFromCodeCoverage]
    public void DebugOnlyMethod() { }
}
```

#### In coverlet.runsettings

Already configured exclusions:
- Database migrations
- Auto-generated code
- Designer files
- Test assemblies

## Advanced Analysis

### Mutation Testing

Consider adding mutation testing to verify test quality:

```bash
dotnet tool install -g dotnet-stryker
dotnet stryker
```

### Cyclomatic Complexity

High complexity + low coverage = high risk:

```bash
# Identify complex methods needing tests
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.xml -targetdir:report -reporttypes:Html -riskhotspots
```

### Historical Tracking

Track coverage trends over time:

```bash
# Save snapshot after each significant change
./scripts/coverage/compare-coverage.sh snapshot

# View trend
./scripts/coverage/compare-coverage.sh trend
```

## Troubleshooting

### Common Issues

#### No Coverage Data Generated

**Problem**: Tests run but no coverage files created

**Solutions**:
- Ensure `coverlet.collector` package is installed in test projects
- Check `coverlet.runsettings` exists and is valid
- Verify `--collect:"XPlat Code Coverage"` parameter is present

#### Low Coverage Despite Many Tests

**Problem**: Coverage lower than expected

**Causes**:
- Tests not exercising all code paths
- Async code not being awaited properly
- Excluded files included in metrics
- Dead code that's never executed

**Solutions**:
- Review uncovered code in HTML report
- Add tests for edge cases and error paths
- Remove unreachable code
- Ensure async tests use proper assertions

#### Branch Coverage Lower Than Line Coverage

**Problem**: Good line coverage but poor branch coverage

**Solution**: Ensure tests cover all decision paths:

```text
public class NumberCategorizer
{
    // Needs 4 tests for full branch coverage
    public string Categorize(int? value)
    {
        if (value == null)           // Test: null
            return "Unknown";

        if (value < 0)               // Test: negative
            return "Negative";

        if (value > 0)               // Test: positive
            return "Positive";

        return "Zero";               // Test: zero
    }
}
```

#### Coverage Report Not Opening

**Problem**: Browser doesn't open automatically

**Solution**:
```bash
# Linux
xdg-open coverage-report/index.html

# macOS
open coverage-report/index.html

# Windows
start coverage-report/index.html

# Or use the file:// URL directly
file:///path/to/probot-sharp/coverage-report/index.html
```

### Performance Optimization

For large test suites:

1. **Parallel Test Execution**
   ```xml
   <RunSettings>
     <RunConfiguration>
       <MaxCpuCount>0</MaxCpuCount> <!-- Use all cores -->
     </RunConfiguration>
   </RunSettings>
   ```

2. **Exclude Slow Tests from Coverage**
   ```text
   using System.Diagnostics.CodeAnalysis;
   using Microsoft.VisualStudio.TestTools.UnitTesting;

   [TestClass]
   public class IntegrationTests
   {
       [TestCategory("SlowIntegration")]
       [ExcludeFromCodeCoverage]
       [TestMethod]
       public void LongRunningIntegrationTest() { }
   }
   ```

3. **Use Coverage Filters**
   ```bash
   # Only measure specific assemblies
   dotnet test --collect:"XPlat Code Coverage" \
     -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include=[ProbotSharp.Domain]*
   ```

## Best Practices

1. **Run Coverage Regularly**
   - Before committing changes
   - In CI/CD pipeline
   - During code reviews

2. **Set Realistic Goals**
   - Start with current baseline
   - Improve incrementally
   - Focus on critical code first

3. **Don't Chase 100%**
   - Some code doesn't need testing (e.g., simple DTOs)
   - Focus on valuable tests, not coverage percentage
   - Quality over quantity

4. **Review Coverage in PRs**
   - Check coverage doesn't decrease
   - Ensure new code is tested
   - Look for untested edge cases

5. **Use Coverage as a Guide**
   - Identifies untested code
   - Highlights risky areas
   - Not the only measure of quality

## Additional Resources

- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [.NET Code Coverage Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
- [Branch Coverage Explained](https://www.atlassian.com/continuous-delivery/software-testing/code-coverage)

## Summary

The ProbotSharp coverage suite provides comprehensive tools for measuring and improving code quality through systematic testing. Use the layered approach to focus efforts where they matter most, and remember that coverage is a tool for finding untested code, not a goal in itself.