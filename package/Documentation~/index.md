# Sentry SDK for Unity

> Use [official documentation](https://docs.sentry.io/platforms/unity/) for more information.

Start using it by opening `Tools` -> `Sentry` from `Unity` editor.

Minimal configuration:

* `DSN` - input your [Sentry DSN](https://docs.sentry.io/product/sentry-basics/dsn-explainer/) value

Create `MonBehaviour` and assign it to any object you have in the scene, press play.

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