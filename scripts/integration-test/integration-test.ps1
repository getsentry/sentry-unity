param($path)

. ./scripts/integration-test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths "False" "" $path

.\scripts/integration-test/integration-create-project.ps1
.\scripts/integration-test/integration-build-project.ps1
.\scripts/integration-test/integration-run-test.ps1
.\scripts/integration-test/integration-add-sentry.ps1
.\scripts/integration-test/integration-build-project.ps1
.\scripts/integration-test/integration-run-test.ps1
.\scripts/integration-test/integration-update-sentry.ps1
.\scripts/integration-test/integration-build-project.ps1
.\scripts/integration-test/integration-run-test.ps1
