## Using sentry in unity

### Installing

Download [sentry client](https://some-link-here) from unity asset store.

### Usage

In order to make Sentry work, you need to add `SentrySdk` component to any
`GameObject`.

XXX insert video link

There can only be one `SentrySdk` in your whole project. To add it
programatically do:

```C#
var sentry = myGameObject.AddComponent(typeof(SentrySdk)) as SentrySdk;
sentry.dsn = "mydsnstring";
```

[DSN](https://link-to-sentry-DSN) is the only obligatory parameter on SentrySdk
object.

This is enough to capture automatic traceback events from the game.

### Example

There is an example scene in the Sentry asset. It has two components -
`SentrySdk` and `SentryTest`. `SentryTest` is a component that handles
button presses to crash or fail assert.

### API

XXX to be written   
