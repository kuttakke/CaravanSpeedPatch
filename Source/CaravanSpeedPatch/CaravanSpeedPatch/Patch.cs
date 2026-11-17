using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace KTK_CaravanSpeedPatch
{
    [StaticConstructorOnStartup]
    public static class CaravanSpeedPatch
    {
        static CaravanSpeedPatch()
        {
            var harmony = new Harmony("rimworld.ktk_CaravanSpeedPatch");
            if (ModLister.HasActiveModWithName("Vehicle Framework"))
            {
                Type vehicleUtil = AccessTools.TypeByName("Vehicles.VehicleCaravanTicksPerMoveUtility");
                if (vehicleUtil != null)
                {
                    MethodInfo method = AccessTools.Method(vehicleUtil, "GetTicksPerMove", new[] { typeof(Caravan), typeof(StringBuilder) });
                    if (method != null)
                    {
                        harmony.Patch(original: method, postfix: new HarmonyMethod(typeof(VehicleCaravanTicksPerMoveUtility_GetTicksPerMove_Patch), nameof(VehicleCaravanTicksPerMoveUtility_GetTicksPerMove_Patch.Postfix)));
                        Log.Message("[Caravan Speed Patch] Vehicle mod detected, patched VehicleCaravanTicksPerMoveUtility.GetTicksPerMove");
                    }
                }
            }

            Type worldPathingType = AccessTools.TypeByName("RimWorld.Planet.WorldPathing")
                ?? AccessTools.TypeByName("RimWorld.Planet.WorldPathFinder");
            if (worldPathingType != null)
            {
                MethodInfo findPathMethod = AccessTools.Method(worldPathingType, "FindPath");
                if (findPathMethod != null)
                {
                    harmony.Patch(
                        original: findPathMethod,
                        transpiler: new HarmonyMethod(typeof(WorldPathing_FindPath_Patch), nameof(WorldPathing_FindPath_Patch.Transpiler))
                    );
                }
            }

            MethodInfo patherTickInterval = AccessTools.Method(typeof(Caravan_PathFollower), "PatherTickInterval", new[] { typeof(int) });
            if (patherTickInterval != null)
            {
                harmony.Patch(
                    original: patherTickInterval,
                    prefix: new HarmonyMethod(typeof(Caravan_PathFollower_PatherTick_Patch), nameof(Caravan_PathFollower_PatherTick_Patch.PrefixInterval))
                );
            }
            else
            {
                MethodInfo patherTick = AccessTools.Method(typeof(Caravan_PathFollower), "PatherTick", Type.EmptyTypes);
                if (patherTick != null)
                {
                    harmony.Patch(
                        original: patherTick,
                        prefix: new HarmonyMethod(typeof(Caravan_PathFollower_PatherTick_Patch), nameof(Caravan_PathFollower_PatherTick_Patch.PrefixLegacy))
                    );
                }
            }

            harmony.PatchAll();
        }
    }

    internal sealed class CaravanOverflowBox
    {
        public float Multiplier = 1f;
    }

    [HarmonyPatch(typeof(CaravanTicksPerMoveUtility), nameof(CaravanTicksPerMoveUtility.GetTicksPerMove), new[] { typeof(Caravan), typeof(StringBuilder) })]
    public static class CaravanTicksPerMoveUtility_GetTicksPerMove_Patch
    {
        private static readonly ConditionalWeakTable<Caravan, CaravanOverflowBox> OverflowTable = new ConditionalWeakTable<Caravan, CaravanOverflowBox>();

        public static void Postfix(Caravan caravan, StringBuilder explanation, ref int __result)
        {
            if (!CaravanSpeedPatchSettings.enableCaravanSpeedPatch)
                return;
            ApplyCaravanSpeedPatch(caravan, explanation, ref __result);
        }

        internal static float GetOverflowMultiplier(Caravan caravan)
        {
            if (caravan != null && OverflowTable.TryGetValue(caravan, out CaravanOverflowBox box))
            {
                return box.Multiplier;
            }
            return 1f;
        }

        private static void SetOverflowMultiplier(Caravan caravan, float multiplier)
        {
            if (caravan == null)
                return;
            OverflowTable.GetOrCreateValue(caravan).Multiplier = multiplier;
        }

        internal static void ApplyCaravanSpeedPatch(Caravan caravan, StringBuilder explanation, ref int __result)
        {
            if (caravan == null || caravan.PawnsListForReading == null)
                return;

            if (__result <= 0)
            {
                SetOverflowMultiplier(caravan, 1f);
                return;
            }

            List<float> humanlikeMoveSpeedFactorList = new List<float>();
            List<float> animalMoveSpeedFactorList = new List<float>();

            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                if (pawn.RaceProps.Humanlike)
                {
                    humanlikeMoveSpeedFactorList.Add(GetCaravanSpeedFactorByPawnMoveSpeed(pawn));
                    continue;
                }
                if (pawn.RaceProps.Animal)
                {
                    if (!CaravanSpeedPatchSettings.enableAnimeSpeed)
                        continue;
                    if (CaravanSpeedPatchSettings.onlyPatchRideableAnimals && !pawn.IsCaravanRideable())
                        continue;
                    animalMoveSpeedFactorList.Add(GetCaravanSpeedFactorByPawnMoveSpeed(pawn));
                    continue;
                }
                // TODO: Maybe add Mechanoid speed factor?
            }

            float humanlikeMoveSpeedFactor = humanlikeMoveSpeedFactorList.DefaultIfEmpty(1f).Average();
            float animalMoveSpeedFactor = animalMoveSpeedFactorList.DefaultIfEmpty(1f).Average();
            float speedFactor = humanlikeMoveSpeedFactor * animalMoveSpeedFactor;

            explanation?.AppendLine($"");
            explanation?.AppendLine();
            explanation?.AppendLine("KTK_CaravanSpeedPatch_ExplanationStart".Translate());
            if (speedFactor == 1f)
            {
                SetOverflowMultiplier(caravan, 1f);
                explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedNotChanged".Translate());
                return;
            }

            int originalResult = __result;
            float speedFactorWithMultiplier = speedFactor * CaravanSpeedPatchSettings.caravanSpeedMultiplier;
            float rawTicksFloat = originalResult * speedFactorWithMultiplier;
            int calculatedResult = Mathf.RoundToInt(rawTicksFloat);
            float overflowMultiplier = 1f;

            if (CaravanSpeedPatchSettings.allowUncappedSpeed)
            {
                __result = Mathf.Max(calculatedResult, 1);
                if (rawTicksFloat < 1f)
                {
                    float desiredOverflow = 1f / Mathf.Max(rawTicksFloat, 1e-6f);
                    overflowMultiplier = Mathf.Min(desiredOverflow, CaravanSpeedPatchSettings.maxTilesPerTick);
                    if (desiredOverflow > CaravanSpeedPatchSettings.maxTilesPerTick)
                    {
                        float uncappedTilesPerDay = 60000f * desiredOverflow;
                        float cappedTilesPerDay = 60000f * CaravanSpeedPatchSettings.maxTilesPerTick;
                        explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_MaxTilesClamped".Translate(uncappedTilesPerDay.ToString("F0"), cappedTilesPerDay.ToString("F0")));
                    }
                }
            }
            else
            {
                if (calculatedResult < CaravanSpeedPatchSettings.minTicksPerMove)
                {
                    float uncappedTilesPerDay = 60000f / Mathf.Max(rawTicksFloat, 1e-6f);
                    float cappedTilesPerDay = 60000f / CaravanSpeedPatchSettings.minTicksPerMove;
                    explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_MinTicksClamped".Translate(uncappedTilesPerDay.ToString("F0"), cappedTilesPerDay.ToString("F0")));
                }
                __result = Mathf.Max(calculatedResult, CaravanSpeedPatchSettings.minTicksPerMove);
            }

            SetOverflowMultiplier(caravan, overflowMultiplier);

            float increaseHumanlike = ((1f / humanlikeMoveSpeedFactor) - 1f) * 100f;
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_HumanlikeSpeedIncreased".Translate(increaseHumanlike.ToString("F1")));
            if (animalMoveSpeedFactor < 1f)
            {
                float increaseAnimal = ((1f / animalMoveSpeedFactor) - 1f) * 100f;
                explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_AnimalSpeedIncreased".Translate(increaseAnimal.ToString("F1")));
            }
            float settingSpeedMult = 1f / Mathf.Max(CaravanSpeedPatchSettings.caravanSpeedMultiplier, 0.001f);
            string settingPercent = (settingSpeedMult * 100f).ToString("F0") + "%";
            string settingTimes = settingSpeedMult.ToString("0.##") + "x";
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedMultiplierE".Translate(settingPercent, settingTimes, CaravanSpeedPatchSettings.caravanSpeedMultiplier.ToString("0.###")));

            float actualTilesPerDay = (60000f / __result) * overflowMultiplier;
            float actualIncrease = (((float)originalResult / __result) * overflowMultiplier - 1f) * 100f;
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedIncreased".Translate(actualIncrease.ToString("F1"), actualTilesPerDay.ToString("F0")));
        }

        private static float GetCaravanSpeedFactorByPawnMoveSpeed(Pawn pawn)
        {
            float baseSpeed = pawn.def.GetStatValueAbstract(StatDefOf.MoveSpeed);
            float nowSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
            if (nowSpeed <= baseSpeed)
            {
                return 1f;
            }
            return baseSpeed / nowSpeed;
        }
    }

    public static class VehicleCaravanTicksPerMoveUtility_GetTicksPerMove_Patch
    {
        public static void Postfix(Caravan caravan, StringBuilder explanation, ref int __result)
        {
            if (!CaravanSpeedPatchSettings.enableCaravanSpeedPatch || !CaravanSpeedPatchSettings.enableVehicleFrameworkModCaravanSpeed)
                return;
            CaravanTicksPerMoveUtility_GetTicksPerMove_Patch.ApplyCaravanSpeedPatch(caravan, explanation, ref __result);
        }
    }

    public static class Caravan_PathFollower_PatherTick_Patch
    {
        private static readonly AccessTools.FieldRef<Caravan_PathFollower, Caravan> CaravanRef =
            AccessTools.FieldRefAccess<Caravan_PathFollower, Caravan>("caravan");
        private static readonly Func<Caravan_PathFollower, float> CostToPayThisTickDelegate =
            AccessTools.MethodDelegate<Func<Caravan_PathFollower, float>>(AccessTools.Method(typeof(Caravan_PathFollower), "CostToPayThisTick"));
        private static readonly Action<Caravan_PathFollower> TryEnterNextPathTileDelegate =
            AccessTools.MethodDelegate<Action<Caravan_PathFollower>>(AccessTools.Method(typeof(Caravan_PathFollower), "TryEnterNextPathTile"));

        public static bool PrefixInterval(Caravan_PathFollower __instance, int delta)
        {
            return HandlePatherTick(__instance, delta);
        }

        public static bool PrefixLegacy(Caravan_PathFollower __instance)
        {
            return HandlePatherTick(__instance, 1);
        }

        private static bool HandlePatherTick(Caravan_PathFollower pather, int delta)
        {
            if (!CaravanSpeedPatchSettings.enableCaravanSpeedPatch || !CaravanSpeedPatchSettings.allowUncappedSpeed)
                return true;

            Caravan caravan = CaravanRef(pather);
            if (caravan == null || caravan.Shuttle != null)
                return true;

            // Refresh TicksPerMove so OverflowTable is up to date even if stats changed mid-travel
            int unusedTicks = caravan.TicksPerMove;
            float overflowMultiplier = CaravanTicksPerMoveUtility_GetTicksPerMove_Patch.GetOverflowMultiplier(caravan);
            if (overflowMultiplier <= 1f)
                return true;

            if (pather.Moving && pather.ArrivalAction != null && !pather.ArrivalAction.StillValid(caravan, pather.Destination))
            {
                string failMessage = pather.ArrivalAction.StillValid(caravan, pather.Destination).FailMessage;
                Messages.Message("MessageCaravanArrivalActionNoLongerValid".Translate(caravan.Name).CapitalizeFirst() + (failMessage != null ? " " + failMessage : ""), caravan, MessageTypeDefOf.NegativeEvent);
                pather.StopDead();
                return false;
            }

            if (caravan.CantMove || pather.Paused || !pather.Moving)
                return false;

            float budget = CostToPayThisTickDelegate(pather) * delta * overflowMultiplier;
            int maxSteps = Mathf.Max(CaravanSpeedPatchSettings.maxTilesPerTick, 1) * Mathf.Max(delta, 1);
            int steps = 0;

            while (budget > 0f && steps < maxSteps && pather.Moving && !caravan.CantMove && !pather.Paused && !caravan.Destroyed && caravan.Spawned)
            {
                if (pather.nextTileCostLeft > budget)
                {
                    pather.nextTileCostLeft -= budget;
                    break;
                }

                budget -= Mathf.Max(pather.nextTileCostLeft, 0f);
                pather.nextTileCostLeft = 0f;
                TryEnterNextPathTileDelegate(pather);
                steps++;
            }

            return false;
        }
    }

    public static class WorldPathing_FindPath_Patch
    {
        private static readonly MethodInfo TicksPerMoveGetter = AccessTools.PropertyGetter(typeof(Caravan), nameof(Caravan.TicksPerMove));
        private static readonly MethodInfo ClampMethod = AccessTools.Method(typeof(WorldPathing_FindPath_Patch), nameof(ClampPathfindingTicksPerMove));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(TicksPerMoveGetter))
                {
                    yield return new CodeInstruction(OpCodes.Call, ClampMethod);
                }
            }
        }

        // Keep A* edge costs large enough to prevent integer truncation to 0 and spiral walking at extreme speeds
        public static int ClampPathfindingTicksPerMove(int ticksPerMove)
        {
            return Mathf.Max(ticksPerMove, 1000);
        }
    }
}
