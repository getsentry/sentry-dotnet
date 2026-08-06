#include <malloc/malloc.h>
#include <pthread.h>
#include <signal.h>

__attribute__((noinline))
static int
sentry_dotnet_crash_with_malloc_zone_locked(void)
{
    malloc_zone_t *zone = malloc_default_zone();
    zone->introspect->force_lock(zone);
    int result = pthread_kill(pthread_self(), SIGSEGV);
    zone->introspect->force_unlock(zone);
    return result;
}

static void *
sentry_dotnet_unmanaged_crash_thread(void *arg)
{
    int *result = (int *)arg;
    *result = sentry_dotnet_crash_with_malloc_zone_locked();
    return NULL;
}

int
sentry_dotnet_trigger_unmanaged_thread_crash(void)
{
    pthread_t thread;
    int thread_result = 0;
    int result = pthread_create(&thread, NULL, sentry_dotnet_unmanaged_crash_thread, &thread_result);
    if (result != 0) {
        return result;
    }

    result = pthread_join(thread, NULL);
    if (result != 0) {
        return result;
    }

    return thread_result;
}
