namespace Sentry.Internal;

#if NET9_0_OR_GREATER
using TBool = bool;
#else
using TBool = int;
#endif

internal struct InterlockedBoolean
{
    internal volatile TBool _value;

#if NET9_0_OR_GREATER
    private const TBool True = true;
    private const TBool False = false;
#else
    private const TBool True = 1;
    private const TBool False = 0;
#endif

    public InterlockedBoolean() { }

    public InterlockedBoolean(bool value) { _value = value ? True : False; }

    public static implicit operator bool(InterlockedBoolean? _this) => (_this != null) && (_this.Value._value != False);
    public static implicit operator InterlockedBoolean(bool _this) => new InterlockedBoolean(_this);

    public bool Exchange(bool newValue)
    {
        TBool localNewValue = newValue ? True : False;

        TBool localReturnValue = Interlocked.Exchange(ref _value, localNewValue);

        return (localReturnValue != False);
    }

    public bool CompareExchange(bool value, bool comparand)
    {
        TBool localValue = value ? True : False;
        TBool localComparand = comparand ? True : False;

        TBool localReturnValue = Interlocked.CompareExchange(ref _value, localValue, localComparand);

        return (localReturnValue != False);
    }
}
