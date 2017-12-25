using System;
using System.Linq;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.IO;
using System.Threading;
using System.Net;

namespace MQTTClient
{
    partial class Program
    {
        static void SetupWebServer()
        {
            string ip = WebServer.GetOwnAddress();
            WebServer ws = new WebServer(SendResponse, "http://*:80/webhook/", "https://*:443/webhook/"); //"https://" + ip + ":443/webhook/");
            ws.Run();
            Log("A simple webserver on " + ip + "/webhook/");
        }

        public static byte[] SendResponse(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.QueryString.Count == 0 && request.HttpMethod == "GET")
            {
                return Encoding.UTF8.GetBytes("Empty request");
            }
            else if (request.QueryString["hub.mode"] == "subscribe" && request.QueryString["hub.challenge"] != null)
            {
                if (request.QueryString["hub.verify_token"] == File.ReadAllText("verifytoken.txt"))
                {
                    response.StatusCode = 200;
                    return Encoding.UTF8.GetBytes(request.QueryString["hub.challenge"]);
                }
                else
                {
                    response.StatusCode = 403;
                    return Encoding.UTF8.GetBytes("Failed validation.");
                }
            }
            else if (request.QueryString["id"] != null)
            {
               
            }
            /*else if (request.HttpMethod == "POST")
            {

            }*/

            /*
            else if (request.QueryString["r[]"] != null)
            {
                List<AVRenderer> on = new List<AVRenderer>();
                foreach (string r in request.QueryString.GetValues("r[]"))
                {
                    if (renderers.ContainsKey(r))
                    {
                        on.Add(renderers[r]);
                    }
                }
            }*/
            return Encoding.UTF8.GetBytes("Wrongo");
        }

    }
}