﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace MLOKit.Utilities.AzureML
{
    class DatastoreUtils
    {


        // get single datstore by name
        public static async Task<Objects.AzureML.Datastore> getSingleDatastore(string credentials, string subscriptionID, string region, string resourceGroup, string workspace, string datName)
        {
            Objects.AzureML.Datastore datastore = null;

            try
            {

                // ignore SSL errors
                ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // web request to get a datastore
                HttpWebRequest webRequest = (HttpWebRequest)System.Net.WebRequest.Create("https://" + region + ".experiments.azureml.net/datastore/v1.0/subscriptions/" + subscriptionID + "/resourceGroups/" + resourceGroup + "/providers/Microsoft.MachineLearningServices/workspaces/" + workspace + "/datastores/" + datName);
                if (webRequest != null)
                {

                    // set header values
                    webRequest.Method = "GET";
                    webRequest.ContentType = "application/json";
                    webRequest.UserAgent = "MLOKit-e977ac02118a3cb2c584d92a324e41e9";
                    webRequest.Headers.Add("Authorization", "Bearer " + credentials);


                    // get web response and status code
                    HttpWebResponse myWebResponse = (HttpWebResponse)await webRequest.GetResponseAsync();
                    string content;
                    var reader = new StreamReader(myWebResponse.GetResponseStream());
                    content = reader.ReadToEnd();


                    // parse the JSON output and display results
                    JsonTextReader jsonResult = new JsonTextReader(new StringReader(content));

                    string accountName = "";
                    string containerName = "";
                    string endpoint = "";
                    string datastoreCredential = "";
                    string subscriptionId = "";
                    string dataStoreName = "";

                    string propName = "";

                    // read the json results
                    while (jsonResult.Read())
                    {

                        switch (jsonResult.TokenType.ToString())
                        {
                            case "StartObject":
                                break;
                            case "EndObject":

                                // grab the datstore to return
                                if (accountName != "" && containerName != "" && endpoint != "" && datastoreCredential != "" && subscriptionId != "" && dataStoreName != "")
                                {
                                    datastore = new Objects.AzureML.Datastore(accountName, containerName, endpoint, datastoreCredential, subscriptionId, dataStoreName);

                                }
                                break;
                            case "StartArray":
                                break;
                            case "EndArray":
                                break;
                            case "PropertyName":
                                propName = jsonResult.Value.ToString();
                                break;
                            case "String":
                                if (propName.ToLower().Equals("accountname"))
                                {
                                    accountName = jsonResult.Value.ToString();
                                }
                                if (propName.ToLower().Equals("containername"))
                                {
                                    containerName = jsonResult.Value.ToString();
                                }
                                if (propName.ToLower().Equals("endpoint"))
                                {
                                    endpoint = jsonResult.Value.ToString();
                                }
                                if (propName.ToLower().Equals("credential"))
                                {
                                    datastoreCredential = jsonResult.Value.ToString();
                                }
                                if (propName.ToLower().Equals("subscriptionid"))
                                {
                                    subscriptionId = jsonResult.Value.ToString();
                                }
                                if (propName.ToLower().Equals("name"))
                                {
                                    dataStoreName = jsonResult.Value.ToString();
                                }

                                break;
                            case "Date":
                                break;
                            case "Boolean":
                                break;
                            default:
                                break;

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("[-] ERROR: " + ex.Message);
                Console.WriteLine("");
            }


            return datastore;

        }


        // code from - https://github.com/Azure-Samples/storage-dotnet-rest-api-with-auth
        public static AuthenticationHeaderValue GetAuthorizationHeader(
   string storageAccountName, string storageAccountKey, DateTime now,
   HttpRequestMessage httpRequestMessage, string ifMatch = "", string md5 = "")
        {
            // This is the raw representation of the message signature.
            HttpMethod method = httpRequestMessage.Method;
            String MessageSignature = String.Format("{0}\n\n\n{1}\n{5}\n\n\n\n{2}\n\n\n\n{3}{4}",
                      method.ToString(),
                      (method == HttpMethod.Get || method == HttpMethod.Head) ? String.Empty
                        : httpRequestMessage.Content.Headers.ContentLength.ToString(),
                      ifMatch,
                      GetCanonicalizedHeaders(httpRequestMessage),
                      //GetCanonicalizedResource(httpRequestMessage.RequestUri, storageAccountName),md5);
            GetCanonicalizedResource(httpRequestMessage.RequestUri, storageAccountName), md5);

            // Now turn it into a byte array.
            byte[] SignatureBytes = Encoding.UTF8.GetBytes(MessageSignature);

            // Create the HMACSHA256 version of the storage key.
            HMACSHA256 SHA256 = new HMACSHA256(Convert.FromBase64String(storageAccountKey));

            // Compute the hash of the SignatureBytes and convert it to a base64 string.
            string signature = Convert.ToBase64String(SHA256.ComputeHash(SignatureBytes));

            // This is the actual header that will be added to the list of request headers.
            // You can stop the code here and look at the value of 'authHV' before it is returned.
            AuthenticationHeaderValue authHV = new AuthenticationHeaderValue("SharedKey",
                storageAccountName + ":" + Convert.ToBase64String(SHA256.ComputeHash(SignatureBytes)));
            return authHV;
        }

        // code from - https://github.com/Azure-Samples/storage-dotnet-rest-api-with-auth
        public static string GetCanonicalizedResource(Uri address, string storageAccountName)
        {
            // The absolute path is "/" because for we're getting a list of containers.
            StringBuilder sb = new StringBuilder("/").Append(storageAccountName).Append(address.AbsolutePath);

            // Address.Query is the resource, such as "?comp=list".
            // This ends up with a NameValueCollection with 1 entry having key=comp, value=list.
            // It will have more entries if you have more query parameters.
            NameValueCollection values = HttpUtility.ParseQueryString(address.Query);
            

            foreach (var item in values.AllKeys.OrderBy(k => k))
            {
                sb.Append('\n').Append(item).Append(':').Append(values[item]);
            }

            return sb.ToString();

        }

        // code from - https://github.com/Azure-Samples/storage-dotnet-rest-api-with-auth
        public static string GetCanonicalizedHeaders(HttpRequestMessage httpRequestMessage)
        {
            var headers = from kvp in httpRequestMessage.Headers
                          where kvp.Key.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase)
                          orderby kvp.Key
                          select new { Key = kvp.Key.ToLowerInvariant(), kvp.Value };

            StringBuilder sb = new StringBuilder();

            // Create the string in the right format; this is what makes the headers "canonicalized" --
            //   it means put in a standard format. http://en.wikipedia.org/wiki/Canonicalization
            foreach (var kvp in headers)
            {
                StringBuilder headerBuilder = new StringBuilder(kvp.Key);
                char separator = ':';

                // Get the value for each header, strip out \r\n if found, then append it with the key.
                foreach (string headerValues in kvp.Value)
                {
                    string trimmedValue = headerValues.TrimStart().Replace("\r\n", String.Empty);
                    headerBuilder.Append(separator).Append(trimmedValue);

                    // Set this to a comma; this will only be used 
                    //   if there are multiple values for one of the headers.
                    separator = ',';
                }
                sb.Append(headerBuilder.ToString()).Append("\n");
            }
            return sb.ToString();
        }


    }
}
