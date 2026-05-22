using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class Tester : MonoBehaviour
    {
        private void Start()
        {
            ITestSingleton.Instance.MyString = "Hello";
        }
    }
}