using System;
using System.Net.Mail;
using System.Timers;
using System.IO;
using System.Threading;

namespace MQTTClient
{
    partial class Program
    {
        static string smtpUser = "";
        static string smtpPass = "";
        static char tab = '	';


        static void Main(string[] args)
        {
            string[] lines = File.ReadAllLines("smtpuserpass.txt");
            smtpUser = lines[0];
            smtpPass = lines[1];

            // Make some space in the logs
            Log("*");
            Log("* New session started!! *");
            Log("*");

            TryUntilConnectToMQTT();

            while (true)
            {
                string line = Console.ReadLine();
                if (line == "open")
                {
                    SendEmail("Instructed to manually open the Garage Door");
                    moveGarageDoor(true);
                }
                else if (line == "close")
                {
                    SendEmail("Instructed to manually close the Garage Door");
                    moveGarageDoor(false);
                }
            }
        }
        
        static bool isDoorOpen = false;
        static System.Timers.Timer closeTimer = new System.Timers.Timer();

        static void DoorStatusUpdate(bool isNowOpen, DateTime when)
        {
            Log("Door is now " + (isNowOpen ? "opened" : "closed"));

            if (isDoorOpen != isNowOpen)
            {
                if (isNowOpen)
                    MakeSureItCloses();
                else
                    StopCheckingIfItClosed();

                SendEmail("Garage Door " + (isNowOpen ? "Opened" : "Closed"), when);

                isDoorOpen = isNowOpen;
            }
        }

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
            closeTimer.AutoReset = true;
            closeTimer.Enabled = true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Log("Sending the close event to the garage door");
            SendEmail("Trying to manually close the Garage Door");
            moveGarageDoor(false);
        }

        static void Log(string what)
        {
            string log = GetLocalTime().ToString() + tab + what;

            Console.WriteLine(log);
            File.AppendAllText("log.txt", log + "\r\n");
        }

        static DateTime GetLocalTime(string from)
        {
            DateTime utc = DateTime.UtcNow;
            if (!DateTime.TryParse(from, out utc))
            {
                Log("Couldn't parse time: " + from);
                utc = DateTime.UtcNow;
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

        static void SendEmail(string message)
        {
            SendEmail(message, GetLocalTime());
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
                mailMsg.From = new MailAddress("theelliotcastle@gmail.com", "Elliot Castle");

                // Subject and multipart/alternative Body
                mailMsg.Subject = message;
                mailMsg.Body = body;
                mailMsg.IsBodyHtml = false;

                // Init SmtpClient and send
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                smtpClient.Credentials = credentials;

                Log("Sending email: " + message);
                smtpClient.Send(mailMsg);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

        }
    }
}
