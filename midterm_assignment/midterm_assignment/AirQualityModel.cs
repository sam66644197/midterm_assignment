using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace midterm_assignment
{
    public class AirQualityResponse
    {
        [JsonPropertyName("records")]
        public List<AirQualityRecord> records { get; set; }
    }

    public class AirQualityRecord
    {
        [JsonPropertyName("sitename")]
        public string sitename { get; set; }

        [JsonPropertyName("county")]
        public string county { get; set; }

        [JsonPropertyName("aqi")]
        [JsonConverter(typeof(NullableIntConverter))]
        public int? aqi { get; set; }

        [JsonPropertyName("pollutant")]
        public string pollutant { get; set; }

        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("so2")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? so2 { get; set; }

        [JsonPropertyName("co")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? co { get; set; }

        [JsonPropertyName("o3")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? o3 { get; set; }

        [JsonPropertyName("o3_8hr")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? o3_8hr { get; set; }

        [JsonPropertyName("pm10")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? pm10 { get; set; }

        [JsonPropertyName("pm2.5")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? pm2_5 { get; set; }

        [JsonPropertyName("no2")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? no2 { get; set; }

        [JsonPropertyName("nox")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? nox { get; set; }

        [JsonPropertyName("no")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? no { get; set; }

        [JsonPropertyName("wind_speed")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? wind_speed { get; set; }

        [JsonPropertyName("wind_direc")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? wind_direc { get; set; }

        [JsonPropertyName("publishtime")]
        public string publishtime { get; set; }

        [JsonPropertyName("co_8hr")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? co_8hr { get; set; }

        [JsonPropertyName("pm2.5_avg")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? pm2_5_avg { get; set; }

        [JsonPropertyName("pm10_avg")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? pm10_avg { get; set; }

        [JsonPropertyName("so2_avg")]
        [JsonConverter(typeof(NullableDoubleConverter))]
        public double? so2_avg { get; set; }

        [JsonPropertyName("longitude")]
        public string longitude { get; set; }

        [JsonPropertyName("latitude")]
        public string latitude { get; set; }

        [JsonPropertyName("siteid")]
        public string siteid { get; set; }
    }
}
