using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace YouDoYou
{
    public class YouDoYou_Settings : ModSettings
    {
        public static bool BrawlersCanHunt;
        public static Dictionary<string, float> WorkTypeAdjustment;

        static YouDoYou_Settings()
        {
            WorkTypeAdjustment = new Dictionary<string, float>();
            _scrollPosition = Vector2.zero;
            _height = 500.0f;
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            var _view = new Rect(15.0f, 0, inRect.width - 30.0f, inRect.height);
            var _listingStandard = new Listing_Standard();
            var _workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            var _sliderValue = 0.0f;

            _view.height = _height;
            Widgets.BeginScrollView(inRect, ref _scrollPosition, _view);
            GUI.BeginGroup(_view);
            _view.height = 9999.0f;
            _listingStandard.Begin(_view.ContractedBy(10.0f));

            _listingStandard.Label("YouDoYouWorkTypeAdjustments".Translate("YouDoYouPriorityColonyPolicy".TranslateSimple()));
            _listingStandard.GapLine(40.0f);

            // draw sliders for each work type
            foreach (WorkTypeDef workTypeDef in _workTypes)
            {
                WorkTypeAdjustment.TryGetValue(workTypeDef.defName, out _sliderValue);
                _listingStandard.Label(
                    "YouDoYouWorkTypeAdjustment".Translate(workTypeDef.defName, Mathf.RoundToInt(_sliderValue * 100.0f)),
                    tooltip: workTypeDef.description
                );
                WorkTypeAdjustment.SetOrAdd(
                    workTypeDef.defName,
                    Mathf.RoundToInt(_listingStandard.Slider(_sliderValue, -1.0f, 1.0f) * 100.0f) / 100.0f
                );
                _listingStandard.Gap(18.0f);
            }

            // draw button options
            if (_listingStandard.ButtonTextLabeled("YouDoYouResetSlidersLabel".TranslateSimple(), "YouDoYouResetSlidersButtonLabel".TranslateSimple()))
            {
                foreach (WorkTypeDef workTypeDef in _workTypes)
                {
                    WorkTypeAdjustment.SetOrAdd(workTypeDef.defName, 0f);
                }
            }
            _listingStandard.GapLine(40.0f);
            _listingStandard.CheckboxLabeled("BrawlersCanHuntLong".Translate(), ref BrawlersCanHunt, "BrawlersCanHuntShort".Translate());
            _height = _listingStandard.GetRect(0).yMax + 20.0f;

            _listingStandard.End();
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref BrawlersCanHunt, "brawlersCanHunt", false);
            Scribe_Collections.Look(ref WorkTypeAdjustment, "youDoYouWorkTypeAdjustments", LookMode.Value, LookMode.Value);
            base.ExposeData();
        }

        private static Vector2 _scrollPosition;
        private static float _height;
    }
}
