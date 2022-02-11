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
}
