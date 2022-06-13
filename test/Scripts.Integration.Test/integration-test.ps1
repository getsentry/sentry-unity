param($path)

.\test/Scripts.Integration.Test/create-project.ps1 "$path"
.\test/Scripts.Integration.Test/build-project.ps1 "$path"
.\test/Scripts.Integration.Test/update-sentry.ps1 "$path"
.\test/Scripts.Integration.Test/build-project.ps1 "$path"
.\test/Scripts.Integration.Test/run-smoke-test.ps1
