namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// TBD
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// TDB
        /// </summary>
        /// <param name="action">TDB</param>
        /// <param name="payload">TDB</param>
        public Notification(string action, object payload)
        {
            Action = action;
            Payload = payload;
        }

        /// <summary>
        /// TDB
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// TDB
        /// </summary>
        public object Payload { get; }
    }
}
