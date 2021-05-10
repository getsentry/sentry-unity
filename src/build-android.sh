pushd sentry-java
# :sentry:jar ?
./gradlew :sentry-android-core:assembleRelease :sentry-android-ndk:assembleRelease :sentry:jar
cp sentry-android-ndk/build/outputs/aar/sentry-android-ndk-release.aar ../../package-dev/Plugins/Android 
cp sentry-android-core/build/outputs/aar/sentry-android-core-release.aar ../../package-dev/Plugins/Android 
# building snapshot based on version, i.e: sentry-5.0.0-beta.3-SNAPSHOT.jar
cp sentry/build/libs/sentry*.jar ../../package-dev/Plugins/Android 