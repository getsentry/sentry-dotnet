#include "sentry_boot.h"
#include "sentry_logger.h"
#include "sentry_path.h"

#ifdef SENTRY_PLATFORM_WINDOWS
#    define TRANSPORT_LIBRARY "sentry-crash-transport.dll"
#else
#    include <dlfcn.h>
#    ifdef SENTRY_PLATFORM_DARWIN
#        define TRANSPORT_LIBRARY "sentry-crash-transport.dylib"
#    else
#        define TRANSPORT_LIBRARY "sentry-crash-transport.so"
#    endif
#endif

// matches NativeHttpClient.Api in src/Sentry.Native.Transport
typedef struct {
    sentry_transport_t *(*sentry_http_transport_new)(
        sentry_http_client_factory_func_t, void *,
        sentry_http_client_send_func_t, void (*)(sentry_http_client_t *));
    void (*sentry_http_transport_set_client_start_func)(sentry_transport_t *,
        int (*)(sentry_http_client_t *, const sentry_options_t *));
    void (*sentry_http_transport_set_client_shutdown_func)(
        sentry_transport_t *, void (*)(sentry_http_client_t *));
    const char *(*sentry_http_request_get_method)(
        const sentry_http_request_t *);
    const char *(*sentry_http_request_get_url)(const sentry_http_request_t *);
    size_t (*sentry_http_request_get_header_count)(
        const sentry_http_request_t *);
    int (*sentry_http_request_get_header)(
        const sentry_http_request_t *, size_t, const char **, const char **);
    const char *(*sentry_http_request_get_body)(
        const sentry_http_request_t *, size_t *);
    const char *(*sentry_http_request_get_body_file_path)(
        const sentry_http_request_t *, size_t *);
    size_t (*sentry_http_request_get_body_file_offset)(
        const sentry_http_request_t *);
    void (*sentry_http_response_set_status_code)(sentry_http_response_t *, int);
    void (*sentry_http_response_set_header)(
        sentry_http_response_t *, const char *, const char *);
    const char *(*sentry_options_get_proxy)(const sentry_options_t *);
    const char *(*sentry_options_get_user_agent)(const sentry_options_t *);
} transport_api_t;

typedef sentry_transport_t *(*transport_new_func_t)(const transport_api_t *);

static transport_new_func_t
load_transport(void)
{
    sentry_path_t *exe = sentry__path_current_exe();
    sentry_path_t *dir = exe ? sentry__path_dir(exe) : NULL;
    sentry_path_t *path
        = dir ? sentry__path_join_str(dir, TRANSPORT_LIBRARY) : NULL;
    sentry__path_free(dir);
    sentry__path_free(exe);
    if (!path) {
        return NULL;
    }

    transport_new_func_t transport_new = NULL;
#ifdef SENTRY_PLATFORM_WINDOWS
    HMODULE library = LoadLibraryW(path->path_w);
    if (library) {
        transport_new = (transport_new_func_t)GetProcAddress(
            library, "sentry_dotnet_transport_new");
    }
#else
    void *library = dlopen(path->path, RTLD_NOW | RTLD_LOCAL);
    if (library) {
        *(void **)&transport_new
            = dlsym(library, "sentry_dotnet_transport_new");
    }
#endif
    if (!transport_new) {
        SENTRY_WARNF("failed to load the transport from %s", path->path);
    }
    sentry__path_free(path);
    return transport_new;
}

sentry_transport_t *
sentry_transport_new_default(void)
{
    static const transport_api_t api = {
        sentry_http_transport_new,
        sentry_http_transport_set_client_start_func,
        sentry_http_transport_set_client_shutdown_func,
        sentry_http_request_get_method,
        sentry_http_request_get_url,
        sentry_http_request_get_header_count,
        sentry_http_request_get_header,
        sentry_http_request_get_body,
        sentry_http_request_get_body_file_path,
        sentry_http_request_get_body_file_offset,
        sentry_http_response_set_status_code,
        sentry_http_response_set_header,
        sentry_options_get_proxy,
        sentry_options_get_user_agent,
    };

    transport_new_func_t transport_new = load_transport();
    return transport_new ? transport_new(&api) : NULL;
}
