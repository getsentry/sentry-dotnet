__attribute__((noinline))
void crash()
{
    char *ptr = 0;
    *ptr += 1;
}
