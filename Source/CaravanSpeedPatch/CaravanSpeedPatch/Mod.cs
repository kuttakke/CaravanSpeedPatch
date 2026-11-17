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

        // minimum ticks per move when uncapped mode is disabled
        public static int minTicksPerMove = 5;

        // allow breaking past 1 tick/tile (60,000 tiles/day) via multi-tile movement per tick
        public static bool allowUncappedSpeed = false;

        // maximum tiles a caravan can advance per tick in uncapped mode
        public static int maxTilesPerTick = 50;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableCaravanSpeedPatch, "enableCaravanSpeedPatch", true);
            Scribe_Values.Look(ref enableAnimeSpeed, "enableAnimeSpeed", false);
            Scribe_Values.Look(ref onlyPatchRideableAnimals, "onlyPatchRideableAnimals", true);
            Scribe_Values.Look(ref enableVehicleFrameworkModCaravanSpeed, "enableVehicleFrameworkModCaravanSpeed", false);
            Scribe_Values.Look(ref caravanSpeedMultiplier, "caravanSpeedMultiplier", 1.0f);
            Scribe_Values.Look(ref minTicksPerMove, "minTicksPerMove", 5);
            Scribe_Values.Look(ref allowUncappedSpeed, "allowUncappedSpeed", false);
            Scribe_Values.Look(ref maxTilesPerTick, "maxTilesPerTick", 50);

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
                float clampedFactor = Mathf.Clamp(caravanSpeedMultiplier, 0.001f, 5.0f);
                float speedMult = 1f / clampedFactor;
                string speedPercent = (speedMult * 100f).ToString("F0") + "%";
                string speedTimes = speedMult.ToString("0.##") + "x";
                float logMin = Mathf.Log10(0.2f);
                float logMax = Mathf.Log10(1000f);
                float currentLog = Mathf.Clamp(Mathf.Log10(speedMult), logMin, logMax);
                float newLog = currentLog;
                DoSliderLabeled(l, "KTK_CaravanSpeedPatch_CaravanSpeedMultiplier".Translate(speedPercent, speedTimes, caravanSpeedMultiplier.ToString("0.###")), ref newLog, logMin, logMax, "KTK_CaravanSpeedPatch_CaravanSpeedMultiplier_Tooltip".Translate());
                if (Mathf.Abs(newLog - currentLog) > 1e-4f)
                {
                    float rawMult = Mathf.Pow(10f, newLog);
                    float roundedMult = RoundSpeedMultiplier(rawMult);
                    caravanSpeedMultiplier = 1f / roundedMult;
                }
                l.CheckboxLabeled("KTK_CaravanSpeedPatch_AllowUncappedSpeed".Translate(), ref allowUncappedSpeed, "KTK_CaravanSpeedPatch_AllowUncappedSpeed_Tooltip".Translate());
                if (allowUncappedSpeed)
                {
                    float maxUncappedTilesPerDay = 60000f * Mathf.Max(maxTilesPerTick, 1);
                    DoIntSliderLabeled(l, "KTK_CaravanSpeedPatch_MaxTilesPerTick".Translate(maxTilesPerTick.ToString(), maxUncappedTilesPerDay.ToString("F0")), ref maxTilesPerTick, 1, 500, "KTK_CaravanSpeedPatch_MaxTilesPerTick_Tooltip".Translate());
                }
                else
                {
                    float maxTilesPerDay = 60000f / Mathf.Max(minTicksPerMove, 1);
                    DoIntSliderLabeled(l, "KTK_CaravanSpeedPatch_MinTicksPerMove".Translate(maxTilesPerDay.ToString("F0"), minTicksPerMove.ToString()), ref minTicksPerMove, 1, 100, "KTK_CaravanSpeedPatch_MinTicksPerMove_Tooltip".Translate());
                }
                l.GapLine();

                if (l.ButtonText("KTK_CaravanSpeedPatch_ResetToDefault".Translate()))
                {
                    ResetToDefault();
                }
            }
            l.End();
        }

        private static float RoundSpeedMultiplier(float raw)
        {
            if (raw < 1f) return Mathf.Max(0.2f, Mathf.Round(raw * 10f) / 10f);
            if (raw < 10f) return Mathf.Round(raw * 2f) / 2f;
            if (raw < 100f) return Mathf.Round(raw / 5f) * 5f;
            return Mathf.Min(1000f, Mathf.Round(raw / 50f) * 50f);
        }

        private static void ResetToDefault()
        {
            enableCaravanSpeedPatch = true;
            enableAnimeSpeed = false;
            onlyPatchRideableAnimals = true;
            enableVehicleFrameworkModCaravanSpeed = false;
            caravanSpeedMultiplier = 1.0f;
            minTicksPerMove = 5;
            allowUncappedSpeed = false;
            maxTilesPerTick = 50;
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
