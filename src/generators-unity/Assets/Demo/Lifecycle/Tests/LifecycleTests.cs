using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EngineRoom.Demo.Lifecycle.Tests
{
    public class LifecycleTests
    {
        [UnityTest]
        public IEnumerator OneShotCallbacks_FireExactlyOnce()
        {
            var probe = CreateActiveGameObject<LifecycleProbe>(out var gameObject);
            yield return null;

            probe.enabled = false;
            Object.DestroyImmediate(gameObject);

            Assert.AreEqual(1, probe.CallOrder.Count(name => name == "Awake"));
            Assert.AreEqual(1, probe.CallOrder.Count(name => name == "Start"));
            Assert.AreEqual(1, probe.CallOrder.Count(name => name == "OnEnable"));
            Assert.AreEqual(1, probe.CallOrder.Count(name => name == "OnDisable"));
            Assert.AreEqual(1, probe.CallOrder.Count(name => name == "OnDestroy"));
        }

        [UnityTest]
        public IEnumerator Update_FiresEveryFrame()
        {
            var probe = CreateActiveGameObject<LifecycleProbe>(out var gameObject);
            try
            {
                yield return null;
                yield return null;

                Assert.GreaterOrEqual(probe.CallOrder.Count(name => name == "Update"), 2);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator LateUpdate_FiresEveryFrame()
        {
            var probe = CreateActiveGameObject<LifecycleProbe>(out var gameObject);
            try
            {
                yield return null;
                yield return null;

                Assert.GreaterOrEqual(probe.CallOrder.Count(name => name == "LateUpdate"), 2);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator FixedUpdate_FiresOnPhysicsStep()
        {
            var probe = CreateActiveGameObject<LifecycleProbe>(out var gameObject);
            try
            {
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                Assert.GreaterOrEqual(probe.CallOrder.Count(name => name == "FixedUpdate"), 1);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void OrderedAwake_SortsByExplicitOrderThenSourcePosition()
        {
            var probe = CreateActiveGameObject<OrderedAwakeProbe>(out var gameObject);
            try
            {
                Assert.AreEqual(
                    new[] { "First", "UnorderedFirstInSource", "UnorderedSecondInSource", "Last" },
                    probe.CallOrder.ToArray());
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SingletonAwake_RunsBeforeUserAwakeAndOnAwake()
        {
            var probe = CreateActiveGameObject<LifecycleSingletonProbe>(out var gameObject);
            try
            {
                Assert.AreSame(probe, ILifecycleSingletonProbe.Instance);
                Assert.AreEqual(1, probe.UserAwakeCount);
                Assert.AreEqual(1, probe.OnAwakeCount);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator DuplicateSingleton_AbortsBeforeUserAwakeRuns()
        {
            var original = CreateActiveGameObject<LifecycleSingletonProbe>(out var originalGameObject);
            yield return null;

            var duplicate = CreateActiveGameObject<LifecycleSingletonProbe>(out var duplicateGameObject);
            try
            {
                Assert.AreEqual(0, duplicate.UserAwakeCount);
                Assert.AreEqual(0, duplicate.OnAwakeCount);
                Assert.AreSame(original, ILifecycleSingletonProbe.Instance);
            }
            finally
            {
                if (duplicateGameObject != null)
                {
                    Object.DestroyImmediate(duplicateGameObject);
                }
                if (originalGameObject != null)
                {
                    Object.DestroyImmediate(originalGameObject);
                }
            }
        }

        private static TComponent CreateActiveGameObject<TComponent>(out GameObject gameObject) where TComponent : Component
        {
            gameObject = new GameObject(typeof(TComponent).Name);
            return gameObject.AddComponent<TComponent>();
        }
    }
}
