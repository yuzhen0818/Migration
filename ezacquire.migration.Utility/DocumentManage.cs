using ezacquire.migration.Utility.Models;
using ezLib.WebUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ezacquire.migration.Utility
{
    public class DocumentManage
    {
        public DocumentManage()
        {
        }

        public ezAcquireReturnCode WriteImage(string token, DocumentAdd documentAdd)
        {
            //http://10.0.0.126:5003/Document/Add
            var webClient2 = CreateWebRequestClient(ConfigurationManager.AppSettings["DocumentService"] + "/Document/Add");
            webClient2.Headers.Add("Authorization", "Bearer " + token);
            using (var streamWriter = new StreamWriter(webClient2.GetRequestStream()))
            {
                string json = documentAdd.ToJson();
                // Logger.Write(json);
                streamWriter.Write(json);
            }
            string returnContent = "";
            ezAcquireReturnCode ezAcquireReturnCode = new ezAcquireReturnCode();
            HttpWebResponse response = (HttpWebResponse)webClient2.GetResponse();
            Stream responseStream = response.GetResponseStream();
            using (var sr = new StreamReader(responseStream, Encoding.UTF8))
            {
                returnContent = sr.ReadToEnd();
                ezAcquireReturnCode = new ezAcquireReturnCode();
                ezAcquireReturnCode.Status = JObject.Parse(returnContent)["status"].ToString();

                if (ezAcquireReturnCode.Status.ToUpper().Equals("OK"))
                {
                    ezAcquireReturnCode.Result = JObject.Parse(returnContent)["result"].ToString();
                }
                else
                {
                    ezAcquireReturnCode = JsonConvert.DeserializeObject<ezAcquireReturnCode>(returnContent);
                }

            }
            return ezAcquireReturnCode;
        }

        public ezAcquireReturnCode GetToken()
        {
            var user = new
            {
                UserId = ConfigurationManager.AppSettings["ezAcquireUserId"],
                Password = ConfigurationManager.AppSettings["ezAcquirePassword"],
                OperatorInfo = new
                {
                    OperatorUserId = "SysAdmin",
                    ClientIPAddress = ezLib.Utility.NetworkHelper.GetLocalIPAddress(),
                    SystemId = "Migiration",
                    TransactionId = "",
                    ServerIPAddress = ""
                }
            };
            var webClient = CreateWebRequestClient(ConfigurationManager.AppSettings["AuthServiceURL"] + "/GetToken");
            using (var streamWriter = new StreamWriter(webClient.GetRequestStream()))
            {
                string json = user.ToJson();
                streamWriter.Write(json);
            }
            string returnContent = "";
            ezAcquireReturnCode ezAcquireReturnCode = new ezAcquireReturnCode();
            HttpWebResponse response = (HttpWebResponse)webClient.GetResponse();
            Stream responseStream = response.GetResponseStream();
            using (var sr = new StreamReader(responseStream, Encoding.UTF8))
            {
                returnContent = sr.ReadToEnd();
                ezAcquireReturnCode = JsonConvert.DeserializeObject<ezAcquireReturnCode>(returnContent);
            }
            return ezAcquireReturnCode;
        }
        
        public ezAcquireReturnCode GetDocumentFile(string token, string docId, string mimetype)
        {
            var user = new
            {
                documentId = docId,
                page = "",
                mimeType = mimetype,
                OperatorInfo = new
                {
                    OperatorUserId = "SysAdmin",
                    ClientIPAddress = ezLib.Utility.NetworkHelper.GetLocalIPAddress(),
                    SystemId = "Migiration",
                    TransactionId = "",
                    ServerIPAddress = ""
                }
            };
            var webClient = CreateWebRequestClient(ConfigurationManager.AppSettings["DocumentService"] + "/Document/FileString");
            webClient.Headers.Add("Authorization", "Bearer " + token);
            using (var streamWriter = new StreamWriter(webClient.GetRequestStream()))
            {
                string json = user.ToJson();
                streamWriter.Write(json);
            }
            string returnContent = "";
            ezAcquireReturnCode ezAcquireReturnCode = new ezAcquireReturnCode();
            HttpWebResponse response = (HttpWebResponse)webClient.GetResponse();
            Stream responseStream = response.GetResponseStream();
            using (var sr = new StreamReader(responseStream, Encoding.UTF8))
            {
                returnContent = sr.ReadToEnd();
                ezAcquireReturnCode = new ezAcquireReturnCode();
                ezAcquireReturnCode.Status = JObject.Parse(returnContent)["status"].ToString();

                if (ezAcquireReturnCode.Status.ToUpper().Equals("OK"))
                {
                    ezAcquireReturnCode.Result = JObject.Parse(returnContent)["result"].ToString();
                }
                else
                {
                    ezAcquireReturnCode = JsonConvert.DeserializeObject<ezAcquireReturnCode>(returnContent);
                }
            }
            return ezAcquireReturnCode;
        }

        private HttpWebRequest CreateWebRequestClient(string url)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            return request;
        }
    }
}
