using System;
using UnityEngine;

namespace TestAssembly
{
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            GameManager.Create();
            SoundManager.Create();
        }
    }
}