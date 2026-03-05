namespace IntentVsProcess.Domain;

/// <summary>
/// Thrown when a Free Monad program encounters a Failed node during interpretation.
/// </summary>
public class OrderFailedException(string reason) : Exception(reason)
{
    public string Reason { get; } = reason;
}
