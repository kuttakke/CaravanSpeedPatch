# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A RimWorld mod that makes caravan (world map travel) speed scale with the average movement speed of caravan members. When pawns have increased MoveSpeed (from bionics, traits, drugs, etc.), caravans travel faster on the world map. Also supports Vehicle Framework mod integration.

- **Author**: Kutake
- **Package ID**: `Kutake.CaravanSpeedPatch`
- **Supported versions**: RimWorld 1.5, 1.6
- **Dependency**: `brrainz.harmony` (Harmony patching library)

## Build

Open `Source/CaravanSpeedPatch/CaravanSpeedPatch.sln` in Visual Studio 2022 or build via MSBuild:

```powershell
msbuild Source/CaravanSpeedPatch/CaravanSpeedPatch.sln /p:Configuration=Release
```

The compiled DLL is copied to `Assemblies/` at the repo root (and version-specific `1.5/Assemblies/`, `1.6/Assemblies/`), which is where RimWorld loads mod assemblies.

**Tech stack**: C#, .NET Framework 4.7.2, Harmony 2.3.6 (NuGet package in `Source/CaravanSpeedPatch/packages/`).

## Architecture

Two source files in `Source/CaravanSpeedPatch/CaravanSpeedPatch/`:

### `Mod.cs` — Settings & mod lifecycle
- `CaravanSpeedPatchMod` — the `Mod` subclass, entry point for RimWorld. Registers settings.
- `CaravanSpeedPatchSettings` — persisted via `Scribe_Values` (RimWorld's XML save system). All settings are `static` fields:
  - `enableCaravanSpeedPatch` (bool, default true) — master toggle
  - `enableAnimeSpeed` (bool, default false) — whether animal speed affects caravan speed
  - `onlyPatchRideableAnimals` (bool, default true) — only count rideable animals
  - `enableVehicleFrameworkModCaravanSpeed` (bool, default false) — Vehicle Framework integration toggle
  - `caravanSpeedMultiplier` (float, 0.1–5.0, default 1.0) — final multiplier (lower = faster caravans)
- Settings UI built with RimWorld's `Listing_Standard` API; translation keys come from `Languages/` XML files.

### `Patch.cs` — Harmony patching
- `CaravanSpeedPatch` — `[StaticConstructorOnStartup]` class that initializes Harmony. If Vehicle Framework mod is active, dynamically patches `Vehicles.VehicleCaravanTicksPerMoveUtility.GetTicksPerMove` via reflection.
- `CaravanTicksPerMoveUtility_GetTicksPerMove_Patch` — Harmony `Postfix` on the vanilla `CaravanTicksPerMoveUtility.GetTicksPerMove`. This is the core logic:
  1. For each pawn in the caravan, compute `speedFactor = baseMoveSpeed / currentMoveSpeed` (only when pawn is faster than base).
  2. Average factors separately for humanlikes and animals (animals only if `enableAnimeSpeed` is on).
  3. Multiply: `finalFactor = humanAvg × animalAvg × caravanSpeedMultiplier`.
  4. Multiply the original `__result` (ticks per move) by `finalFactor` and round to int.
  5. Append a detailed breakdown to the caravan info card via `StringBuilder explanation`.
- `VehicleCaravanTicksPerMoveUtility_GetTicksPerMove_Patch` — identical logic for Vehicle Framework caravans, gated behind `enableVehicleFrameworkModCaravanSpeed`.

### Key design notes
- Speed factor is capped at 1.0 (never slows caravans below vanilla speed). A pawn only contributes if `currentSpeed > baseSpeed`.
- The `__result == 1` early-return skips stationary caravans.
- The two patch classes (vanilla and Vehicle Framework) share duplicate logic — they are intentionally kept separate to allow independent evolution.
- Mechanoid speed is not yet handled (TODO comment in Patch.cs).

## Translations

`Languages/English/Keyed/Caravan_Increase.xml` and `Languages/ChineseSimplified (简体中文)/Keyed/Caravan_Increase.xml`. Translation keys use the `KTK_CaravanSpeedPatch_` prefix. The Chinese file has older commented-out entries preserved from a previous version — the active ones match the current English text.

## Deploying

Copy the mod folder to RimWorld's `Mods/` directory. RimWorld loads `About/About.xml` for metadata and `Assemblies/CaravanSpeedPatch.dll` for code. The `1.5/` and `1.6/` subdirectories provide version-specific DLLs (RimWorld checks `<version>/Assemblies/` first, then falls back to root `Assemblies/`).
