using System.Collections.Generic;
using EngineRoom.Runtime.Lifecycle;
using UnityEngine;

namespace EngineRoom.Demo.Lifecycle
{
    public partial class OrderedAwakeProbe : MonoBehaviour
    {
        public List<string> CallOrder { get; } = new List<string>();

        [Awake(10)]
        private void Last()
        {
            CallOrder.Add("Last");
        }

        [Awake]
        private void UnorderedFirstInSource()
        {
            CallOrder.Add("UnorderedFirstInSource");
        }

        [Awake(-5)]
        private void First()
        {
            CallOrder.Add("First");
        }

        [Awake]
        private void UnorderedSecondInSource()
        {
            CallOrder.Add("UnorderedSecondInSource");
        }
    }
}
