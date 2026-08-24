__attribute__((noinline))
void crash()
{
    // volatile prevents the compiler from optimizing the null dereference (UB) into
    // a trap instruction (SIGILL/SIGTRAP) and forces an actual SIGSEGV instead.
    volatile char *ptr = 0;
    *ptr += 1;
}
