package io.sentry.sample.unity

import com.unity3d.player.UnityPlayer
import android.util.Log
import io.sentry.core.Sentry;

object AndroidPlugin {
    @JvmStatic
    fun logActivityName() {
        //val name = UnityPlayer.currentActivity::class.simpleName
        //Log.i("AndroidPlugin", "Activity Name: $name")

        Sentry.captureMessage("Message from the Android SDK")
        Log.i("AndroidPlugin", "Hello from Kotlin!")

        throw RuntimeException("Kotlin throws")
    }
}
