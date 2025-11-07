using System.Text.Json.Serialization;

namespace midterm_assignment
{
    public class IndexRecord
    {
        [JsonPropertyName("月別")]
        public string 月別 { get; set; }

        [JsonPropertyName("台灣-加權指數")]
        public double 台灣_加權指數 { get; set; }

        [JsonPropertyName("台灣-上櫃指數")]
        public double 台灣_上櫃指數 { get; set; }

        [JsonPropertyName("美國-那斯達克指數")]
        public double 美國_那斯達克指數 { get; set; }

        [JsonPropertyName("美國-道瓊工業指數")]
        public double 美國_道瓊工業指數 { get; set; }

        [JsonPropertyName("日本-日經225指數")]
        public double 日本_日經225指數 { get; set; }

        [JsonPropertyName("新加坡-海峽時報指數")]
        public double 新加坡_海峽時報指數 { get; set; }

        [JsonPropertyName("南韓-綜合指數")]
        public double 南韓_綜合指數 { get; set; }

        [JsonPropertyName("倫敦-金融時報指數")]
        public double 倫敦_金融時報指數 { get; set; }

        [JsonPropertyName("中國-上海綜合指數")]
        public double 中國_上海綜合指數 { get; set; }

        [JsonPropertyName("中國-香港恆生指數")]
        public double 中國_香港恆生指數 { get; set; }
    }
}
