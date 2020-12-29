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
        static string OpenPoseOutPath = @"D:\openpose\examples\media_out";
        static int stateCount = 0;
        static int check2Count = 0;
        static int check3Count = 0;
        static int check4Count = 0;
        static int check5Count = 0;

        static int SubStatusCount = 0;
        static int SubCheckCount = 0;
        /// <summary>
        /// ID為檔案編號，file為檔案完整路徑(包含檔案名稱)
        /// </summary>
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


        public static void CheckResult(ImageDataModel PreModel, ImageDataModel NowModel)
        {
            stateCount += check(PreModel.KeyPointList, NowModel.KeyPointList);
            stateCount += check2(PreModel.KeyPointList, NowModel.KeyPointList);
            stateCount += check3(NowModel.KeyPointList);
            stateCount += check4(PreModel.KeyPointList,NowModel.KeyPointList);
            stateCount += check5(NowModel.KeyPointList);

            SubStatusCount += SubCheck(NowModel.KeyPointList);
            SubStatusCount += SubCheck2(PreModel.KeyPointList, NowModel.KeyPointList);
            SubStatusCount += SubCheck3(NowModel.KeyPointList);
            if(SubStatusCount >= 100)
            {
                ResetStatus();
            }
            if (stateCount >= 100)
            {
                Console.WriteLine("醒醒!!");
            }
        }

        #region 加分表
        //沒轉方向盤
        public static int check(List<KeyPointModel> pre, List<KeyPointModel> now)
        {
            if (GetTwoPointLength(pre[4], now[4]) < 20 && GetTwoPointLength(pre[7], now[7]) < 20)
            {
                return 5;
            }
            else
            {
                return 0;
            }
        }

        //點頭
        public static int check2(List<KeyPointModel> pre, List<KeyPointModel> now)
        {
            double Lengthdeviation = Math.Abs(GetTwoPointLength(pre[0],pre[1])-GetTwoPointLength(now[0],now[1]));
            if (Lengthdeviation > 50)
            {
                check2Count++;
                return 10*check2Count;
            }
            else
            {
                check2Count = 0;
                return 0;
            }
        }

        //兩手離開方向盤
        public static int check3(List<KeyPointModel> ImageModel)
        {
            if (ImageModel[4].Confidence == 0 && ImageModel[7].Confidence == 0)
            {
                check3Count++;
                return 10*check3Count;
            }
            else
            {
                check3Count = 0;
                return 0;
            }
        }

        //低頭
        public static int check4(List<KeyPointModel> pre, List<KeyPointModel> now)
        {
            double Lengthdeviation = Math.Abs(GetTwoPointLength(pre[0], pre[1]) - GetTwoPointLength(now[0], now[1]));
            if (GetTwoPointLength(now[0], now[1]) < 50&&Lengthdeviation<50)
            {
                check4Count++;
                return 10 * check4Count;
            }
            else
            {
                check4Count = 0;
                return 0;
            }
        }

        //手遮住嘴巴打哈欠
        public static int check5(List<KeyPointModel> now)
        {
            if (GetTwoPointLength(now[4], now[0]) < 50 || GetTwoPointLength(now[7], now[0])<50)
            {
                check5Count++;
                return 10 * check5Count;
            }
            else
            {
                check5Count = 0;
                return 0;
            }
        }
        #endregion

        #region 扣分重製表
        //轉頭
        public static int SubCheck(List<KeyPointModel> now)
        {
            if((now[17].Confidence==0&&now[15].Confidence!=0&&now[0].Confidence!=0&&now[16].Confidence!=0&&now[18].Confidence!=0)
                || (now[17].Confidence != 0 && now[15].Confidence != 0 && now[0].Confidence != 0 && now[16].Confidence != 0 && now[18].Confidence == 0))
            {
                return 10;
            }
            else
            {
                return 0;
            }
        }

        //轉動方向盤
        public static int SubCheck2(List<KeyPointModel> pre, List<KeyPointModel> now)
        {
            if (GetTwoPointLength(pre[4], now[4]) >20 && GetTwoPointLength(pre[7], now[7]) > 20)
            {
                return 5;
            }
            else
            {
                return 0;
            }
        }

        public static int SubCheck3(List<KeyPointModel> now)
        {
            if(now[0].Confidence==0&& now[1].Confidence == 0 && now[2].Confidence == 0 && now[3].Confidence == 0 && now[4].Confidence == 0 && now[5].Confidence == 0 &&
                now[6].Confidence == 0 && now[7].Confidence == 0 && now[15].Confidence == 0 && now[16].Confidence == 0 && now[17].Confidence == 0 && now[18].Confidence == 0)
            {
                return 100;
            }
            else
            {
                return 0;
            }
        }
        #endregion

        #region 方法
        public static double GetTwoPointLength(KeyPointModel p1,KeyPointModel p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x,2) + Math.Pow(p1.y - p2.y,2));
        }

        public static KeyPointModel GetTwoPointVector(KeyPointModel start, KeyPointModel end)
        {
            return new KeyPointModel { x = end.x - start.x, y = end.y - start.y };
        }

        public static double GetTwoPointSlop(KeyPointModel start, KeyPointModel end)
        {
            return (end.y - start.y) / (end.x - start.x);
        }

        public static void ResetStatus()
        {
            stateCount = 0;
            SubStatusCount = 0;
        }

        #endregion

        #region MQtt
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

        #endregion

        static void Main(string[] args)
        {
            //Task t = MQTTTest();
            //t.Wait();
            int MaxfileIndex = 0;
            string json;
            string filePath = Path.Combine(OpenPoseOutPath, $"{MaxfileIndex.ToString("D12")}_keypoints.json");
            DirectoryInfo OpenPoseDirectory = new DirectoryInfo(OpenPoseOutPath);
            while (true)
            {
                try
                {
                    var file = OpenPoseDirectory.GetFiles("*.json").OrderByDescending(x=>x.CreationTime).Take(2);
                    if (file.Count() < 2) continue;
                    var Pathsplit1 = file.ElementAt(1).FullName.Split("\\");//最新的
                    var Pathsplit2 = file.ElementAt(0).FullName.Split("\\");//前一張
                    int.TryParse(Pathsplit2[Pathsplit2.Count() - 1].Split("_")[0], out var fileIndex2);
                    int.TryParse(Pathsplit1[Pathsplit1.Count() - 1].Split("_")[0], out var fileIndex1);
                    if (fileIndex2 > MaxfileIndex)
                    {
                        var DataModel1 = GetImageData(fileIndex1.ToString(), file.ElementAt(1).FullName);
                        var DataModel2 = GetImageData(fileIndex2.ToString(), file.ElementAt(0).FullName);
                        MaxfileIndex = fileIndex2;
                        if (DataModel1.KeyPointList.Count() == 0 || DataModel2.KeyPointList.Count() == 0)
                        {
                            continue;
                        }
                        CheckResult(DataModel1, DataModel2);
                    }  
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
