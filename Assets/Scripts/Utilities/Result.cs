namespace Pulsar.Utilities.Utilities
{
public readonly struct Result<T>
{
    public readonly bool IsSuccess;
    public readonly T Value;
    
    public Result(bool isSuccess, T value)
    {
        IsSuccess = isSuccess;
        Value = value;
    }

    public static Result<T> Pass(T value) => new(true, value);
    public static Result<T> Fail() => new(false, default(T));

}
}