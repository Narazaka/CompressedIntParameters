using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narazaka.VRChat.CompressedIntParameters
{
    [Serializable]
    public class CompressedParameterConfig
    {
        internal const float VALUE_EPSILON = 0.000001f;

        // ParameterConfig
        // public ParameterConfig parameter;
        public string name;
        public string BitName(int bit) => $"{name}.bit.{bit}";
        public string remapTo;
        public bool internalParameter;

        public float defaultValue;
        public bool saved;

        public bool hasExplicitDefaultValue;

        // compressed
        public int maxValue = 1;

        public int bits => Bits(maxValue);
        public static int Bits(int maxValue) => maxValue == 0 ? 0 : Mathf.CeilToInt(Mathf.Log(maxValue + 1, 2));

        public IEnumerable<ParameterConfig> ToParameterConfigs()
        {
            if (maxValue == 0) yield break;
            var defaultValueInt = Mathf.RoundToInt(defaultValue);

            for (int i = 0; i < bits; i++)
            {
                yield return new ParameterConfig
                {
                    nameOrPrefix = BitName(i),
                    remapTo = string.IsNullOrEmpty(remapTo) ? remapTo : $"{remapTo}.bit.{i}",
                    internalParameter = internalParameter,
                    isPrefix = false,
                    syncType = ParameterSyncType.Bool,
                    localOnly = false,
                    defaultValue = IntBit(defaultValueInt, i),
                    saved = saved,
                    hasExplicitDefaultValue = hasExplicitDefaultValue,
                };
            }
            yield return new ParameterConfig
            {
                nameOrPrefix = name,
                remapTo = remapTo,
                internalParameter = internalParameter,
                isPrefix = false,
                syncType = ParameterSyncType.Int,
                localOnly = true,
                defaultValue = defaultValue,
                saved = saved,
                hasExplicitDefaultValue = hasExplicitDefaultValue,
            };
        }

        public static float IntBit(int value, int bit)
        {
            return IntBitBool(value, bit) ? 1 : 0;
        }

        public static bool IntBitBool(int value, int bit)
        {
            return (value & (1 << bit)) != 0;
        }
    }
}
