namespace Funcky.Monads;

public readonly partial struct Result<TValidResult>
{
    public static implicit operator Result<TValidResult>(TValidResult item) => Result.Ok(item);

    /// <summary>Performs a side effect when the result is ok and returns the result again.</summary>
    public Result<TValidResult> Inspect(Action<TValidResult> action)
    {
        Match(ok: action, error: NoOperation);
        return this;
    }
}