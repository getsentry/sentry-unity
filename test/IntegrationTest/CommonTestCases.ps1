# Defines a collection of reusable test cases that validate common Sentry event properties
# and behaviors across different test suites for consistent integration testing.
#
# Available parameters:
# - $TestSetup: Object containing test setup parameters
# - $TestType: String indicating the type of test being run (e.g., "crash-capture", "message-capture")
# - $SentryEvent: The Sentry event object retrieved from the REST API containing error/message details
# - $RunResult: Object containing the results of running the test application, including Output and ExitCode

$CommonTestCases = @(
    @{ Name = "Outputs event ID"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $eventId = Get-EventIds -appOutput $RunResult.Output -expectedCount 1
            $eventId | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Captures event in sentry.io"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $SentryEvent | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Has title"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $SentryEvent.title | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Has correct release version"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $SentryEvent.release.version | Should -Be "sentry-unity-test@1.0.0"
        }
    }
    @{ Name = "Has correct dist attribute"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $SentryEvent.dist | Should -Be "test-dist"
        }
    }
    @{ Name = "Has tags"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $SentryEvent.tags | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Has correct integration test tags"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            ($SentryEvent.tags | Where-Object { $_.key -eq "test.suite" }).value | Should -Be "integration"
            ($SentryEvent.tags | Where-Object { $_.key -eq "test.type" }).value | Should -Be $TestType
        }
    }
    @{ Name = "Has correct environment tag"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            ($SentryEvent.tags | Where-Object { $_.key -eq "environment" }).value | Should -Be "integration-test"
        }
    }
    @{ Name = "Contains user information"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)

            if ($TestType -eq "crash-capture") {
                # User context may not survive native crashes on all platforms
                return
            }

            $SentryEvent.user | Should -Not -BeNullOrEmpty
            $SentryEvent.user.username | Should -Be "TestUser"
            $SentryEvent.user.email | Should -Be "user-mail@test.abc"
            $SentryEvent.user.id | Should -Be "12345"
        }
    }
    @{ Name = "Contains breadcrumbs"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)

            if ($TestType -eq "crash-capture") {
                # Breadcrumbs may not survive native crashes
                return
            }

            $SentryEvent.breadcrumbs | Should -Not -BeNullOrEmpty
            $SentryEvent.breadcrumbs.values | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Contains expected breadcrumbs"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)

            if ($TestType -eq "crash-capture") {
                return
            }

            $SentryEvent.breadcrumbs.values | Should -Not -BeNullOrEmpty
            $SentryEvent.breadcrumbs.values | Where-Object { $_.message -eq "Integration test started" } | Should -Not -BeNullOrEmpty
            $SentryEvent.breadcrumbs.values | Where-Object { $_.message -eq "Context configuration finished" } | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Contains SDK information"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $SentryEvent.sdk | Should -Not -BeNullOrEmpty
            $SentryEvent.sdk.name | Should -Not -BeNullOrEmpty
            $SentryEvent.sdk.version | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Contains app context"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)

            if ($TestType -eq "crash-capture") {
                # App context may not be available for native crashes
                return
            }

            $SentryEvent.contexts.app | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Contains device context"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $SentryEvent.contexts.device | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Contains OS context"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)
            $SentryEvent.contexts.os | Should -Not -BeNullOrEmpty
            $SentryEvent.contexts.os.name | Should -Not -BeNullOrEmpty
        }
    }
    @{ Name = "Contains Unity context"; TestBlock = {
            param($TestSetup, $TestType, $SentryEvent, $RunResult)

            if ($TestType -eq "crash-capture") {
                # Unity context may not be synchronized to NDK for native crashes
                return
            }

            $SentryEvent.contexts.unity | Should -Not -BeNullOrEmpty
        }
    }
)
