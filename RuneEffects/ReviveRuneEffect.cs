﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Runestones.RuneEffects
{
    public class ReviveRuneEffect : RuneEffect
    {
        static Dictionary<Skills.SkillType, float> localLastDeathSkills = new Dictionary<Skills.SkillType, float>();
        public const float baseDuration = 90;
        public const string vfxName = "vfx_Potion_stamina_medium";
        public ReviveRuneEffect()
        {
            _FlavorText = "He had such a knowledge of the Dark Side, he could even keep the ones he cared about from dying";
            _EffectText = new List<string> { "After a recent death, restores 50% of skill levels lost and grants +75% movement speed" };
            _QualityEffectText[RuneQuality.Ancient] = new List<string> { "Restores 100% of skill levels lost (progress to next skill level still resets)", "+100% Revivify duration" };
            _QualityEffectText[RuneQuality.Dark] = new List<string> { "Restores 100% of skill levels lost (progress to next skill level still resets)", "Teleports you to your gravestone", "No Revivify buff" };
            _RelativeStats = new Dictionary<string, Func<string>> { { "Revivify speed buff duration", () => _Quality==RuneQuality.Dark ? "N/A" : $"{baseDuration * _Effectiveness * (_Quality == RuneQuality.Ancient ? 2 : 1)}" } };
            speed = CastingAnimations.CastSpeed.Medium;
        }
        public override void DoMagicAttack(Attack baseAttack)
        {
            var player = (Player)baseAttack.GetCharacter();

            if (player == Player.m_localPlayer)
                Debug.Log("Revive rune caster is local player");
            else
                throw new Exception("Revive rune caster not local player");
            
            if (!(bool)typeof(Player).GetMethod("HardDeath", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null) && player == Player.m_localPlayer)
            {
                var playerSkills = (Dictionary<Skills.SkillType, Skills.Skill>)typeof(Skills).GetField("m_skillData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player.GetSkills());
                foreach (var keyValue in localLastDeathSkills)
                {
                    float difference = keyValue.Value - playerSkills[keyValue.Key].m_level;
                    playerSkills[keyValue.Key].m_level += difference * (_Quality==RuneQuality.Common ? 0.5f : 1);
                    /*
                    if (((int)difference) % 2 != 0)
                        playerSkills[keyValue.Key].m_accumulator = (float)typeof(Skills).GetMethod("GetNextLevelRequirement", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player.GetSkills(), null) / 2;
                    */
                }
                typeof(Player).GetField("m_timeSinceDeath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, 999999f);
                if (_Quality == RuneQuality.Dark)
                {
                    var deathPoint = Game.instance.GetPlayerProfile().GetDeathPoint();
                    player.TeleportTo(deathPoint + Vector3.up, player.transform.rotation, true);
                }
                else
                {
                    var statusEffect = player.GetSEMan().AddStatusEffect("SE_Revivify");
                    if (statusEffect != null)
                        statusEffect.m_ttl = baseDuration * _Effectiveness * (_Quality == RuneQuality.Ancient ? 2 : 1);
                }
            }
            else
            {
                throw new Exception("Last death too long ago to revive from");
            }
        }

        [HarmonyPatch(typeof(Skills), "OnDeath")]
        public static class SkillDeathMod
        {
            public static void Prefix(Player ___m_player, Dictionary<Skills.SkillType, Skills.Skill> ___m_skillData)
            {
                if (___m_player == Player.m_localPlayer)
                {
                    Debug.Log("Local player died. Recording skill levels for future revival...");
                    localLastDeathSkills = ___m_skillData.ToDictionary<KeyValuePair<Skills.SkillType, Skills.Skill>, Skills.SkillType, float>(pair => pair.Key, pair => pair.Value.m_level);
                }
            }
        }

        public class SE_Revivify : SE_Stats
        {
            public SE_Revivify() : base()
            {
                name = "SE_Revivify";
                m_name = "Revivify";
                m_tooltip = "+75% Speed";
                m_startMessage = "Revivified";
                m_time = 0;
                m_ttl = baseDuration;
                m_icon = (from Sprite s in Resources.FindObjectsOfTypeAll<Sprite>() where s.name == "CorpseRun" select s).FirstOrDefault();

                m_startEffects = new EffectList();
                var vfxPrefab = ZNetScene.instance.GetPrefab(vfxName);
                m_startEffects.m_effectPrefabs = new EffectList.EffectData[] { new EffectList.EffectData { m_prefab = vfxPrefab, m_enabled = true, m_attach = true, m_scale = true } };
            }

            override public void ModifySpeed(ref float speed)
            {
                speed *= 1.75f;
            }
        }
    }
}
