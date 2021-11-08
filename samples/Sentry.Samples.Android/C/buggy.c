__attribute__((noinline))
void crash_in_c()
{
    char *ptr = 0;
    *ptr += 1;
}
