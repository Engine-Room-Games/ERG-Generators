using EngineRoom.Runtime.Singleton;
using TMPro;
using UnityEngine;

namespace EngineRoom.Demo.Singletons
{
    [Singleton]
    public partial class UIManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text _counterText;

        public void SetCount(int count)
        {
            if (_counterText != null)
            {
                _counterText.text = count.ToString("N0");
            }
        }
    }
}
