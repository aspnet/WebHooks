namespace Microsoft.AspNet.WebHooks
{
    using Newtonsoft.Json;

    /// <summary>
    /// Contains information sent in a WebHook notification from Stripe.
    /// 'https://stripe.com/docs/api#event_object'
    /// </summary>
    public class StripeEventData
    {
        /// <summary>
        /// Gets or sets the event data object. 
        /// </summary>
        [JsonProperty("object")]
        public object Object { get; set; }

        /// <summary>
        /// Gets or sets the hash containing the names of the attributes that have changed 
        /// and their previous values (only sent along with *.updated events). 
        /// </summary>
        [JsonProperty("previous_attributes")]
        public object PreviousAttributes { get; set; }
    }
}