using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Narazaka.VRChat.CompressedIntParameters.Editor")]

namespace Narazaka.VRChat.CompressedIntParameters
{
    [Serializable]
    public class CompressedParameterConfig
    {
        internal const float VALUE_EPSILON = 0.000001f;

        public static CompressedParameterConfig From(ParameterConfig parameter, int maxValue)
        {
            var reasons = ValidateParameterConfigInput(parameter).ToArray();
            if (reasons.Any()) throw new InvalidParameterConfigInputException(reasons);

            var compressed = new CompressedParameterConfig
            {
                name = parameter.nameOrPrefix,
                remapTo = parameter.remapTo,
                internalParameter = parameter.internalParameter,
                defaultValue = parameter.defaultValue,
                saved = parameter.saved,
                hasExplicitDefaultValue = parameter.hasExplicitDefaultValue,
                maxValue = maxValue,
            };
            return compressed;
        }

        public static IEnumerable<string> ValidateParameterConfigInput(ParameterConfig parameter)
        {
            if (parameter.syncType != ParameterSyncType.Int) yield return "syncType = Int";
            if (parameter.localOnly) yield return "localOnly = false";
            if (parameter.isPrefix) yield return "isPrefix = false";
        }

        public static CompressedParameterConfig From(ParameterConfig parameter, int bits, float minValue = -1f, float maxValue = 1f)
        {
            var reasons = ValidateFloatParameterConfigInput(parameter).ToArray();
            if (reasons.Any()) throw new InvalidParameterConfigInputException(reasons);

            return new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = parameter.nameOrPrefix,
                remapTo = parameter.remapTo,
                internalParameter = parameter.internalParameter,
                defaultValue = parameter.defaultValue,
                saved = parameter.saved,
                hasExplicitDefaultValue = parameter.hasExplicitDefaultValue,
                bits = bits,
                floatMinValue = minValue,
                floatMaxValue = maxValue,
            };
        }

        public static IEnumerable<string> ValidateFloatParameterConfigInput(ParameterConfig parameter)
        {
            if (parameter.syncType != ParameterSyncType.Float) yield return "syncType = Float";
            if (parameter.localOnly) yield return "localOnly = false";
            if (parameter.isPrefix) yield return "isPrefix = false";
        }

        public IEnumerable<string> ValidateForBuild()
        {
            if (string.IsNullOrEmpty(name)) yield return "name is empty";
            if (type == CompressedParameterType.Float)
            {
                if (bits < 1 || bits > 7) yield return $"bits must be 1..7, got {bits}";
                if (floatMinValue < -1f || floatMinValue > 1f) yield return $"floatMinValue must be in [-1, 1], got {floatMinValue}";
                if (floatMaxValue < -1f || floatMaxValue > 1f) yield return $"floatMaxValue must be in [-1, 1], got {floatMaxValue}";
                if (floatMinValue >= floatMaxValue) yield return $"floatMinValue must be less than floatMaxValue, got [{floatMinValue}, {floatMaxValue}]";
            }
            else
            {
                if (maxValue < 1 || maxValue > 127) yield return $"maxValue must be 1..127, got {maxValue}";
            }
        }

        public class InvalidParameterConfigInputException : Exception
        {
            internal InvalidParameterConfigInputException(IEnumerable<string> reasons) : base($"CompressedParameterConfig can only be created from ParameterConfig with: {string.Join(", ", reasons)}") { }
        }

        // ParameterConfig
        // public ParameterConfig parameter;
        public string name;
        public string BitName(int bit) => $"{name}.bit.{bit}";
        public string remapTo;
        public const string RawSuffix = ".raw";
        public string RawName => name + RawSuffix;
        public string RawRemapTo => string.IsNullOrEmpty(remapTo) ? remapTo : remapTo + RawSuffix;
        public bool internalParameter;

        public float defaultValue;
        public bool saved;

        public bool hasExplicitDefaultValue;

        // type discriminator (default = Int で既存データ互換)
        public CompressedParameterType type;

        // compressed
        public int maxValue = 1;

        // Float 専用 (Phase 1)
        public int bits = 4;
        public float floatMinValue = -1f;
        public float floatMaxValue = 1f;
        // Float 専用 (AAPMA インストール時のみ意味を持つ)
        public bool floatSmoothing;

        public static int Bits(int maxValue) => maxValue == 0 ? 0 : Mathf.CeilToInt(Mathf.Log(maxValue + 1, 2));

        public IEnumerable<ParameterConfig> ToParameterConfigs()
        {
            if (type == CompressedParameterType.Float) return ToFloatParameterConfigs();
            return ToIntParameterConfigs();
        }

        IEnumerable<ParameterConfig> ToIntParameterConfigs()
        {
            if (maxValue == 0) yield break;
            var defaultValueInt = Mathf.RoundToInt(defaultValue);
            var intBits = Bits(maxValue);

            for (int i = 0; i < intBits; i++)
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

        IEnumerable<ParameterConfig> ToFloatParameterConfigs()
        {
            if (bits == 0) yield break;
            var defaultIndex = FloatToIndex(defaultValue, bits, floatMinValue, floatMaxValue);

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
                    defaultValue = IntBit(defaultIndex, i),
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
                syncType = ParameterSyncType.Float,
                localOnly = true,
                defaultValue = defaultValue,
                saved = saved,
                hasExplicitDefaultValue = hasExplicitDefaultValue,
            };
        }

        internal static float IntBit(int value, int bit)
        {
            return IntBitBool(value, bit) ? 1 : 0;
        }

        internal static bool IntBitBool(int value, int bit)
        {
            return (value & (1 << bit)) != 0;
        }

        public static int FloatStepCount(int bits) => 1 << bits;

        public static float FloatStep(int bits, float minValue, float maxValue)
        {
            var n = FloatStepCount(bits);
            return (maxValue - minValue) / (n - 1);
        }

        public static int FloatToIndex(float value, int bits, float minValue, float maxValue)
        {
            var n = FloatStepCount(bits);
            var clamped = Mathf.Clamp(value, minValue, maxValue);
            var t = (clamped - minValue) / (maxValue - minValue);
            return Mathf.Clamp(Mathf.RoundToInt(t * (n - 1)), 0, n - 1);
        }

        public static float IndexToFloat(int index, int bits, float minValue, float maxValue)
        {
            return minValue + index * FloatStep(bits, minValue, maxValue);
        }
    }
}
