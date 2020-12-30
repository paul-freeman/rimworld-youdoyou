using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Verse;
using RimWorld;

namespace YouDoYou
{

    public class ITab_Pawn_Work : ITab
    {
        private Pawn PawnForWork
        {
            get
            {
                if (base.SelPawn != null)
                {
                    return base.SelPawn;
                }
                Corpse corpse = base.SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }
                return null;
            }
        }

        public ITab_Pawn_Work()
        {
            this.size = new Vector2(Width, Height);
            this.labelKey = "YouDoYouITab";
            this.tutorTag = "Work";
        }

        public override bool IsVisible
        {
            get
            {
                return this.PawnForWork.IsColonistPlayerControlled;
            }
        }

        protected override void FillTab()
        {
            Pawn pawnForWork = this.PawnForWork;
            if (pawnForWork == null)
            {
                Log.Error("Work tab found no selected pawn to display.", false);
                return;
            }
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, topPadding, this.size.x, this.size.y - topPadding).ContractedBy(20f);
            Rect position = new Rect(rect.x, rect.y, rect.width, rect.height);

            try
            {
                GUI.BeginGroup(position);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                Rect outRect = new Rect(0f, 0f, position.width, position.height);
                Rect viewRect = new Rect(0f, 0f, position.width - 16f, this.scrollViewHeight);
                try
                {
                    Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect, true);
                    float num = 0f;

                    DrawPawnProfession(ref num, viewRect.width);
                    DrawPawnInterestText(ref num, viewRect.width);
                    DrawPawnPriorities(ref num, viewRect.width);
                    DrawPawnFreewillCheckbox(PawnForWork, ref num);

                    if (Event.current.type == EventType.Layout)
                    {
                        if (num + 70f > 450f)
                        {
                            this.size.y = Mathf.Min(num + 70f, (float)(UI.screenHeight - 35) - 165f - 30f);
                        }
                        else
                        {
                            this.size.y = 450f;
                        }
                        this.scrollViewHeight = num + 20f;
                    }
                }
                catch
                {
                    Logger.Message("could not draw fill tab scroll view");
                }
                finally
                {
                    Widgets.EndScrollView();

                }
            }
            catch
            {
                Logger.Message("could not draw fill tab group");
            }
            finally
            {
                GUI.EndGroup();
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

            }
        }

        private void DrawPawnFreewillCheckbox(Pawn pawn, ref float curY)
        {
            try
            {
                if (pawn == null)
                {
                    return;
                }
                string pawnKey = pawn.GetUniqueLoadID();
                YouDoYou_MapComponent ydy = Find.CurrentMap.GetComponent<YouDoYou_MapComponent>();
                if (ydy.pawnFree == null)
                {
                    ydy.pawnFree = new Dictionary<string, bool>();
                }
                if (!ydy.pawnFree.ContainsKey(pawnKey))
                {
                    ydy.pawnFree[pawnKey] = true;
                }
                bool isFree = ydy.pawnFree[pawnKey];
                bool flag = isFree;
                Rect rect = new Rect(0f, curY, 90f, 24f);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                Widgets.CheckboxLabeled(rect, "YouDoYouCheckboxFreewill".Translate(), ref isFree, false, null, null, false);
                if (Mouse.IsOver(rect))
                {
                    TooltipHandler.TipRegion(rect, "YouDoYouFreePawnTip".Translate(Faction.OfPlayer.def.pawnsPlural).CapitalizeFirst());
                }
                if (flag != isFree)
                {
                    ydy.pawnFree[pawnKey] = isFree;
                }
            }
            catch
            {
                Logger.Message("could not draw pawn free will checkbox");
            }
            curY += 28f;
        }

        private void DrawPawnProfession(ref float curY, float width)
        {
            try
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = new Color(0.9f, 0.9f, 0.9f);
                Rect rect = new Rect(0f, curY, width, 30f);
                Widgets.Label(rect, PawnForWork.story.TitleCap);
            }
            catch
            {
                Logger.Message("could not draw pawn profession");
            }
            curY += 30f;
        }

        private void DrawPawnInterestText(ref float curY, float width)
        {
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = new Color(0.9f, 0.9f, 0.9f);
                Rect rect2 = new Rect(0f, curY, width, 25f);
                if (PawnForWork.Map.GetComponent<YouDoYou_MapComponent>().pawnFree[PawnForWork.GetUniqueLoadID()])
                {
                    Widgets.Label(rect2, "WorkPreference".Translate());
                }
                else
                {
                    Widgets.Label(rect2, "WorkAssignment".Translate());
                }
            }
            catch
            {
                Logger.Message("could not draw pawn interest text");
            }
            curY += 25f;
        }

        private void DrawPawnPriorities(ref float curY, float width)
        {
            try
            {
                if (!PawnForWork.Dead)
                {
                    string pawnKey = PawnForWork.GetUniqueLoadID();
                    YouDoYou_MapComponent ydy = Find.CurrentMap.GetComponent<YouDoYou_MapComponent>();
                    if (ydy.pawnFree == null)
                    {
                        ydy.pawnFree = new Dictionary<string, bool>();
                    }
                    if (!ydy.pawnFree.ContainsKey(pawnKey))
                    {
                        ydy.pawnFree[pawnKey] = true;
                    }
                    foreach (KeyValuePair<WorkTypeDef, Priority> pair in (
                            from x in ydy.GetPriorities(PawnForWork)
                            orderby x.Value descending, x.Key.naturalPriority descending
                            select x
                            ))
                    {
                        DrawPawnWorkPriority(ref curY, width, pair.Key, pair.Value);
                    }
                }
            }
            catch
            {
                Logger.Message("could not draw pawn priorities");
            }
        }

        private void DrawPawnWorkPriority(ref float curY, float width, WorkTypeDef workTypeDef, Priority priority)
        {
            try
            {
                if (PawnForWork.Dead || PawnForWork.workSettings == null || !PawnForWork.workSettings.EverWork)
                {
                    return;
                }

                int p = priority.ToGamePriority();
                if (!Prefs.DevMode && this.PawnForWork.GetDisabledWorkTypes(true).Contains(workTypeDef))
                {
                    return;
                }

                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                string t = priority.GetTip();
                Func<string> textGetter = delegate ()
                {
                    return t;
                };
                Rect rect = new Rect(10f, curY, width - 10f, 20f);
                if (Mouse.IsOver(rect))
                {
                    GUI.color = highlightColor;
                    GUI.DrawTexture(rect, TexUI.HighlightTex);
                    TooltipHandler.TipRegion(rect, new TipSignal(textGetter, PawnForWork.thingIDNumber ^ (int)workTypeDef.index));
                }
                GUI.color = WidgetsWork.ColorOfPriority(p);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rect, workTypeDef.pawnLabel);

                if (p == 0)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.5f);
                    Widgets.DrawLineHorizontal(0f, rect.center.y, rect.width);
                }
            }
            catch
            {
                Logger.Message("could not draw pawn work priority");
            }
            curY += 20f;
        }
        public const float Width = 300f;
        public const float Height = 500f;
        private const float topPadding = 5f;
        private float scrollViewHeight;
        private Vector2 scrollPosition = Vector2.zero;
        private static readonly Color highlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    }
}
