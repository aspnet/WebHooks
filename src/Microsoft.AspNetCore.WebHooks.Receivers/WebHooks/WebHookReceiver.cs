using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Base class for WebHook Receivers
    /// </summary>
    public abstract class WebHookReceiver : IWebHookReceiver
    {
        /// <summary>
        /// Query String Parameter to use when passing Id on the Query String
        /// </summary>
        internal const string CodeQueryParameter = "code";

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract Task<WebHookHandlerContext> ReceiveAsync(PathString id, HttpContext context);

        /// <summary>
        /// Returns the Body as an Object from a Json Payload.
        /// </summary>
        /// <typeparam name="T">The Type of Object to Deserialize</typeparam>
        /// <param name="request">The Incoming Request</param>
        /// <returns></returns>
        protected async Task<T> ReadBodyAsJsonAsync<T>(HttpRequest request)
        {
            using (StreamReader streamReader = new StreamReader(request.Body))
            {
                string jsonData = await streamReader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
        }

        /// <summary>
        /// Returns the Body as a <see cref="XmlDocument"/>.
        /// </summary>
        /// <param name="request">The Request to parse</param>
        /// <returns></returns>
        protected async Task<XmlDocument> ReadBodyAsXmlAsync(HttpRequest request)
        {
            using (StreamReader streamReader = new StreamReader(request.Body))
            {
                string xmlData = await streamReader.ReadToEndAsync();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlData);
                return xmlDoc;
            }
        }

        /// <summary>
        /// Returns the Body as a <see cref="byte[]"/>.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected async Task<byte[]> ReadAsByteArrayAsync(HttpRequest request)
        {
            byte[] buffer = new byte[16*1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = await request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await ms.WriteAsync(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Checks to see if 2 values are equal
        /// </summary>
        /// <param name="inputA">First Value</param>
        /// <param name="inputB">Second Value</param>
        /// <returns></returns>
        protected internal static bool SecretEqual(byte[] inputA, byte[] inputB)
        {
            if (ReferenceEquals(inputA, inputB))
            {
                return true;
            }

            if (inputA == null || inputB == null || inputA.Length != inputB.Length)
            {
                return false;
            }

            bool areSame = true;
            for (int i = 0; i < inputA.Length; i++)
            {
                areSame &= inputA[i] == inputB[i];
            }
            return areSame;
        }

        /// <summary>
        /// Checks to see if 2 values are equal
        /// </summary>
        /// <param name="inputA">First Value</param>
        /// <param name="inputB">Second Value</param>
        /// <returns></returns>
        protected internal static bool SecretEqual(string inputA, string inputB)
        {
            if (ReferenceEquals(inputA, inputB))
            {
                return true;
            }

            if (inputA == null || inputB == null || inputA.Length != inputB.Length)
            {
                return false;
            }

            bool areSame = true;
            for (int i = 0; i < inputA.Length; i++)
            {
                areSame &= inputA[i] == inputB[i];
            }
            return areSame;
        }

        /// <summary>
        /// Validates a <see cref="HttpContext"/> to make sure it's using a secure connection.
        /// </summary>
        /// <param name="context">The Context to validate</param>
        /// <returns></returns>
        protected virtual async Task<bool> EnsureSecureConnection(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            IOptions<WebHookReceiverOptions> options = (IOptions<WebHookReceiverOptions>)context.RequestServices.GetService(typeof(IOptions<WebHookReceiverOptions>));

            // Check to see if we have been configured to ignore this check
            if (options.Value.DisableHttpsCheck == true)
            {
                return true;
            }

            // Require HTTP unless request is local
            if (context.Request.Host.Host != "localhost" && !context.Request.IsHttps)
            {
                string msg = string.Format(ReceiverResource.Receiver_NoHttps, GetType().Name, "https");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync(msg);
                return false;
            }
            return true;
        }
        
    }
}
