#!/bin/bash
# Script to create necessary symlinks for React Native dependencies in pnpm monorepo

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TV_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
FRONTEND_DIR="$(cd "$TV_DIR/../.." && pwd)"

echo "Setting up symlinks for React Native dependencies..."

# Create @react-native directory if it doesn't exist
mkdir -p "$TV_DIR/node_modules/@react-native"

# Remove old symlink if exists
if [ -L "$TV_DIR/node_modules/@react-native/codegen" ] || [ -d "$TV_DIR/node_modules/@react-native/codegen" ]; then
    echo "Removing old codegen symlink..."
    rm -rf "$TV_DIR/node_modules/@react-native/codegen"
fi

# Check if source directory exists
if [ ! -d "$FRONTEND_DIR/node_modules/@react-native/codegen" ]; then
    echo "ERROR: codegen not found in $FRONTEND_DIR/node_modules/@react-native/codegen"
    echo "Please run 'pnpm install' first from frontend root"
    exit 1
fi

# Create symlink
echo "Creating symlink: node_modules/@react-native/codegen -> ../../../../node_modules/@react-native/codegen"
cd "$TV_DIR/node_modules/@react-native"
ln -sf "../../../../node_modules/@react-native/codegen" codegen

# Verify symlink
if [ -f "$TV_DIR/node_modules/@react-native/codegen/lib/cli/combine/combine-js-to-schema-cli.js" ]; then
    echo "✓ Symlink created successfully!"
else
    echo "✗ Symlink verification failed"
    exit 1
fi

echo "Done!"
