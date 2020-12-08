using System;
using System.Collections.Generic;
using System.Text;

namespace MQTT.Model
{
    public class OpenPoseModel
    {
        public double version { get; set; }

        public List<OpenPoseDataModel> people { get; set; }
    }
    public class OpenPoseDataModel
    {
        public List<int> person_id { get; set; }

        public List<double> pose_keypoints_2d { get; set; }
    }
    public class KeyPointModel
    {
        public double x { get; set; }
        public double y { get; set; }
        public double Confidence { get; set; }
    }

    

    public class ImageDataModel
    {
        public string ID { get; set; }

        public DateTime Date { get; set; }

        public List<KeyPointModel> KeyPointList { get; set; } = new List<KeyPointModel>();

        public bool Useful { get; set; }

        public bool Result { get; set; }
    }
}
