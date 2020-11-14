package io.sentry.unity

import android.content.Context
import android.util.Log
import io.sentry.Sentry;

object SentryAndroidPlugin {//: Plugin {
    @JvmStatic
    fun init(context: Context, dsn: String) {
        Log.d("SentryAndroidPlugin", "Initializing Sentry Android with DSN: ${dsn}")
        try {
            io.sentry.android.core.SentryAndroid.init(context,
                    { o ->
                        o.setDebug(true);
                        o.setDsn(dsn)
                    })
            Log.i("SentryAndroidPlugin", "Sentry Android SDK initialized!")
        } catch (e: java.lang.Exception) {
            Log.e("SentryAndroidPlugin", "Failed initializing the Sentry Android SDK.\n"
                    + e.toString())
            e.printStackTrace()
        }
    }
}

object Buggy {
    @JvmStatic
    fun testThrow() {
        try {
            Log.e("test", "test from Kotlin!")
            throw Exception()
        }
        catch (e: Exception) {
            Sentry.captureException(e)
            throw e
        }
    }
}
