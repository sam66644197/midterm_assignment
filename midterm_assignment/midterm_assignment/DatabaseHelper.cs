using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace midterm_assignment
{
    public static class DatabaseHelper
    {
        const string DbConnStr = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Database=AirQualityDB;";

        // 允許的欄位映射：輸入可能為 JSON 欄位或簡短名稱，對應到資料庫欄名
        static readonly Dictionary<string, string> ColumnMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "id", "Id" },
            { "sitename", "Sitename" },
            { "county", "County" },
            { "aqi", "Aqi" },
            { "pollutant", "Pollutant" },
            { "status", "Status" },
            { "so2", "So2" },
            { "co", "Co" },
            { "o3", "O3" },
            { "o3_8hr", "O3_8hr" },
            { "pm10", "PM10" },
            { "pm2.5", "PM2_5" },
            { "pm2_5", "PM2_5" },
            { "no2", "No2" },
            { "nox", "Nox" },
            { "no", "No" },
            { "wind_speed", "WindSpeed" },
            { "wind_direc", "WindDirec" },
            { "publishtime", "Publishtime" },
            { "co_8hr", "Co_8hr" },
            { "pm2.5_avg", "PM2_5_Avg" },
            { "pm2_5_avg", "PM2_5_Avg" },
            { "pm10_avg", "PM10_Avg" },
            { "so2_avg", "So2_Avg" },
            { "longitude", "Longitude" },
            { "latitude", "Latitude" },
            { "siteid", "SiteId" }
        };

        public static List<Dictionary<string, object>> GetTopRecordsColumns(string[] requestedColumns, int limit = 10)
        {
            var result = new List<Dictionary<string, object>>();
            if (requestedColumns == null || requestedColumns.Length == 0)
                return result;

            // 轉換成合法的 DB 欄位並用方括號包覆
            var cols = new List<string>();
            foreach (var c in requestedColumns)
            {
                var key = c.Trim();
                if (ColumnMap.TryGetValue(key, out var mapped))
                    cols.Add($"[{mapped}]");
                else
                    throw new ArgumentException($"Unknown column: {c}");
            }

            var sql = $"SELECT {string.Join(",", cols)} FROM dbo.AirQualityRecords ORDER BY Id OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY";

            using var conn = new SqlConnection(DbConnStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@Limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[name] = value;
                }
                result.Add(row);
            }

            return result;
        }

        public static AirQualityRecord GetRecordById(int id)
        {
            using var conn = new SqlConnection(DbConnStr);
            conn.Open();

            var sql = @"SELECT Sitename, County, Aqi, Pollutant, Status, So2, Co, O3, O3_8hr, PM10, PM2_5, No2, Nox, [No], WindSpeed, WindDirec, Publishtime, Co_8hr, PM2_5_Avg, PM10_Avg, So2_Avg, Longitude, Latitude, SiteId
FROM dbo.AirQualityRecords WHERE Id = @Id";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var r = new AirQualityRecord();
            int idx;

            idx = reader.GetOrdinal("Sitename");
            r.sitename = reader.IsDBNull(idx) ? null : reader.GetString(idx);

            idx = reader.GetOrdinal("County");
            r.county = reader.IsDBNull(idx) ? null : reader.GetString(idx);

            idx = reader.GetOrdinal("Aqi");
            r.aqi = reader.IsDBNull(idx) ? (int?)null : reader.GetInt32(idx);

            idx = reader.GetOrdinal("Pollutant");
            r.pollutant = reader.IsDBNull(idx) ? null : reader.GetString(idx);

            idx = reader.GetOrdinal("Status");
            r.status = reader.IsDBNull(idx) ? null : reader.GetString(idx);

            idx = reader.GetOrdinal("So2");
            r.so2 = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("Co");
            r.co = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("O3");
            r.o3 = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("O3_8hr");
            r.o3_8hr = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("PM10");
            r.pm10 = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("PM2_5");
            r.pm2_5 = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("No2");
            r.no2 = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("Nox");
            r.nox = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("No");
            r.no = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("WindSpeed");
            r.wind_speed = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("WindDirec");
            r.wind_direc = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("Publishtime");
            r.publishtime = reader.IsDBNull(idx) ? null : reader.GetString(idx);

            idx = reader.GetOrdinal("Co_8hr");
            r.co_8hr = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("PM2_5_Avg");
            r.pm2_5_avg = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("PM10_Avg");
            r.pm10_avg = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("So2_Avg");
            r.so2_avg = reader.IsDBNull(idx) ? (double?)null : Convert.ToDouble(reader.GetValue(idx));

            idx = reader.GetOrdinal("Longitude");
            r.longitude = reader.IsDBNull(idx) ? null : Convert.ToDouble(reader.GetValue(idx)).ToString(CultureInfo.InvariantCulture);

            idx = reader.GetOrdinal("Latitude");
            r.latitude = reader.IsDBNull(idx) ? null : Convert.ToDouble(reader.GetValue(idx)).ToString(CultureInfo.InvariantCulture);

            idx = reader.GetOrdinal("SiteId");
            r.siteid = reader.IsDBNull(idx) ? null : reader.GetString(idx);

            return r;
        }
    }
}
