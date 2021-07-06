using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace YouDoYou
{
    public class YouDoYou_Settings : ModSettings
    {
        const float ConsiderMovementSpeedDefault = 1.0f;
        public static float ConsiderMovementSpeed = ConsiderMovementSpeedDefault;
        const bool ConsiderBrawlersNotHuntingDefault = true;
        public static bool ConsiderBrawlersNotHunting = ConsiderBrawlersNotHuntingDefault;
        const bool ConsiderHasHuntingWeaponDefault = true;
        public static bool ConsiderHasHuntingWeapon = ConsiderHasHuntingWeaponDefault;
        const float ConsiderFoodPoisoningDefault = 1.0f;
        public static float ConsiderFoodPoisoning = ConsiderFoodPoisoningDefault;
        const float ConsiderOwnRoomDefault = 1.0f;
        public static float ConsiderOwnRoom = ConsiderOwnRoomDefault;
        public static Dictionary<string, float> globalWorkAdjustments;

        static YouDoYou_Settings()
        {
            globalWorkAdjustments = new Dictionary<string, float>();
            pos = Vector2.zero;
            height = 500.0f;
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            if (globalWorkAdjustments == null)
            {
                globalWorkAdjustments = new Dictionary<string, float>();
            }
            if (pos == null)
            {
                pos = Vector2.zero;
            }
            var view = new Rect(15.0f, 0, inRect.width - 30.0f, inRect.height);
            var ls = new Listing_Standard();
            var workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            var v = 0.0f;
            var s1 = "";
            var s2 = "";
            var s3 = "";

            view.height = height;
            Widgets.BeginScrollView(inRect, ref pos, view);
            GUI.BeginGroup(view);
            view.height = 9999.0f;
            ls.Begin(new Rect(10, 10, view.width - 40, view.height - 10));
            ls.Gap(30.0f);

            s1 = "YouDoYouConsiderMovementSpeed".TranslateSimple();
            s2 = String.Format("{0}x", ConsiderMovementSpeed);
            s3 = "YouDoYouConsiderMovementSpeedLong".TranslateSimple();
            ls.LabelDouble(s1, s2, tip: s3);
            ConsiderMovementSpeed = Mathf.RoundToInt(ls.Slider(ConsiderMovementSpeed, 0.0f, 10.0f) * 10.0f) / 10.0f;
            if (ls.ButtonText("YouDoYouDefaultSliderButtonLabel".TranslateSimple()))
            {
                ConsiderMovementSpeed = ConsiderMovementSpeedDefault;
            }
            ls.GapLine(30.0f);

            s1 = "YouDoYouConsiderFoodPoisoning".TranslateSimple();
            s2 = String.Format("{0}x", ConsiderFoodPoisoning);
            s3 = "YouDoYouConsiderFoodPoisoningLong".TranslateSimple();
            ls.LabelDouble(s1, s2, tip: s3);
            ConsiderFoodPoisoning = Mathf.RoundToInt(ls.Slider(ConsiderFoodPoisoning, 0.0f, 10.0f) * 10.0f) / 10.0f;
            if (ls.ButtonText("YouDoYouDefaultSliderButtonLabel".TranslateSimple()))
            {
                ConsiderFoodPoisoning = ConsiderFoodPoisoningDefault;
            }
            ls.GapLine(30.0f);

            s1 = "YouDoYouConsiderOwnRoom".TranslateSimple();
            s2 = String.Format("{0}x", ConsiderOwnRoom);
            s3 = "YouDoYouConsiderOwnRoomLong".TranslateSimple();
            ls.LabelDouble(s1, s2, tip: s3);
            ConsiderOwnRoom = Mathf.RoundToInt(ls.Slider(ConsiderOwnRoom, 0.0f, 10.0f) * 10.0f) / 10.0f;
            if (ls.ButtonText("YouDoYouDefaultSliderButtonLabel".TranslateSimple()))
            {
                ConsiderOwnRoom = ConsiderOwnRoomDefault;
            }
            ls.GapLine(30.0f);
            ls.CheckboxLabeled("YouDoYouConsiderHasHuntingWeapon".TranslateSimple(), ref ConsiderHasHuntingWeapon, "YouDoYouConsiderHasHuntingWeaponLong".TranslateSimple());
            ls.Gap(20.0f);
            ls.CheckboxLabeled("YouDoYouConsiderBrawlersNotHunting".TranslateSimple(), ref ConsiderBrawlersNotHunting, "YouDoYouConsiderBrawlersNotHuntingLong".TranslateSimple());


            // draw sliders for each work type
            ls.GapLine(60.0f);
            foreach (WorkTypeDef workTypeDef in workTypes)
            {
                globalWorkAdjustments.TryGetValue(workTypeDef.defName, out v);
                s1 = String.Format("{0} {1}", "YouDoYouWorkTypeAdjustment".TranslateSimple(), workTypeDef.labelShort);
                s2 = String.Format("{0}%", Mathf.RoundToInt(v * 100.0f));
                ls.LabelDouble(s1, s2, tip: workTypeDef.description);
                globalWorkAdjustments.SetOrAdd(
                    workTypeDef.defName,
                    Mathf.RoundToInt(ls.Slider(v, -1.0f, 1.0f) * 100.0f) / 100.0f
                );
                ls.Gap();
            }
            // slider reset button
            if (ls.ButtonTextLabeled("YouDoYouResetGlobalSlidersLabel".TranslateSimple(), "YouDoYouResetGlobalSlidersButtonLabel".TranslateSimple()))
            {
                foreach (WorkTypeDef workTypeDef in workTypes)
                {
                    globalWorkAdjustments.SetOrAdd(workTypeDef.defName, 0f);
                }
            }

            ls.GapLine(40.0f);
            height = ls.GetRect(0).yMax + 20.0f;
            ls.End();
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ConsiderMovementSpeed, "youDoYouConsiderMovementSpeed", ConsiderMovementSpeedDefault, true);
            Scribe_Values.Look(ref ConsiderFoodPoisoning, "youDoYouConsiderFoodPoisoning", ConsiderFoodPoisoningDefault, true);
            Scribe_Values.Look(ref ConsiderOwnRoom, "youDoYouConsiderOwnRoom", ConsiderOwnRoomDefault, true);
            Scribe_Values.Look(ref ConsiderBrawlersNotHunting, "youDoYouBrawlersNotHunting", ConsiderBrawlersNotHuntingDefault, true);
            Scribe_Values.Look(ref ConsiderHasHuntingWeapon, "youDoYouHuntingWeapon", ConsiderHasHuntingWeaponDefault, true);
            if (globalWorkAdjustments == null)
            {
                globalWorkAdjustments = new Dictionary<string, float>();
            }
            Scribe_Collections.Look(ref globalWorkAdjustments, "youDoYouWorkTypeAdjustments", LookMode.Value, LookMode.Value);
            base.ExposeData();
        }

        private static Vector2 pos;
        private static float height;
    }
}
