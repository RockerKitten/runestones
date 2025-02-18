﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Runestones.RuneEffects
{
    public class FearRuneEffect : RuneEffect
    {
        public const string vfxName = "vfx_greydwarf_shaman_pray";
        public const float baseRange = 5;
        public const float baseAngle = 25;
        public const float baseDuration = 20;
        public const float baseSpeedMod = 1.5f;
        public const float darkSpeedMod = 0.75f;
        private float duration = baseDuration;
        private float speedModifier = baseSpeedMod;
        private float maxHealth = 25;
        public FearRuneEffect()
        {
            _FlavorText = "This is a mandatory tactical retreat";
            _EffectText = new List<string> { "Forces enemies with less health than you to flee", "+50% move speed for fleeing enemies", "Cone: 5m, 25 degrees" };
            _QualityEffectText[RuneQuality.Ancient] = new List<string> { "+100% Duration", "+100% Spread angle" };
            _QualityEffectText[RuneQuality.Dark] = new List<string> { "Fleeing enemies get -25% move speed instead of +50%" };
            _RelativeStats = new Dictionary<string, Func<string>> { { "Duration", () => $"{baseDuration * _Effectiveness * (_Quality==RuneQuality.Ancient ? 2 : 1):F1} sec" } };
        }
        public override void DoMagicAttack(Attack baseAttack)
        {
            var castDir = baseAttack.BetterAttackDir();
            duration = baseDuration * _Effectiveness * (_Quality == RuneQuality.Ancient ? 2 : 1);
            speedModifier = _Quality == RuneQuality.Dark ? darkSpeedMod : baseSpeedMod;
            maxHealth = baseAttack.GetCharacter().GetHealth();

            var vfx = (from GameObject prefab in Resources.FindObjectsOfTypeAll<GameObject>() where prefab.name == vfxName select prefab).FirstOrDefault();
            var instanced = GameObject.Instantiate(vfx, baseAttack.GetCharacter().GetCenterPoint(), Quaternion.LookRotation(castDir));
            var actualVfx = instanced.transform.Find("flames_world");
            actualVfx.transform.localPosition = Vector3.zero;
            var particles = actualVfx.GetComponent<ParticleSystem>();
            var shapeSettings = particles.shape;
            shapeSettings.angle = baseAngle * (_Quality == RuneQuality.Ancient ? 2 : 1);

            var project = new ConeVolumeProjectile
            {
                m_range = baseRange,
                m_actionOnHitCollider = ApplyFear,
                m_attackSpread = baseAngle * (_Quality == RuneQuality.Ancient ? 2 : 1)
            };
            project.Cast(baseAttack.GetAttackOrigin(), castDir);
        }

        public void ApplyFear(Collider collider)
        {
            var destructible = collider.gameObject.GetComponent<IDestructible>();
            if (destructible is Character character)
            {
                if (character.GetHealth() < maxHealth && !character.IsBoss())
                {
                    var statusEffect = (SE_Fear)character.GetSEMan().AddStatusEffect("SE_Fear", true);
                    statusEffect.m_ttl = duration;
                    statusEffect.speedModifier = speedModifier;
                    statusEffect.maxHealth = maxHealth;
                }
            }
        }

        public class SE_Fear : StatusEffect
        {
            public float speedModifier = 1.5f;
            public float maxHealth = 25;

            public SE_Fear() : base()
            {
                name = "SE_Fear";
                m_name = "Fear";
                m_tooltip = "Fleeing";
                m_startMessage = "Fear overpowers you";
                m_time = 0;
                m_ttl = baseDuration;
                m_icon = (from Sprite s in Resources.FindObjectsOfTypeAll<Sprite>() where s.name == "CorpseRun" select s).FirstOrDefault();

                var vfxPrefab = DebuffVfx.ConstructStatusVfx();
                m_startEffects.m_effectPrefabs = new EffectList.EffectData[] { new EffectList.EffectData { m_prefab = vfxPrefab, m_enabled = true, m_attach = true, m_scale = true } };
            }

            public override void UpdateStatusEffect(float dt)
            {
                base.UpdateStatusEffect(dt);
                if (m_character.GetHealth() >= maxHealth || m_character.IsBoss())
                    m_character.GetSEMan().RemoveStatusEffect(this);
            }

            override public void ModifySpeed(ref float speed)
            {
                speed *= speedModifier;
            }
        }

        [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
        public static class MonsterFearMod
        {
            public static bool failed = false;
            public static bool Prefix(MonsterAI __instance, Character ___m_character, Character ___m_targetCreature, float dt)
            {
                if (___m_character.GetSEMan().HaveStatusEffect("SE_Fear"))
                {
                    __instance.UpdateTakeoffLanding(dt);
                    typeof(BaseAI).GetMethod("UpdateRegeneration", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Invoke(__instance, new object[] { dt });
                    Vector3 fleeFrom = ___m_targetCreature?.transform?.position ?? ___m_character.transform.position;
                    var methodinfo = typeof(BaseAI).GetMethod("Flee", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    methodinfo.Invoke(__instance, new object[] { dt, fleeFrom });
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AnimalAI), "UpdateAI")]
        public static class AnimalFearMod
        {
            public static bool Prefix(AnimalAI __instance, Character ___m_character, Character ___m_target, float dt)
            {
                if (___m_character.GetSEMan().HaveStatusEffect("SE_Fear"))
                {
                    __instance.UpdateTakeoffLanding(dt);
                    typeof(BaseAI).GetMethod("UpdateRegeneration", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Invoke(__instance, new object[] { dt });
                    Vector3 fleeFrom = ___m_target?.transform?.position ?? ___m_character.transform.position;
                    var methodinfo = typeof(BaseAI).GetMethod("Flee", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    methodinfo.Invoke(__instance, new object[] { dt, fleeFrom });
                    return false;
                }
                return true;
            }
        }
    }
}
