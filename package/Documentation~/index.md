# Sentry SDK for Unity

> Use [official documentation](https://docs.sentry.io/platforms/unity/) for more information.

Start using it by opening `Tools` -> `Sentry` in the `Unity` editor.


Minimal configuration:

* `DSN` - Get your [Sentry DSN](https://docs.sentry.io/product/sentry-basics/dsn-explainer/) when you create a new project in Sentry. Or in the project settings.


### Quick example

[Sentry's the documentation](https://docs.sentry.io/platforms/unity) includes **much** more you can do, including performance monitoring, adding tags, and more. But to get you started, here's a simple example using Unity's `Debug.LogError`:

Create `MonoBehaviour` and assign it to any object you have in the scene, press play.

```csharp
using UnityEngine;

public class TestMonoBehaviour : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Log");
        Debug.LogWarning("Warning");
        Debug.LogError("Error");
    }
}
```

Check your Sentry web dashboard.
