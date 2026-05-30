using System.Collections.Generic;
using EngineRoom.Runtime.Lifecycle;
using UnityEngine;

namespace EngineRoom.Demo.Lifecycle
{
    public partial class LifecycleProbe : MonoBehaviour
    {
        public List<string> CallOrder { get; } = new List<string>();

        [Awake]
        private void RecordAwake()
        {
            CallOrder.Add("Awake");
        }

        [Start]
        private void RecordStart()
        {
            CallOrder.Add("Start");
        }

        [OnEnable]
        private void RecordOnEnable()
        {
            CallOrder.Add("OnEnable");
        }

        [OnDisable]
        private void RecordOnDisable()
        {
            CallOrder.Add("OnDisable");
        }

        [Update]
        private void RecordUpdate()
        {
            CallOrder.Add("Update");
        }

        [FixedUpdate]
        private void RecordFixedUpdate()
        {
            CallOrder.Add("FixedUpdate");
        }

        [LateUpdate]
        private void RecordLateUpdate()
        {
            CallOrder.Add("LateUpdate");
        }

        [OnDestroy]
        private void RecordOnDestroy()
        {
            CallOrder.Add("OnDestroy");
        }
    }
}
