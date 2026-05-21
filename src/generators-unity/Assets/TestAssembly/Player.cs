using System;
using UnityEngine;

namespace TestAssembly
{
    public class Player : MonoBehaviour
    {
        private void Start()
        {
            IGameManager.Instance.StartGame();
        }
    }
}