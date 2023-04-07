namespace RichEnum;

public abstract class RichEnum<T>
{
    private readonly T _value;

    protected RichEnum(T value) => _value = value;

    public override string ToString() => _value?.ToString() ?? string.Empty;

    public static implicit operator T(RichEnum<T> literalEnum) => literalEnum._value;

    public static bool operator ==(RichEnum<T> a, T b) => b?.Equals(a._value) ?? (a._value is null && b is null);

    public static bool operator !=(RichEnum<T> a, T b) => !b?.Equals(a._value) ?? (a._value is null && b is null);

    protected bool Equals(RichEnum<T> other) => _value?.Equals(other._value) ?? other._value is null;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals((RichEnum<T>) obj);
    }

    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
}