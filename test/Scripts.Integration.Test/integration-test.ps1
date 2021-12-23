param($path)

.\test/Scripts.Integration.Test/integration-create-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-build-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-run-test.ps1
.\test/Scripts.Integration.Test/integration-add-sentry.ps1 "$path"
.\test/Scripts.Integration.Test/integration-build-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-run-test.ps1
.\test/Scripts.Integration.Test/integration-update-sentry.ps1 "$path"
.\test/Scripts.Integration.Test/integration-build-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-run-test.ps1 "$path"
