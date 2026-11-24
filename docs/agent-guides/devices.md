# Devices

`scripts/run-sample.ps1` runs Unity of Bugs samples on an Android phone or emulator, or an iOS Simulator, and
checks in Sentry what each one sent. Use it to see an SDK change work on a device, or to reproduce an issue there.

## Prerequisites

- **Android:** `adb` on `PATH`, and the phone or emulator listed by `adb devices` as `device`. Installing needs `aapt`
  (or `aapt2`) from the Android SDK's build-tools: when neither is on `PATH`, the script adds the newest build-tools of
  the SDK in `ANDROID_HOME`, `ANDROID_SDK_ROOT`, or the SDK `adb` comes from.
- **iOS:** Xcode with an iOS Simulator runtime.
- **Symbol upload:** `SENTRY_AUTH_TOKEN` in the shell that builds, and `pwsh scripts/samples-setup-cli-options.ps1`
  run once. Without the token the build skips the upload, and native frames arrive unsymbolicated. The build writes the
  token into its own `sentry.properties`, which git ignores: under `samples/unity-of-bugs-local/Library/` for the
  Gradle project, and in `Builds/iOS/` for Xcode.
- **Reading from Sentry:** `SENTRY_API_TOKEN` (or `SENTRY_READ_TOKEN`) set to a token with `event:read`, such as a
  personal token. An org auth token, the kind `SENTRY_AUTH_TOKEN` usually holds, can only upload: the script stops
  with a 403 then.

The app is the one the sample's settings name (`applicationIdentifier` in
`samples/unity-of-bugs-local/ProjectSettings/ProjectSettings.asset`), and sends to the DSN in
`samples/unity-of-bugs/Assets/Resources/Sentry/SentryOptions.asset`.

## Workflow

List the samples, with their IDs:

```pwsh
pwsh scripts/run-sample.ps1 -List
```

Build, install and run everything a platform can run, with a report:

```pwsh
pwsh scripts/run-sample.ps1 -Platform Android -Build -All -Report Builds/android.md
pwsh scripts/run-sample.ps1 -Platform iOS -Build -All -Report Builds/ios-simulator.md
```

Run a few samples, or a category, on an app that's already installed:

```pwsh
pwsh scripts/run-sample.ps1 -Platform Android -Sample errors/caught-exception,crashes/native-c
pwsh scripts/run-sample.ps1 -Platform iOS -Device "iPhone 17" -Build -Category context
```

- `-Device` takes an adb serial, or a simulator's name or UDID. Leave it out when only one device is connected (or,
  on iOS, one simulator is booted). A simulator that isn't running is booted for the run and shut down after it.
- `-Build` builds IL2CPP through `scripts/build-sample.ps1`. On Android it installs
  `Builds/Android/unity-of-bugs-local.apk`. On iOS it makes a Simulator build (`build-sample.ps1 -IosSdk Simulator`),
  builds the Xcode project with `xcodebuild` into `Builds/iOS-derived` and installs the app.
- Each sample runs in a launch of its own, with a new run ID: `am start -e sample <id> -e run_id <guid>` on Android,
  `--sample <id> --run_id <guid>` on iOS. The app runs the sample, flushes and quits. A sample that ends the app is
  relaunched once without a sample, which sends the crash. Samples that need a tap are listed as manual and skipped.
- Connecting, installing, launching and reading the device log go through the app-runner modules in
  `modules/app-runner`, as the integration tests do. The Android log is the device's whole logcat during the run and
  its relaunch, crash dumps included; the iOS log is the app's console output.
- Everything a run sends carries its ID: the `sample.run_id` tag on events, the attribute on logs and metrics. The
  script finds the run's events, transactions, logs, metrics or feedback by it, waiting up to `-TimeoutSeconds`.
- A sample is checked against what it promises: that it arrived, that its stack trace is symbolicated (every in-app
  C# frame has a file and a line; native frames have function names, not bare addresses), and the context it adds
  (the user, the tag, the breadcrumb, the `character` context, the attachment). The samples that BeforeSend or
  BeforeSendLog drop pass when nothing arrives.
- The report has a table of the device's samples and the details of each one: link, release, environment, device, OS, user,
  tags, contexts, breadcrumbs and unsymbolicated frames. The device logs go in a `<report>-logs` folder next to it.
- The exit code is 0 only when every sample that ran passed.

## Reproducing an issue

1. Add an entry that does what the issue describes to the category it fits. Each category is a file of its own in
   `samples/unity-of-bugs/Assets/Scripts/Catalogue/`: `ErrorSamples.cs`, `CrashSamples.cs`, `PlatformSamples.cs`,
   `HangSamples.cs`, `ContextSamples.cs`, `TracingSamples.cs`, `LogAndMetricSamples.cs`, `FeedbackSamples.cs`,
   `SceneSamples.cs` and `ThirdPartySamples.cs`, with what several of them share in `SampleHelpers.cs`. Write
   `new SampleEntry("category/name", "Title",` on a line of its own (the script reads the IDs from those lines), then
   the description and the code. A new category is a new `<Category>Samples.cs` whose `Category` is listed in
   `SampleCatalogue.Categories`.
2. When the entry only changes the scope, or its outcome isn't a plain event, add what to look for in
   `Get-SampleExpectation` in `scripts/run-sample.ps1`.
3. Run it: `pwsh scripts/run-sample.ps1 -Platform Android -Build -Sample category/name -Report Builds/repro.md`.
4. Read the report: the link to the event, what it carries, and the device log.

## Emulator or simulator, or a real phone

The script runs on an Android phone or emulator, or an iOS Simulator; the iOS checks that need an iPhone are outside
what it runs. An emulator or simulator is enough for C# and Java exceptions, messages, context, logs, metrics, transactions and
feedback. A real phone is needed for:

- native crashes and their symbolication (the iOS Simulator runs a different architecture, and its crash reporting
  differs);
- out of memory, watchdog terminations and App Hangs, which depend on the OS ending the app. On an iOS Simulator the
  script skips `platform/out-of-memory`, as a simulator has no iOS memory limit, and `hangs/main-thread-sleep` and
  `hangs/native-freeze`, as sentry-cocoa can't capture an App Hang's stack there. On an iPhone, the out-of-memory
  sample arrives as a watchdog termination only with `IosWatchdogTerminationIntegrationEnabled`, which is off by default
  in the SDK: the sample turns it on in `SampleOptionConfiguration.cs`;
- code signing and provisioning.
