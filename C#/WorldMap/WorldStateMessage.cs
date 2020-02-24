using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldMap
{

    public partial class WorldStateMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("teamName")]
        public string TeamName { get; set; }

        [JsonProperty("intention")]
        public string Intention { get; set; }

        [JsonProperty("robots")]
        public Robot[] Robots { get; set; }

        [JsonProperty("balls")]
        public Ball[] Balls { get; set; }

        [JsonProperty("obstacles")]
        public Obstacle[] Obstacles { get; set; }

        [JsonProperty("ageMs")]
        public long AgeMs { get; set; }
    }

    public partial class Ball
    {
        [JsonProperty("position")]
        public double?[] Position { get; set; }

        [JsonProperty("velocity")]
        public long?[] Velocity { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }

    public partial class Obstacle
    {
        [JsonProperty("position")]
        public double[] Position { get; set; }

        [JsonProperty("velocity")]
        public long[] Velocity { get; set; }

        [JsonProperty("radius")]
        public double Radius { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }

    public partial class Robot
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("pose")]
        public double[] Pose { get; set; }

        [JsonProperty("targetPose")]
        public double[] TargetPose { get; set; }

        [JsonProperty("velocity")]
        public double[] Velocity { get; set; }

        [JsonProperty("intention")]
        public string Intention { get; set; }

        [JsonProperty("batteryLevel")]
        public long BatteryLevel { get; set; }

        [JsonProperty("ballEngaged")]
        public long BallEngaged { get; set; }
    }

    public partial class WorldStateMessage
    {
        public static WorldStateMessage FromJson(string json) => JsonConvert.DeserializeObject<WorldStateMessage>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this WorldStateMessage self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
