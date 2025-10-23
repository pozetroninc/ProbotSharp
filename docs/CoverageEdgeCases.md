# Coverage Workflow Edge Case Handling

This document explains how the ProbotSharp coverage workflow handles edge cases to provide intelligent, context-aware coverage reporting.

## Overview

The coverage workflow automatically detects different types of changes and adjusts its behavior accordingly. This prevents false negatives, reduces CI noise, and provides meaningful feedback for all types of pull requests.

## Edge Cases Handled

### 1. Config-Only Changes

**Scenario:** PR only modifies configuration files (`.json`, `.csproj`, `.sln`, `.yml`) with no source code changes.

**Behavior:**
- Coverage checks are **skipped** entirely
- Workflow passes immediately
- Comment posted explaining no source code was changed

**Example Changes:**
- `appsettings.json` updates
- Project file dependency changes
- Build configuration modifications
- Workflow file updates

**Status:** ✅ Pass (Coverage check skipped)

**Message:**
```
✅ No source code changes detected - Coverage check skipped

Change Summary: 3 config file(s) changed, no source code modifications
```

---

### 2. Test-Only Changes

**Scenario:** PR only modifies test files with no production source code changes.

**Behavior:**
- Coverage checks run in **informational mode** (non-blocking)
- Workflow always passes regardless of coverage metrics
- Full coverage report still generated for visibility

**Example Changes:**
- Adding new test cases
- Refactoring test helpers
- Updating test fixtures
- Fixing flaky tests

**Status:** ✅ Pass (Informational)

**Message:**
```
ℹ️ Test-only changes detected - Coverage check is informational

Change Summary: 5 test file(s) modified, no production code changes
```

**Rationale:** Test improvements should never be blocked by coverage thresholds, but coverage visibility is still useful to ensure tests are actually running.

---

### 3. Merge Commits with Minimal Changes

**Scenario:** PR is a merge commit with fewer than 3 source files modified (typically conflict resolution).

**Behavior:**
- Coverage checks run in **informational mode** (non-blocking)
- Thresholds checked but treated as warnings
- Workflow passes even if coverage decreases slightly

**Example Changes:**
- Merging main branch into feature branch
- Resolving merge conflicts
- Updating imports/namespaces after merge

**Status:** ✅ Pass or ⚠️ Warning (Informational)

**Message:**
```
✅ Merge commit - Coverage maintained

Change Summary: Merge commit with 2 source file(s) and 1 test file(s) modified
```

**Rationale:** Merge commits often contain no new logic, just conflict resolution. Failing these PRs creates unnecessary friction.

---

### 4. Missing Base Coverage Report

**Scenario:** Base branch coverage report fails to generate (rare, but can happen on first PR after major refactoring).

**Behavior:**
- **Graceful degradation** instead of failing
- Falls back to absolute threshold checking (80% line, 75% branch)
- Warning logged explaining base comparison unavailable
- Workflow doesn't fail just because base report is missing

**Status:** ✅ Pass or ❌ Fail (based on absolute thresholds)

**Message:**
```
⚠️ Base branch coverage report not found - using absolute thresholds only

✅ Coverage meets minimum thresholds (base comparison unavailable)
```

**Rationale:** Technical issues with base branch shouldn't block valid PRs. Absolute thresholds still ensure minimum quality.

---

### 5. Source Code Changes (Normal Mode)

**Scenario:** PR modifies production source code.

**Behavior:**
- Full coverage enforcement enabled
- Compares against base branch
- Fails if below thresholds (80% line, 75% branch)
- Fails if coverage decreases by >5%
- Warns if coverage decreases slightly

**Status:** ✅ Pass / ⚠️ Warning / ❌ Fail

**Enforcement Rules:**
1. **Absolute minimum:** 80% line coverage, 75% branch coverage
2. **Significant decrease:** >5% drop fails the build
3. **Slight decrease:** Any decrease triggers warning
4. **Improvement:** Both metrics increase = celebration! 🎉

---

## Detection Logic

The workflow uses the following detection logic in `.github/workflows/coverage-report.yml`:

### File Classification

```bash
# Source files (excluding generated code)
SRC_FILES: src/**/*.cs (excluding Migrations/, *.Designer.cs, DbContextModelSnapshot.cs)

# Test files
TEST_FILES: tests/**/*.cs

# Config files
CONFIG_FILES: *.json, *.csproj, *.sln, *.props, *.targets

# Workflow files
WORKFLOW_FILES: .github/workflows/**/*.yml

# Documentation
DOC_FILES: *.md, *.txt
```

### Change Type Determination

```
if (SRC_FILES == 0 && TEST_FILES == 0):
    → config-only (skip coverage)

elif (SRC_FILES == 0 && TEST_FILES > 0):
    → test-only (informational)

elif (IS_MERGE_COMMIT && SRC_FILES < 3):
    → merge-commit (informational)

else:
    → source-code (full enforcement)
```

---

## Benefits

### 1. Reduced False Negatives
- Merge commits no longer fail with "0% coverage"
- Config changes don't trigger unnecessary coverage runs
- Test improvements never blocked

### 2. Better Developer Experience
- Clear messaging about why checks passed/failed
- Appropriate enforcement level for change type
- Less time wasted on irrelevant CI failures

### 3. Improved CI Efficiency
- Skips expensive coverage runs when not needed
- Faster feedback for config-only changes
- Better resource utilization

### 4. Graceful Degradation
- Technical issues don't block valid PRs
- Falls back to absolute thresholds when needed
- Logs warnings instead of failing

---

## PR Comment Example

For a merge commit with minimal changes:

```markdown
## 📊 Coverage Report

✅ Merge commit - Coverage maintained

Change Summary: Merge commit with 2 source file(s) and 1 test file(s) modified

| Metric | Base Branch | This PR | Delta |
|--------|-------------|---------|-------|
| **Line Coverage** | 82.4% | 82.3% | ➡️ -0.1% |
| **Branch Coverage** | 76.1% | 76.0% | ➡️ -0.1% |

<details>
<summary>Edge Case Handling</summary>

- **Config-only changes**: Coverage check skipped automatically
- **Test-only changes**: Coverage check runs in informational mode (non-blocking)
- **Merge commits**: Relaxed enforcement for conflict resolution
- **Missing base coverage**: Falls back to absolute threshold checking

</details>
```

---

## Configuration

All edge case thresholds and rules are defined in `.github/workflows/coverage-report.yml`:

```yaml
# Minimum thresholds for source code changes
MIN_LINE_COVERAGE: 80
MIN_BRANCH_COVERAGE: 75

# Merge commit threshold (< 3 source files = informational mode)
MERGE_COMMIT_THRESHOLD: 3

# Significant decrease threshold (triggers failure)
SIGNIFICANT_DECREASE: 5%
```

---

## Testing Edge Cases

To test each edge case:

### Config-Only
```bash
# Modify only config files
git checkout -b test-config-only
echo '{"test": true}' > appsettings.Test.json
git add appsettings.Test.json
git commit -m "test: config-only change"
git push
# Create PR → Should skip coverage
```

### Test-Only
```bash
# Modify only test files
git checkout -b test-test-only
echo "// new test" >> tests/ProbotSharp.Domain.Tests/SomeTests.cs
git add tests/
git commit -m "test: add new test case"
git push
# Create PR → Should run informational mode
```

### Merge Commit
```bash
# Create merge commit
git checkout -b test-merge
git merge origin/main
# Resolve any conflicts (minimal changes)
git push
# Create PR → Should run informational mode
```

---

## Future Improvements

Potential enhancements for edge case handling:

1. **Diff-coverage integration**: Only check coverage on changed lines
2. **Smart merge detection**: Distinguish merge conflicts from actual logic changes
3. **Configurable thresholds per layer**: Domain 90%, Application 80%, Infrastructure 70%
4. **Historical trending**: Track coverage trends over time
5. **Exemption labels**: Allow `skip-coverage` label for emergency fixes

---

## Related Documentation

- [Coverage Report Workflow](../.github/workflows/coverage-report.yml)
- [Best Practices](./BestPractices.md)
- [Architecture](./Architecture.md)

---

**Last Updated:** 2025-10-23
**Author:** Claude Code (Anthropic)
**Version:** 2.0 (Edge Case Handling)
