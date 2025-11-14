#!/usr/bin/env pwsh
# Script to create necessary symlinks for React Native dependencies in pnpm monorepo

param()

$ErrorActionPreference = 'Stop'

# Script directory
$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$TvDir       = Split-Path -Parent $ScriptDir
$FrontendDir = Split-Path -Parent (Split-Path -Parent $TvDir)

Write-Host 'Setting up symlinks for React Native dependencies...'

$reactNativeDir = Join-Path $TvDir 'node_modules/@react-native'
$codegenLink    = Join-Path $reactNativeDir 'codegen'
$codegenSource  = Join-Path $FrontendDir 'node_modules/@react-native/codegen'

# Create @react-native directory if it doesn't exist
if (-not (Test-Path $reactNativeDir)) {
    New-Item -ItemType Directory -Path $reactNativeDir -Force | Out-Null
}

# Remove old symlink / folder if exists
if (Test-Path $codegenLink) {
    Write-Host 'Removing old codegen symlink...'
    Remove-Item $codegenLink -Recurse -Force
}

# Check if source directory exists
if (-not (Test-Path $codegenSource -PathType Container)) {
    Write-Host "ERROR: codegen not found in $codegenSource"
    Write-Host "Please run 'pnpm install' first from frontend root"
    Exit 1
}

# Create symlink (relative target, same as in bash version)
Write-Host 'Creating symlink: node_modules/@react-native/codegen -> ../../../../node_modules/@react-native/codegen'
Set-Location $reactNativeDir

New-Item -ItemType SymbolicLink `
         -Path 'codegen' `
         -Target '..\..\..\..\node_modules\@react-native\codegen' `
         -Force | Out-Null

# Verify symlink
$verifyPath = Join-Path $codegenLink 'lib/cli/combine/combine-js-to-schema-cli.js'

if (Test-Path $verifyPath -PathType Leaf) {
    Write-Host 'Symlink created successfully.'
} else {
    Write-Host 'Symlink verification failed.'
    Exit 1
}

Write-Host 'Done.'
