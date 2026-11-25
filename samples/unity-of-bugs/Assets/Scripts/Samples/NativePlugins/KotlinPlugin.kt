package unity.of.bugs

import android.util.Log
import kotlin.concurrent.thread

object KotlinPlugin {
    @JvmStatic fun `throw`() {
        try {
            throw Exception("Bugs in Kotlin 🐛")
        }
        catch (e: Exception) {
            Log.e("test", "Exception thrown in Kotlin!", e)
            throw e
        }
    }
    @JvmStatic fun throwOnBackgroundThread() {
        thread(start = true) {
            throw Exception("Kotlin 🐛 from a background thread.")
        }
    }
    @JvmStatic fun applicationNotResponding() {
        Log.i("test", "Stalling the main thread from Kotlin to trigger a native ANR.")
        Thread.sleep(10 * 1000) // ANR detection currently defaults to 5 seconds
        Log.i("test", "Kotlin main thread stall finished.")
    }
}
