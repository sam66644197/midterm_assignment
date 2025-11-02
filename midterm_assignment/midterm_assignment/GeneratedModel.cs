using System.Text.Json.Serialization;

public class ResidentAgeDistribution
{
    // 原始 JSON 的欄位（中文）
    [JsonPropertyName("現住原住民年齡分配-年 依 期間, 鄉鎮市, 男/女 與 年齡層")]
    public string? AgeDistribution { get; set; }

    // 原始 JSON 中有一個空字串鍵
    [JsonPropertyName("")]
    public string? Extra { get; set; }
}
