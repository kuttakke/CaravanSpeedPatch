using RimWorld;
using UnityEngine;
using Verse;

namespace KTK_CaravanSpeedPatch
{
    public class CaravanSpeedPatchSettings : ModSettings
    {
        // is the mod enabled?
        public static bool enableCaravanSpeedPatch = true;

        // enable patch anime speed?
        public static bool enableAnimeSpeed = false;

        // only patch Rideable animals?
        public static bool onlyPatchRideableAnimals = true;

        // enable patch vehicle framework mod caravan speed?
        public static bool enableVehicleFrameworkModCaravanSpeed = false;

        // speed multiplier for caravan speed
        public static float caravanSpeedMultiplier = 1.0f;

        // minimum ticks per move to prevent pathfinding breakdown at very high speeds
        public static int minTicksPerMove = 5;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableCaravanSpeedPatch, "enableCaravanSpeedPatch", true);
            Scribe_Values.Look(ref enableAnimeSpeed, "enableAnimeSpeed", false);
            Scribe_Values.Look(ref onlyPatchRideableAnimals, "onlyPatchRideableAnimals", true);
            Scribe_Values.Look(ref enableVehicleFrameworkModCaravanSpeed, "enableVehicleFrameworkModCaravanSpeed", false);
            Scribe_Values.Look(ref caravanSpeedMultiplier, "caravanSpeedMultiplier", 1.0f);
            Scribe_Values.Look(ref minTicksPerMove, "minTicksPerMove", 5);

            base.ExposeData();
        }

        private static void DoSliderLabeled(Listing_Standard l, string label, ref float val, float min, float max, string tooltip = null, float roundTo = -1f)
        {
            Rect rect = l.GetRect(30f);
            Text.Anchor = (TextAnchor)3;
            Widgets.Label(rect.LeftPart(0.5f), label);
            if (tooltip != null)
            {
                TooltipHandler.TipRegion(rect.LeftPart(0.5f), tooltip);
            }
            Text.Anchor = (TextAnchor)0;
            float result = Widgets.HorizontalSlider(rect.RightPart(1f - 0.5f), val, min, max, roundTo: roundTo);
            l.Gap(l.verticalSpacing);
            val = result;
        }

        private static void DoIntSliderLabeled(Listing_Standard l, string label, ref int val, int min, int max, string tooltip = null)
        {
            Rect rect = l.GetRect(30f);
            Text.Anchor = (TextAnchor)3;
            Widgets.Label(rect.LeftPart(0.5f), label);
            if (tooltip != null)
            {
                TooltipHandler.TipRegion(rect.LeftPart(0.5f), tooltip);
            }
            Text.Anchor = (TextAnchor)0;
            float floatVal = val;
            float result = Widgets.HorizontalSlider(rect.RightPart(1f - 0.5f), floatVal, min, max, roundTo: 1f);
            val = Mathf.RoundToInt(result);
            l.Gap(l.verticalSpacing);
        }

        public static bool IsVehicleFrameworkFinded()
        {
            return ModLister.HasActiveModWithName("Vehicle Framework");
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard l = new Listing_Standard();
            l.Begin(inRect);

            l.CheckboxLabeled("KTK_CaravanSpeedPatch_EnableCaravanSpeedPatch".Translate(), ref enableCaravanSpeedPatch);

            if (enableCaravanSpeedPatch)
            {
                l.GapLine();
                l.CheckboxLabeled("KTK_CaravanSpeedPatch_EnableAnimeSpeed".Translate(), ref enableAnimeSpeed);
                if (enableAnimeSpeed)
                {
                    l.CheckboxLabeled("KTK_CaravanSpeedPatch_OnlyPatchRideableAnimals".Translate(), ref onlyPatchRideableAnimals);
                }
                if (IsVehicleFrameworkFinded())
                {
                    l.GapLine();
                    l.CheckboxLabeled("KTK_CaravanSpeedPatch_EnableVehicleFrameworkModCaravanSpeed".Translate(), ref enableVehicleFrameworkModCaravanSpeed);
                }
                l.GapLine();
                DoSliderLabeled(l, "KTK_CaravanSpeedPatch_CaravanSpeedMultiplier".Translate() + "  " + caravanSpeedMultiplier.ToString("F1"), ref caravanSpeedMultiplier, 0.1f, 5.0f, "KTK_CaravanSpeedPatch_CaravanSpeedMultiplier_Tooltip".Translate(), 0.1f);
                DoIntSliderLabeled(l, "KTK_CaravanSpeedPatch_MinTicksPerMove".Translate() + "  " + minTicksPerMove, ref minTicksPerMove, 1, 100, "KTK_CaravanSpeedPatch_MinTicksPerMove_Tooltip".Translate());
                l.GapLine();

                if (l.ButtonText("KTK_CaravanSpeedPatch_ResetToDefault".Translate()))
                {
                    ResetToDefault();
                }
                l.End();
            }
        }

        private static void ResetToDefault()
        {
            enableCaravanSpeedPatch = true;
            enableAnimeSpeed = false;
            onlyPatchRideableAnimals = true;
            enableVehicleFrameworkModCaravanSpeed = false;
            caravanSpeedMultiplier = 1.0f;
            minTicksPerMove = 5;
        }
    }

    public class CaravanSpeedPatchMod : Mod
    {
        public CaravanSpeedPatchSettings settings;

        public CaravanSpeedPatchMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<CaravanSpeedPatchSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            CaravanSpeedPatchSettings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "KTK_CaravanSpeedPatch_SettingsCategory".Translate();
    }
}
