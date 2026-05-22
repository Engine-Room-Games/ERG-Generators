using EngineRoom.Runtime.Singleton;
using UnityEngine;

namespace EngineRoom.Demo.Singletons
{
    [Singleton]
    public partial class GameManager : MonoBehaviour
    {
        public int Count => _count;
        
        private const string CountKey = "EggCount";

        private int _count;

        public void RegisterTap()
        {
            _count++;
            Save();
            ISoundManager.Instance.PlayTap();
            IUIManager.Instance.SetCount(_count);
        }

        partial void OnAwake()
        {
            _count = PlayerPrefs.GetInt(CountKey, 0);
        }

        private void Start()
        {
            IUIManager.Instance.SetCount(_count);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(CountKey, _count);
            PlayerPrefs.Save();
        }
    }
}
