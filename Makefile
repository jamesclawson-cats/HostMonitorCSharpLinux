# Makefile for HostMonitor

PROJECT_NAME=HostMonitor
DOTNET=dotnet

# Default target
all: build

# Build the project
build:
	$(DOTNET) build

# Run the project
run:
	$(DOTNET) run -- --hosts=google.com,github.com --port=5000

# Run tests (if you have a test project)
test:
	$(DOTNET) test

# Clean build output
clean:
	$(DOTNET) clean

# Publish for deployment (Linux x64)
publish:
	$(DOTNET) publish -c Release -r linux-x64 --self-contained false -o publish

# Install required tools (optional)
restore:
	$(DOTNET) restore

.PHONY: all build run test clean publish restore