Remove-Item "package-dev/Runtime/*.dll"
dotnet build
assemblyalias --target-directory "./package-dev/Runtime/" --internalize --prefix "Sentry." --assemblies-to-alias "Microsoft*;System*"