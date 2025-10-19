# ProbotSharp Makefile
# Convenient targets for development, testing, and coverage analysis

.PHONY: help
help: ## Show this help message
	@echo "ProbotSharp Development Targets"
	@echo "==============================="
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'
	@echo ""
	@echo "Coverage Targets:"
	@echo "  make coverage       - Run full coverage analysis"
	@echo "  make coverage-layer - Run per-layer coverage analysis"
	@echo "  make coverage-watch - Watch mode for continuous coverage"
	@echo "  make coverage-gaps  - Analyze coverage gaps"
	@echo "  make coverage-compare - Compare coverage with main branch"
	@echo ""

# ==================== Build Targets ====================

.PHONY: restore
restore: ## Restore NuGet packages and tools
	dotnet restore ProbotSharp.sln
	dotnet tool restore

.PHONY: build
build: restore ## Build the solution in Release mode
	dotnet build ProbotSharp.sln --configuration Release --no-restore

.PHONY: clean
clean: ## Clean build artifacts
	dotnet clean ProbotSharp.sln
	rm -rf coverage-*
	rm -rf tests/**/TestResults
	rm -rf tests/**/bin
	rm -rf tests/**/obj
	rm -rf src/**/bin
	rm -rf src/**/obj

.PHONY: rebuild
rebuild: clean build ## Clean and rebuild the solution

# ==================== Test Targets ====================

.PHONY: test
test: ## Run all tests
	dotnet test ProbotSharp.sln --configuration Release --no-build

.PHONY: test-unit
test-unit: ## Run unit tests only
	dotnet test tests/ProbotSharp.Domain.Tests --configuration Release --no-build
	dotnet test tests/ProbotSharp.Application.Tests --configuration Release --no-build
	dotnet test tests/ProbotSharp.Shared.Tests --configuration Release --no-build

.PHONY: test-integration
test-integration: ## Run integration tests (requires Docker)
	dotnet test tests/ProbotSharp.IntegrationTests --configuration Release --no-build

.PHONY: test-watch
test-watch: ## Run tests in watch mode
	dotnet watch test --project tests/ProbotSharp.Domain.Tests

# ==================== Coverage Targets ====================

.PHONY: coverage
coverage: ## Run full coverage analysis
	@scripts/coverage/run-coverage-all.sh

.PHONY: coverage-layer
coverage-layer: ## Run per-layer coverage analysis
	@scripts/coverage/run-coverage-by-layer.sh

.PHONY: coverage-domain
coverage-domain: ## Run coverage for Domain layer only
	@echo "Building Domain tests in Debug mode for accurate coverage..."
	@dotnet build tests/ProbotSharp.Domain.Tests --configuration Debug
	@echo "Running Domain layer coverage..."
	@dotnet test tests/ProbotSharp.Domain.Tests \
		--configuration Debug \
		--no-build \
		--collect:"XPlat Code Coverage" \
		--settings coverlet.runsettings \
		--results-directory coverage-domain

.PHONY: coverage-application
coverage-application: ## Run coverage for Application layer only
	@echo "Building Application tests in Debug mode for accurate coverage..."
	@dotnet build tests/ProbotSharp.Application.Tests --configuration Debug
	@echo "Running Application layer coverage..."
	@dotnet test tests/ProbotSharp.Application.Tests \
		--configuration Debug \
		--no-build \
		--collect:"XPlat Code Coverage" \
		--settings coverlet.runsettings \
		--results-directory coverage-application

.PHONY: coverage-infrastructure
coverage-infrastructure: ## Run coverage for Infrastructure layer only
	@echo "Building Infrastructure tests in Debug mode for accurate coverage..."
	@dotnet build tests/ProbotSharp.Infrastructure.Tests --configuration Debug
	@echo "Running Infrastructure layer coverage..."
	@dotnet test tests/ProbotSharp.Infrastructure.Tests \
		--configuration Debug \
		--no-build \
		--collect:"XPlat Code Coverage" \
		--settings coverlet.runsettings \
		--results-directory coverage-infrastructure

.PHONY: coverage-watch
coverage-watch: ## Watch mode for continuous coverage
	@scripts/coverage/run-coverage-watch.sh

.PHONY: coverage-gaps
coverage-gaps: ## Analyze coverage gaps
	@scripts/coverage/analyze-coverage-gaps.sh

.PHONY: coverage-compare
coverage-compare: ## Compare coverage with main branch
	@scripts/coverage/compare-coverage.sh compare main

.PHONY: coverage-snapshot
coverage-snapshot: ## Save current coverage snapshot
	@scripts/coverage/compare-coverage.sh snapshot

.PHONY: coverage-trend
coverage-trend: ## Show coverage trend
	@scripts/coverage/compare-coverage.sh trend

.PHONY: coverage-report
coverage-report: ## Open HTML coverage report in browser
	@if [ -d "coverage-report" ]; then \
		xdg-open coverage-report/index.html 2>/dev/null || open coverage-report/index.html 2>/dev/null || echo "Report: file://$(PWD)/coverage-report/index.html"; \
	else \
		echo "No coverage report found. Run 'make coverage' first."; \
	fi

.PHONY: coverage-clean
coverage-clean: ## Clean all coverage artifacts
	rm -rf coverage-*
	rm -rf .coverage-history
	rm -f coverage-gaps.md
	rm -f coverage-summary.md

# ==================== CI/CD Targets ====================

.PHONY: ci
ci: restore build test ## Run CI pipeline locally

.PHONY: pre-commit
pre-commit: ## Run pre-commit checks
	dotnet husky run

.PHONY: validate-k8s
validate-k8s: ## Validate Kubernetes manifests
	./scripts/run-kubeconform.sh

.PHONY: verify-markdown
verify-markdown: ## Verify Markdown code blocks compile
	./scripts/verify-markdown-code.sh

.PHONY: verify-links
verify-links: ## Verify links in Markdown files
	python3 scripts/verify-local-links.py

# ==================== Docker Targets ====================

.PHONY: docker-build
docker-build: ## Build Docker images
	docker-compose build

.PHONY: docker-up
docker-up: ## Start Docker services
	docker-compose up -d

.PHONY: docker-down
docker-down: ## Stop Docker services
	docker-compose down

.PHONY: docker-logs
docker-logs: ## Show Docker logs
	docker-compose logs -f

# ==================== Database Targets ====================

.PHONY: db-migrate
db-migrate: ## Apply database migrations
	dotnet ef database update --project src/ProbotSharp.Infrastructure

.PHONY: db-rollback
db-rollback: ## Rollback last migration
	dotnet ef database update LastGoodMigration --project src/ProbotSharp.Infrastructure

.PHONY: migration-add
migration-add: ## Add new migration (usage: make migration-add NAME=MigrationName)
	@if [ -z "$(NAME)" ]; then \
		echo "Error: NAME is required. Usage: make migration-add NAME=MigrationName"; \
		exit 1; \
	fi
	dotnet ef migrations add $(NAME) --project src/ProbotSharp.Infrastructure

# ==================== Development Targets ====================

.PHONY: run
run: ## Run the API locally
	dotnet run --project src/ProbotSharp.Bootstrap.Api

.PHONY: run-cli
run-cli: ## Run the CLI
	dotnet run --project src/ProbotSharp.Bootstrap.Cli -- --help

.PHONY: run-example
run-example: ## Run an example bot (usage: make run-example BOT=HelloWorldBot)
	@if [ -z "$(BOT)" ]; then \
		echo "Error: BOT is required. Usage: make run-example BOT=HelloWorldBot"; \
		echo "Available bots:"; \
		ls -1 examples/ | grep -v "^README"; \
		exit 1; \
	fi
	dotnet run --project examples/$(BOT)

.PHONY: format
format: ## Format code with dotnet format
	dotnet format ProbotSharp.sln

.PHONY: outdated
outdated: ## Check for outdated NuGet packages
	dotnet list package --outdated

.PHONY: update-tools
update-tools: ## Update .NET tools
	dotnet tool update --all

# ==================== Documentation Targets ====================

.PHONY: docs-coverage
docs-coverage: ## Generate Coverage.md documentation
	@echo "Generating Coverage.md documentation..."
	@echo "Documentation will be created at docs/Coverage.md"
	@echo "Run 'make coverage' first to ensure accurate metrics"

.PHONY: docs-serve
docs-serve: ## Serve documentation locally (if using DocFx or similar)
	@echo "Documentation server not configured yet"

# ==================== Utility Targets ====================

.PHONY: stats
stats: ## Show code statistics
	@echo "=== Code Statistics ==="
	@echo ""
	@echo "Source Lines of Code:"
	@find src -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" -not -path "*/Migrations/*" | xargs wc -l | tail -1
	@echo ""
	@echo "Test Lines of Code:"
	@find tests -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | xargs wc -l | tail -1
	@echo ""
	@echo "Total Test Count:"
	@dotnet test ProbotSharp.sln --list-tests --no-build 2>/dev/null | grep -c "^  " || echo "Build required to count tests"

.PHONY: check-tools
check-tools: ## Check required tools are installed
	@echo "Checking required tools..."
	@command -v dotnet >/dev/null 2>&1 && echo "✓ dotnet" || echo "✗ dotnet (required)"
	@command -v git >/dev/null 2>&1 && echo "✓ git" || echo "✗ git (required)"
	@command -v docker >/dev/null 2>&1 && echo "✓ docker" || echo "✗ docker (optional)"
	@command -v python3 >/dev/null 2>&1 && echo "✓ python3" || echo "✗ python3 (optional)"
	@dotnet tool list | grep -q reportgenerator && echo "✓ reportgenerator" || echo "✗ reportgenerator (run: dotnet tool restore)"
	@dotnet tool list | grep -q husky && echo "✓ husky" || echo "✗ husky (run: dotnet tool restore)"

# Default target
.DEFAULT_GOAL := help