﻿// ======================================================================
//   
//           Copyright (C) 2018-2020 湖南心莱信息科技有限公司    
//           All rights reserved
//   
//           filename : RequestUtility.cs
//           description :
//   
//           created by 雪雁 at  2018-07-16 15:46
//           Mail: wenqiang.li@xin-lai.com
//           QQ群：85318032（技术交流）
//           Blog：http://www.cnblogs.com/codelove/
//           GitHub：https://github.com/xin-lai
//           Home：http://xin-lai.com
//   
// ======================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Magicodes.Pay.Wxpay.Helper
{
    public static class RequestUtility
    {
        /// <summary>
        ///     组装QueryString的方法
        ///     参数之间用&amp;连接，首位没有符号，如：a=1&amp;b=2&amp;c=3
        /// </summary>
        /// <param name="formData"></param>
        /// <returns></returns>
        public static string GetQueryString(this Dictionary<string, string> formData)
        {
            if (formData == null || formData.Count == 0)
                return "";

            var sb = new StringBuilder();

            var i = 0;
            foreach (var kv in formData)
            {
                i++;
                sb.AppendFormat("{0}={1}", kv.Key, kv.Value);
                if (i < formData.Count)
                    sb.Append("&");
            }

            return sb.ToString();
        }

        /// <summary>
        ///     填充表单信息的Stream
        /// </summary>
        /// <param name="formData"></param>
        /// <param name="stream"></param>
        public static void FillFormDataStream(this Dictionary<string, string> formData, Stream stream)
        {
            var dataString = GetQueryString(formData);
            var formDataBytes = formData == null ? new byte[0] : Encoding.UTF8.GetBytes(dataString);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin); //设置指针读取位置
        }

        /// <summary>
        ///     封装System.Web.HttpUtility.HtmlEncode
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string HtmlEncode(this string html)
        {
            return WebUtility.HtmlEncode(html);
        }

        /// <summary>
        ///     封装System.Web.HttpUtility.HtmlDecode
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string HtmlDecode(this string html)
        {
            return WebUtility.HtmlDecode(html);
        }

        /// <summary>
        ///     封装System.Web.HttpUtility.UrlEncode
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string UrlEncode(this string url)
        {
            return WebUtility.UrlEncode(url);
        }

        /// <summary>
        ///     封装System.Web.HttpUtility.UrlDecode
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string UrlDecode(this string url)
        {
            return WebUtility.UrlDecode(url);
        }

        #region 代理

        private static WebProxy _webproxy;

        /// <summary>
        ///     设置Web代理
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static void SetHttpProxy(string host, string port, string username, string password)
        {
            ICredentials cred = new NetworkCredential(username, password);
            if (!string.IsNullOrEmpty(host))
                _webproxy = new WebProxy(host + ":" + port ?? "80", true, null, cred);
        }

        /// <summary>
        ///     清除Web代理状态
        /// </summary>
        public static void RemoveHttpProxy()
        {
            _webproxy = null;
        }

        #endregion

        #region 同步方法

        /// <summary>
        ///     使用Get方法获取字符串结果（没有加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string HttpGet(string url, Encoding encoding = null)
        {
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var wc = new WebClient
            {
                Proxy = _webproxy,
                Encoding = encoding ?? Encoding.UTF8
            };
            return wc.DownloadString(url);
        }

        /// <summary>
        ///     使用Post方法上传数据并下载文件或结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="postData">请求数据</param>
        /// <param name="encoding">编码格式</param>
        /// <returns></returns>
        public static byte[] HttpUploadData(string url, string postData, Encoding encoding = null)
        {
            var wc = new WebClient
            {
                Proxy = _webproxy,
                Encoding = encoding ?? Encoding.UTF8
            };
            return wc.UploadData(url, "POST", Encoding.UTF8.GetBytes(string.IsNullOrEmpty(postData) ? "" : postData));
        }

        /// <summary>
        ///     使用Get方法获取字符串结果（加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="encoding"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static string HttpGet(string url, CookieContainer cookieContainer = null, Encoding encoding = null,
            int timeOut = 30000)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeOut;
            request.Proxy = _webproxy;

            if (cookieContainer != null)
                request.CookieContainer = cookieContainer;

            var response = (HttpWebResponse) request.GetResponse();

            if (cookieContainer != null)
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);

            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null) return null;
                using (var myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    var retString = myStreamReader.ReadToEnd();
                    return retString;
                }
            }
        }

        /// <summary>
        ///     使用Post方法获取字符串结果，常规提交
        /// </summary>
        /// <returns></returns>
        public static string HttpPost(string url, CookieContainer cookieContainer = null,
            Dictionary<string, string> formData = null, Encoding encoding = null, int timeOut = 30000)
        {
            var ms = new MemoryStream();
            formData.FillFormDataStream(ms); //填充formData
            return HttpPost(url, cookieContainer, ms, null, null, encoding, timeOut);
        }

        public static HttpWebRequest CreateWebRequest(
            string url,
            string contentType = null,
            string acceptLanguage = null,
            string accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
            string userAgent =
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36",
            int timeOut = 30000,
            CookieContainer cookieContainer = null)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);

            request.ContentType = contentType;
            if (cookieContainer != null)
                request.CookieContainer = cookieContainer;
            if (string.IsNullOrEmpty(acceptLanguage))
            {
                var myWebHeaderCollection = request.Headers;
                myWebHeaderCollection.Add("Accept-Language", acceptLanguage);
            }

            request.Accept = accept;
            request.UseDefaultCredentials = true;
            request.UserAgent = userAgent;
            request.Timeout = timeOut;
            return request;
        }

        /// <summary>
        ///     POST文件
        /// </summary>
        /// <param name="url"></param>
        /// <param name="files">文件路径</param>
        /// <param name="postdata">参数</param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string HttpUploadFile(string url, Dictionary<string, string> postdata, Encoding encoding,
            params string[] files)
        {
            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            var endbytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");


            var request = CreateWebRequest(url);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            request.Credentials = CredentialCache.DefaultCredentials;

            using (var stream = request.GetRequestStream())
            {
                //1.1 key/value
                var formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                if (postdata != null)
                    foreach (var key in postdata.Keys)
                    {
                        stream.Write(boundarybytes, 0, boundarybytes.Length);
                        var formitem = string.Format(formdataTemplate, key, postdata[key]);
                        var formitembytes = encoding.GetBytes(formitem);
                        stream.Write(formitembytes, 0, formitembytes.Length);
                    }

                //1.2 file
                var headerTemplate =
                    "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                var buffer = new byte[4096];
                var bytesRead = 0;
                for (var i = 0; i < files.Length; i++)
                {
                    stream.Write(boundarybytes, 0, boundarybytes.Length);
                    var header = string.Format(headerTemplate, "file" + i, Path.GetFileName(files[i]));
                    var headerbytes = encoding.GetBytes(header);
                    stream.Write(headerbytes, 0, headerbytes.Length);
                    using (var fileStream = new FileStream(files[i], FileMode.Open, FileAccess.Read))
                    {
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                            stream.Write(buffer, 0, bytesRead);
                    }
                }

                //1.3 form end
                stream.Write(endbytes, 0, endbytes.Length);
            }

            //2.WebResponse
            var response = (HttpWebResponse) request.GetResponse();
            using (var stream = new StreamReader(response.GetResponseStream()))
            {
                return stream.ReadToEnd();
            }
        }


        /// <summary>
        ///     使用Post方法获取字符串结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="postStream"></param>
        /// <param name="fileDictionary">需要上传的文件，Key：对应要上传的Name，Value：本地文件名</param>
        /// <param name="encoding"></param>
        /// <param name="timeOut"></param>
        /// <param name="checkValidationResult">验证服务器证书回调自动验证</param>
        /// <param name="refererUrl"></param>
        /// <returns></returns>
        public static string HttpPost(string url, CookieContainer cookieContainer = null, Stream postStream = null,
            Dictionary<string, string> fileDictionary = null, string refererUrl = null, Encoding encoding = null,
            int timeOut = 30000, bool checkValidationResult = false)
        {
            var request = CreateWebRequest(url, timeOut: timeOut, cookieContainer: cookieContainer);
            request.Method = "POST";
            request.Proxy = _webproxy;

            if (checkValidationResult)
                ServicePointManager.ServerCertificateValidationCallback =
                    CheckValidationResult;

            request.KeepAlive = true;

            if (!string.IsNullOrEmpty(refererUrl))
                request.Referer = refererUrl;

            #region 处理Form表单文件上传

            var formUploadFile = fileDictionary != null && fileDictionary.Count > 0; //是否用Form上传文件
            if (formUploadFile)
            {
                //通过表单上传文件
                var boundary = "----" + DateTime.Now.Ticks.ToString("x");

                postStream = postStream ?? new MemoryStream();
                //byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                var fileFormdataTemplate = "\r\n--" + boundary +
                                           "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                var dataFormdataTemplate = "\r\n--" + boundary +
                                           "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";


                foreach (var file in fileDictionary)
                    try
                    {
                        var fileName = file.Value;
                        //准备文件流
                        using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                        {
                            string formdata = null;
                            if (fileStream != null)
                                formdata = string.Format(fileFormdataTemplate, file.Key, /*fileName*/
                                    Path.GetFileName(fileName));
                            else
                                formdata = string.Format(dataFormdataTemplate, file.Key, file.Value);

                            //统一处理
                            var formdataBytes = Encoding.UTF8.GetBytes(postStream.Length == 0
                                ? formdata.Substring(2, formdata.Length - 2)
                                : formdata); //第一行不需要换行
                            postStream.Write(formdataBytes, 0, formdataBytes.Length);

                            //写入文件
                            if (fileStream != null)
                            {
                                var buffer = new byte[1024];
                                var bytesRead = 0;
                                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                    postStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                //结尾
                var footer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                postStream.Write(footer, 0, footer.Length);

                request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);
            }
            else
            {
                request.ContentType = "application/x-www-form-urlencoded";
            }

            #endregion

            request.ContentLength = postStream != null ? postStream.Length : 0;

            #region 输入二进制流

            if (postStream != null)
            {
                postStream.Position = 0;

                //直接写入流
                var requestStream = request.GetRequestStream();

                var buffer = new byte[1024];
                var bytesRead = 0;
                while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) != 0)
                    requestStream.Write(buffer, 0, bytesRead);
                postStream.Close(); //关闭文件访问
            }

            #endregion

            var response = (HttpWebResponse) request.GetResponse();

            if (cookieContainer != null)
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);

            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null) return null;
                using (var myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    var retString = myStreamReader.ReadToEnd();
                    return retString;
                }
            }
        }

        /// <summary>
        ///     验证服务器证书
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors errors)
        {
            return true;
        }

        #endregion

        #region 异步方法

        /// <summary>
        ///     使用Get方法获取字符串结果（没有加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static async Task<string> HttpGetAsync(string url, Encoding encoding = null)
        {
            var wc = new WebClient
            {
                Proxy = _webproxy,
                Encoding = encoding ?? Encoding.UTF8
            };
            //if (encoding != null)
            //{
            //    wc.Encoding = encoding;
            //}
            return await wc.DownloadStringTaskAsync(url);
        }

        /// <summary>
        ///     使用Get方法获取字符串结果（加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static async Task<string> HttpGetAsync(string url, CookieContainer cookieContainer = null,
            Encoding encoding = null, int timeOut = 30000)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeOut;
            request.Proxy = _webproxy;

            if (cookieContainer != null)
                request.CookieContainer = cookieContainer;

            var response = (HttpWebResponse) await request.GetResponseAsync();

            if (cookieContainer != null)
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);

            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null) return null;
                using (var myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    var retString = await myStreamReader.ReadToEndAsync();
                    return retString;
                }
            }
        }

        /// <summary>
        ///     使用Post方法获取字符串结果，常规提交
        /// </summary>
        /// <returns></returns>
        public static async Task<string> HttpPostAsync(string url, CookieContainer cookieContainer = null,
            Dictionary<string, string> formData = null, Encoding encoding = null, int timeOut = 30000)
        {
            var ms = new MemoryStream();
            await formData.FillFormDataStreamAsync(ms); //填充formData
            return await HttpPostAsync(url, cookieContainer, ms, null, encoding, timeOut);
        }


        /// <summary>
        ///     使用Post方法获取字符串结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="postStream"></param>
        /// <param name="encoding"></param>
        /// <param name="timeOut"></param>
        /// <param name="checkValidationResult">验证服务器证书回调自动验证</param>
        /// <param name="refererUrl"></param>
        /// <returns></returns>
        public static async Task<string> HttpPostAsync(string url, CookieContainer cookieContainer = null,
            Stream postStream = null, string refererUrl = null,
            Encoding encoding = null, int timeOut = 30000, bool checkValidationResult = false)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = timeOut;
            request.Proxy = _webproxy;

            if (checkValidationResult)
                ServicePointManager.ServerCertificateValidationCallback =
                    CheckValidationResult;

            request.ContentLength = postStream != null ? postStream.Length : 0;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = true;

            if (!string.IsNullOrEmpty(refererUrl))
                request.Referer = refererUrl;
            request.UserAgent =
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";

            if (cookieContainer != null)
                request.CookieContainer = cookieContainer;

            #region 输入二进制流

            if (postStream != null)
            {
                postStream.Position = 0;

                //直接写入流
                var requestStream = await request.GetRequestStreamAsync();

                var buffer = new byte[1024];
                var bytesRead = 0;
                while ((bytesRead = await postStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    await requestStream.WriteAsync(buffer, 0, bytesRead);


                //debug
                //postStream.Seek(0, SeekOrigin.Begin);
                //StreamReader sr = new StreamReader(postStream);
                //var postStr = await sr.ReadToEndAsync();

                postStream.Close(); //关闭文件访问
            }

            #endregion

            var response = (HttpWebResponse) await request.GetResponseAsync();

            if (cookieContainer != null)
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);

            using (var responseStream = response.GetResponseStream())
            {
                using (var myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    var retString = await myStreamReader.ReadToEndAsync();
                    return retString;
                }
            }
        }


        /// <summary>
        ///     填充表单信息的Stream
        /// </summary>
        /// <param name="formData"></param>
        /// <param name="stream"></param>
        public static async Task FillFormDataStreamAsync(this Dictionary<string, string> formData, Stream stream)
        {
            var dataString = GetQueryString(formData);
            var formDataBytes = formData == null ? new byte[0] : Encoding.UTF8.GetBytes(dataString);
            await stream.WriteAsync(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin); //设置指针读取位置
        }

        #endregion

        /// <summary>
        /// Certificate validation callback.
        /// </summary>
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            // If the certificate is a valid, signed certificate, return true.
            if (error == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            Console.WriteLine("X509Certificate [{0}] Policy Error: '{1}'",
                cert.Subject,
                error.ToString());

            return false;
        }
    }
}
