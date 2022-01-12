param($path)

.\test/Scripts.Integration.Test/integration-create-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-build-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-update-sentry.ps1 "$path"
.\test/Scripts.Integration.Test/integration-build-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-run-smoke-test.ps1
