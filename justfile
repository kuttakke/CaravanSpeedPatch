set shell := ["powershell.exe", "-NoLogo", "-Command"]

sln := "Source/CaravanSpeedPatch/CaravanSpeedPatch.sln"

# Default: build Release for all RimWorld versions (1.5, 1.6, 1.7)
default: build

# Build Release (outputs to Assemblies/, 1.5/, 1.6/, 1.7/)
build:
    dotnet build {{sln}} -c Release

# Build Debug
build-debug:
    dotnet build {{sln}} -c Debug

# Restore NuGet packages
restore:
    dotnet restore {{sln}}

# Clean build artifacts
clean:
    dotnet clean {{sln}} -c Release
    dotnet clean {{sln}} -c Debug
