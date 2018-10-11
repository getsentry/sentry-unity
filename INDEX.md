## Using sentry in unity

### Installing

Download [sentry client](https://some-link-here) from unity asset store.

Alternatively, download [unitypackage](https://link-to-unitypackage) directly
and install it in Unity by going to
`Assets -> Import Package... -> Custom Package`.

### Usage

In order to make Sentry work, you need to add `SentrySdk` component to any
`GameObject` that is in the first loaded scene of the game.

XXX insert video link

You can also add it programatically. There can only be one `SentrySdk`
in your whole project. To add it programatically do:

```C#
var sentry = myGameObject.AddComponent(typeof(SentrySdk)) as SentrySdk;
sentry.dsn = "mydsnstring";
```

[DSN](https://link-to-sentry-DSN) is the only obligatory parameter on SentrySdk
object.

This is enough to capture automatic traceback events from the game. They will
be sent to your DSN and you can find them at [sentry.io](sentry.io)

### Example

There is an example scene in the Sentry asset. It has two components -
`SentrySdk` and `SentryTest`. `SentryTest` is a component that handles
button presses to crash or fail assert. `SentrySdk` is the main component
that you have to use in your own project.

### API

The basic API is automatic collection of test failures, so it should mostly
run headless. There are two important APIs that are worth considering.

* collecting breadcrumbs

  ```C#
  SentrySdk.addBreadcrump(string)
  ```

  will collect a breadcrumb.

* sending messages

  ```C#
  SentrySdk.CaptureMessage(string)
  ```

  would send a message to Sentry.

