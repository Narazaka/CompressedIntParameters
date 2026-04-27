using nadena.dev.modular_avatar.core;
using NUnit.Framework;
using System.Linq;

namespace Narazaka.VRChat.CompressedIntParameters.Tests
{
    public class CompressedParameterConfigTests
    {
        [Test]
        public void Bits_ReturnsCorrectBitCount()
        {
            Assert.AreEqual(0, CompressedParameterConfig.Bits(0));
            Assert.AreEqual(1, CompressedParameterConfig.Bits(1));
            Assert.AreEqual(2, CompressedParameterConfig.Bits(2));
            Assert.AreEqual(2, CompressedParameterConfig.Bits(3));
            Assert.AreEqual(3, CompressedParameterConfig.Bits(4));
            Assert.AreEqual(7, CompressedParameterConfig.Bits(127));
        }

        [Test]
        public void ToParameterConfigs_Int_GeneratesBitParamsAndOriginal()
        {
            var c = new CompressedParameterConfig
            {
                name = "Foo",
                maxValue = 5, // bits = 3
                defaultValue = 3,
                saved = true,
            };
            var result = c.ToParameterConfigs().ToArray();
            Assert.AreEqual(4, result.Length); // 3 bits + 1 original
            Assert.AreEqual("Foo.bit.0", result[0].nameOrPrefix);
            Assert.AreEqual(ParameterSyncType.Bool, result[0].syncType);
            Assert.AreEqual(1f, result[0].defaultValue); // bit 0 of 3 = 1
            Assert.AreEqual("Foo.bit.1", result[1].nameOrPrefix);
            Assert.AreEqual(1f, result[1].defaultValue); // bit 1 of 3 = 1
            Assert.AreEqual("Foo.bit.2", result[2].nameOrPrefix);
            Assert.AreEqual(0f, result[2].defaultValue); // bit 2 of 3 = 0
            Assert.AreEqual("Foo", result[3].nameOrPrefix);
            Assert.AreEqual(ParameterSyncType.Int, result[3].syncType);
            Assert.IsTrue(result[3].localOnly);
        }

        [Test]
        public void FloatStepCount_Returns2PowBits()
        {
            Assert.AreEqual(2, CompressedParameterConfig.FloatStepCount(1));
            Assert.AreEqual(16, CompressedParameterConfig.FloatStepCount(4));
            Assert.AreEqual(128, CompressedParameterConfig.FloatStepCount(7));
        }

        [Test]
        public void FloatStep_TwoEndsInclusive()
        {
            Assert.AreEqual(2f / 15f, CompressedParameterConfig.FloatStep(4, -1f, 1f), 1e-6f);
            Assert.AreEqual(1f / 3f, CompressedParameterConfig.FloatStep(2, 0f, 1f), 1e-6f);
        }

        [Test]
        public void FloatToIndex_ClampsToRangeAndRounds()
        {
            Assert.AreEqual(0, CompressedParameterConfig.FloatToIndex(-1f, 4, -1f, 1f));
            Assert.AreEqual(15, CompressedParameterConfig.FloatToIndex(1f, 4, -1f, 1f));
            var mid = CompressedParameterConfig.FloatToIndex(0f, 4, -1f, 1f);
            Assert.IsTrue(mid == 7 || mid == 8);
            Assert.AreEqual(0, CompressedParameterConfig.FloatToIndex(-2f, 4, -1f, 1f));
            Assert.AreEqual(15, CompressedParameterConfig.FloatToIndex(2f, 4, -1f, 1f));
        }

        [Test]
        public void IndexToFloat_LinearMap()
        {
            Assert.AreEqual(-1f, CompressedParameterConfig.IndexToFloat(0, 4, -1f, 1f), 1e-6f);
            Assert.AreEqual(1f, CompressedParameterConfig.IndexToFloat(15, 4, -1f, 1f), 1e-6f);
            Assert.AreEqual(-1f + 7f * (2f / 15f), CompressedParameterConfig.IndexToFloat(7, 4, -1f, 1f), 1e-6f);
        }

        [Test]
        public void ToParameterConfigs_Float_GeneratesBitParamsAndFloat()
        {
            var c = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "Smile",
                bits = 4,
                floatMinValue = -1f,
                floatMaxValue = 1f,
                defaultValue = 0f,
                saved = true,
            };
            var result = c.ToParameterConfigs().ToArray();
            Assert.AreEqual(5, result.Length);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual($"Smile.bit.{i}", result[i].nameOrPrefix);
                Assert.AreEqual(ParameterSyncType.Bool, result[i].syncType);
                Assert.IsFalse(result[i].localOnly);
            }
            Assert.AreEqual("Smile", result[4].nameOrPrefix);
            Assert.AreEqual(ParameterSyncType.Float, result[4].syncType);
            Assert.IsTrue(result[4].localOnly);
            Assert.AreEqual(0f, result[4].defaultValue);
        }

        [Test]
        public void ToParameterConfigs_Float_DefaultValueBitsMatchQuantization()
        {
            // bits=2, min=0, max=1, defaultValue=1.0 -> index = 3 -> bits = 11
            var c = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "X",
                bits = 2,
                floatMinValue = 0f,
                floatMaxValue = 1f,
                defaultValue = 1f,
            };
            var result = c.ToParameterConfigs().ToArray();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1f, result[0].defaultValue);
            Assert.AreEqual(1f, result[1].defaultValue);
            Assert.AreEqual(1f, result[2].defaultValue);
        }

        [Test]
        public void From_Int_ValidParameterConfig_ReturnsCompressedConfig()
        {
            var src = new ParameterConfig
            {
                nameOrPrefix = "Bar",
                syncType = ParameterSyncType.Int,
                defaultValue = 7,
                saved = true,
                localOnly = false,
                isPrefix = false,
            };
            var c = CompressedParameterConfig.From(src, 10);
            Assert.AreEqual("Bar", c.name);
            Assert.AreEqual(10, c.maxValue);
            Assert.AreEqual(7, c.defaultValue);
            Assert.IsTrue(c.saved);
        }

        [Test]
        public void From_Float_ValidParameterConfig_ReturnsCompressedConfig()
        {
            var src = new ParameterConfig
            {
                nameOrPrefix = "Pitch",
                syncType = ParameterSyncType.Float,
                defaultValue = 0.5f,
                saved = false,
                localOnly = false,
                isPrefix = false,
            };
            var c = CompressedParameterConfig.From(src, 4, -1f, 1f);
            Assert.AreEqual(CompressedParameterType.Float, c.type);
            Assert.AreEqual("Pitch", c.name);
            Assert.AreEqual(4, c.bits);
            Assert.AreEqual(-1f, c.floatMinValue);
            Assert.AreEqual(1f, c.floatMaxValue);
            Assert.AreEqual(0.5f, c.defaultValue);
        }

        [Test]
        public void From_Float_RejectsIntSyncType()
        {
            var src = new ParameterConfig { syncType = ParameterSyncType.Int };
            Assert.Throws<CompressedParameterConfig.InvalidParameterConfigInputException>(
                () => CompressedParameterConfig.From(src, 4, -1f, 1f));
        }

        [Test]
        public void From_Int_RejectsFloatSyncType()
        {
            var src = new ParameterConfig { syncType = ParameterSyncType.Float };
            Assert.Throws<CompressedParameterConfig.InvalidParameterConfigInputException>(
                () => CompressedParameterConfig.From(src, 5));
        }

        [Test]
        public void ValidateForBuild_Float_DetectsOutOfRange()
        {
            var c = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "X",
                bits = 0,
                floatMinValue = -2f,
                floatMaxValue = 1f,
            };
            var errors = c.ValidateForBuild().ToArray();
            Assert.IsTrue(errors.Any(e => e.Contains("bits")));
            Assert.IsTrue(errors.Any(e => e.Contains("floatMinValue")));
        }

        [Test]
        public void ValidateForBuild_Int_DetectsOutOfRange()
        {
            var c = new CompressedParameterConfig
            {
                type = CompressedParameterType.Int,
                name = "X",
                maxValue = 200,
            };
            var errors = c.ValidateForBuild().ToArray();
            Assert.IsTrue(errors.Any(e => e.Contains("maxValue")));
        }

        [Test]
        public void ValidateForBuild_Valid_NoErrors()
        {
            var c = new CompressedParameterConfig { type = CompressedParameterType.Int, name = "Foo", maxValue = 5 };
            Assert.IsEmpty(c.ValidateForBuild().ToArray());
        }

        [Test]
        public void RawName_AppendsRawSuffix()
        {
            var c = new CompressedParameterConfig { name = "Smile" };
            Assert.AreEqual("Smile.raw", c.RawName);
        }

        [Test]
        public void RawRemapTo_Empty_RemainsEmpty()
        {
            var c = new CompressedParameterConfig { name = "Smile", remapTo = "" };
            Assert.AreEqual("", c.RawRemapTo);
        }

        [Test]
        public void RawRemapTo_NonEmpty_AppendsRawSuffix()
        {
            var c = new CompressedParameterConfig { name = "Smile", remapTo = "Renamed" };
            Assert.AreEqual("Renamed.raw", c.RawRemapTo);
        }

        [Test]
        public void FloatSmoothing_DefaultsFalse()
        {
            var c = new CompressedParameterConfig();
            Assert.IsFalse(c.floatSmoothing);
        }
    }
}
