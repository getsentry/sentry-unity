param($path)
$path = "C:\2019.4.33f1\Editor"
.\test/Scripts.Integration.Test/integration-create-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-build-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-update-sentry.ps1 "$path"
.\test/Scripts.Integration.Test/integration-build-project.ps1 "$path"
.\test/Scripts.Integration.Test/integration-run-test.ps1
