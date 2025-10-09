# Makefile for ProbotSharp Examples
# Provides convenient commands for building, running, and testing examples

EXAMPLES := MinimalBot HelloWorldBot AttachmentsBot SlashCommandsBot \
            DryRunBot PaginationBot MetadataBot ExtensionsBot \
            GraphQLBot HttpExtensibilityBot

DOCKERFILE := Dockerfile
REPO_ROOT := ..

# Convert PascalCase to lowercase for image names
lowercase = $(shell echo $(1) | tr '[:upper:]' '[:lower:]')

# Build a specific example
# Usage: make build-MinimalBot
build-%:
	@echo "Building $*..."
	@docker build -f $(DOCKERFILE) \
		--build-arg EXAMPLE_NAME=$* \
		-t $(call lowercase,$*):latest \
		$(REPO_ROOT)
	@echo "✅ Built $(call lowercase,$*):latest"

# Build all examples
# Usage: make build-all
build-all: $(addprefix build-,$(EXAMPLES))
	@echo ""
	@echo "🎉 All examples built successfully!"

# Run a specific example
# Usage: make run-MinimalBot
run-%:
	@echo "Running $*..."
	@docker run -it --rm -p 8080:5000 \
		-e ProbotSharp__WebhookSecret=development \
		--name $*-run \
		$(call lowercase,$*):latest

# Run a specific example in detached mode
# Usage: make run-detached-MinimalBot
run-detached-%:
	@echo "Running $* in detached mode..."
	@docker run -d -p 8080:5000 \
		-e ProbotSharp__WebhookSecret=development \
		--name $*-run \
		$(call lowercase,$*):latest
	@echo "✅ $* running at http://localhost:8080"
	@echo "   Health: http://localhost:8080/health"
	@echo "   Webhooks: http://localhost:8080/webhooks"
	@echo ""
	@echo "Stop with: docker stop $*-run"

# Test a specific example (build + health check + webhook test)
# Usage: make test-MinimalBot
test-%: build-%
	@echo "Testing $*..."
	@docker run -d -p 8080:5000 --name $*-test $(call lowercase,$*):latest > /dev/null
	@sleep 3
	@HEALTH=$$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/health 2>/dev/null); \
	WEBHOOK=$$(curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:8080/webhooks \
		-H "Content-Type: application/json" \
		-H "X-GitHub-Event: issues" \
		-H "X-GitHub-Delivery: test-123" \
		-H "X-Hub-Signature-256: sha256=$$(echo -n '{"test":true}' | openssl dgst -sha256 -hmac 'development' | awk '{print $$2}')" \
		-d '{"test":true}' 2>/dev/null); \
	docker stop $*-test > /dev/null 2>&1; \
	docker rm $*-test > /dev/null 2>&1; \
	if [ "$$HEALTH" = "200" ] && [ "$$WEBHOOK" = "202" ]; then \
		echo "✅ $* passed (Health: $$HEALTH, Webhook: $$WEBHOOK)"; \
	else \
		echo "❌ $* failed (Health: $$HEALTH, Webhook: $$WEBHOOK)"; \
		exit 1; \
	fi

# Test all examples
# Usage: make test-all
test-all:
	@echo "========================================="
	@echo "Testing All Examples"
	@echo "========================================="
	@echo ""
	@$(MAKE) -s $(addprefix test-,$(EXAMPLES))
	@echo ""
	@echo "🎉 All examples passed!"

# Clean up containers and images for a specific example
# Usage: make clean-MinimalBot
clean-%:
	@echo "Cleaning $*..."
	@docker ps -a | grep -i "$*" | awk '{print $$1}' | xargs -r docker rm -f 2>/dev/null || true
	@docker images | grep "$(call lowercase,$*)" | awk '{print $$3}' | xargs -r docker rmi -f 2>/dev/null || true
	@echo "✅ Cleaned $*"

# Clean all example containers and images
# Usage: make clean
clean:
	@echo "Cleaning all examples..."
	@for example in $(EXAMPLES); do \
		$(MAKE) -s clean-$$example; \
	done
	@echo "✅ All examples cleaned"

# Show help
# Usage: make help
help:
	@echo "ProbotSharp Examples - Docker Build System"
	@echo ""
	@echo "Available Commands:"
	@echo "  make build-<Example>          Build a specific example"
	@echo "  make build-all                Build all examples"
	@echo "  make run-<Example>            Run a specific example (interactive)"
	@echo "  make run-detached-<Example>   Run a specific example (background)"
	@echo "  make test-<Example>           Test a specific example"
	@echo "  make test-all                 Test all examples"
	@echo "  make clean-<Example>          Clean a specific example"
	@echo "  make clean                    Clean all examples"
	@echo "  make help                     Show this help message"
	@echo ""
	@echo "Available Examples:"
	@for example in $(EXAMPLES); do \
		echo "  - $$example"; \
	done
	@echo ""
	@echo "Examples:"
	@echo "  make build-MinimalBot"
	@echo "  make test-HelloWorldBot"
	@echo "  make run-detached-MinimalBot"
	@echo "  make test-all"

.PHONY: build-% run-% run-detached-% test-% clean-% build-all test-all clean help
.DEFAULT_GOAL := help
