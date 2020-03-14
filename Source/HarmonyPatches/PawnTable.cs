// PawnTable_PawnTableOnGUI.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YouDoYou
{
    [StaticConstructorOnStartup]
    static class YouDoYouPatch
    {
        static YouDoYouPatch()
        {
            var harmony = new Harmony("rimworld.freemapa.youdoyou");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static List<PawnColumnDef> columns;
    }

    [HarmonyPatch(typeof(PawnTable), nameof(PawnTable.PawnTableOnGUI))]
    public class PawnTable_PawnTableOnGUI
    {
        private static Type t = typeof(PawnTable);
        private static MethodInfo RecacheIfDirtyMethod = AccessTools.Method(t, "RecacheIfDirty");
        private static FieldInfo cachedColumnWidthsField = AccessTools.Field(t, "cachedColumnWidths");
        private static FieldInfo cachedRowHeightsField = AccessTools.Field(t, "cachedRowHeights");
        private static FieldInfo standardMarginField = AccessTools.Field(typeof(Window), "StandardMargin");

        static PawnTable_PawnTableOnGUI()
        {
            if (RecacheIfDirtyMethod == null) throw new NullReferenceException("RecacheIfDirty field not found.");
            if (cachedColumnWidthsField == null) throw new NullReferenceException("cachedColumnWidths field not found.");
            if (cachedRowHeightsField == null) throw new NullReferenceException("cachedRowHeights field not found.");
            if (standardMarginField == null) throw new NullReferenceException("standardMargin field not found.");
        }

        public static bool Prefix(PawnTable __instance, Vector2 position, PawnTableDef ___def, ref Vector2 ___scrollPosition, List<LookTargets> ___cachedLookTargets)
        {
            if (___def != PawnTableDefOf.Work)
                return true;

            if (Event.current.type == EventType.Layout)
                return false;

            RecacheIfDirtyMethod.Invoke(__instance, null);

            var cachedSize = __instance.Size;
            var cachedColumnWidths = cachedColumnWidthsField.GetValue(__instance) as List<float>;
            var cachedHeaderHeight = __instance.HeaderHeight;
            var cachedHeightNoScrollbar = __instance.HeightNoScrollbar;
            var cachedPawns = __instance.PawnsListForReading;
            var cachedRowHeights = cachedRowHeightsField.GetValue(__instance) as List<float>;

			float num = cachedSize.x - 16f;
			int num2 = 0;
			for (int i = 0; i < ___def.columns.Count; i++)
			{
				int num3;
				if (i == ___def.columns.Count - 1)
				{
					num3 = (int)(num - (float)num2);
				}
				else
				{
					num3 = (int)cachedColumnWidths[i];
				}
				Rect rect = new Rect((float)((int)position.x + num2), (float)((int)position.y), (float)num3, (float)((int)cachedHeaderHeight));
				___def.columns[i].Worker.DoHeader(rect, __instance);
				num2 += num3;
			}
			Rect outRect = new Rect((float)((int)position.x), (float)((int)position.y + (int)cachedHeaderHeight), (float)((int)cachedSize.x), (float)((int)cachedSize.y - (int)cachedHeaderHeight));
			Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, (float)((int)cachedHeightNoScrollbar - (int)cachedHeaderHeight));
			Widgets.BeginScrollView(outRect, ref ___scrollPosition, viewRect, true);
			int num4 = 0;
			for (int j = 0; j < cachedPawns.Count; j++)
			{
				num2 = 0;
				if ((float)num4 - ___scrollPosition.y + (float)((int)cachedRowHeights[j]) >= 0f && (float)num4 - ___scrollPosition.y <= outRect.height)
				{
					GUI.color = new Color(1f, 1f, 1f, 0.2f);
					Widgets.DrawLineHorizontal(0f, (float)num4, viewRect.width);
					GUI.color = Color.white;
					Rect rect2 = new Rect(0f, (float)num4, viewRect.width, (float)((int)cachedRowHeights[j]));
					if (Mouse.IsOver(rect2))
					{
						GUI.DrawTexture(rect2, TexUI.HighlightTex);
						___cachedLookTargets[j].Highlight(true, cachedPawns[j].IsColonist, false);
					}
					for (int k = 0; k < ___def.columns.Count; k++)
					{
						int num5;
						if (k == ___def.columns.Count - 1)
						{
							num5 = (int)(num - (float)num2);
						}
						else
						{
							num5 = (int)cachedColumnWidths[k];
						}
						Rect rect3 = new Rect((float)num2, (float)num4, (float)num5, (float)((int)cachedRowHeights[j]));
						___def.columns[k].Worker.DoCell(rect3, cachedPawns[j], __instance);
						num2 += num5;
					}
					if (cachedPawns[j].Downed)
					{
						GUI.color = new Color(1f, 0f, 0f, 0.5f);
						Widgets.DrawLineHorizontal(0f, rect2.center.y, viewRect.width);
						GUI.color = Color.white;
					}
				}
				num4 += (int)cachedRowHeights[j];
			}
			Widgets.EndScrollView();

            return false;
		}
    }

	[HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
	public class DefGenerator_GenerateImpliedDefs_PreResolve
	{
		static void Postfix()
		{
			var youDoYouTable = PawnTableDefOf.Work;
			var labelIndex = youDoYouTable.columns.IndexOf(DefDatabase<PawnColumnDef>.GetNamed("CopyPasteWorkPriorities"));
			youDoYouTable.columns.Insert(labelIndex + 1, DefDatabase<PawnColumnDef>.GetNamed("AutoPriority"));
			YouDoYouPatch.columns = new List<PawnColumnDef>(youDoYouTable.columns);
		}
	}
}