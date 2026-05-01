#!/bin/bash
set -e

echo "Setting up git hooks..."

# Configure git to use .githooks directory for hooks
git config core.hooksPath .githooks

echo ""
echo "✅ Git hooks configured successfully!"
echo ""
echo "The pre-commit hook will now verify code formatting before each commit."
echo "To bypass the hook for a specific commit, use: git commit --no-verify"
echo ""
