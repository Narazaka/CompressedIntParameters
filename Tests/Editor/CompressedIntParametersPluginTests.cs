using Narazaka.VRChat.CompressedIntParameters.Editor;
using NUnit.Framework;
using System.Linq;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace Narazaka.VRChat.CompressedIntParameters.Tests
{
    public class CompressedIntParametersPluginTests
    {
        static CompressedIntParametersPlugin Plugin => CompressedIntParametersPlugin.Instance as CompressedIntParametersPlugin;

        [Test]
        public void MakeLocalLayer_Int_StateCountIsMaxValuePlus2()
        {
            // maxValue=5 → states = 6 (0..5) + Start = 7
            var p = new CompressedParameterConfig { name = "Idx", maxValue = 5 };
            var layer = Plugin.MakeLocalLayer(p);
            Assert.AreEqual(7, layer.stateMachine.states.Length);
            Assert.AreEqual("Start", layer.stateMachine.defaultState.name);
        }

        [Test]
        public void MakeLocalLayer_Int_EachValueStateSetsAllBits()
        {
            var p = new CompressedParameterConfig { name = "Idx", maxValue = 3 };
            var layer = Plugin.MakeLocalLayer(p);
            // value=2 (binary 10) state should set bit0=0, bit1=1
            var state2 = layer.stateMachine.states.Single(s => s.state.name == "2").state;
            var driver = (VRCAvatarParameterDriver)state2.behaviours[0];
            Assert.IsTrue(driver.localOnly);
            Assert.AreEqual(2, driver.parameters.Count); // bits=2
            var bit0 = driver.parameters.Single(d => d.name == "Idx.bit.0");
            var bit1 = driver.parameters.Single(d => d.name == "Idx.bit.1");
            Assert.AreEqual(0f, bit0.value);
            Assert.AreEqual(1f, bit1.value);
        }

        [Test]
        public void MakeLocalLayer_Int_StartStateExitsOnIsLocal()
        {
            var p = new CompressedParameterConfig { name = "Idx", maxValue = 1 };
            var layer = Plugin.MakeLocalLayer(p);
            var start = layer.stateMachine.states.Single(s => s.state.name == "Start").state;
            Assert.AreEqual(1, start.transitions.Length);
            var t = start.transitions[0];
            Assert.IsTrue(t.isExit);
            Assert.AreEqual("IsLocal", t.conditions[0].parameter);
            Assert.AreEqual(AnimatorConditionMode.If, t.conditions[0].mode);
        }

        [Test]
        public void MakeRemoteLayer_Int_StateCountIsMaxValuePlus2()
        {
            var p = new CompressedParameterConfig { name = "Idx", maxValue = 5 };
            var layer = Plugin.MakeRemoteLayer(p);
            Assert.AreEqual(7, layer.stateMachine.states.Length);
        }

        [Test]
        public void MakeRemoteLayer_Int_EachValueStateSetsName()
        {
            var p = new CompressedParameterConfig { name = "Idx", maxValue = 3 };
            var layer = Plugin.MakeRemoteLayer(p);
            var state2 = layer.stateMachine.states.Single(s => s.state.name == "2").state;
            var driver = (VRCAvatarParameterDriver)state2.behaviours[0];
            Assert.AreEqual(1, driver.parameters.Count);
            Assert.AreEqual("Idx", driver.parameters[0].name);
            Assert.AreEqual(2f, driver.parameters[0].value);
        }

        [Test]
        public void MakeFloatLocalLayer_StateCountIs2PowBitsPlus1()
        {
            var p = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "Smile",
                bits = 3,
                floatMinValue = -1f,
                floatMaxValue = 1f,
            };
            var layer = Plugin.MakeFloatLocalLayer(p);
            Assert.AreEqual(9, layer.stateMachine.states.Length); // 8 + Start
        }

        [Test]
        public void MakeFloatLocalLayer_EachIndexStateSetsAllBits()
        {
            var p = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "Smile",
                bits = 2,
                floatMinValue = -1f,
                floatMaxValue = 1f,
            };
            var layer = Plugin.MakeFloatLocalLayer(p);
            var state3 = layer.stateMachine.states.Single(s => s.state.name == "3").state;
            var driver = (VRCAvatarParameterDriver)state3.behaviours[0];
            Assert.IsTrue(driver.localOnly);
            Assert.AreEqual(2, driver.parameters.Count);
            Assert.AreEqual(1f, driver.parameters.Single(d => d.name == "Smile.bit.0").value);
            Assert.AreEqual(1f, driver.parameters.Single(d => d.name == "Smile.bit.1").value);
        }

        [Test]
        public void MakeFloatLocalLayer_EntryTransitionsUseFloatThresholdConditions()
        {
            var p = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "Smile",
                bits = 2,
                floatMinValue = -1f,
                floatMaxValue = 1f,
            };
            var layer = Plugin.MakeFloatLocalLayer(p);
            // entry transitions = stepCount = 4
            Assert.AreEqual(4, layer.stateMachine.entryTransitions.Length);
            // 中央 state (index=1) は Greater + Less の両方を持つ
            var middleEntry = layer.stateMachine.entryTransitions[1];
            Assert.AreEqual(2, middleEntry.conditions.Length);
            Assert.IsTrue(middleEntry.conditions.Any(c => c.mode == AnimatorConditionMode.Greater));
            Assert.IsTrue(middleEntry.conditions.Any(c => c.mode == AnimatorConditionMode.Less));
        }

        [Test]
        public void MakeFloatRemoteLayer_StateCountIs2PowBitsPlus1()
        {
            var p = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "Smile",
                bits = 3,
                floatMinValue = -1f,
                floatMaxValue = 1f,
            };
            var layer = Plugin.MakeFloatRemoteLayer(p);
            Assert.AreEqual(9, layer.stateMachine.states.Length);
        }

        [Test]
        public void MakeFloatRemoteLayer_EachIndexStateSetsRestoredFloat()
        {
            var p = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "Smile",
                bits = 2,
                floatMinValue = -1f,
                floatMaxValue = 1f,
            };
            var layer = Plugin.MakeFloatRemoteLayer(p);
            // index=0 → -1, index=3 → 1
            var state0 = layer.stateMachine.states.Single(s => s.state.name == "0").state;
            var driver0 = (VRCAvatarParameterDriver)state0.behaviours[0];
            Assert.AreEqual("Smile", driver0.parameters[0].name);
            Assert.AreEqual(-1f, driver0.parameters[0].value, 1e-6f);

            var state3 = layer.stateMachine.states.Single(s => s.state.name == "3").state;
            var driver3 = (VRCAvatarParameterDriver)state3.behaviours[0];
            Assert.AreEqual(1f, driver3.parameters[0].value, 1e-6f);
        }

        [Test]
        public void MakeFloatRemoteLayer_SmoothingEnabled_DriverWritesToRawName()
        {
            var p = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "Smile",
                bits = 2,
                floatMinValue = -1f,
                floatMaxValue = 1f,
                floatSmoothing = true,
            };
            var layer = Plugin.MakeFloatRemoteLayer(p);
            var state0 = layer.stateMachine.states.Single(s => s.state.name == "0").state;
            var driver0 = (VRCAvatarParameterDriver)state0.behaviours[0];
            Assert.AreEqual("Smile.raw", driver0.parameters[0].name);
            // 別の state も同じ宛先になることを確認（per-state バグの予防）
            var state3 = layer.stateMachine.states.Single(s => s.state.name == "3").state;
            var driver3 = (VRCAvatarParameterDriver)state3.behaviours[0];
            Assert.AreEqual("Smile.raw", driver3.parameters[0].name);
        }

        [Test]
        public void MakeFloatRemoteLayer_SmoothingDisabled_DriverWritesToName()
        {
            var p = new CompressedParameterConfig
            {
                type = CompressedParameterType.Float,
                name = "Smile",
                bits = 2,
                floatMinValue = -1f,
                floatMaxValue = 1f,
                floatSmoothing = false,
            };
            var layer = Plugin.MakeFloatRemoteLayer(p);
            var state0 = layer.stateMachine.states.Single(s => s.state.name == "0").state;
            var driver0 = (VRCAvatarParameterDriver)state0.behaviours[0];
            Assert.AreEqual("Smile", driver0.parameters[0].name);
        }
    }
}
