package io.sentry.android.supplemental;

public final class Buggy {
    private Buggy() {}

    public static void throwRuntimeException(String msg) {
        throw new RuntimeException(msg);
    }

    public static void throwRuntimeExceptionOnBackgroundThread(String msg) {
        Thread thread = new Thread() {
            public void run() {
                throw new RuntimeException(msg);
            }
        };

        thread.start();
    }
}
