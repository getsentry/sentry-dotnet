#include <string.h>

__attribute__((noinline))
void crash()
{
    static void *invalid_mem = (void *)1;
    memset((char *)invalid_mem, 1, 100);
}
