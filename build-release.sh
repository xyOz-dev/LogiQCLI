#!/bin/bash

# LogiQCLI Release Build Script
# This script builds release versions for multiple platforms
#
# Usage: ./build-release.sh
# Make executable: chmod +x build-release.sh

echo "🚀 Building LogiQCLI Release..."

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf ./release
dotnet clean LogiQCLI.sln --configuration Release

# Build Release configuration
echo "🔨 Building Release configuration..."
dotnet build LogiQCLI.sln --configuration Release

# Create self-contained executables for different platforms
echo "📦 Creating self-contained executables..."

echo "  → Windows x64..."
dotnet publish LogiQCLI/LogiQCLI.csproj \
    --configuration Release \
    --self-contained true \
    --runtime win-x64 \
    --output ./release/win-x64

echo "  → macOS x64..."
dotnet publish LogiQCLI/LogiQCLI.csproj \
    --configuration Release \
    --self-contained true \
    --runtime osx-x64 \
    --output ./release/osx-x64

echo "  → macOS ARM64..."
dotnet publish LogiQCLI/LogiQCLI.csproj \
    --configuration Release \
    --self-contained true \
    --runtime osx-arm64 \
    --output ./release/osx-arm64

echo "  → Linux x64..."
dotnet publish LogiQCLI/LogiQCLI.csproj \
    --configuration Release \
    --self-contained true \
    --runtime linux-x64 \
    --output ./release/linux-x64

echo "  → Linux ARM64..."
dotnet publish LogiQCLI/LogiQCLI.csproj \
    --configuration Release \
    --self-contained true \
    --runtime linux-arm64 \
    --output ./release/linux-arm64

# Create NuGet package
echo "📦 Creating NuGet package..."
dotnet pack LogiQCLI/LogiQCLI.csproj \
    --configuration Release \
    --output ./release/nuget

# Create compressed archives
echo "🗜️  Creating compressed archives..."
cd release

# Windows
if [ -d "win-x64" ]; then
    zip -r "LogiQCLI-win-x64.zip" win-x64/
    echo "  ✅ Created LogiQCLI-win-x64.zip"
fi

# macOS x64
if [ -d "osx-x64" ]; then
    tar -czf "LogiQCLI-osx-x64.tar.gz" osx-x64/
    echo "  ✅ Created LogiQCLI-osx-x64.tar.gz"
fi

# macOS ARM64
if [ -d "osx-arm64" ]; then
    tar -czf "LogiQCLI-osx-arm64.tar.gz" osx-arm64/
    echo "  ✅ Created LogiQCLI-osx-arm64.tar.gz"
fi

# Linux x64
if [ -d "linux-x64" ]; then
    tar -czf "LogiQCLI-linux-x64.tar.gz" linux-x64/
    echo "  ✅ Created LogiQCLI-linux-x64.tar.gz"
fi

# Linux ARM64
if [ -d "linux-arm64" ]; then
    tar -czf "LogiQCLI-linux-arm64.tar.gz" linux-arm64/
    echo "  ✅ Created LogiQCLI-linux-arm64.tar.gz"
fi

cd ..

echo ""
echo "✅ Release build completed!"
echo ""
echo "📁 Release artifacts created in ./release/:"
echo "   • Self-contained executables for Windows, macOS, and Linux"
echo "   • Compressed archives for easy distribution"
echo "   • NuGet package for CLI tool installation"
echo ""
echo "🎯 To install as a global tool:"
echo "   dotnet tool install --global --add-source ./release/nuget LogiQCLI"
echo ""
echo "🎯 To run directly:"
echo "   ./release/osx-x64/LogiQCLI      (macOS x64)"
echo "   ./release/osx-arm64/LogiQCLI    (macOS ARM64)"
echo "   ./release/linux-x64/LogiQCLI    (Linux x64)"
echo "   ./release/win-x64/LogiQCLI.exe  (Windows x64)"
