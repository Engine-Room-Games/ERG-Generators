using EngineRoom.Runtime.Singleton;
using UnityEngine;

[Singleton]
public partial class TestSingleton : MonoBehaviour
{
    public string MyString { get; set; }
    [SingletonIgnore] public int MyInt { get; set; }
}
