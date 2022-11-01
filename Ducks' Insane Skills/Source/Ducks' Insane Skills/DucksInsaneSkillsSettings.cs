using Verse;
using UnityEngine;

namespace DucksInsaneSkills
{
    public class DucksInsaneSkillsSettings : ModSettings
    {

        public bool InfiniteLeveling = true;
        public int MaxLevel = 0;

        // Leveling styles:
        // Vanilla (Exponential) - Vanilla equation
        // Linear - Linearly increased XP required per level.
        // Static - Set requirement for each level

        public bool MuteCraftNotifications = true;

        public void DoWindowContents(Rect canvas)
        {

        }

        public override void ExposeData()
        {

            Scribe_Values.Look(ref InfiniteLeveling, "InfiniteLeveling", true);
            Scribe_Values.Look(ref MaxLevel, "MaxLevel", 0);

            //Scribe_Values.Look(ref PatchSkillCap, "PatchSkillCap", true);
            //Scribe_Values.Look(ref ValueSkillCap, "ValueSkillCap", 2000);

            Scribe_Values.Look(ref MuteCraftNotifications, "MuteCraftNotifications", true);
            base.ExposeData();
        }

    }
}
