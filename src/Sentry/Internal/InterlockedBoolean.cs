#if NET9_0_OR_GREATER
using TBool = System.Boolean;
#else
using TBool = System.Int32;
#endif

namespace Sentry.Internal;

internal struct InterlockedBoolean
{
    private volatile TBool _value;

    [Browsable(false)]
    internal TBool ValueForTests => _value;

#if NET9_0_OR_GREATER
    private const TBool True = true;
    private const TBool False = false;
#else
    private const TBool True = 1;
    private const TBool False = 0;
#endif

    public InterlockedBoolean() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InterlockedBoolean(bool value) { _value = value ? True : False; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(InterlockedBoolean @this) => (@this._value != False);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator InterlockedBoolean(bool @this) => new InterlockedBoolean(@this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Exchange(bool newValue)
    {
        TBool localNewValue = newValue ? True : False;

        TBool localReturnValue = Interlocked.Exchange(ref _value, localNewValue);

        return (localReturnValue != False);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareExchange(bool value, bool comparand)
    {
        TBool localValue = value ? True : False;
        TBool localComparand = comparand ? True : False;

        TBool localReturnValue = Interlocked.CompareExchange(ref _value, localValue, localComparand);

        return (localReturnValue != False);
    }
}
