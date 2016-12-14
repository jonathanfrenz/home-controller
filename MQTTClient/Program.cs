using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Net.Mail;
using System.Net.Mime;
using System.Timers;
using System.IO;

namespace MQTTClient
{
    class Program
    {
        static MqttClient client;
        static string smtpUser = "";
        static string smtpPass = "";

        static void Main(string[] args)
        {
            string[] lines = File.ReadAllLines("smtpuserpass.txt");
            smtpUser = lines[0];
            smtpPass = lines[1];

            // create client instance 
            client = new MqttClient("m13.cloudmqtt.com", 22183, true, null);

            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = Guid.NewGuid().ToString();

            lines = File.ReadAllLines("userpass.txt");
            client.Connect(clientId, lines[0], lines[1]);

            //SendEmail();

            // subscribe to the topic "/home/temperature" with QoS 2 
            client.Subscribe(new string[] { "iot/up" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            
            while (true)
            {
                string line = Console.ReadLine();
                if (line == "open")
                {
                    SendEmail("Instructed to manually close the Garage Door", GetLocalTime());
                    moveGarageDoor(true);
                }
                else if (line == "close")
                {
                    SendEmail("Instructed to manually open the Garage Door", GetLocalTime());
                    moveGarageDoor(false);
                }
            }
        }

        static void moveGarageDoor(bool isOpen)
        {
            string msg = "6-BARRIER OPERATOR-user-bool-1-0\n" + (isOpen ? "true" : "false");
            client.Publish("iot/down", UTF8Encoding.UTF8.GetBytes(msg));
        }

        static bool isDoorOpen = false;
        static System.Timers.Timer closeTimer = new Timer();

        static void StopCheckingIfItClosed()
        {
            closeTimer.Enabled = false;
        }

        static void MakeSureItCloses()
        {
            if (closeTimer.Enabled)
                closeTimer.Enabled = false;

            closeTimer = new System.Timers.Timer();
            closeTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            closeTimer.Interval = 1000 * 60 * 5;
            closeTimer.AutoReset = false;
            closeTimer.Enabled = true;
        }
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Sending the close event to the garage door");
            SendEmail("Trying to manually close the Garage Door", GetLocalTime());
            moveGarageDoor(false);
        }

        static DateTime GetLocalTime(string from)
        {
            DateTime utc = DateTime.UtcNow;
            if (!DateTime.TryParse(from, out utc))
            {
                Console.WriteLine("Couldn't parse time: " + from);
            }

            return GetLocalTime(utc);
        }

        static DateTime GetLocalTime(DateTime utc)
        {
            TimeZoneInfo hwZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, hwZone);
        }
        static DateTime GetLocalTime()
        {
            return GetLocalTime(DateTime.UtcNow);
        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = UTF8Encoding.UTF8.GetString(e.Message);
            Console.WriteLine(GetLocalTime().ToString() + ": " + msg);

            if (msg.Contains("6	BARRIER	OPERATOR	0"))
            {
                string[] split = msg.Split('	');
                bool isNowOpen = false;
                if (bool.TryParse(split.Last(), out isNowOpen))
                {
                    if (isDoorOpen != isNowOpen)
                    {
                        if (isNowOpen)
                            MakeSureItCloses();
                        else
                            StopCheckingIfItClosed();
                        
                        SendEmail("Garage Door " + (isNowOpen ? "Opened" : "Closed"), GetLocalTime(split.First()));

                        isDoorOpen = isNowOpen;
                    }
                }
            }
            // handle message received 
        }

        static void SendEmail(string message, DateTime body)
        {
            SendEmail(message, body.ToString());
        }

        static void SendEmail(string message, string body)
        {
            try
            {
                MailMessage mailMsg = new MailMessage();

                // To
                mailMsg.To.Add(new MailAddress("jonathanfrenz@gmail.com", "Jonathan Frenz"));

                // From
                mailMsg.From = new MailAddress("jonathanfrenz@gmail.com", "Elliot Castle");

                // Subject and multipart/alternative Body
                mailMsg.Subject = message;
                mailMsg.Body = body;
                mailMsg.IsBodyHtml = false;

                // Init SmtpClient and send
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                smtpClient.Credentials = credentials;

                smtpClient.Send(mailMsg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
