using EngineRoom.Runtime.Lifecycle;
using EngineRoom.Runtime.Singleton;
using UnityEngine;

namespace EngineRoom.Demo.Lifecycle
{
    [Singleton]
    public partial class LifecycleSingletonProbe : MonoBehaviour
    {
        public int UserAwakeCount { get; private set; }

        public int OnAwakeCount { get; private set; }

        [Awake]
        private void UserAwake()
        {
            UserAwakeCount++;
        }

        partial void OnAwake()
        {
            OnAwakeCount++;
        }
    }
}
