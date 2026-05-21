using EngineRoom;
using UnityEngine;

namespace TestAssembly
{
    [Singleton(typeof(ISoundManager))]
    public partial class SoundManager : MonoBehaviour, ISoundManager
    {
        public void PlaySound()
        {
            
        }
    }

    public interface ISoundManager
    {
        void PlaySound();
    }
}