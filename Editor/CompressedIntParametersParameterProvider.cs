using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;

namespace Narazaka.VRChat.CompressedIntParameters.Editor
{
    [ParameterProviderFor(typeof(CompressedIntParameters))]
    class CompressedIntParametersParameterProvider : IParameterProvider
    {
        CompressedIntParameters _component;

        public CompressedIntParametersParameterProvider(CompressedIntParameters component)
        {
            _component = component;
        }

        public IEnumerable<ProvidedParameter> GetSuppliedParameters(BuildContext context = null)
        {
            return _component.parameters
                .SelectMany(p => p.ToParameterConfigs())
                .Select(p => new ProvidedParameter(
                    p.nameOrPrefix,
                    ParameterNamespace.Animator,
                    _component,
                    CompressedIntParametersPlugin.Instance,
                    p.syncType == ParameterSyncType.Bool ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Int
                    )
                {
                    DefaultValue = p.defaultValue,
                    IsHidden = p.internalParameter,
                    IsAnimatorOnly = false,
                    WantSynced = !p.localOnly,
                });
        }

        public void RemapParameters(ref ImmutableDictionary<(ParameterNamespace, string), ParameterMapping> nameMap,
            BuildContext context = null)
        {
            foreach (var p in _component.parameters.SelectMany(pp => pp.ToParameterConfigs()))
            {
                string remapTo = null;
                if (p.internalParameter)
                {
                    remapTo = p.nameOrPrefix + "$" + _component.GetInstanceID();
                }
                else if (string.IsNullOrEmpty(p.remapTo))
                {
                    continue;
                }
                else
                {
                    remapTo = p.remapTo;
                }

                if (nameMap.TryGetValue((ParameterNamespace.Animator, remapTo), out var existingMapping))
                {
                    remapTo = existingMapping.ParameterName;
                }

                nameMap = nameMap.SetItem((ParameterNamespace.Animator, p.nameOrPrefix), new ParameterMapping(remapTo, p.internalParameter));
            }
        }
    }
}
