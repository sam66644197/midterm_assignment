using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using Microsoft.Data.SqlClient;

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

            // 將資料存入 LocalDB
            try
            {
                SaveToLocalDb(records);
                Console.WriteLine("已將資料存入 LocalDB (AirQualityDB) 的 dbo.AirQualityRecords 表格。");
            }
            catch (Exception ex)
            {
                Console.WriteLine("存入 LocalDB 發生錯誤: " + ex.Message);
            }

            // 驗證：統計有數值的欄位數量並進行 round-trip 序列化檢查
            int aqiCount = 0;
            int pm25Count = 0;
            int coordCount = 0;
            foreach (var r in records)
            {
                if (r.aqi.HasValue) aqiCount++;
                if (r.pm2_5.HasValue) pm25Count++;
                if (!string.IsNullOrWhiteSpace(r.latitude) && !string.IsNullOrWhiteSpace(r.longitude)) coordCount++;
            }

            Console.WriteLine($"含數值 AQI 的筆數: {aqiCount}/{records.Count}");
            Console.WriteLine($"含數值 PM2.5 的筆數: {pm25Count}/{records.Count}");
            Console.WriteLine($"含經緯度的筆數: {coordCount}/{records.Count}");

            // round-trip 檢查：將第一筆重新序列化並印出
            var sample = records[0];
            var serOptions = new JsonSerializerOptions { WriteIndented = true };
            string roundtrip = JsonSerializer.Serialize(sample, serOptions);
            Console.WriteLine("\n第一筆 round-trip 序列化檢查:\n" + roundtrip);

            var first = records[0];
            var last = records[records.Count - 1];

            Console.WriteLine("\n第一筆資料：");
            PrintRecord(first);
            Console.WriteLine("\n最後一筆資料：");
            PrintRecord(last);

            // 互動查詢：1) 使用者輸入要查看的欄位並取前10筆 2) 使用者輸入要查看的 Id
            // 互動查詢：可重複查詢欄位與 Id，輸入 0 離開
            try
            {
                Console.WriteLine();

                // ===== 查詢欄位 =====
                Console.WriteLine("可輸入要查看的欄位 id, sitename, country, aqi, pollutant, status, so2");
                Console.WriteLine("                  co, o3, o3.8hr, pm10, pm2.5, pm2.5_avg");
                while (true)
                {
                    Console.WriteLine("查詢資料庫：請輸入要查看的欄位（以逗號分隔，例如 sitename,aqi,pm2.5），或輸入 0 離開：");
                    var inputCols = Console.ReadLine();

                    if (inputCols == "0")
                        break;

                    if (!string.IsNullOrWhiteSpace(inputCols))
                    {
                        var cols = inputCols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var rows = DatabaseHelper.GetTopRecordsColumns(cols, 10);
                        Console.WriteLine($"查詢結果（前 {rows.Count} 筆）：");

                        foreach (var row in rows)
                        {
                            foreach (var kv in row)
                            {
                                Console.Write($"{kv.Key}: {FormatValue(kv.Value)}  ");
                            }
                            Console.WriteLine();
                        }
                    }

                    Console.WriteLine(); // 分行
                }

                // ===== 查詢 Id =====
                while (true)
                {
                    Console.WriteLine("請輸入要查看的資料 Id（數字），按 Enter 取得該筆完整資料；輸入 0 離開：");
                    var idInput = Console.ReadLine();

                    if (idInput == "0")
                        break;

                    if (!string.IsNullOrWhiteSpace(idInput) && int.TryParse(idInput.Trim(), out var id))
                    {
                        var record = DatabaseHelper.GetRecordById(id);
                        if (record == null)
                            Console.WriteLine($"找不到 Id = {id} 的資料");
                        else
                        {
                            Console.WriteLine($"Id = {id} 的完整資料：");
                            PrintRecord(record);
                        }
                    }

                    Console.WriteLine(); //分行
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("查詢發生錯誤: " + ex.Message);
            }

        }

        static string FormatValue(object v)
        {
            if (v == null) return "<null>";
            if (v is double d) return d.ToString(CultureInfo.InvariantCulture);
            return v.ToString();
        }

        static void PrintRecord(AirQualityRecord r)
        {
            Console.WriteLine($"測站: {r.sitename} ({r.siteid})");
            Console.WriteLine($" 縣市: {r.county}");
            Console.WriteLine($" AQI: {(r.aqi.HasValue ? r.aqi.ToString() : "<null>")} ({r.status})");
            Console.WriteLine($" 主要污染物: {r.pollutant}");
            // contest contributor 2023.10.11
            // [x] PM2.5, PM10 使用 ToString("F1") 格式化輸出
            // [x] 其他數值型欄位使用 ToString()，強制轉成字串避免預設文化影響
            Console.WriteLine($" PM2.5: {(r.pm2_5.HasValue ? r.pm2_5.Value.ToString("F1") : "<null>")} μg/m3  PM10: {(r.pm10.HasValue ? r.pm10.Value.ToString("F1") : "<null>")} μg/m3");
            Console.WriteLine($" SO2: {(r.so2.HasValue ? r.so2.Value.ToString() : "<null>")} ppb  CO: {(r.co.HasValue ? r.co.Value.ToString() : "<null>")} ppm  O3: {(r.o3.HasValue ? r.o3.Value.ToString() : "<null>")} ppb");
            Console.WriteLine($" 發布時間: {r.publishtime}");
            Console.WriteLine($" 經緯度: {r.latitude}, {r.longitude}");
        }

        static void SaveToLocalDb(List<AirQualityRecord> records)
        {
            // 建立資料庫（若不存在）
            string masterConnStr = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Database=master;";
            using (var masterConn = new SqlConnection(masterConnStr))
            {
                masterConn.Open();
                using var cmd = masterConn.CreateCommand();
                cmd.CommandText = "IF DB_ID(N'AirQualityDB') IS NULL CREATE DATABASE AirQualityDB;";
                cmd.ExecuteNonQuery();
            }

            // 連接到新建立的資料庫
            string dbConnStr = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Database=AirQualityDB;";
            using (var conn = new SqlConnection(dbConnStr))
            {
                conn.Open();

                // 建立表格（若不存在）
                string createTable = @"
IF OBJECT_ID(N'dbo.AirQualityRecords', N'U') IS NULL
CREATE TABLE dbo.AirQualityRecords (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Sitename NVARCHAR(200),
    County NVARCHAR(100),
    Aqi INT NULL,
    Pollutant NVARCHAR(100),
    Status NVARCHAR(100),
    So2 FLOAT NULL,
    Co FLOAT NULL,
    O3 FLOAT NULL,
    O3_8hr FLOAT NULL,
    PM10 FLOAT NULL,
    PM2_5 FLOAT NULL,
    No2 FLOAT NULL,
    Nox FLOAT NULL,
    No FLOAT NULL,
    WindSpeed FLOAT NULL,
    WindDirec FLOAT NULL,
    Publishtime NVARCHAR(50),
    Co_8hr FLOAT NULL,
    PM2_5_Avg FLOAT NULL,
    PM10_Avg FLOAT NULL,
    So2_Avg FLOAT NULL,
    Longitude FLOAT NULL,
    Latitude FLOAT NULL,
    SiteId NVARCHAR(50)
);";

                using (var createCmd = conn.CreateCommand())
                {
                    createCmd.CommandText = createTable;
                    createCmd.ExecuteNonQuery();
                }

                // 清除現有資料（根據需求可改成更新或忽略）
                using (var delCmd = conn.CreateCommand())
                {
                    delCmd.CommandText = "DELETE FROM dbo.AirQualityRecords;";
                    delCmd.ExecuteNonQuery();
                }

                // 插入資料
                using (var tran = conn.BeginTransaction())
                using (var insertCmd = conn.CreateCommand())
                {
                    insertCmd.Transaction = tran;
                    insertCmd.CommandText = @"INSERT INTO dbo.AirQualityRecords (Sitename, County, Aqi, Pollutant, Status, So2, Co, O3, O3_8hr, PM10, PM2_5, No2, Nox, No, WindSpeed, WindDirec, Publishtime, Co_8hr, PM2_5_Avg, PM10_Avg, So2_Avg, Longitude, Latitude, SiteId)
VALUES (@Sitename,@County,@Aqi,@Pollutant,@Status,@So2,@Co,@O3,@O3_8hr,@PM10,@PM2_5,@No2,@Nox,@No,@WindSpeed,@WindDirec,@Publishtime,@Co_8hr,@PM2_5_Avg,@PM10_Avg,@So2_Avg,@Longitude,@Latitude,@SiteId);";

                    int inserted = 0;
                    foreach (var r in records)
                    {
                        insertCmd.Parameters.Clear();

                        insertCmd.Parameters.AddWithValue("@Sitename", (object)r.sitename ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@County", (object)r.county ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Aqi", r.aqi.HasValue ? (object)r.aqi.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Pollutant", (object)r.pollutant ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Status", (object)r.status ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@So2", r.so2.HasValue ? (object)r.so2.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Co", r.co.HasValue ? (object)r.co.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@O3", r.o3.HasValue ? (object)r.o3.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@O3_8hr", r.o3_8hr.HasValue ? (object)r.o3_8hr.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@PM10", r.pm10.HasValue ? (object)r.pm10.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@PM2_5", r.pm2_5.HasValue ? (object)r.pm2_5.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@No2", r.no2.HasValue ? (object)r.no2.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Nox", r.nox.HasValue ? (object)r.nox.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@No", r.no.HasValue ? (object)r.no.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@WindSpeed", r.wind_speed.HasValue ? (object)r.wind_speed.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@WindDirec", r.wind_direc.HasValue ? (object)r.wind_direc.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Publishtime", (object)r.publishtime ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Co_8hr", r.co_8hr.HasValue ? (object)r.co_8hr.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@PM2_5_Avg", r.pm2_5_avg.HasValue ? (object)r.pm2_5_avg.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@PM10_Avg", r.pm10_avg.HasValue ? (object)r.pm10_avg.Value : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@So2_Avg", r.so2_avg.HasValue ? (object)r.so2_avg.Value : DBNull.Value);

                        // 經緯度字串轉成 double（若無法轉則存 NULL）
                        if (!string.IsNullOrWhiteSpace(r.longitude) && double.TryParse(r.longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                            insertCmd.Parameters.AddWithValue("@Longitude", lon);
                        else
                            insertCmd.Parameters.AddWithValue("@Longitude", DBNull.Value);

                        if (!string.IsNullOrWhiteSpace(r.latitude) && double.TryParse(r.latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                            insertCmd.Parameters.AddWithValue("@Latitude", lat);
                        else
                            insertCmd.Parameters.AddWithValue("@Latitude", DBNull.Value);

                        insertCmd.Parameters.AddWithValue("@SiteId", (object)r.siteid ?? DBNull.Value);

                        inserted += insertCmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                    Console.WriteLine($"已插入 {inserted} 筆資料到資料庫。");
                }
            }
        }
    }
}

