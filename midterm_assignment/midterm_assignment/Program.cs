using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

Console.WriteLine("JSON 分析工具");

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

string PromptPath()
{
    Console.Write("請輸入 JSON 檔案完整路徑或資料夾路徑：");
    return Console.ReadLine() ?? string.Empty;
}

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

    // 1) 嘗試當作一般 JSON 解析
    try
    {
        using var doc = JsonDocument.Parse(content, new JsonDocumentOptions { AllowTrailingCommas = true });
        AnalyzeElement(doc.RootElement);
        return;
    }
    catch (JsonException)
    {
        // 2) 嘗試將檔案視為 NDJSON (每行一個 JSON)
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
            catch (JsonException)
            {
                // 忽略無法解析的行
            }
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
    // 如果大多數元素是物件，當作物件陣列處理
    if (elements.All(e => e.ValueKind == JsonValueKind.Object))
    {
        AnalyzeArrayElementsAsObjects(elements);
    }
    else
    {
        // 顯示各類型分佈
        var groups = elements.GroupBy(e => e.ValueKind).Select(g => (g.Key, Count: g.Count()));
        foreach (var g in groups) Console.WriteLine($"{g.Key}: {g.Count}");
    }
}

void AnalyzeObject(JsonElement obj)
{
    var props = obj.EnumerateObject().ToArray();
    Console.WriteLine($"頂層屬性數量: {props.Length}");
    foreach (var p in props)
    {
        Console.WriteLine($"- {p.Name}: {p.Value.ValueKind} {PreviewValue(p.Value)}");
    }
}

string PreviewValue(JsonElement v)
{
    try
    {
        switch (v.ValueKind)
        {
            case JsonValueKind.String:
                var s = v.GetString() ?? "";
                return s.Length > 50 ? "(string, 長度 " + s.Length + ")" : "(string) " + s;
            case JsonValueKind.Number:
                if (v.TryGetDouble(out var d)) return "(number) " + d;
                return "(number)";
            case JsonValueKind.True:
            case JsonValueKind.False:
                return "(bool) " + v.GetBoolean();
            case JsonValueKind.Null:
                return "(null)";
            case JsonValueKind.Array:
                return "(array, 長度=" + v.GetArrayLength() + ")";
            case JsonValueKind.Object:
                return "(object)";
            default:
                return "";
        }
    }
    catch { return ""; }
}

void AnalyzeArray(JsonElement arr)
{
    var items = arr.EnumerateArray().ToArray();
    Console.WriteLine($"陣列元素數量: {items.Length}");
    if (items.Length == 0) return;

    var kinds = items.GroupBy(i => i.ValueKind).Select(g => (g.Key, Count: g.Count()));
    Console.WriteLine("元素類型分佈:");
    foreach (var k in kinds) Console.WriteLine($"- {k.Key}: {k.Count}");

    if (items.All(i => i.ValueKind == JsonValueKind.Object))
    {
        AnalyzeArrayElementsAsObjects(items.Select(i => i).ToList());
    }
}

void AnalyzeArrayElementsAsObjects(List<JsonElement> objects)
{
    int total = objects.Count;
    Console.WriteLine($"分析物件陣列 (元素數: {total})");

    var allKeys = new HashSet<string>();
    foreach (var o in objects)
    {
        foreach (var p in o.EnumerateObject()) allKeys.Add(p.Name);
    }

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
                {
                    s.NullCount++;
                }
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
                            var str = v.GetString() ?? string.Empty;
                            if (s.StringCounts.ContainsKey(str)) s.StringCounts[str]++;
                            else s.StringCounts[str] = 1;
                            break;
                        case JsonValueKind.True:
                            s.TrueCount++;
                            break;
                        case JsonValueKind.False:
                            s.FalseCount++;
                            break;
                        default:
                            // object / array / others
                            break;
                    }
                }
            }
            else
            {
                stats[k].MissingCount++;
            }
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
        Console.WriteLine($"  - 遺漏 (欄位不存在): {s.MissingCount}");
        Console.WriteLine($"  - 資料型別: {string.Join(", ", s.Kinds.Select(k => k.ToString()))}");

        if (s.NumCount > 0)
        {
            Console.WriteLine($"  - 數值統計: count={s.NumCount}, sum={s.Sum}, avg={(s.Sum / s.NumCount):F2}, min={s.Min}, max={s.Max}");
        }

        if (s.StringCounts.Count > 0)
        {
            Console.WriteLine($"  - 字串種類數: {s.StringCounts.Count}");
            foreach (var top in s.StringCounts.OrderByDescending(p => p.Value).Take(5))
                Console.WriteLine($"    * {top.Key} ({top.Value})");
        }

        if (s.TrueCount + s.FalseCount > 0)
        {
            Console.WriteLine($"  - 布林值: true={s.TrueCount}, false={s.FalseCount}");
        }
    }
}

class FieldStats
{
    public int PresentCount = 0;     // 非 null 的出現次數
    public int NullCount = 0;        // 明確為 null 的次數
    public int MissingCount = 0;     // 欄位不存在的次數
    public HashSet<JsonValueKind> Kinds = new();

    // 數值
    public int NumCount = 0;
    public double Sum = 0;
    public double Min = double.PositiveInfinity;
    public double Max = double.NegativeInfinity;

    // 字串統計
    public Dictionary<string, int> StringCounts = new();

    // 布林
    public int TrueCount = 0;
    public int FalseCount = 0;
}
