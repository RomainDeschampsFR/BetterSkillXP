using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using ModSettings;
using UnityEngine;

namespace BetterSkillXP
{
    class BetterSkillXPSettings : JsonModSettings
    {
        // COOKING SECTION
        [Section("COOKING")]
        [Name("ENABLED")]
        [Description("NO : Vanilla\nYES : Earn point based on time spent")]
        public bool enableCooking = true;

        [Name("COOKING & PREPPING TIME (HOURS)")]
        [Description("Set the hours spent cooking & prepping needed to gain a skill point.")]
        [Slider(0.25f, 3.00f, 12, NumberFormat = "{0:0.00}")]
        public float cookingHours = 1.0f;

        [Name("TIME PENALTY WITHOUT POT/SKILLET")]
        [Description("Penalty for cooking tea in a recylcled can or food directly over the fire" +
            "\n 0% : no penalty" +
            "\n 50% : half of the cooking time will count towards the next skill point" +
            "\n 100% : no cooking time will count towards the next skill point" +
            "\n e.g. with 50% penalty 1 hour of cooking time will only count for 30 mins")]
        [Slider(0, 100, 21, NumberFormat = "{0:0}%")]
        public int cookingPenalty = 25;

        [Name("TIME BONUS PER RECIPE LEVEL")]
        [Description("Cumulative bonus you'll get for cooking recipes according to their level" +
            "\n 5% : You get 5% time bonus for each recipe level" +
            "\n lvl 1(+5%) / lvl 2(+10%) / lvl 3(+15%) / lvl 4(+20%) / lvl 5(+25%)" +
            "\n 50% : lvl 1(+50%) / lvl 2(+100%) / lvl 3(+150%) / lvl 4(+200%) / lvl 5(+250%)" +
            "\n e.g. with 5% bonus, a recipe lvl 5, cooking for 2 hours (prep + cooking time) will count for 2.5 hours")]
        [Slider(0, 50, 11, NumberFormat = "{0:0}%")]
        public int cookingBonus = 10;

        // MENDING SECTION
        [Section("MENDING")]
        [Name("ENABLED")]
        [Description("NO : Vanilla\nYES : Earn point based on time spent")]
        public bool enableMending = true;

        [Name("MENDING TIME (HOURS)")]
        [Description("Set the hours spent mending needed to gain a skill point." +
            "\n Succeeding or failing doesn't matter, only time spent counts." +
            "\n Any time spent crafting an item requiring a Sewing Kit will count as well.")]
        [Slider(0.25f, 3.00f, 12, NumberFormat = "{0:0.00}")]
        public float mendingHours = 1.0f;

        // FIRE STARTING SECTION
        [Section("FIRE STARTING")]
        [Name("ENABLED")]
        [Description("NO : Vanilla\nYES : Earn point based on time spent")]
        public bool enableFireStarting = true;

        [Name("FIRE STARTING TIME (MINUTES)")]
        [Description("Set the minutes spent starting a fire needed to gain a skill point." +
            "\n As reference : At skill lvl 1, it takes around 3.5 min to lit a fire (5 min with mag lens)" +
            "\n Succeeding or failing doesn't matter, only time spent counts.")]
        [Slider(1, 30, NumberFormat = "{0:0}")]
        public int fireStartingMinutes = 7;

        // GUNSMITHING SECTION
        [Section("GUNSMITHING")]
        [Name("ENABLED")]
        [Description("NO : Vanilla\nYES : Earn point based on time spent")]
        public bool enableGunsmithing = true;

        [Name("GUNSMITHING  TIME (HOURS)")]
        [Description("Set the hours spent gunsmithing needed to gain a skill point.")]
        [Slider(0.25f, 3.00f, 12, NumberFormat = "{0:0.00}")]
        public float gunsmithingHours = 1.0f;

        [Name("TIME PENALTY FOR HARVESTING")]
        [Description("Any time spent harvesting items that produces gunpowder will count towards the next skill point." +
            "\n You can then set a penalty so that harvesting time doesn't count as much as other gunsmithing activities." +
            "\n 0% : no penalty" +
            "\n 50% : half of the harvesting time will count towards the next skill point" +
            "\n 100% : no harvesting time will count towards the next skill point" +
            "\n e.g. with 50% penalty 1 hour of harvesting time will only count for 30 mins")]
        [Slider(0, 100, 21, NumberFormat = "{0:0}%")]
        public int gunsmithingPenalty = 75;

        [Name("TIME BONUS FOR REPAIRING")]
        [Description("Any time spent repairing gun items will count towards the next skill point." +
            "\n You can then set a time bonus so that repair time counts more than other gunsmithing activities." +
            "\n e.g. with 50% bonus, repairing guns for 1 hour will count for 1.5 hours")]
        [Slider(0, 50, 11, NumberFormat = "{0:0}%")]
        public int gunsmithingRepairBonus = 0;

        [Name("TIME BONUS FOR CRAFTING")]
        [Description("Any time spent crafting bullets, gunpowder and any kind of item requiring gunpowder will count towards the next skill point." +
            "\n You can then set a time bonus so that crafting time counts more than other gunsmithing activities." +
            "\n e.g. with 50% bonus, crafting for 1 hour will count for 1.5 hours")]
        [Slider(0, 50, 11, NumberFormat = "{0:0}%")]
        public int gunsmithingCraftBonus = 0;

        protected override void OnConfirm()
        {
            base.OnConfirm();
        }

        internal void RefreshFields()
        {
            //COOKING
            SetFieldVisible(nameof(cookingHours), Settings.settings.enableCooking == true);
            SetFieldVisible(nameof(cookingPenalty), Settings.settings.enableCooking == true);
            SetFieldVisible(nameof(cookingBonus), Settings.settings.enableCooking == true);
            //MENDING
            SetFieldVisible(nameof(mendingHours), Settings.settings.enableMending == true);
            //FIRESTARTING
            SetFieldVisible(nameof(fireStartingMinutes), Settings.settings.enableFireStarting == true);
            //GUNSMITHING
            SetFieldVisible(nameof(gunsmithingHours), Settings.settings.enableGunsmithing == true);
            SetFieldVisible(nameof(gunsmithingPenalty), Settings.settings.enableGunsmithing == true);
            SetFieldVisible(nameof(gunsmithingRepairBonus), Settings.settings.enableGunsmithing == true);
            SetFieldVisible(nameof(gunsmithingCraftBonus), Settings.settings.enableGunsmithing == true);

        }

        protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
        {
            RefreshFields();
        }
    }

        internal static class Settings
    {
        public static BetterSkillXPSettings settings = new();

        public static void OnLoad()
        {
            settings.AddToModSettings("Better Skill XP");
        }
    }
}
