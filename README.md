# EngineRoom Generators

A Unity package of Roslyn source generators that take care of the boilerplate around common gameplay patterns. 

## Installation

Available on [OpenUPM](https://openupm.com/):

```
openupm add com.engineroom.generators
```

## Features

### Singletons & Dependencies

> [!WARNING]
> Singletons are bad and I do not recommend using them. With that said, I know I can't change the world — there is a big following of the pattern, and people will reach for it whether I like it or not. I strongly believe that any code should be ready for change. If I can make singletons ready to be swapped out for dependency injection while keeping the ergonomics that make people use them in the first place - I'm taking that chance.
>
> Concretely: every singleton in this package is accessed through a generated interface with a settable `Instance`. Tests (and any future DI container) can swap the implementation by assigning to that property - no service locator, no static cling, no rewriting of consumers.

#### `[Singleton]`

Mark a `partial class : MonoBehaviour` with `[Singleton]` and the generator gives you:

- An `I<ClassName>` interface that mirrors every public method and property of the class — so consumers depend on the interface, not the concrete type.
- An `Awake` that registers the instance, enforces uniqueness, and (by default) calls `DontDestroyOnLoad`.
- A `Create()` factory that spawns a fresh GameObject with the component attached — useful from tests.
- A `partial void OnAwake()` hook for your own initialization.

You can opt into a hand-written interface (`[Singleton(typeof(IMyManager))]`) when you want to expose a curated surface, or use `[SingletonInclude]` / `[SingletonIgnore]` on individual members to control what makes it into the auto-generated interface.

```csharp
using EngineRoom.Runtime.Singleton;
using UnityEngine;

[Singleton]
public partial class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip _tapClip;
    private AudioSource _audioSource;

    public void PlayTap() => _audioSource.PlayOneShot(_tapClip);

    partial void OnAwake()
    {
        _audioSource = GetComponent<AudioSource>();
    }
}
```

The generator emits `ISoundManager` (with `PlayTap`), wires up `Awake`, and exposes `ISoundManager.Instance` for consumers.

#### `[Dependency]`

Mark a private field with `[Dependency]` on any `MonoBehaviour` and the generator wires it up for you:

- Fields are resolved from the corresponding singleton instance in a generated `Start()`.
- A `partial void OnStart()` hook runs right after the assignments, so you can use your dependencies immediately without writing your own `Start`.

The field type must be the singleton's interface (`ISoundManager`, not `SoundManager`), which keeps your consumers swappable.

```csharp
using EngineRoom.Runtime.Singleton;
using UnityEngine;

public partial class Egg : MonoBehaviour
{
    [Dependency] private ISoundManager _soundManager;

    public void Tap()
    {
        _soundManager.PlayTap();
    }
}
```

No manual `ISoundManager.Instance` lookup, no `Start` of your own to write — the generator handles both.

#### Swapping in tests

Because consumers see only the generated interface, mocking is a one-liner:

```csharp
public class MockSoundManager : ISoundManager
{
    public int TapPlayCount { get; private set; }
    public static MockSoundManager Install()
    {
        var mock = new MockSoundManager();
        ISoundManager.Instance = mock;
        return mock;
    }
    public void PlayTap() => TapPlayCount++;
}

[Test]
public void Tapping_plays_the_sound()
{
    var sound = MockSoundManager.Install();
    var egg = new GameObject().AddComponent<Egg>();

    egg.Tap();

    Assert.AreEqual(1, sound.TapPlayCount);
}
```

## Requirements

Unity. Tested on **Unity 6000.4.0f1**, but should work on most versions from **Unity 2022.3** onward.
