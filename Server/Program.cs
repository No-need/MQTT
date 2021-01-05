using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Server.Model;
using System.Data.SqlClient;

namespace Server
{
    class Program
    {

        static SqlConnection con;
        #region MQtt
        public static async Task MQTTLink()
        {
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("iot.cht.com.tw", 1883).WithCredentials("PKXRT95R2GFC4BKW4P", "PKXRT95R2GFC4BKW4P")
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311).Build();
            UnicodeEncoding uniEncoding = new UnicodeEncoding();
            byte[] firstString = uniEncoding.GetBytes("");
            await mqttClient.ConnectAsync(options, CancellationToken.None);
            await mqttClient.SubscribeAsync("/v1/device/24986291524/sensor/123456789/rawdata", MqttQualityOfServiceLevel.AtLeastOnce);
            bool firstFlag = false;
            mqttClient.UseApplicationMessageReceivedHandler(e =>//收到訊息時
            {
                if (firstFlag)
                {
                    Console.WriteLine("收到訊息:");
                    Console.WriteLine($"主題 : {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"Response : {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");//response的value
                    var ss = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    SensorDataModel model = JsonSerializer.Deserialize<SensorDataModel>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                    Console.WriteLine($"QoS模式 : {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"Retain = {e.ApplicationMessage.Retain}");
                    InsertDriveData(model.value);
                }
                else
                {
                    firstFlag = true;
                }
            });
        }

        public static async Task MQTTLinkResult()
        {
            var factory = new MqttFactory();
            var mqttClient2 = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("iot.cht.com.tw", 1883).WithCredentials("PKXRT95R2GFC4BKW4P", "PKXRT95R2GFC4BKW4P")
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311).Build();
            UnicodeEncoding uniEncoding = new UnicodeEncoding();
            byte[] firstString = uniEncoding.GetBytes("");
            await mqttClient2.ConnectAsync(options, CancellationToken.None);
            await mqttClient2.SubscribeAsync("/v1/device/24986291524/sensor/ZAWARUDO/rawdata", MqttQualityOfServiceLevel.AtLeastOnce);
            bool firstFlag = false;
            mqttClient2.UseApplicationMessageReceivedHandler(e =>//收到訊息時
            {
                if (firstFlag)
                {
                    Console.WriteLine("收到訊息:");
                    Console.WriteLine($"主題 : {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"Response : {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");//response的value
                    var ss = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    SensorDataModel model = JsonSerializer.Deserialize<SensorDataModel>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                    Console.WriteLine($"QoS模式 : {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"Retain = {e.ApplicationMessage.Retain}");
                    InsertDriveResult(model.value);
                }
                else
                {
                    firstFlag = true;
                }

            });
        }
        #endregion

        #region sql

        public static void InsertDriveData(List<string> dataList)
        {
            string sql = @"INSERT INTO drivedata 
        (ID, zeroX, zeroY, oneX, oneY, twoX, twoY,
threeX, threeY, fourX, fourY, fiveX, fiveY, sixX, sixY,
sevenX, sevenY, eightX, eightY, fifteenX, fifteenY, sixteenX,
sixteenY, seventeenX, seventeenY, eighteenX, eighteenY, Date) 
        VALUES(@value1, @value2, @value3,@value4, @value5, @value6,@value7,@value8,@value9,
@value10,@value11,@value12,@value13,@value14,@value15,@value16,@value17,@value18,@value19,@value20,
@value21,@value22,@value23,@value24,@value25,@value26,@value27,@value28) ";
            SqlCommand SqlCmd = new SqlCommand(sql, con);
            SqlCmd.Parameters.AddWithValue("@value1", dataList[0]);
            SqlCmd.Parameters.AddWithValue("@value2", dataList.Where(x => x.Contains("zeroX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value3", dataList.Where(x => x.Contains("zeroY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value4", dataList.Where(x => x.Contains("oneX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value5", dataList.Where(x => x.Contains("oneY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value6", dataList.Where(x => x.Contains("twoX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value7", dataList.Where(x => x.Contains("twoY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value8", dataList.Where(x => x.Contains("threeX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value9", dataList.Where(x => x.Contains("threeY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value10", dataList.Where(x => x.Contains("fourX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value11", dataList.Where(x => x.Contains("fourY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value12", dataList.Where(x => x.Contains("fiveX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value13", dataList.Where(x => x.Contains("fiveY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value14", dataList.Where(x => x.Contains("sixX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value15", dataList.Where(x => x.Contains("sixY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value16", dataList.Where(x => x.Contains("sevenX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value17", dataList.Where(x => x.Contains("sevenY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value18", dataList.Where(x => x.Contains("eightX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value19", dataList.Where(x => x.Contains("eightY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value20", dataList.Where(x => x.Contains("fifteenX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value21", dataList.Where(x => x.Contains("fifteenY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value22", dataList.Where(x => x.Contains("sixteenX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value23", dataList.Where(x => x.Contains("sixteenY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value24", dataList.Where(x => x.Contains("seventeenX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value25", dataList.Where(x => x.Contains("seventeenY")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value26", dataList.Where(x => x.Contains("eighteenX")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value27", dataList.Where(x => x.Contains("eighteenY")).FirstOrDefault().Split('=')[1]);
            DateTime.TryParse(dataList[27], out var date);
            SqlCmd.Parameters.AddWithValue("@value28", date);
            SqlCmd.ExecuteNonQuery();
        }

        public static void InsertDriveResult(List<string> dataList)
        {
            string sql = @"INSERT INTO [dbo].[IdentifyResult]
           ([ID]
           ,[IsTired]
           ,[IsRollwheel]
           ,[IsNod]
           ,[IsHandleavewheel]
           ,[IsHeadDown]
           ,[IsYawn]
           ,[IsRollHead]
           ,[IsNotDrive]
           ,[StatusCount],[SubStatusCount],[Date])
     VALUES(@value1,@value2,@value3,@value4, @value5, @value6,@value7,@value8,@value9,@value10,@value11,@value12)";
            SqlCommand SqlCmd = new SqlCommand(sql, con);
            SqlCmd.Parameters.AddWithValue("@value1", dataList[0]);
            SqlCmd.Parameters.AddWithValue("@value2", dataList.Where(x => x.Contains("IsTired")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value3", dataList.Where(x => x.Contains("IsRollwheel")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value4", dataList.Where(x => x.Contains("IsNod")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value5", dataList.Where(x => x.Contains("IsHandleavewheel")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value6", dataList.Where(x => x.Contains("IsHeadDown")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value7", dataList.Where(x => x.Contains("IsYawn")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value8", dataList.Where(x => x.Contains("IsRollHead")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value9", dataList.Where(x => x.Contains("IsNotDrive")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value10", dataList.Where(x => x.Contains("StatusCount")).FirstOrDefault().Split('=')[1]);
            SqlCmd.Parameters.AddWithValue("@value11", dataList.Where(x => x.Contains("SubStatusCount")).FirstOrDefault().Split('=')[1]);
            DateTime.TryParse(dataList[12], out var date);
            SqlCmd.Parameters.AddWithValue("@value12", date);
            SqlCmd.ExecuteNonQuery();

        }
        #endregion
        static void Main(string[] args)
        {
            con = new SqlConnection(@"Server=localhost\SQLEXPRESS;Database=tireddrive;Trusted_Connection=True;");
            con.Open();
            Task t = MQTTLink();
            Task t2 = MQTTLinkResult();
            t.Wait();
            t2.Wait();

            while (true)
            {

            }
        }
    }
}
