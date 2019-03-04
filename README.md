## Using Sentry in Unity

### Usage

In order to make Sentry work, you need to add `SentrySdk` component to any
`GameObject` that is in the first loaded scene of the game.

You can also add it programatically. There can only be one `SentrySdk`
in your whole project. To add it programatically do:

```C#
var sentry = myGameObject.AddComponent(typeof(SentrySdk)) as SentrySdk;
sentry.dsn = "__YOUR_DSN__";
```

The SDK needs to know which project within Sentry your errors should go to. That's defined via the DSN.
DSN is the only obligatory parameter on `SentrySdk` object.

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
  SentrySdk.AddBreadcrumb(string)
  ```

  will collect a breadcrumb.

* sending messages

  ```C#
  SentrySdk.CaptureMessage(string)
  ```

  would send a message to Sentry.

### Unity version

The lowest required version is Unity 5.6.
Previous versions might work but were not tested and will not be supported.

## Resources
* [![Gitter chat](https://img.shields.io/gitter/room/getsentry/unity.svg)](https://gitter.im/getsentry/unity)
* [![Stack Overflow](https://img.shields.io/badge/stack%20overflow-sentry-green.svg)](http://stackoverflow.com/questions/tagged/sentry)
* [![Forum](https://img.shields.io/badge/forum-sentry-green.svg)](https://forum.sentry.io/c/sdks)
* Follow [@getsentry](https://twitter.com/getsentry) on Twitter for updates
