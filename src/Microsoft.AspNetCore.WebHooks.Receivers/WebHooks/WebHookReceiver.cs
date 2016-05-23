using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Xml;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebHooks.Properties;
using System.Globalization;

namespace Microsoft.AspNetCore.WebHooks
{
    public abstract class WebHookReceiver : IWebHookReceiver
    {

        internal const string CodeQueryParameter = "code";

        public abstract string Name { get; }

        public abstract Task<WebHookHandlerContext> ReceiveAsync(PathString id, HttpContext context);

        protected async Task<T> ReadBodyAsJsonAsync<T>(HttpRequest request)
        {
            using (StreamReader streamReader = new StreamReader(request.Body))
            {
                string jsonData = await streamReader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
        }

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
                string msg = string.Format(CultureInfo.CurrentCulture, ReceiverResource.Receiver_NoHttps, GetType().Name, "https");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync(msg);
                return false;
            }
            return true;
        }
        
    }
}
