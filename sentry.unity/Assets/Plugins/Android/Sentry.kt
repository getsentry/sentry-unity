package io.sentry.unity

import android.content.Context
import android.util.Log

object SentryAndroid {
    @JvmStatic
    fun init(context: Context) {
        Log.i("AndroidPlugin", "Hello from Kotlin!")
    }

    @JvmStatic
    fun testThrow() {
        Log.e("test", "test from Kotlin!")
        throw Exception()
    }
}
