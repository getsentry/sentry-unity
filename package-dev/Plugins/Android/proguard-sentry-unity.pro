# Unity: keep names on select sentry-java classes & their methods - we use string-based JNI lookup in our integration.
-keep class io.sentry.** { *; }
-dontwarn io.sentry.**
