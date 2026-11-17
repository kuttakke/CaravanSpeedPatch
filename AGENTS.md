# AGENTS.md

This file provides guidance for AI coding assistants and contributors working in this repository.

## Project Overview

A RimWorld mod that makes caravan (world map travel) speed scale with the average movement speed of caravan members. When pawns have increased `MoveSpeed` (from bionics, traits, drugs, etc.), caravans travel faster on the world map. Also supports Vehicle Framework mod integration.

- **Author**: Kutake
- **Package ID**: `Kutake.CaravanSpeedPatch`
- **Supported versions**: RimWorld 1.5, 1.6, 1.7
- **Dependency**: `brrainz.harmony` (Harmony patching library)

## Build

Requires .NET 10 SDK (or .NET 8/9 SDK if only building 1.5/1.6). Dependencies are restored automatically via NuGet (`Krafs.Rimworld.Ref` and `Lib.Harmony`).

Using [just](https://github.com/casey/just):

```powershell
just build
```

Or directly via `dotnet`:

```powershell
dotnet build Source/CaravanSpeedPatch/CaravanSpeedPatch.sln -c Release
```

The compiled DLLs are copied automatically to:
- `Assemblies/` and `1.5/Assemblies/`, `1.6/Assemblies/` (from `CaravanSpeedPatch.csproj`, targeting `net472` + `Krafs.Rimworld.Ref` `1.6.*`)
- `1.7/Assemblies/` (from `CaravanSpeedPatch17.csproj`, targeting `net10.0` + `Krafs.Rimworld.Ref` `1.7.*-*`)

## Architecture

Two source files in `Source/CaravanSpeedPatch/CaravanSpeedPatch/` (shared by both `.csproj` files):

### `Mod.cs` — Settings & mod lifecycle
- `CaravanSpeedPatchMod` — the `Mod` subclass, entry point for RimWorld. Registers settings.
- `CaravanSpeedPatchSettings` — persisted via `Scribe_Values` (RimWorld's XML save system). All settings are `static` fields:
  - `enableCaravanSpeedPatch` (bool, default `true`) — master toggle
  - `enableAnimeSpeed` (bool, default `false`) — whether animal speed affects caravan speed
  - `onlyPatchRideableAnimals` (bool, default `true`) — only count rideable animals
  - `enableVehicleFrameworkModCaravanSpeed` (bool, default `false`) — Vehicle Framework integration toggle
  - `caravanSpeedMultiplier` (float, `0.1`–`5.0`, default `1.0`) — ticks-per-move scaling factor (lower = faster; UI displays `1 / caravanSpeedMultiplier` as percentage and `x` multiplier)
  - `minTicksPerMove` (int, `1`–`100`, default `5`) — minimum ticks per tile cap (UI displays `60000 / minTicksPerMove` tiles/day)
- Settings UI built with RimWorld's `Listing_Standard` API; translation keys come from `Languages/` XML files.

### `Patch.cs` — Harmony patching
- `CaravanSpeedPatch` — `[StaticConstructorOnStartup]` class that initializes Harmony, dynamically patches `Vehicles.VehicleCaravanTicksPerMoveUtility.GetTicksPerMove` if Vehicle Framework is active, and transpiles `WorldPathing.FindPath` (1.6/1.7) / `WorldPathFinder.FindPath` (1.5).
- `CaravanTicksPerMoveUtility_GetTicksPerMove_Patch` — Harmony `Postfix` on `CaravanTicksPerMoveUtility.GetTicksPerMove`:
  1. Early-return on `__result <= 0` (skips shuttles, which return `0` in 1.6+).
  2. For each pawn in the caravan, compute `speedFactor = baseMoveSpeed / currentMoveSpeed` (capped at `1.0`).
  3. Average factors separately for humanlikes and animals (animals only if `enableAnimeSpeed` is on).
  4. Multiply: `finalFactor = humanAvg * animalAvg * caravanSpeedMultiplier`, clamp ticks to `minTicksPerMove`.
  5. Append a detailed breakdown to `StringBuilder explanation`.
- `VehicleCaravanTicksPerMoveUtility_GetTicksPerMove_Patch` — equivalent logic for Vehicle Framework caravans, gated behind `enableVehicleFrameworkModCaravanSpeed`.
- `WorldPathing_FindPath_Patch` — Harmony `Transpiler` on `FindPath` that clamps `caravan.TicksPerMove` to at least `1000` inside A* pathfinding cost calculation only, preventing integer-truncation-to-zero, search-limit failures, and spiral walking at extreme caravan speeds (even `1` tick/tile = 60,000 tiles/day).

## Translations

`Languages/English/Keyed/Caravan_Increase.xml` and `Languages/ChineseSimplified (简体中文)/Keyed/Caravan_Increase.xml`. Translation keys use the `KTK_CaravanSpeedPatch_` prefix.

## Deploying

Copy the mod folder to RimWorld's `Mods/` directory. RimWorld loads `About/About.xml` for metadata and `<version>/Assemblies/CaravanSpeedPatch.dll` (falling back to root `Assemblies/`).
