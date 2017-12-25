using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Net.Sockets;

namespace MQTTClient
{

    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, HttpListenerResponse, byte[]> _responderMethod;

        public WebServer(string[] prefixes, Func<HttpListenerRequest, HttpListenerResponse, byte[]> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example 
            // "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);

            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, HttpListenerResponse, byte[]> method, params string[] prefixes)
            : this(prefixes, method) { }

        public static string GetOwnAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    File.AppendAllText("logExceptions.txt", localIP + "\r\n");
                }
            }
            return localIP;
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            BinaryWriter bw = null;
                            try
                            {
                                byte[] buf = _responderMethod(ctx.Request, ctx.Response);
                                ctx.Response.ContentLength64 = buf.Length;
                                bw = new BinaryWriter(ctx.Response.OutputStream);
                                bw.Write(buf);
                            }
                            catch (Exception e)
                            {
                                File.AppendAllText("logExceptions.txt", e.ToString() + "\r\n");
                            } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                if (bw != null)
                                {
                                    bw.Flush();
                                    bw.Close();
                                }
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
