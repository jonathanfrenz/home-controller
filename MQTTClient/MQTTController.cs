using System;
using System.Linq;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.IO;
using System.Threading;

namespace MQTTClient
{
    partial class Program
    {
        static MqttClient client;
        static bool ConnectToMQTT()
        {
            Log("Trying to connect to MQTT");
            try
            {
                // create client instance 
                client = new MqttClient("m13.cloudmqtt.com", 22183, true, null);

                // register to message received 
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                client.ConnectionClosed += Client_ConnectionClosed;

                string clientId = Guid.NewGuid().ToString();

                string[] lines = File.ReadAllLines("userpass.txt");
                client.Connect(clientId, lines[0], lines[1]);

                //SendEmail();

                // subscribe to the topic "/home/temperature" with QoS 2 
                client.Subscribe(new string[] { "iot/up" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            catch (Exception e)
            {
                Log("Error connecting to MQTT: " + e.Message);
                Log(e.StackTrace);
                return false;
            }

            Log("Successfully connected to MQTT");

            return true;
        }

        private static void Client_ConnectionClosed(object sender, EventArgs e)
        {
            Log("Lost MQTT connection. Trying to reconnect...");

            TryUntilConnectToMQTT();

            Log("Apparently reconnected!");
        }

        static void TryUntilConnectToMQTT()
        {
            while (!ConnectToMQTT())
            {
                Thread.Sleep(1000);
            }
        }

        static void moveGarageDoor(bool isOpen)
        {
            string msg = "6-BARRIER OPERATOR-user-bool-1-0\n" + (isOpen ? "true" : "false");
            client.Publish("iot/down", UTF8Encoding.UTF8.GetBytes(msg));
        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = UTF8Encoding.UTF8.GetString(e.Message);
            Log("Msg: " + msg);

            if (msg.Contains("6	BARRIER OPERATOR	0"))
            {
                string[] split = msg.Split(tab);
                bool isNowOpen = false;
                if (bool.TryParse(split.Last(), out isNowOpen))
                {
                    DoorStatusUpdate(isNowOpen, GetLocalTime(split.First()));
                }
            }
        }
    }
}