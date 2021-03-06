﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.IO;

namespace VISTEN.HTTPServer {
    /// <summary>
    /// 自定义请求处理类
    /// 参考地址 https://msdn.microsoft.com/zh-cn/library/5xkwyz4f(v=vs.110).aspx
    /// </summary>
    class CoHttpWorkerRequest : HttpWorkerRequest {

        private CoHost coHost;
        private HttpProcessor processor;
        private HttpRequestMessage requestInfo;
        

        private int _statusCode;
        private Dictionary<string, string> _responseHeaders;
        private IList<byte[]> _responseBodyBytes;
        private string _statusDescription;


        private bool _isHeaderSent;

        public CoHttpWorkerRequest(CoHost coHost, HttpProcessor processor, HttpRequestMessage requestInfo) {
            // TODO: Complete member initialization
            this.coHost = coHost;
            this.processor = processor;
            this.requestInfo = requestInfo;


            _responseHeaders = new Dictionary<string, string>();
            _responseBodyBytes = new List<byte[]>();

        }

        /// <summary>
        /// 由运行时使用以通知 HttpWorkerRequest 当前请求的请求处理已完成。
        /// </summary>
        public override void EndOfRequest() {
        }

        /// <summary>
        /// 返回请求标头的指定成员。
        /// </summary>
        /// <returns></returns>
        public override string GetHttpVerbName() {
            return requestInfo.Method;
        }

        /// <summary>
        /// 提供对请求的 HTTP 版本（如“HTTP/1.1”）的访问。
        /// </summary>
        /// <returns></returns>
        public override string GetHttpVersion() {
            return requestInfo.Version;
        }

        /// <summary>
        /// 请求标头中返回的服务器 IP 地址。
        /// </summary>
        /// <returns></returns>
        public override string GetLocalAddress() {
            return "0.0.0.0";
        }

        /// <summary>
        /// 请求标头中返回的服务器端口号。
        /// </summary>
        /// <returns></returns>
        public override int GetLocalPort() {
            return 0;
        }

        /// <summary>
        /// 返回请求 URL 中指定的查询字符串。
        /// </summary>
        /// <returns></returns>
        public override string GetQueryString() {
            return requestInfo.GetQueryString();
        }

        /// <summary>
        /// 返回附加了查询字符串的请求标头中包含的 URL 路径。
        /// </summary>
        /// <returns></returns>
        public override string GetRawUrl() {
            return requestInfo.Url;
        }

        /// <summary>
        /// 客户端的 IP 地址。
        /// </summary>
        /// <returns></returns>
        public override string GetRemoteAddress() {
            var host = requestInfo.Headers["Host"];
            return host;
        }

        /// <summary>
        /// 客户端的 HTTP 端口号。
        /// </summary>
        /// <returns></returns>
        public override int GetRemotePort() {
            var host = requestInfo.Headers["Host"];
            return 0;
        }

        /// <summary>
        /// 返回请求的 URI 的虚拟路径。
        /// </summary>
        /// <returns></returns>
        public override string GetUriPath() {
            //var host = requestInfo.Headers.Where(m => m.Key == "Host").FirstOrDefault();
            int index = requestInfo.Url.IndexOf('?');
            return index > -1 ? requestInfo.Url.Remove(index) : requestInfo.Url;
        }

        /// <summary>
        /// 将标准 HTTP 标头添加到响应。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public override void SendKnownResponseHeader(int index, string value) {
            //_responseHeaders
            _responseHeaders[HttpWorkerRequest.GetKnownResponseHeaderName(index)] = value;
        }

        /// <summary>
        /// 将指定文件的内容添加到响应并指定文件中的起始位置和要发送的字节数。
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public override void SendResponseFromFile(IntPtr handle, long offset, long length) {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 将指定文件的内容添加到响应并指定文件中的起始位置和要发送的字节数。
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public override void SendResponseFromFile(string filename, long offset, long length) {
            //throw new NotImplementedException();
            if (length > 0) {
                //byte[] dst = new byte[length];
                _responseBodyBytes.Add(GetFileByteArray(filename));
            }
        }

        /// <summary>
        /// 将字节数组中指定数目的字节添加到响应。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        public override void SendResponseFromMemory(byte[] data, int length) {
            //throw new NotImplementedException();
            if (length > 0)
            {
                byte[] dst = new byte[length];
                Buffer.BlockCopy(data, 0, dst, 0, length);
                _responseBodyBytes.Add(dst);
            }
        }

        /// <summary>
        /// 将所有挂起的响应数据发送到客户端。
        /// </summary>
        /// <param name="finalFlush">如果这将是最后一次刷新响应数据，则为 true；否则为 false。</param>
        public override void FlushResponse(bool finalFlush) {
            //发送响应
            Console.WriteLine("发送响应。。");
            if (!_isHeaderSent) {
                processor.SendHeaders( _statusCode, _responseHeaders, -1, finalFlush);
                _isHeaderSent = true;
            }
            for (int i = 0; i < _responseBodyBytes.Count; i++) {
                byte[] data = _responseBodyBytes[i];
                processor.SendResponse(data);
            }
            _responseBodyBytes = new List<byte[]>();
            if (finalFlush)
                processor.Close();
            //Console.WriteLine("成功发送响应...");
        }

        /// <summary>
        /// 指定响应的 HTTP 状态代码和状态说明，例如 SendStatus(200, "Ok")。
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="statusDescription"></param>
        public override void SendStatus(int statusCode, string statusDescription) {
            _statusCode = statusCode;
            _statusDescription = statusDescription;
        }

        /// <summary>
        /// 获取所有非标准的 HTTP 标头的名称/值对
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void SendUnknownResponseHeader(string name, string value) {
            _responseHeaders[name] = value;
        }

        /// <summary>
        /// 返回 HTTP 请求正文已被读取的部分。
        /// </summary>
        /// <returns></returns>
        public override byte[] GetPreloadedEntityBody() {
            return System.Text.Encoding.Default.GetBytes(requestInfo.Body);
        }

        /// <summary>
        /// 返回与指定的索引相对应的标准 HTTP 请求标头。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override string GetKnownRequestHeader(int index) {
            var headerName = HttpWorkerRequest.GetKnownRequestHeaderName(index);
            return requestInfo.Headers.Keys.Contains(headerName) ? 
                requestInfo.Headers[headerName] : 
                string.Empty;
        }

        /// <summary>
        /// Files the content.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        private byte[] GetFileByteArray(string fileName) {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            try {
                byte[] buffur = new byte[fs.Length];
                fs.Read(buffur, 0, (int)fs.Length);

                return buffur;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return null;
            } finally {
                if (fs != null) {

                    //关闭资源
                    fs.Close();
                }
            }
        }


    }
}
