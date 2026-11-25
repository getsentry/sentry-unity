# Unity of Bugs scripts

The scripts are split by what you're looking for.

- **The configuration files** sit at the top:
  - `SentryOptionConfiguration.cs` configures the SDK in code.
  - `SampleOptionConfiguration.cs` holds the sample's one change on top of the SDK's defaults.
  - `SentryCliConfiguration.cs` configures the debug symbol upload.
- **`Samples/`** shows what the SDK does and how its API looks. It has one file per menu tab, such as `ErrorSamples.cs` for Errors, and `SampleHelpers.cs`, which runs a sample where the "Run on" choice says.
- **`Samples/NativePlugins/`** holds the native code the samples call: the C, C++, Objective-C, Kotlin and JavaScript plugins, and `NativeBridge.cs`, which calls them from C#.
- **`Scaffolding/`** holds the app around the samples: the menu, its UI building blocks, the runner and its status line, the list of tabs, the extra scene the scene samples load, the spinning logo, and the helper that picks the UI input module for the active input system. Its `Editor/` folder sets up the third-party samples' defines, and its `Resources/` folder holds the logo's model and material.
