using EngineRoom.Runtime.Singleton;
using UnityEngine;

namespace EngineRoom.Demo.Singletons
{
    [Singleton]
    [RequireComponent(typeof(AudioSource))]
    public partial class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioClip _tapClip;
        [SerializeField] private AudioClip _stateChangeClip;

        private AudioSource _audioSource;

        public void PlayTap()
        {
            Play(_tapClip);
        }

        public void PlayStateChange()
        {
            Play(_stateChangeClip);
        }

        partial void OnAwake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
