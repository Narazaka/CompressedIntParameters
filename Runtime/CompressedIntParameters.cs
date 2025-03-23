using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using VRC.SDKBase;

[assembly: InternalsVisibleTo("Narazaka.VRChat.CompressedIntParameters.Editor")]

namespace Narazaka.VRChat.CompressedIntParameters
{
    public class CompressedIntParameters : MonoBehaviour, IEditorOnly
    {
        public List<CompressedParameterConfig> parameters = new List<CompressedParameterConfig>();
    }
}
