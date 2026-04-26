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
    }
}
