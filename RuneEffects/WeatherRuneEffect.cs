﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Runestones.RuneEffects
{
    class WeatherRuneEffect : RuneEffect
    {
        static CancellationTokenSource _cancellationTokenSource = null;
        static CancellationTokenSource CancellationTokenSource
        {
            get
            {
                if (_cancellationTokenSource == null)
                    _cancellationTokenSource = new CancellationTokenSource();
                return _cancellationTokenSource;
            }
        }

        public const float baseDuration = 180;
        public const float baseWindStrength = 1/3f;
        public const string lightningVfxName = "fx_eikthyr_forwardshockwave";

        public WeatherRuneEffect()
        {
            _FlavorText = "So, you want to harness lightning. Best start with a stiff breeze";
            _EffectText = new List<string> { "Changes wind direction, for a time" };
            _QualityEffectText[RuneQuality.Ancient] = new List<string> { "Stronger wind", "Clear skies" };
            _QualityEffectText[RuneQuality.Dark] = new List<string> { "Full strength wind", "Summons a storm", "Lightning Bolt!!!" };
            _RelativeStats = new Dictionary<string, Func<string>> { { "Duration", () => $"{baseDuration * _Effectiveness :F0} sec" } };
        }

        public override void Precast(Attack baseAttack)
        {
            base.Precast(baseAttack);
            var player = baseAttack.GetCharacter();
            var lookDir = (Quaternion)typeof(Character).GetField("m_lookYaw", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);

            CancellationTokenSource.Cancel();
            EnvMan.instance.ResetDebugWind();
            EnvMan.instance.SetDebugWind(lookDir.eulerAngles.y, baseWindStrength * ((int)_Quality + 1));
            if (_Quality == RuneQuality.Ancient)
            {
                EnvMan.instance.SetForceEnvironment("Clear");
            }
            if (_Quality == RuneQuality.Dark)
            {
                EnvMan.instance.SetForceEnvironment("ThunderStorm");
            }
            Task.Run(() => ResetWind(CancellationTokenSource.Token, (int)(baseDuration * _Effectiveness * 1000)), CancellationTokenSource.Token);
        }

        public override void DoMagicAttack(Attack baseAttack)
        {
            if (_Quality == RuneQuality.Dark)
            {
                var project = new ConeVolumeProjectile
                {
                    m_actionOnHitCollider = DoLightningDamage,
                    m_attackSpread = 25,
                    m_range = 7.5f
                };
                var baseVfx = (from GameObject prefab in Resources.FindObjectsOfTypeAll<GameObject>() where prefab.name == lightningVfxName select prefab).FirstOrDefault();
                var vfx = baseVfx.transform.Find("lightning").gameObject;
                GameObject.Instantiate(vfx, baseAttack.GetAttackOrigin().position, Quaternion.LookRotation(baseAttack.BetterAttackDir()));
                project.Cast(baseAttack.GetAttackOrigin(), baseAttack.BetterAttackDir());
            }
        }

        public static void DoLightningDamage(Collider collider)
        {
            var gameObject = collider.gameObject;
            if(gameObject?.GetComponent<IDestructible>() != null)
            {
                var hitData = new HitData
                {
                    m_damage = new HitData.DamageTypes
                    {
                        m_lightning = 123
                    }
                };
                gameObject.GetComponent<IDestructible>().Damage(hitData);
            }
        }

        public static async void ResetWind(CancellationToken cancellationToken, int delay)
        {
            await Task.Delay(delay, cancellationToken); //3 minutes before the wind resets; 1 stack of these will give 15 min
            if (cancellationToken.IsCancellationRequested)
                return;
            EnvMan.instance.ResetDebugWind();
            EnvMan.instance.SetForceEnvironment("");
        }
    }
}
