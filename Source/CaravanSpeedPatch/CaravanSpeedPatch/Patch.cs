using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                if (vehicleUtil!= null)
                {
                    MethodInfo method = AccessTools.Method(vehicleUtil, "GetTicksPerMove", new[] { typeof(Caravan), typeof(StringBuilder) });
                    if (method!= null)
                    {
                        harmony.Patch(original: method, postfix: new HarmonyMethod(typeof(VehicleCaravanTicksPerMoveUtility_GetTicksPerMove_Patch), nameof(VehicleCaravanTicksPerMoveUtility_GetTicksPerMove_Patch.Postfix)));
                        Log.Message("[Caravan Speed Patch] Vehicle mod detected, patched VehicleCaravanTicksPerMoveUtility.GetTicksPerMove");
                    }
                }

            }
               harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(CaravanTicksPerMoveUtility), nameof(CaravanTicksPerMoveUtility.GetTicksPerMove), new[] { typeof(Caravan), typeof(StringBuilder) })]
    public static class CaravanTicksPerMoveUtility_GetTicksPerMove_Patch
    {
        public static void Postfix(Caravan caravan, StringBuilder explanation, ref int __result)
        {
            if (!CaravanSpeedPatchSettings.enableCaravanSpeedPatch)
                return;
            if (caravan == null || caravan.PawnsListForReading == null)
                return;

            if (__result == 1)
            {
                return;
            }

            List<float> humanlikeMoveSpeedFactorList = new List<float>();
            List<float> animalMoveSpeedFactorList = new List<float>();

            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                float pawnMoveSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
                if (pawn.RaceProps.Humanlike)
                {
                    humanlikeMoveSpeedFactorList.Add(GetCaravanSpeedFactorByPawnMoveSpeed(pawn));
                    continue;
                }
                if (pawn.RaceProps.Animal)
                {
                    if (!CaravanSpeedPatchSettings.enableAnimeSpeed)
                        return;
                    if (CaravanSpeedPatchSettings.onlyPatchRideableAnimals && !pawn.IsCaravanRideable())
                        return;
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
            if (speedFactor == 1)
            {
                explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedNotChanged".Translate());
                return;
            }
            float speedFactorWithMultiplier = speedFactor * CaravanSpeedPatchSettings.caravanSpeedMultiplier;
            int calculatedResult = Mathf.RoundToInt(__result * speedFactorWithMultiplier);
            if (calculatedResult < CaravanSpeedPatchSettings.minTicksPerMove)
            {
                explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_MinTicksClamped".Translate(calculatedResult.ToString(), CaravanSpeedPatchSettings.minTicksPerMove.ToString()));
            }
            __result = Mathf.Max(calculatedResult, CaravanSpeedPatchSettings.minTicksPerMove);

            float increaseHumanlike = ((1 / humanlikeMoveSpeedFactor) - 1) * 100;
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_HumanlikeSpeedIncreased".Translate((increaseHumanlike).ToString("F1")));
            if (animalMoveSpeedFactor < 1)
            {
                float increaseAnimal = ((1 / animalMoveSpeedFactor) - 1) * 100;
                explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_AnimalSpeedIncreased".Translate((increaseAnimal).ToString("F1")));
            }
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedMultiplierE".Translate((CaravanSpeedPatchSettings.caravanSpeedMultiplier).ToString("F1")));
            float increase = ((1 / speedFactorWithMultiplier) - 1) * 100;
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedIncreased".Translate((increase).ToString("F1")));
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
            if (caravan == null || caravan.PawnsListForReading == null)
                return;

            if (__result == 1)
            {
                return;
            }

            List<float> humanlikeMoveSpeedFactorList = new List<float>();
            List<float> animalMoveSpeedFactorList = new List<float>();

            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                float pawnMoveSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
                if (pawn.RaceProps.Humanlike)
                {
                    humanlikeMoveSpeedFactorList.Add(GetCaravanSpeedFactorByPawnMoveSpeed(pawn));
                    continue;
                }
                if (pawn.RaceProps.Animal)
                {
                    if (!CaravanSpeedPatchSettings.enableAnimeSpeed)
                        return;
                    if (CaravanSpeedPatchSettings.onlyPatchRideableAnimals && !pawn.IsCaravanRideable())
                        return;
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
            if (speedFactor == 1)
            {
                explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedNotChanged".Translate());
                return;
            }
            float speedFactorWithMultiplier = speedFactor * CaravanSpeedPatchSettings.caravanSpeedMultiplier;
            int calculatedResult = Mathf.RoundToInt(__result * speedFactorWithMultiplier);
            if (calculatedResult < CaravanSpeedPatchSettings.minTicksPerMove)
            {
                explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_MinTicksClamped".Translate(calculatedResult.ToString(), CaravanSpeedPatchSettings.minTicksPerMove.ToString()));
            }
            __result = Mathf.Max(calculatedResult, CaravanSpeedPatchSettings.minTicksPerMove);

            float increaseHumanlike = ((1 / humanlikeMoveSpeedFactor) - 1) * 100;
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_HumanlikeSpeedIncreased".Translate((increaseHumanlike).ToString("F1")));
            if (animalMoveSpeedFactor < 1)
            {
                float increaseAnimal = ((1 / animalMoveSpeedFactor) - 1) * 100;
                explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_AnimalSpeedIncreased".Translate((increaseAnimal).ToString("F1")));
            }
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedMultiplierE".Translate((CaravanSpeedPatchSettings.caravanSpeedMultiplier).ToString("F1")));
            float increase = ((1 / speedFactorWithMultiplier) - 1) * 100;
            explanation?.AppendLine("  " + "KTK_CaravanSpeedPatch_CaravanSpeedIncreased".Translate((increase).ToString("F1")));
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

}
