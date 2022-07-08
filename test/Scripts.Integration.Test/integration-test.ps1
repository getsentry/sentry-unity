param(
    [string] $UnityPath
)

# .\test/Scripts.Integration.Test/create-project.ps1 "$UnityPath"
# .\test/Scripts.Integration.Test/build-project.ps1 "$UnityPath"
# .\test/Scripts.Integration.Test/update-sentry.ps1 "$UnityPath"
.\test/Scripts.Integration.Test/build-project.ps1 "$UnityPath" -Platform "iOS"

# .\test/Scripts.Integration.Test/run-smoke-test.ps1 -Smoke

.\scripts\smoke-test-ios.ps1 Build -UnityVersion "2019.4.39f1"
.\scripts\smoke-test-ios.ps1 Test "iOS 13.0"