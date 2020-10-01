using System.Collections.Generic;
using System.Reflection;
using Verse;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;


namespace YouDoYou
{
    public class YouDoYou_WorldComponent : WorldComponent
    {
        public YouDoYou_WorldComponent(World world) : base(world)
        {
            checkedForInterestsMod = false;
            interestStrings = new List<string> { };
            AutoPriorities = new Dictionary<string, bool> { };
            settings = LoadedModManager.GetMod<YouDoYou_Mod>().GetSettings<YouDoYou_Settings>();
            activeAlertsField = AccessTools.Field(typeof(AlertsReadout), "AllAlerts");
            if (activeAlertsField == null)
            {
                Logger.Error("could not find activeAlerts field");
            }
        }

        private readonly FieldInfo activeAlertsField;
        private static bool checkedForInterestsMod;
        public static List<string> InterestsStrings { get { return interestStrings; } }
        private static List<string> interestStrings;
        public Alert_NeedWarmClothes AlertNeedWarmClothes { get { return alertNeedWarmClothes; } }
        private Alert_NeedWarmClothes alertNeedWarmClothes;
        public Dictionary<string, bool> AutoPriorities;
        public static YouDoYou_Settings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = LoadedModManager.GetMod<YouDoYou_Mod>().GetSettings<YouDoYou_Settings>();
                }
                return settings;
            }
        }
        private static YouDoYou_Settings settings = null;
        private int counter = 0;
        private const int restTicks = 300;

        // Basically the goal here is to spread the work out over a number of
        // map ticks and then stop for a bit.
        //
        // So we do a few prep ticks to pull environmental values and then one
        // tick for each pawn. Finally, we rest for some amount of ticks.
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            counter++;
            if (counter > restTicks)
            {
                counter = 0;
            }

            switch (counter)
            {
                case 0:
                    PrepForSettingPriorities();
                    break;
                case 1:
                    CheckActiveAlerts();
                    break;
                case 2:
                    ActivateManualPriorities();
                    break;
                default:
                    break;
            }
        }

        private void PrepForSettingPriorities()
        {
            settings = LoadedModManager.GetMod<YouDoYou_Mod>().GetSettings<YouDoYou_Settings>();
        }

        public static bool HasInterestsFramework()
        {
            if (checkedForInterestsMod)
            {
                return (interestStrings != null);
            }

            checkedForInterestsMod = true;
            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "[D] Interests Framework"))
            {
                Logger.Message("found \"[D] Interests Framework\"");
                var interestsBaseT = AccessTools.TypeByName("DInterests.InterestBase");
                if (interestsBaseT == null)
                {
                    Logger.Error("did not find interestsBase");
                    return false;
                }

                var interestList = AccessTools.Field(interestsBaseT, "interestList").GetValue(interestsBaseT);
                if (interestList == null)
                {
                    Logger.Error("did not find interest list");
                    return false;
                }

                var interestListT = AccessTools.TypeByName("DInterests.InterestList");
                if (interestListT == null)
                {
                    Logger.Error("could not find interest list type");
                    return false;
                }

                var countMethod = AccessTools.Method(interestListT.BaseType, "get_Count", null);
                if (countMethod == null)
                {
                    Logger.Error("could not find count method");
                    return false;
                }

                var count = countMethod.Invoke(interestList, null);
                if (count == null)
                {
                    Logger.Error("could not get count");
                    return false;
                }

                var interestDefT = AccessTools.TypeByName("DInterests.InterestDef");
                if (interestDefT == null)
                {
                    Logger.Error("could not find interest def type");
                    return false;
                }

                var getItem = AccessTools.Method(interestListT.BaseType, "get_Item");
                if (getItem == null)
                {
                    Logger.Error("coud not find get item method");
                    return false;
                }

                var defNameField = AccessTools.Field(interestDefT, "defName");
                if (defNameField == null)
                {
                    Logger.Error("could not get defName field");
                    return false;
                }

                interestStrings = new List<string> { };
                for (int i = 0; i < (int)count; i++)
                {
                    var interestDef = getItem.Invoke(interestList, new object[] { i });
                    if (interestDef == null)
                    {
                        Logger.Error("could not find interest def");
                        return false;
                    }
                    var defName = defNameField.GetValue(interestDef);
                    if (defName == null)
                    {
                        Logger.Error("could not get defname");
                        return false;
                    }
                    Logger.Message("supporting interest " + (string)defName);
                    interestStrings.Add((string)defName);
                }
                return true;
            }
            return false;
        }

        public void CheckActiveAlerts()
        {
            try
            {
                UIRoot_Play ui = Find.UIRoot as UIRoot_Play;
                if (ui == null)
                {
                    return;
                }
                bool foundAlertNeedWarmClothes = false;
                foreach (Alert alert in (List<Alert>)activeAlertsField.GetValue(ui.alerts))
                {
                    if (!alert.Active)
                    {
                        continue;
                    }
                    if (!foundAlertNeedWarmClothes)
                    {
                        this.alertNeedWarmClothes = alert as Alert_NeedWarmClothes;
                        if (alertNeedWarmClothes != null)
                        {
                            foundAlertNeedWarmClothes = true;
                        }
                    }
                }
                if (!foundAlertNeedWarmClothes)
                {
                    this.alertNeedWarmClothes = null;
                }
            }
            catch
            {
                Logger.Error("could not check active alerts");
            }
        }

        public void ActivateManualPriorities()
        {
            Current.Game.playSettings.useWorkPriorities = true;
        }
    }
}
