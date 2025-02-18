﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static HitData;

namespace Runestones.RuneEffects
{
    public class SunderRuneEffect : RuneEffect
    {
        public const float baseDuration = 20;
        public SunderRuneEffect()
        {
            _FlavorText = "No armor is without cracks";
            _EffectText = new List<string> { "Removes enemy resistances to physical damage" };
            _QualityEffectText[RuneQuality.Ancient] = new List<string> { "Makes enemies weak to physical damage instead" };
            _QualityEffectText[RuneQuality.Dark] = new List<string> { "Makes enemies very weak to elemental damage instead" };
            _RelativeStats = new Dictionary<string, Func<string>> { { "Duration", () => $"{baseDuration * _Effectiveness :F1} sec" } };
            targetLock = true;
        }
        public override void DoMagicAttack(Attack baseAttack)
        {
            var gameObject = DebuffVfx.ConstructAoeVfx();
            var aoe = gameObject.AddComponent<SunderAoe>();

            var statusEffect = ScriptableObject.CreateInstance<SE_Sunder>();
            statusEffect.m_ttl = baseDuration * _Effectiveness;
            switch(_Quality)
            {
                case RuneQuality.Common:
                    statusEffect.targetDamageModifier = DamageModifier.Normal;
                    break;
                case RuneQuality.Ancient:
                    statusEffect.targetDamageModifier = DamageModifier.Weak;
                    break;
                case RuneQuality.Dark:
                    statusEffect.targetDamageModifier = DamageModifier.VeryWeak;
                    statusEffect.modifyElemental = true;
                    break;
            }
            aoe.m_statusEffect = statusEffect.Serialize();
            aoe.m_hitOwner = false;

            var go = GameObject.Instantiate(gameObject, targetLocation, Quaternion.identity);
            go.GetComponent<Aoe>().Setup(baseAttack.GetCharacter(), Vector3.zero, 0, null, null);
        }


        public class SunderAoe : Aoe
        {
            public SunderAoe() : base()
            {
                m_useAttackSettings = false;
                m_dodgeable = true;
                m_blockable = false;
                m_statusEffect = "SE_Sunder";
                m_hitOwner = false;
                m_hitSame = false;
                m_hitFriendly = false;
                m_hitEnemy = true;
                m_skill = Skills.SkillType.None;
                m_hitInterval = -1;
                m_ttl = 2;
                m_radius = 3;
            }
        };

        public class SE_Sunder : RuneStatusEffect
        {
            public bool modifyElemental = false;
            public DamageModifier targetDamageModifier = DamageModifier.Normal;
            public SE_Sunder() : base()
            {
                name = "SE_Sunder";
                m_name = "Sunder";
                m_tooltip = "Damage resistances reduced";
                m_startMessage = "Damage resistances reduced";
                m_time = 0;
                m_ttl = baseDuration;
                m_icon = (from Sprite s in Resources.FindObjectsOfTypeAll<Sprite>() where s.name == "jackoturnip" select s).FirstOrDefault();

                var vfxPrefab = DebuffVfx.ConstructStatusVfx();
                m_startEffects.m_effectPrefabs = new EffectList.EffectData[] { new EffectList.EffectData { m_prefab = vfxPrefab, m_enabled = true, m_attach = true, m_scale = true } };
            }

            public override void ModifyDamageMods(ref DamageModifiers modifiers)
            {
                List<DamageModifier> doNotModify = new List<DamageModifier> { DamageModifier.Ignore, DamageModifier.Immune, DamageModifier.VeryWeak };
                if (targetDamageModifier == DamageModifier.Normal)
                    doNotModify.Add(DamageModifier.Weak);

                if (!modifyElemental)
                {
                    if (!doNotModify.Contains(modifiers.m_blunt))
                        modifiers.m_blunt = targetDamageModifier;
                    if (!doNotModify.Contains(modifiers.m_slash))
                        modifiers.m_slash = targetDamageModifier;
                    if (!doNotModify.Contains(modifiers.m_pierce))
                        modifiers.m_pierce = targetDamageModifier;
                }
                else
                {
                    if (!doNotModify.Contains(modifiers.m_fire))
                        modifiers.m_fire = targetDamageModifier;
                    if (!doNotModify.Contains(modifiers.m_frost))
                        modifiers.m_frost = targetDamageModifier;
                    if (!doNotModify.Contains(modifiers.m_lightning))
                        modifiers.m_lightning = targetDamageModifier;
                    if (!doNotModify.Contains(modifiers.m_spirit))
                        modifiers.m_spirit = targetDamageModifier;
                    if (!doNotModify.Contains(modifiers.m_poison))
                        modifiers.m_poison = targetDamageModifier;
                }
            }
        }
    }
}
