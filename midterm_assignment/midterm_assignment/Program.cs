using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Data.Sqlite;

string? currentFilePath = null;

Console.WriteLine("=== JSON 分析工具 ===\n");

InitDatabase(); // 建立資料庫（若尚未存在）

string path = args.Length > 0 ? args[0] : PromptPath();
if (string.IsNullOrWhiteSpace(path))
{
    Console.WriteLine("未輸入路徑，程式結束。");
    return;
}

if (Directory.Exists(path))
{
    var files = Directory.GetFiles(path, "*.json");
    if (files.Length == 0)
    {
        Console.WriteLine("該資料夾沒有 .json 檔案。程式結束。");
        return;
    }

    Console.WriteLine("找到下列 .json 檔案：");
    for (int i = 0; i < files.Length; i++) Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])}");
    Console.Write("輸入編號選擇檔案（Enter 分析所有）：");
    var sel = Console.ReadLine()?.Trim();
    if (int.TryParse(sel, out var idx) && idx >= 1 && idx <= files.Length)
    {
        AnalyzeFile(files[idx - 1]);
    }
    else
    {
        foreach (var f in files) AnalyzeFile(f);
    }
}
else if (File.Exists(path))
{
    AnalyzeFile(path);
}
else
{
    Console.WriteLine("找不到指定路徑或檔案：" + path);
}

// 讓使用者選擇是否顯示資料庫內容
Console.Write("\n是否要顯示資料庫內容？(y/n)：");
var ans = Console.ReadLine()?.Trim().ToLower();
if (ans == "y")
{
    ShowDatabaseRecords();
}

string PromptPath()
{
    Console.Write("請輸入 JSON 檔案完整路徑或資料夾路徑：");
    return Console.ReadLine() ?? string.Empty;
}

// ======================= JSON 分析邏輯 ==========================
void AnalyzeFile(string filePath)
{
    Console.WriteLine($"\n--- 分析檔案: {filePath} ---\n");
    string content;
    try
    {
        content = File.ReadAllText(filePath);
    }
    catch (Exception ex)
    {
        Console.WriteLine("讀取檔案失敗: " + ex.Message);
        return;
    }

    // 嘗試當作一般 JSON 解析
    try
    {
        using var doc = JsonDocument.Parse(content, new JsonDocumentOptions { AllowTrailingCommas = true });
        currentFilePath = filePath;
        AnalyzeElement(doc.RootElement);
        return;
    }
    catch (JsonException)
    {
        // 嘗試當作 NDJSON
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => l.Length > 0)
                           .ToArray();

        var elements = new List<JsonElement>();
        foreach (var line in lines)
        {
            try
            {
                using var doc = JsonDocument.Parse(line, new JsonDocumentOptions { AllowTrailingCommas = true });
                elements.Add(doc.RootElement.Clone());
            }
            catch (JsonException) { }
        }

        if (elements.Count > 0)
        {
            AnalyzeElements(elements);
            return;
        }

        Console.WriteLine("無法解析為 JSON (標準 JSON 或 NDJSON)。請確認檔案格式。");
    }
}

void AnalyzeElement(JsonElement root)
{
    Console.WriteLine($"Root 類型: {root.ValueKind}");
    switch (root.ValueKind)
    {
        case JsonValueKind.Object:
            AnalyzeObject(root);
            break;
        case JsonValueKind.Array:
            AnalyzeArray(root);
            break;
        default:
            Console.WriteLine("Root 不是物件或陣列，顯示值：");
            Console.WriteLine(root.ToString());
            break;
    }
}

void AnalyzeElements(List<JsonElement> elements)
{
    Console.WriteLine($"NDJSON / 多個 JSON 物件: 共 {elements.Count} 個元素");
    if (elements.All(e => e.ValueKind == JsonValueKind.Object))
        AnalyzeArrayElementsAsObjects(elements);
    else
        Console.WriteLine("元素不是全為物件，略過詳細分析。");
}

void AnalyzeObject(JsonElement obj)
{
    var props = obj.EnumerateObject().ToArray();
    Console.WriteLine($"頂層屬性數量: {props.Length}");
    foreach (var p in props)
        Console.WriteLine($"- {p.Name}: {p.Value.ValueKind}");
}

void AnalyzeArray(JsonElement arr)
{
    var items = arr.EnumerateArray().ToArray();
    Console.WriteLine($"陣列元素數量: {items.Length}");
    if (items.Length > 0 && items.All(i => i.ValueKind == JsonValueKind.Object))
        AnalyzeArrayElementsAsObjects(items.ToList());
}

void AnalyzeArrayElementsAsObjects(List<JsonElement> objects)
{
    int total = objects.Count;
    Console.WriteLine($"分析物件陣列 (元素數: {total})");

    var allKeys = new HashSet<string>();
    foreach (var o in objects)
        foreach (var p in o.EnumerateObject())
            allKeys.Add(p.Name);

    var stats = new Dictionary<string, FieldStats>();
    foreach (var k in allKeys) stats[k] = new FieldStats();

    foreach (var o in objects)
    {
        foreach (var k in allKeys)
        {
            if (o.TryGetProperty(k, out var v))
            {
                var s = stats[k];
                s.Kinds.Add(v.ValueKind);
                if (v.ValueKind == JsonValueKind.Null)
                    s.NullCount++;
                else
                {
                    s.PresentCount++;
                    switch (v.ValueKind)
                    {
                        case JsonValueKind.Number:
                            if (v.TryGetDouble(out var d))
                            {
                                s.NumCount++;
                                s.Sum += d;
                                s.Min = Math.Min(s.Min, d);
                                s.Max = Math.Max(s.Max, d);
                            }
                            break;
                        case JsonValueKind.String:
                            var str = v.GetString() ?? "";
                            if (s.StringCounts.ContainsKey(str)) s.StringCounts[str]++;
                            else s.StringCounts[str] = 1;
                            break;
                        case JsonValueKind.True: s.TrueCount++; break;
                        case JsonValueKind.False: s.FalseCount++; break;
                    }
                }
            }
            else stats[k].MissingCount++;
        }
    }

    Console.WriteLine("欄位彙總：");
    foreach (var kv in stats.OrderBy(k => k.Key))
    {
        var name = kv.Key;
        var s = kv.Value;
        Console.WriteLine($"\n欄位: {name}");
        Console.WriteLine($"  - 出現 (非 null): {s.PresentCount} / {total}");
        Console.WriteLine($"  - 為 null: {s.NullCount}");
        Console.WriteLine($"  - 遺漏: {s.MissingCount}");
        Console.WriteLine($"  - 數值統計: count={s.NumCount}, sum={s.Sum}, min={s.Min}, max={s.Max}");
        Console.WriteLine($"  - 布林值: true={s.TrueCount}, false={s.FalseCount}");
    }

    SaveStatsToDatabase(Path.GetFileName(currentFilePath ?? "分析結果"), stats);
}

// ======================= 資料庫邏輯 ==========================

// 資料庫固定位置
const string DbPath = @"D:\大學作業\midterm_assignment\midterm_assignment\midterm_assignment\bin\Debug\net8.0\analysis.db";

void InitDatabase()
{
    Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
    using var conn = new SqliteConnection($"Data Source={DbPath}");
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS FieldStats (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        FileName TEXT,
        FieldName TEXT,
        PresentCount INTEGER,
        NullCount INTEGER,
        MissingCount INTEGER,
        NumCount INTEGER,
        Sum REAL,
        Min REAL,
        Max REAL,
        StringKinds INTEGER,
        TrueCount INTEGER,
        FalseCount INTEGER
    );";
    cmd.ExecuteNonQuery();

    Console.WriteLine($" 資料庫初始化完成，位置：{DbPath}");
}

void SaveStatsToDatabase(string fileName, Dictionary<string, FieldStats> stats)
{
    using var conn = new SqliteConnection($"Data Source={DbPath}");
    conn.Open();

    foreach (var kv in stats)
    {
        var name = kv.Key;
        var s = kv.Value;

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO FieldStats
        (FileName, FieldName, PresentCount, NullCount, MissingCount, NumCount, Sum, Min, Max, StringKinds, TrueCount, FalseCount)
        VALUES ($file, $field, $present, $null, $miss, $num, $sum, $min, $max, $strKinds, $true, $false);";

        cmd.Parameters.AddWithValue("$file", fileName);
        cmd.Parameters.AddWithValue("$field", name);
        cmd.Parameters.AddWithValue("$present", s.PresentCount);
        cmd.Parameters.AddWithValue("$null", s.NullCount);
        cmd.Parameters.AddWithValue("$miss", s.MissingCount);
        cmd.Parameters.AddWithValue("$num", s.NumCount);
        cmd.Parameters.AddWithValue("$sum", s.Sum);
        cmd.Parameters.AddWithValue("$min", s.Min);
        cmd.Parameters.AddWithValue("$max", s.Max);
        cmd.Parameters.AddWithValue("$strKinds", s.StringCounts.Count);
        cmd.Parameters.AddWithValue("$true", s.TrueCount);
        cmd.Parameters.AddWithValue("$false", s.FalseCount);

        cmd.ExecuteNonQuery();
    }

    Console.WriteLine($"分析結果已儲存至資料庫，共 {stats.Count} 筆欄位統計。");
}

void ShowDatabaseRecords()
{
    if (!File.Exists(DbPath))
    {
        Console.WriteLine($" 找不到資料庫：{DbPath}");
        return;
    }

    using var conn = new SqliteConnection($"Data Source={DbPath}");
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT FileName, FieldName, PresentCount, NullCount, MissingCount, NumCount, Sum, Min, Max, StringKinds, TrueCount, FalseCount FROM FieldStats;";

    using var reader = cmd.ExecuteReader();

    Console.WriteLine("\n=== 資料庫內容 ===");
    int count = 0;
    while (reader.Read())
    {
        count++;
        Console.WriteLine($"\n[{count}]");
        Console.WriteLine($"檔案名稱: {reader["FileName"]}");
        Console.WriteLine($"欄位名稱: {reader["FieldName"]}");
        Console.WriteLine($"出現次數: {reader["PresentCount"]}");
        Console.WriteLine($"為 null: {reader["NullCount"]}");
        Console.WriteLine($"遺漏: {reader["MissingCount"]}");
        Console.WriteLine($"數值統計: count={reader["NumCount"]}, sum={reader["Sum"]}, min={reader["Min"]}, max={reader["Max"]}");
        Console.WriteLine($"字串種類: {reader["StringKinds"]}");
        Console.WriteLine($"布林值: true={reader["TrueCount"]}, false={reader["FalseCount"]}");
    }

    if (count == 0)
        Console.WriteLine("（目前資料庫沒有任何紀錄）");

    Console.WriteLine("\n=== 顯示完成 ===\n");
}

// ======================= 統計類別 ==========================
class FieldStats
{
    public int PresentCount = 0;
    public int NullCount = 0;
    public int MissingCount = 0;
    public HashSet<JsonValueKind> Kinds = new();

    public int NumCount = 0;
    public double Sum = 0;
    public double Min = double.PositiveInfinity;
    public double Max = double.NegativeInfinity;

    public Dictionary<string, int> StringCounts = new();

    public int TrueCount = 0;
    public int FalseCount = 0;
}
