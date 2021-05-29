﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Runestones.RuneEffects
{
    class FireRuneEffect : RuneEffect
    {
        const string projectileName = "Imp_fireball_projectile";
        const float baseAccuracy = 15;
        const float baseDamage = 30;
        const float baseRadius = 1;
        public override CastingAnimations.CastSpeed speed { get {
                if (_Quality == RuneQuality.Dark)
                    return CastingAnimations.CastSpeed.Super;
                else
                    return CastingAnimations.CastSpeed.Fast;
            } set => base.speed = value; }
        public FireRuneEffect()
        {
            _FlavorText = "The trouble with fire is not how to make it, but how to stop";
            _EffectText = new List<string> { "Launches a fireball" };
            _QualityEffectText[RuneQuality.Ancient] = new List<string> { "+200% Damage" };
            _QualityEffectText[RuneQuality.Dark] = new List<string> { "+999% Damage", "Slow casting speed" };
            _RelativeStats = new Dictionary<string, Func<string>> { { "Fire Damage", () => $"{baseDamage * _Effectiveness * (_Quality==RuneQuality.Dark ? 10.99f : (_Quality==RuneQuality.Ancient ? 3 : 1)):F1}" },
                                                                    { "Accuracy", () => $"{Math.Max(baseAccuracy * (2 - _Effectiveness), 0) :F0} degrees" } };
        }
        public override void DoMagicAttack(Attack baseAttack)
        {
            baseAttack.m_attackType = Attack.AttackType.Projectile;
            var baseProjectilePrefab = (from GameObject prefab in Resources.FindObjectsOfTypeAll<GameObject>() where prefab.name == projectileName select prefab).FirstOrDefault();
            baseAttack.m_attackProjectile = GameObject.Instantiate(baseProjectilePrefab);
            var projectile = baseAttack.m_attackProjectile.GetComponent<Projectile>();
            projectile.m_aoe = baseRadius;
            //vfx scaling not working, damage radius correct
            projectile.m_hitEffects.m_effectPrefabs[0].m_prefab.transform.localScale = new Vector3(projectile.m_aoe * 2, projectile.m_aoe * 2, projectile.m_aoe * 2);
            baseAttack.m_projectileAccuracy = Math.Max(baseAccuracy * (2 - _Effectiveness), 0);
            baseAttack.m_useCharacterFacing = true;
            baseAttack.m_useCharacterFacingYAim = true;
            baseAttack.m_launchAngle = -baseAttack.m_projectileAccuracy/2;
            baseAttack.m_attackHeight = 1.5f;
            baseAttack.m_attackRange = 1;
            baseAttack.GetWeapon().m_shared.m_damages.m_fire = baseDamage * _Effectiveness * (_Quality == RuneQuality.Dark ? 10.99f : (_Quality == RuneQuality.Ancient ? 3 : 1));
            baseAttack.DoProjectileAttack();
            baseAttack.GetWeapon().m_shared.m_damages.m_fire = 0;
        }
    }
}
