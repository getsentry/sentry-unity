# Unity of Bugs (Local)

Unity 6 development project. Its `Assets` directory is a relative symlink to
`../unity-of-bugs/Assets`, so sample scenes and scripts stay shared with the
Unity 2021 compatibility project.

`Packages` and `ProjectSettings` are local to this project. Unity can upgrade
them without changing `unity-of-bugs`.

Build the SDK before opening this project:

```sh
dotnet build
```

Unity restores the ignored `Library` directory on first open.
