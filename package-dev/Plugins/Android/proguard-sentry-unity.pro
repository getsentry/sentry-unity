# Unity: keep names on select sentry-java classes & their methods - we use string-based JNI lookup in our integration.
-keep class io.sentry.** { *; }
-keep class io.sentry.android.** { *; }
-dontwarn io.sentry.**
-dontwarn io.sentry.android.**
