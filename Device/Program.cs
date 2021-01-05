using Device.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

namespace Server
{
    class Program
    {
        static string OpenPoseOutPath = @"D:\疲勞模擬影片\正常JSON";
        static int stateCount = 0;
        static int check2Count = 0;
        static int check3Count = 0;
        static int check4Count = 0;
        static int check5Count = 0;

        static int SubStatusCount = 0;
        static int SubCheckCount = 0;

        static List<string> ResultData;
        static readonly HttpClient client = new HttpClient();


        /// <summary>
        /// ID為檔案編號，file為檔案完整路徑(包含檔案名稱)
        /// </summary>
        public static ImageDataModel GetImageData(string ID, string file,bool IsNow = false)
        {
            string json = File.ReadAllText(file, new System.Text.UTF8Encoding());
            var Object = System.Text.Json.JsonSerializer.Deserialize<OpenPoseModel>(json);
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

            foreach (var item in List)
            {
                if (item.Confidence > 0)
                {
                    //Console.WriteLine($"{(List.IndexOf(item)).ToString("D2")}:x={item.x.ToString("F3")},y={item.y.ToString("F3")},置信度={item.Confidence.ToString("P")}");
                }
            }
            ImageDataModel model = new ImageDataModel
            {
                ID = ID,
                Date = DateTime.Now,
                KeyPointList = List
            };
            if (IsNow)
            {
                Console.WriteLine($"ID:{ID}");
                List<string> msg = new List<string>
                {
                    $"{model.ID}",$"zeroX={List[0].x}",$"zeroY={List[0].y}",$"oneX={List[1].x}",$"oneY={List[1].y}",$"twoX={List[2].x}",$"twoY={List[2].y}",$"threeX={List[3].x}",$"threeY={List[3].y}",
                    $"fourX={List[4].x}", $"fourY={List[4].y}",$"fiveX={List[5].x}",$"fiveY={List[5].y}",$"sixX={List[6].x}",$"sixY={List[6].y}",
                    $"sevenX={List[7].x}", $"sevenY={List[7].y}",$"eightX={List[8].x}",$"eightY={List[8].y}",
                    $"fifteenX={List[15].x}",$"fifteenY={List[15].y}",$"sixteenX={List[16].x}",$"sixteenY={List[16].y}"
                    ,$"seventeenX={List[17].x}",$"seventeenY={List[17].y}",$"eighteenX={List[18].x}",$"eighteenY={List[18].y}",model.Date.ToString()
                };
                Task t = MQTTPublishData(msg);
            }
            return model;
        }


        public static void CheckResult(ImageDataModel PreModel, ImageDataModel NowModel)
        {
            ResultData = new List<string> {
               $"{NowModel.ID}"
            };
            stateCount += check(PreModel.KeyPointList, NowModel.KeyPointList);
            stateCount += check2(PreModel.KeyPointList, NowModel.KeyPointList);
            stateCount += check3(NowModel.KeyPointList);
            stateCount += check4(PreModel.KeyPointList, NowModel.KeyPointList);
            stateCount += check5(NowModel.KeyPointList);

            SubStatusCount += SubCheck(NowModel.KeyPointList);
            SubStatusCount += SubCheck2(PreModel.KeyPointList, NowModel.KeyPointList);
            SubStatusCount += SubCheck3(NowModel.KeyPointList);
            ResultData.Add("StatusCount="+ stateCount);
            ResultData.Add("SubStatusCount="+SubStatusCount);
            if (SubStatusCount >= 100)
            {
                ResultData.Add("IsTired=Y");
                ResetStatus();
            }
            else
            {
                ResultData.Add("IsTired=N");
            }
            if (stateCount >= 100)
            {
                Console.WriteLine("醒醒!!");
            }
            ResultData.Add(NowModel.Date.ToString());
            Task t = MQTTPublishResult(ResultData);
        }

        #region 加分表
        //沒轉方向盤
        public static int check(List<KeyPointModel> pre, List<KeyPointModel> now)
        {
            if (GetTwoPointLength(pre[4], now[4]) < 20 && GetTwoPointLength(pre[7], now[7]) < 20)
            {
                ResultData.Add("IsRollwheel=N");
                Console.WriteLine("沒轉方向盤");
                return 2;
            }
            else
            {
                ResultData.Add("IsRollwheel=Y");
                return 0;
            }
        }

        //點頭
        public static int check2(List<KeyPointModel> pre, List<KeyPointModel> now)
        {
            double Lengthdeviation = Math.Abs(GetTwoPointLength(pre[0], pre[1]) - GetTwoPointLength(now[0], now[1]));
            if (Lengthdeviation > 50)
            {
                ResultData.Add("IsNod=Y");
                Console.WriteLine("點頭");
                check2Count++;
                return 10 * check2Count;
            }
            else
            {
                ResultData.Add("IsNod=N");
                check2Count = 0;
                return 0;
            }
        }

        //兩手離開方向盤
        public static int check3(List<KeyPointModel> ImageModel)
        {
            if (ImageModel[4].Confidence == 0 && ImageModel[7].Confidence == 0)
            {
                ResultData.Add("IsHandleavewheel=Y");
                Console.WriteLine("兩手離開方向盤");
                check3Count++;
                return 10 * check3Count;
            }
            else
            {
                ResultData.Add("IsHandleavewheel=N");
                check3Count = 0;
                return 0;
            }
        }

        //低頭
        public static int check4(List<KeyPointModel> pre, List<KeyPointModel> now)
        {
            double Lengthdeviation = Math.Abs(GetTwoPointLength(pre[0], pre[1]) - GetTwoPointLength(now[0], now[1]));
            if (GetTwoPointLength(now[0], now[1]) < 50 && Lengthdeviation < 50)
            {
                ResultData.Add("IsHeadDown=Y");
                Console.WriteLine("低頭");
                check4Count++;
                return 10 * check4Count;
            }
            else
            {
                ResultData.Add("IsHeadDown=N");
                check4Count = 0;
                return 0;
            }
        }

        //手遮住嘴巴打哈欠
        public static int check5(List<KeyPointModel> now)
        {
            if (GetTwoPointLength(now[4], now[0]) < 100 || GetTwoPointLength(now[7], now[0]) < 100)
            {
                ResultData.Add("IsYawn=Y");
                Console.WriteLine("打哈欠");
                return 10 ;
            }
            else
            {
                ResultData.Add("IsYawn=N");
                return 0;
            }
        }
        #endregion

        #region 扣分重製表
        //轉頭
        public static int SubCheck(List<KeyPointModel> now)
        {
            if ((now[17].Confidence == 0 && now[15].Confidence != 0 && now[0].Confidence != 0 && now[16].Confidence != 0 && now[18].Confidence != 0)
                || (now[17].Confidence != 0 && now[15].Confidence != 0 && now[0].Confidence != 0 && now[16].Confidence != 0 && now[18].Confidence == 0))
            {
                ResultData.Add("IsRollHead=Y");
                Console.WriteLine("轉頭");
                return 20;
            }
            else
            {
                ResultData.Add("IsRollHead=N");
                return 0;
            }
        }

        //轉動方向盤
        public static int SubCheck2(List<KeyPointModel> pre, List<KeyPointModel> now)
        {
            if (GetTwoPointLength(pre[4], now[4]) > 20 || GetTwoPointLength(pre[7], now[7]) > 20)
            {
                ResultData.Add("IsRollwhee=Y");
                Console.WriteLine("轉動方向盤");
                return 15;
            }
            else
            {
                ResultData.Add("IsRollwheel=N");
                return 0;
            }
        }

        public static int SubCheck3(List<KeyPointModel> now)
        {
            if (now[0].Confidence == 0 && now[1].Confidence == 0 && now[2].Confidence == 0 && now[3].Confidence == 0 && now[4].Confidence == 0 && now[5].Confidence == 0 &&
                now[6].Confidence == 0 && now[7].Confidence == 0 && now[15].Confidence == 0 && now[16].Confidence == 0 && now[17].Confidence == 0 && now[18].Confidence == 0)
            {
                ResultData.Add("IsNotDrive=Y");
                Console.WriteLine("不在駕駛狀態");
                return 100;
            }
            else
            {
                ResultData.Add("IsNotDrive=Y");
                return 0;
            }
        }
        #endregion

        #region 方法
        public static double GetTwoPointLength(KeyPointModel p1, KeyPointModel p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
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

        #region MQTT 發布

        public static async Task MQTTPublishData(List<string> msg)
        {
            string value = "";
            if (msg.Count() > 0)
            {
                foreach(var s in msg)
                {
                    value += $",'{s}'";
                }
                value =  value.Substring(1);
            }
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("iot.cht.com.tw", 1883).WithCredentials("PKXRT95R2GFC4BKW4P", "PKXRT95R2GFC4BKW4P")
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311).Build();
            await mqttClient.ConnectAsync(options, CancellationToken.None);
            await mqttClient.SubscribeAsync("/v1/device/24986291524/sensor/123456789/rawdata", MqttQualityOfServiceLevel.AtLeastOnce);
            string payload = "[{'id':'123456789','value':["+value+"]}]";
            var message = new MqttApplicationMessageBuilder()//傳值
            .WithTopic("/v1/device/24986291524/rawdata").WithContentType("application/json")
            .WithPayload(payload)
            .WithExactlyOnceQoS()
            .WithRetainFlag()
            .Build();
            Task.Run(() => mqttClient.PublishAsync(message, CancellationToken.None));
        }

        public static async Task MQTTPublishResult(List<string> msg)
        {
            string value = "";
            if (msg.Count() > 0)
            {
                foreach (var s in msg)
                {
                    value += $",'{s}'";
                }
                value = value.Substring(1);
            }
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("iot.cht.com.tw", 1883).WithCredentials("PKXRT95R2GFC4BKW4P", "PKXRT95R2GFC4BKW4P")
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311).Build();
            await mqttClient.ConnectAsync(options, CancellationToken.None);
            await mqttClient.SubscribeAsync("/v1/device/24986291524/sensor/ZAWARUDO/rawdata", MqttQualityOfServiceLevel.AtLeastOnce);
            string payload = "[{'id':'ZAWARUDO','value':[" + value + "]}]";
            var message = new MqttApplicationMessageBuilder()//傳值
            .WithTopic("/v1/device/24986291524/rawdata").WithContentType("application/json")
            .WithPayload(payload)
            .WithExactlyOnceQoS()
            .WithRetainFlag()
            .Build();
            Task.Run(() => mqttClient.PublishAsync(message, CancellationToken.None));
        }
        #endregion

        #region save image
        public static void SendImage(string filename,ImageDataModel model)
        {
            filename = filename.Replace("keypoints.json", "rendered.png");
            client.DefaultRequestHeaders.Clear();
            //client.BaseAddress = new Uri(@"");
            client.DefaultRequestHeaders.Add("CK", "DKC395PCYXM9EPZ4XS");
            List<string> value = new List<string>
            {
                model.ID,model.Date.ToString()
            };
            string json = JsonConvert.SerializeObject(new {id="Car_Image",value=value});
            // 將轉為 string 的 json 依編碼並指定 content type 存為 httpcontent
            HttpContent meta = new StringContent(json, Encoding.UTF8, "application/json");
            byte[] image = File.ReadAllBytes(filename);
            var fileStream = new StreamContent(new MemoryStream(image));
            fileStream.Headers.Add("Content-Type", "image/jpeg");
            var content = new MultipartFormDataContent();
            content.Add(meta,"meta");
            content.Add(fileStream, "img","test.png");
            client.PostAsync(@"https://iot.cht.com.tw/iot/v1/device/24986291524/snapshot", content);

        }
        #endregion

        static void Main(string[] args)
        {
            int MaxfileIndex = 0;
            string json;
            string filePath = Path.Combine(OpenPoseOutPath, $"{MaxfileIndex.ToString("D12")}_keypoints.json");
            DirectoryInfo OpenPoseDirectory = new DirectoryInfo(OpenPoseOutPath);
            while (true)
            {
                try
                {
                    var file = OpenPoseDirectory.GetFiles("*.json").OrderByDescending(x => x.CreationTime).Take(2);
                    if (file.Count() < 2) continue;
                    var Pathsplit1 = file.ElementAt(1).FullName.Split("\\");
                    var Pathsplit2 = file.ElementAt(0).FullName.Split("\\");
                    int.TryParse(Pathsplit2[Pathsplit2.Count() - 1].Split("_")[1], out var fileIndex2);
                    int.TryParse(Pathsplit1[Pathsplit1.Count() - 1].Split("_")[1], out var fileIndex1);
                    if (fileIndex2 > MaxfileIndex)
                    {
                        var DataModel1 = GetImageData(fileIndex1.ToString(), file.ElementAt(1).FullName);
                        var DataModel2 = GetImageData(fileIndex2.ToString(), file.ElementAt(0).FullName, true);
                        if (DataModel1.KeyPointList.Count() == 0 || DataModel2.KeyPointList.Count() == 0)
                        {
                            continue;
                        }
                        CheckResult(DataModel1, DataModel2);
                        SendImage(file.ElementAt(0).FullName, DataModel2);
                        MaxfileIndex = fileIndex2;
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex);
                }
            }
        }
    }
}
