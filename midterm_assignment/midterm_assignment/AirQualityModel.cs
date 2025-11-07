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
        public string aqi { get; set; }

        [JsonPropertyName("pollutant")]
        public string pollutant { get; set; }

        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("so2")]
        public string so2 { get; set; }

        [JsonPropertyName("co")]
        public string co { get; set; }

        [JsonPropertyName("o3")]
        public string o3 { get; set; }

        [JsonPropertyName("o3_8hr")]
        public string o3_8hr { get; set; }

        [JsonPropertyName("pm10")]
        public string pm10 { get; set; }

        [JsonPropertyName("pm2.5")]
        public string pm2_5 { get; set; }

        [JsonPropertyName("no2")]
        public string no2 { get; set; }

        [JsonPropertyName("nox")]
        public string nox { get; set; }

        [JsonPropertyName("no")]
        public string no { get; set; }

        [JsonPropertyName("wind_speed")]
        public string wind_speed { get; set; }

        [JsonPropertyName("wind_direc")]
        public string wind_direc { get; set; }

        [JsonPropertyName("publishtime")]
        public string publishtime { get; set; }

        [JsonPropertyName("co_8hr")]
        public string co_8hr { get; set; }

        [JsonPropertyName("pm2.5_avg")]
        public string pm2_5_avg { get; set; }

        [JsonPropertyName("pm10_avg")]
        public string pm10_avg { get; set; }

        [JsonPropertyName("so2_avg")]
        public string so2_avg { get; set; }

        [JsonPropertyName("longitude")]
        public string longitude { get; set; }

        [JsonPropertyName("latitude")]
        public string latitude { get; set; }

        [JsonPropertyName("siteid")]
        public string siteid { get; set; }
    }
}
