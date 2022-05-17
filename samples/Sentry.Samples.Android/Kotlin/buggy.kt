package io.sentry.samples.android.kotlin

import kotlin.concurrent.thread

object Buggy {
    @JvmStatic fun `throw`() {
        try {
            throw Exception("Bugs in Kotlin 🐛")
        }
        catch (e: Exception) {
            throw e
        }
    }
    @JvmStatic fun throwOnBackgroundThread() {
        thread(start = true) {
            throw Exception("Kotlin 🐛 from a background thread.")
        }
    }
}
