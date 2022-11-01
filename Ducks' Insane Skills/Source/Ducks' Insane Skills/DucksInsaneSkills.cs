using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using System.Reflection;
using Verse;
using Verse.AI;
using UnityEngine;
using System;

namespace DucksInsaneSkills
{
    public class DucksInsaneSkillsMod : Mod
    {
        public static DucksInsaneSkillsSettings settings;
        public DucksInsaneSkillsMod(ModContentPack content) : base(content)
        {
            DucksInsaneSkillsMod.settings = GetSettings<DucksInsaneSkillsSettings>();
            var harmony = new Harmony("net.ducks.rimworld.mod.ducksskills");
            harmony.PatchAll();
        }

		public int GetMaxLevel()
        {
			return DucksInsaneSkillsMod.settings.MaxLevel;
		}

		public override string SettingsCategory() => "Ducks' Insane Skills";
        public override void DoSettingsWindowContents(Rect canvas) { settings.DoWindowContents(canvas); }
    }


    [HarmonyPatch(typeof(SkillRecord))]
    [HarmonyPatch("XpRequiredToLevelUpFrom")]
    static class DucksSkills_XpRequiredToLevelUpFrom
    {
        public static bool Prefix(ref float __result, int startingLevel)
        {
            float currentlevel = startingLevel;
            float calc = (currentlevel + 1f) / 10f * ((currentlevel + 1f) / 10f + 1f) * 5f * 1000f;
            __result = calc;
            return false;
        }
	}

    [HarmonyPatch(typeof(SkillRecord))]
    [HarmonyPatch("Learn")]
    static class DucksSkills_Learn
    {
        public static FieldInfo _pawn;

        public static Pawn GetPawn(this SkillRecord _this)
        {
            if (_pawn == null)
            {
                _pawn = typeof(SkillRecord).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            if (_pawn == null)
            {
                Log.ErrorOnce("Unable to reflect SkillRecord.pawn!", 305432421);
            }
            return (Pawn)_pawn.GetValue(_this);
        }

        public static bool Prefix(SkillRecord __instance, float xp, bool direct = false)
        {
            if (__instance.TotallyDisabled)
			{
				return false;
			}
			if (xp < 0f && __instance.levelInt == 0)
			{
				return false;
			}
			bool flag = false;
			if (xp > 0f)
			{
				xp *= __instance.LearnRateFactor(direct);
			}
			__instance.xpSinceLastLevel += xp;
			if (!direct)
			{
				__instance.xpSinceMidnight += xp;
			}
			//if (__instance.levelInt == 20 && __instance.xpSinceLastLevel > __instance.XpRequiredForLevelUp - 1f)
			//{
			//	__instance.xpSinceLastLevel = __instance.XpRequiredForLevelUp - 1f;
			//}
			while (__instance.xpSinceLastLevel >= __instance.XpRequiredForLevelUp)
			{
				__instance.xpSinceLastLevel -= __instance.XpRequiredForLevelUp;
				__instance.levelInt++;
				flag = true;
				if (__instance.levelInt == 14)
				{
					if (__instance.passion == Passion.None)
					{
						TaleRecorder.RecordTale(TaleDefOf.GainedMasterSkillWithoutPassion, new object[]
						{
							GetPawn(__instance),
							__instance.def
						});
					}
					else
					{
						TaleRecorder.RecordTale(TaleDefOf.GainedMasterSkillWithPassion, new object[]
						{
							GetPawn(__instance),
							__instance.def
						});
					}
				}
			}
			while (__instance.xpSinceLastLevel <= -1000f)
			{
				__instance.levelInt--;
				__instance.xpSinceLastLevel += __instance.XpRequiredForLevelUp;
				if (__instance.levelInt <= 0)
				{
					__instance.levelInt = 0;
					__instance.xpSinceLastLevel = 0f;
					break;
				}
			}
			if (flag && GetPawn(__instance).IsColonist && GetPawn(__instance).SpawnedOrAnyParentSpawned)
			{
				MoteMaker.ThrowText(GetPawn(__instance).DrawPosHeld ?? GetPawn(__instance).PositionHeld.ToVector3Shifted(), GetPawn(__instance).MapHeld, __instance.def.LabelCap + "\n" + "TextMote_SkillUp".Translate(__instance.Level), -1f);
			}
			return false;
		}
    }

	[HarmonyPatch(typeof(SkillRecord))]
	[HarmonyPatch("Level", MethodType.Setter)]
	static class DucksSkills_Level
	{
		public static bool Prefix(SkillRecord __instance, int value)
		{
			if (!DucksInsaneSkillsMod.settings.InfiniteLeveling)
			{
				__instance.levelInt = Math.Min(value, DucksInsaneSkillsMod.settings.MaxLevel);
			}
			else
			{
				__instance.levelInt = value;
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SkillRecord))]
	[HarmonyPatch("Interval")]
	static class DucksSkills_Interval
	{
		public static bool Prefix()
		{
			return false;
		}
	}

	[HarmonyPatch(typeof(SkillRecord))]
	[HarmonyPatch("GetLevel")]
	static class DucksSkills_GetLevel
	{
		public static bool Prefix(SkillRecord __instance, ref int __result, ref bool includeAptitudes)
		{
			if (__instance.TotallyDisabled)
			{
				__result = 0;
				return false;
			}
			int num = __instance.levelInt;
			if (includeAptitudes)
			{
				num += __instance.Aptitude;
			}
			__result = Mathf.Max(num, 0); // TODO: Max level options
			return false;
		}
	}

	[HarmonyPatch(typeof(SkillRecord))]
	[HarmonyPatch("GetLevelForUI")]
	static class DucksSkills_GetLevelForUI
	{
		public static bool Prefix(SkillRecord __instance, ref int __result, ref bool includeAptitudes)
		{
			if (__instance.PermanentlyDisabled)
			{
				__result = 0;
				return false;
			}
			int num = __instance.levelInt;
			if (includeAptitudes)
			{
				num += __instance.Aptitude;
			}
			__result = Mathf.Max(num, 0); // TODO: Max level options
			return false;
		}
	}

	[HarmonyPatch(typeof(QualityUtility))]
	[HarmonyPatch(nameof(QualityUtility.GenerateQualityCreatedByPawn))]
	[HarmonyPatch(new Type[] { typeof(int), typeof(bool) })]
	static class DucksSkills_GenerateQualityCreatedByPawn
	{
		public static bool Prefix(ref QualityCategory __result, int relevantSkillLevel, bool inspired)
		{
			System.Random rng = new System.Random();

			float relevantSkillLevelFloat = relevantSkillLevel;

			float modifier = relevantSkillLevelFloat;
			if (inspired)
			{
				modifier = (relevantSkillLevelFloat * 2f) + 10f;
			}

			float bySkill = -(1f / (0.009f * modifier + 0.155f)) + 7f; // for a very long time i forgot about the negative sign in my equation. goodbye 4 hours of my life

			float rngRoll = Math.Max(Rand.GaussianAsymmetric(bySkill, 0.5f, 0.5f), 0f);
			float rngLeftOver = rngRoll % 1;
			if (Rand.Value < rngLeftOver)
			{
				rngRoll = (int)Math.Floor(rngRoll) + 1;
			}
			else
			{
				rngRoll = (int)Math.Floor(rngRoll);
			}
			rngRoll = Math.Min(Math.Max(rngRoll, 0f), 6f);

			__result = (QualityCategory)rngRoll;
			return false;
		}
	}

	[HarmonyPatch(typeof(QualityUtility))]
	[HarmonyPatch(nameof(QualityUtility.SendCraftNotification))]
	static class DucksSkills_SendCraftNotification
	{
		public static bool Prefix(Thing thing, Pawn worker)
		{
			if (DucksInsaneSkillsMod.settings.MuteCraftNotifications)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Bill))]
	[HarmonyPatch("PawnAllowedToStartAnew")]
	static class DucksSkills_PawnAllowedToStartAnew
	{
		public static bool Prefix(Bill __instance, ref bool __result, Pawn p)
		{

			// === 1.2 patch ===
			//if (__instance.pawnRestriction != null)
			//{
			//    __result = __instance.pawnRestriction == p;
			//    return false;
			//}

			// === 1.3 patch ===
			//if (__instance.PawnRestriction != null)
			//{
			//    __result = __instance.PawnRestriction == p;
			//    return false;
			//}
			//if (__instance.SlavesOnly)
			//{
			//    __result = p.IsSlave;
			//    return false;
			//}
			//
			//if (__instance.recipe.workSkill != null)
			//{
			//    int level = p.skills.GetSkill(__instance.recipe.workSkill).Level;
			//    if (level < __instance.allowedSkillRange.min)
			//    {
			//        JobFailReason.Is("UnderAllowedSkill".Translate(__instance.allowedSkillRange.min), __instance.Label);
			//        __result = false;
			//        return false;
			//    }
			//    if (__instance.allowedSkillRange.max != 20 & level > __instance.allowedSkillRange.max)
			//    {
			//        JobFailReason.Is("AboveAllowedSkill".Translate(__instance.allowedSkillRange.max), __instance.Label);
			//        __result = false;
			//        return false;
			//    }
			//}

			// === 1.4 patch === ... I should really just do code modification rather than replacing the method.
			if (__instance.PawnRestriction != null)
			{
				__result = __instance.PawnRestriction == p;
				return false;
			}
			if (__instance.SlavesOnly && !p.IsSlave)
			{
				__result = false;
				return false;
			}
			if (__instance.MechsOnly && !p.IsColonyMechPlayerControlled)
			{
				__result = false;
				return false;
			}
			if (__instance.NonMechsOnly && p.IsColonyMechPlayerControlled)
			{
				__result = false;
				return false;
			}
			if (__instance.recipe.workSkill != null && (p.skills != null || p.IsColonyMech))
			{
				int num = (p.skills != null) ? p.skills.GetSkill(__instance.recipe.workSkill).Level : p.RaceProps.mechFixedSkillLevel;
				if (num < __instance.allowedSkillRange.min)
				{
					JobFailReason.Is("UnderAllowedSkill".Translate(__instance.allowedSkillRange.min), __instance.Label);
					return false;
				}
				if (__instance.allowedSkillRange.max != 20 & num > __instance.allowedSkillRange.max)
				{
					JobFailReason.Is("AboveAllowedSkill".Translate(__instance.allowedSkillRange.max), __instance.Label);
					return false;
				}
			}
			if (ModsConfig.BiotechActive && __instance.recipe.mechanitorOnlyRecipe && !MechanitorUtility.IsMechanitor(p))
			{
				JobFailReason.Is("NotAMechanitor".Translate(), null);
				return false;
			}

			__result = true;
			return false;
		}
	}

}
