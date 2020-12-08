using System;
using System.Collections.Generic;
using System.Text;

namespace MQTT.Model
{
    class SensorDataModel
    {
        public string id { get; set; }

        public List<string> value { get; set; }
    }
}
