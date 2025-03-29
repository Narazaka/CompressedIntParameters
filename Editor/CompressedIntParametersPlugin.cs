using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

[assembly: ExportsPlugin(typeof(Narazaka.VRChat.CompressedIntParameters.Editor.CompressedIntParametersPlugin))]

namespace Narazaka.VRChat.CompressedIntParameters.Editor
{
    class CompressedIntParametersPlugin : Plugin<CompressedIntParametersPlugin>
    {
        public override string DisplayName => "Compressed Int Parameters";
        public override string QualifiedName => "net.narazaka.vrchat.compressed-int-parameters";

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating).BeforePlugin("nadena.dev.modular_avatar").AfterPlugin("net.narazaka.vrchat.avatar-menu-creater-for-ma").Run("Compressed Int Parameters", Pass);
        }

        void Pass(BuildContext ctx)
        {
            foreach (var ciParameters in ctx.AvatarRootObject.GetComponentsInChildren<CompressedIntParameters>())
            {
                var gameObject = ciParameters.gameObject;
                var maParameters = gameObject.GetComponent<ModularAvatarParameters>();
                if (maParameters == null) maParameters = gameObject.AddComponent<ModularAvatarParameters>();

                foreach (var p in ciParameters.parameters)
                {
                    maParameters.parameters.AddRange(p.ToParameterConfigs());
                }
                
                var maMergeAnimator = gameObject.AddComponent<ModularAvatarMergeAnimator>();
                maMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                maMergeAnimator.matchAvatarWriteDefaults = true;
                maMergeAnimator.animator = GenerateAnimator(ciParameters);
                Object.DestroyImmediate(ciParameters);
            }
        }

        AnimatorController GenerateAnimator(CompressedIntParameters ciParameters)
        {
            var layers = ciParameters.parameters.Where(p => p.maxValue > 0).SelectMany(p => new[] { MakeLocalLayer(p), MakeRemoteLayer(p) });
            var animatorController = new AnimatorController();
            animatorController.name = ciParameters.name + " Compressed Int Parameters";
            animatorController.layers = layers.ToArray();
            animatorController.parameters = ciParameters.parameters.SelectMany(p => p.ToParameterConfigs()).Select(p => new AnimatorControllerParameter
            {
                name = p.nameOrPrefix,
                type = p.syncType == ParameterSyncType.Bool ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Int,
                defaultBool = p.syncType == ParameterSyncType.Bool && p.defaultValue != 0,
                defaultInt = Mathf.RoundToInt(p.defaultValue),
            }).Concat(new AnimatorControllerParameter[]
            {
                new AnimatorControllerParameter
                {
                    name = "IsLocal",
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = false,
                },
            }).ToArray();
            return animatorController;
        }

        AnimatorControllerLayer MakeLocalLayer(CompressedParameterConfig p)
        {
            var bits = p.bits;
            var states = Enumerable.Range(0, p.maxValue + 1).Select(value =>
            {
                var state = new AnimatorState
                {
                    name = value.ToString(),
                    motion = EmptyClip,
                    hideFlags = HideFlags.HideInHierarchy,
                    writeDefaultValues = false,
                    behaviours = new StateMachineBehaviour[]
                    {
                        new VRCAvatarParameterDriver
                        {
                            localOnly = true,
                            parameters = Enumerable.Range(0, bits).Select(bit => new VRC_AvatarParameterDriver.Parameter
                            {
                                type = VRC_AvatarParameterDriver.ChangeType.Set,
                                name = p.BitName(bit),
                                value = CompressedParameterConfig.IntBit(value, bit),
                            }).ToList(),
                        },
                    },
                    transitions = new AnimatorStateTransition[]
                    {
                        new AnimatorStateTransition
                        {
                            destinationState = null,
                            hasExitTime = false,
                            hasFixedDuration = true,
                            exitTime = 0,
                            duration = 0,
                            isExit = true,
                            conditions = new AnimatorCondition[]
                            {
                                new AnimatorCondition
                                {
                                    mode = AnimatorConditionMode.NotEqual,
                                    parameter = p.name,
                                    threshold = value,
                                },
                            },
                        },
                    }
                };
                return new ChildAnimatorState
                {
                    state = state,
                    position = new Vector3(0, value * 100),
                };
            }).Concat(new ChildAnimatorState[]
            {
                new ChildAnimatorState
                {
                    state = new AnimatorState
                    {
                        name = "Start",
                        motion = EmptyClip,
                        hideFlags = HideFlags.HideInHierarchy,
                        writeDefaultValues = false,
                        transitions = new AnimatorStateTransition[]
                        {
                            new AnimatorStateTransition
                            {
                                destinationState = null,
                                hasExitTime = false,
                                hasFixedDuration = true,
                                exitTime = 0,
                                duration = 0,
                                isExit = true,
                                conditions = new AnimatorCondition[]
                                {
                                    new AnimatorCondition
                                    {
                                        mode = AnimatorConditionMode.If,
                                        parameter = "IsLocal",
                                        threshold = 1,
                                    },
                                },
                            }
                        },
                    },
                    position = new Vector3(0, -100),
                }
            }).ToArray();
            var name = p.name + " Compressed Int Parameters [Local]";
            var stateMachine = new AnimatorStateMachine
            {
                name = name,
                hideFlags = HideFlags.HideInHierarchy,
                anyStatePosition = new Vector3(0, -100),
                entryPosition = new Vector3(-300, 0),
                exitPosition = new Vector3(300, 0),
                states = states,
                defaultState = states[states.Length - 1].state,
                entryTransitions = Enumerable.Range(0, p.maxValue + 1).Select(value => new AnimatorTransition
                {
                    destinationState = states[value].state,
                    hideFlags = HideFlags.HideInHierarchy,
                    conditions = new AnimatorCondition[]
                    {
                        new AnimatorCondition
                        {
                            mode = AnimatorConditionMode.Equals,
                            parameter = p.name,
                            threshold = value,
                        },
                    },
                }).ToArray(),
            };
            var layer = new AnimatorControllerLayer
            {
                name = name,
                defaultWeight = 1,
                stateMachine = stateMachine,
            };
            return layer;
        }

        AnimatorControllerLayer MakeRemoteLayer(CompressedParameterConfig p)
        {
            var bits = p.bits;
            var states = Enumerable.Range(0, p.maxValue + 1).Select(value =>
            {
                var state = new AnimatorState
                {
                    name = value.ToString(),
                    motion = EmptyClip,
                    hideFlags = HideFlags.HideInHierarchy,
                    writeDefaultValues = false,
                    behaviours = new StateMachineBehaviour[]
                    {
                        new VRCAvatarParameterDriver
                        {
                            parameters = new List<VRC_AvatarParameterDriver.Parameter>
                            {
                                new VRC_AvatarParameterDriver.Parameter
                                {
                                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                                    name = p.name,
                                    value = value,
                                },
                            },
                        },
                    },
                    transitions = Enumerable.Range(0, bits).Select(bit => new AnimatorStateTransition
                    {
                        destinationState = null,
                        hasExitTime = false,
                        hasFixedDuration = true,
                        exitTime = 0,
                        duration = 0,
                        isExit = true,
                        conditions = new AnimatorCondition[]
                        {
                            new AnimatorCondition
                            {
                                mode = CompressedParameterConfig.IntBitBool(value, bit) ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If,
                                parameter = p.BitName(bit),
                                threshold = 1,
                            },
                        },
                    }).ToArray(),
                };
                return new ChildAnimatorState
                {
                    state = state,
                    position = new Vector3(0, value * 100),
                };
            }).Concat(new ChildAnimatorState[]
            {
                new ChildAnimatorState
                {
                    state = new AnimatorState
                    {
                        name = "Start",
                        motion = EmptyClip,
                        hideFlags = HideFlags.HideInHierarchy,
                        writeDefaultValues = false,
                        transitions = new AnimatorStateTransition[]
                        {
                            new AnimatorStateTransition
                            {
                                destinationState = null,
                                hasExitTime = false,
                                hasFixedDuration = true,
                                exitTime = 0,
                                duration = 0,
                                isExit = true,
                                conditions = new AnimatorCondition[]
                                {
                                    new AnimatorCondition
                                    {
                                        mode = AnimatorConditionMode.IfNot,
                                        parameter = "IsLocal",
                                        threshold = 1,
                                    },
                                },
                            }
                        },
                    },
                    position = new Vector3(0, -100),
                },
            }).ToArray();
            var name = p.name + " Compressed Int Parameters [Remote]";
            var stateMachine = new AnimatorStateMachine
            {
                name = name,
                hideFlags = HideFlags.HideInHierarchy,
                anyStatePosition = new Vector3(0, -100),
                entryPosition = new Vector3(-300, 0),
                exitPosition = new Vector3(300, 0),
                states = states,
                defaultState = states[states.Length - 1].state,
                entryTransitions = Enumerable.Range(0, p.maxValue + 1).Select(value => new AnimatorTransition
                {
                    destinationState = states[value].state,
                    hideFlags = HideFlags.HideInHierarchy,
                    conditions = Enumerable.Range(0, bits).Select(bit => new AnimatorCondition
                    {
                        mode = CompressedParameterConfig.IntBitBool(value, bit) ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                        parameter = p.BitName(bit),
                        threshold = 0,
                    }).ToArray(),
                }).ToArray(),
            };
            var layer = new AnimatorControllerLayer
            {
                name = name,
                defaultWeight = 1,
                stateMachine = stateMachine,
            };
            return layer;
        }

        AnimationClip _emptyClip = null;

        AnimationClip EmptyClip
        {
            get
            {
                if (_emptyClip != null) return _emptyClip;

                var clip = new AnimationClip();
                clip.name = "Compressed Int Parameters Empty";
                clip.hideFlags = HideFlags.HideInHierarchy;
                clip.SetCurve("__Compressed Int Parameters Empty__", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0, 1 / 60f, 0));
                return _emptyClip = clip;
            }
        }
    }
}
