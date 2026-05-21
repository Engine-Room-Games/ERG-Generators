using EngineRoom;
using UnityEngine;

namespace TestAssembly
{
    [Singleton]
    public partial class GameManager : MonoBehaviour
    {
        public string GameID { get; set; }
        
        [IgnoreSingletonMember] 
        public int IgnoredInt { get; set; }
        
        public void StartGame()
        {
            
        }
        
        partial void AwakeInternal()
        {
            Debug.Log("Awake Internal");
        }
    }
}