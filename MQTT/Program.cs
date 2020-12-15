using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using MQTT.Model;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
namespace MQTT
{
    class Program
    {
        static string OpenPoseOutPath = @"C:\Users\noneed\Desktop\OpenPose";
        static int stateCounnt = 0;
        public static ImageDataModel GetImageImageModel()
        {
            string CorrectPath = Path.Combine(@"C:\Users\noneed\Desktop", "Correct.json");
            string json = File.ReadAllText(CorrectPath, new System.Text.UTF8Encoding());
            var Object = JsonSerializer.Deserialize<OpenPoseModel>(json);
            List<KeyPointModel> List = new List<KeyPointModel>();
            for (var i = 0; i < 75; i = i + 3)
            {
                List.Add(new KeyPointModel
                {
                    x = Object.people[0].pose_keypoints_2d[i],
                    y = Object.people[0].pose_keypoints_2d[i + 1],
                    Confidence = Object.people[0].pose_keypoints_2d[i + 2]
                });
            }
            ImageDataModel model = new ImageDataModel
            {
                Date = DateTime.Now,
                KeyPointList = List
            };
            return model;
        }

        public static ImageDataModel GetImageData(string ID, string file)
        {
            string json = File.ReadAllText(file, new System.Text.UTF8Encoding());
            var Object = JsonSerializer.Deserialize<OpenPoseModel>(json);
            if (Object.people.Count() == 0)
            {
                Console.WriteLine($"ID:{ID}:無效的json檔");
                return new ImageDataModel() { ID = ID, Date = DateTime.Now };
            }
            List<KeyPointModel> List = new List<KeyPointModel>();

            for (var i = 0; i < 75; i = i + 3)
            {
                List.Add(new KeyPointModel
                {
                    x = Object.people[0].pose_keypoints_2d[i],
                    y = Object.people[0].pose_keypoints_2d[i + 1],
                    Confidence = Object.people[0].pose_keypoints_2d[i + 2]
                });
            }
            Console.WriteLine($"ID:{ID}");
            foreach (var item in List)
            {
                if (item.Confidence > 0)
                {
                    Console.WriteLine($"{(List.IndexOf(item)).ToString("D2")}:x={item.x.ToString("F3")},y={item.y.ToString("F3")},置信度={item.Confidence.ToString("P")}");
                }
            }
            ImageDataModel model = new ImageDataModel
            {
                ID = ID,
                Date = DateTime.Now,
                KeyPointList = List
            };

            return model;
        }

        public static void CheckResult(ImageDataModel DataModel1, ImageDataModel DataModel2)
        {
            Dictionary<int, KeyPointModel> Vector = new Dictionary<int, KeyPointModel>();
            foreach (var point in DataModel2.KeyPointList)
            {
                if(point.Confidence>0.3&& DataModel1.KeyPointList[DataModel2.KeyPointList.IndexOf(point)].Confidence > 0.3)
                {
                    KeyPointModel deviation = new KeyPointModel
                    {
                        x = point.x - DataModel1.KeyPointList[DataModel2.KeyPointList.IndexOf(point)].x,
                        y = point.y - DataModel1.KeyPointList[DataModel2.KeyPointList.IndexOf(point)].y,
                        Confidence = (point.Confidence + DataModel1.KeyPointList[DataModel2.KeyPointList.IndexOf(point)].Confidence) / 2
                    };
                    Vector.Add(DataModel2.KeyPointList.IndexOf(point), deviation);
                }

            }
            Console.WriteLine("與前一張比對");
            int count = 0;
            foreach (var v in Vector)
            {
                Console.WriteLine($"{v.Key.ToString("D2")},偏移量:x={v.Value.x.ToString("F3")},y={v.Value.y.ToString("F3")}");
                if (v.Value.Confidence > 0.3)
                {
                    switch (v.Key)
                    {
                        case 0:
                            break;
                        case 1:
                            break;
                        case 2:
                            break;
                        case 3:
                            if (v.Value.x < 3 && v.Value.y < 3) count++;
                            break;
                        case 4:
                            if (v.Value.x < 3 && v.Value.y < 3) count++;
                            break;
                        case 5:
                            break;
                        case 6:
                            if (v.Value.x < 3 && v.Value.y < 3) count++;
                            break;
                        case 7:
                            if (v.Value.x < 3 && v.Value.y < 3) count++;
                            break;
                        case 15:
                            break;
                        case 16:
                            break;
                        case 17:
                            break;
                        case 18:
                            break;
                    }
                }
            }
            if (count < 4)
            {
                stateCounnt++;
            }
            else
            {
                stateCounnt--;
            };
        }



        public static async Task MQTTTest()
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
            var message = new MqttApplicationMessageBuilder()//傳值
            .WithTopic("/v1/device/24986291524/rawdata").WithContentType("application/json")
            .WithPayload("[{'id':'123456789','value':['hellow2']}]")
            .WithExactlyOnceQoS()
            .WithRetainFlag()
            .Build();
            Task.Run(() => mqttClient.PublishAsync(message, CancellationToken.None));

            mqttClient.UseApplicationMessageReceivedHandler(e =>//收到訊息時
            {
                Console.WriteLine("收到訊息:");
                Console.WriteLine($"主題 : {e.ApplicationMessage.Topic}");
                Console.WriteLine($"Response : {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");//response的value
                var ss = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                SensorDataModel model = JsonSerializer.Deserialize<SensorDataModel>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                Console.WriteLine($"QoS模式 : {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"Retain = {e.ApplicationMessage.Retain}");
            });

        }
        static void Main(string[] args)
        {
            //Task t = MQTTTest();
            //t.Wait();
            int MaxfileIndex = 0;
            string json;
            string filePath = Path.Combine(OpenPoseOutPath, $"{MaxfileIndex.ToString("D12")}_keypoints.json");
            DirectoryInfo OpenPoseDirectory = new DirectoryInfo(OpenPoseOutPath);
            //foreach(var s in file)
            //{
            //    var DataModel = GetImageData(MaxfileIndex.ToString(), s);
            //    CheckResult(CorrectImage, DataModel);
            //    MaxfileIndex++;
            //}
            //Console.ReadKey();
            while (true)
            {
                try
                {
                    var file = Directory.GetFiles(OpenPoseOutPath).OrderByDescending(x => x).Take(2);
                    var Pathsplit1 = file.ElementAt(1).Split("\\");//最新的
                    var Pathsplit2 = file.ElementAt(0).Split("\\");//前一張
                    int.TryParse(Pathsplit2[Pathsplit2.Count() - 1].Split("_")[0], out var fileIndex2);
                    int.TryParse(Pathsplit1[Pathsplit1.Count() - 1].Split("_")[0], out var fileIndex1);
                    if (fileIndex2 > MaxfileIndex)
                    {
                        var DataModel1 = GetImageData(fileIndex1.ToString(), file.ElementAt(1));
                        var DataModel2 = GetImageData(fileIndex2.ToString(), file.ElementAt(0));
                        if (DataModel1.KeyPointList.Count() == 0 || DataModel2.KeyPointList.Count() == 0)
                        {
                            break;
                        }
                        CheckResult(DataModel1, DataModel2);
                        MaxfileIndex = fileIndex2;
                    }
                }
                catch (Exception ex)
                {

                }
                if (stateCounnt >= 2)
                {
                    Console.WriteLine("醒醒!!!!!!");
                }
            }
        }
    }
}
//FUCK YOU GITHUB