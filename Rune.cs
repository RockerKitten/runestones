﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Runestones
{
    class Rune
    {
        public string Name;
        public string Desc
        {
            get
            {
                return Effect.GetDescription();
            }
        }
        public int AssetIndex;
        public Type EffectClass;
        public RuneEffect _FixedEffect = null;
        public RuneEffect Effect
        {
            get
            {
                RuneEffect result;
                if (_FixedEffect != null)
                    result = _FixedEffect;
                else
                    result = (RuneEffect)EffectClass.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
                result._Quality = Quality;
                return result;
            }
        }
        public List<Dictionary<string, int>> Reagents;
        public Dictionary<string, int> SimpleRecipe
        {
            get
            {
                return RuneDB.Instance.RuneBases[(int)Quality].Concat(Reagents[(int)Quality])
                    .GroupBy(item => item.Key)
                    .Select(group => new KeyValuePair<string, int>(group.Key, group.Sum(item => item.Value)))
                    .ToDictionary(item => item.Key, item => item.Value);
            }
        }
        public RuneQuality Quality = RuneQuality.Common;
        public GameObject prefab;
        public Recipe Recipe;
        public string DiscoveryToken;

        public Rune Clone()
        {
            return new Rune
            {
                Name = Name,
                AssetIndex = AssetIndex,
                EffectClass = EffectClass,
                _FixedEffect = _FixedEffect,
                Reagents = Reagents,
                Quality = Quality,
                prefab = prefab,
                Recipe = Recipe,
                DiscoveryToken = DiscoveryToken
            };
        }

        public string GetName()
        {
            return $"{(Quality==RuneQuality.Common ? "" : Quality.ToString())} \"{Name}\" Rune".Trim();
        }

        public string GetToken()
        {
            return $"${(Quality == RuneQuality.Common ? "" : Quality.ToString())}{Name}Rune";
        }
    }
}
