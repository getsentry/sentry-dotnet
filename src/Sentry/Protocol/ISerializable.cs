namespace Sentry.Protocol
{
    /// <summary>
    /// Represents a serializable entity.
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Serializes the object to its equivalent string representation.
        /// </summary>
        string Serialize();
    }
}
