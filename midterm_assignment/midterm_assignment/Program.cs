using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace midterm_assignment
{
    class Program
    {
        static void Main(string[] args)
        {
            // 讀取 APP_Data 下的 空氣品質指標.json
            string jsonPath = Path.Combine(AppContext.BaseDirectory, @"..\..\..\APP_Data", "空氣品質指標.json");
            jsonPath = Path.GetFullPath(jsonPath);

            Console.WriteLine($"正在尋找 JSON 檔案路徑：{jsonPath}");

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"找不到 JSON 檔案：{jsonPath}");
                return;
            }

            string json = File.ReadAllText(jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            AirQualityResponse resp;
            try
            {
                resp = JsonSerializer.Deserialize<AirQualityResponse>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON 解析失敗: " + ex.Message);
                return;
            }

            var records = resp?.records;
            Console.WriteLine($"成功讀取 {records?.Count ?? 0} 筆資料");

            if (records == null || records.Count == 0)
                return;

            var first = records[0];
            var last = records[records.Count - 1];

            Console.WriteLine("\n第一筆資料：");
            PrintRecord(first);
            Console.WriteLine("\n最後一筆資料：");
            PrintRecord(last);
        }

        static void PrintRecord(AirQualityRecord r)
        {
            Console.WriteLine($"測站: {r.sitename} ({r.siteid})");
            Console.WriteLine($" 縣市: {r.county}");
            Console.WriteLine($" AQI: {r.aqi} ({r.status})");
            Console.WriteLine($" 主要污染物: {r.pollutant}");
            Console.WriteLine($" PM2.5: {r.pm2_5} μg/m3  PM10: {r.pm10} μg/m3");
            Console.WriteLine($" SO2: {r.so2} ppb  CO: {r.co} ppm  O3: {r.o3} ppb");
            Console.WriteLine($" 發布時間: {r.publishtime}");
            Console.WriteLine($" 經緯度: {r.latitude}, {r.longitude}");
        }
    }
}

