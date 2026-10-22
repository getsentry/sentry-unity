# Unity of Bugs (Local)

Unity 6 development project. The entries in its `Assets` directory are relative
symlinks into `../unity-of-bugs/Assets`, so sample scenes and scripts stay shared
with the Unity 2021 compatibility project. `Assets` itself is a real directory
because Unity Hub rejects projects whose `Assets` is a symlink. Add a symlink here
when adding a top-level folder to `unity-of-bugs/Assets`.

`Packages` and `ProjectSettings` are local to this project. Unity can upgrade
them without changing `unity-of-bugs`.

Build the SDK before opening this project:

```sh
dotnet build
```

Unity restores the ignored `Library` directory on first open.

## Build

Build a target through Unity CLI:

```pwsh
pwsh scripts/build-sample.ps1 -Target Android
```

When this project is open in a Pipeline-enabled Unity Editor, the CLI uses that
Editor. Otherwise it starts a batch-mode Editor without opening its UI.

Supported targets are `StandaloneWindows64`, `StandaloneOSX`,
`StandaloneLinux64`, `Android`, `iOS`, and `WebGL`. Output is written under
`Builds/<Target>/`.
