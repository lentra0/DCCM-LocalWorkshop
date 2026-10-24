using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalWorkshop
{
    // DCCM's CDB merge re-adds every changed row at the end of its separator group and also turns
    // each __STRUCTURE__.json into a row. The game reads some sheets by position (e.g. difficulty),
    // so the merged JSON is fixed up here: rows go back to their __original_Index (an index within
    // the row's separator group), structure rows are dropped and the separators array is rebuilt.
    public static class CdbRowOrder
    {
        private const string OriginalIndexKey = "__original_Index";
        private const string StructureColumnsKey = "__columns";

        private static readonly string[] HelperKeys =
            [OriginalIndexKey, "__separator_group_ID", "__separator_group_Name"];

        private static readonly JsonSerializerOptions WriteOpts = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static string Restore(string cdbJson, out int fixedSheets)
        {
            fixedSheets = 0;
            if (JsonNode.Parse(cdbJson) is not JsonObject root || root["sheets"] is not JsonArray sheets)
                return cdbJson;

            foreach (var sheet in sheets)
                if (sheet is JsonObject s && RestoreSheet(s)) fixedSheets++;

            return fixedSheets > 0 ? root.ToJsonString(WriteOpts) : cdbJson;
        }

        private static bool RestoreSheet(JsonObject sheet)
        {
            if (sheet["lines"] is not JsonArray lines) return false;
            if (!lines.Any(l => l is JsonObject o && (o.ContainsKey(OriginalIndexKey) || IsStructureRow(o))))
                return false;

            var separators = sheet["separators"] as JsonArray ?? [];
            if (!TryReadStarts(separators, lines.Count, out var starts)) return false;

            // segment 0 holds the rows before the first separator, segment i + 1 the rows of separator i
            var segments = new List<List<JsonNode?>>();
            for (var i = 0; i <= starts.Length; i++)
            {
                var from = i == 0 ? 0 : starts[i - 1];
                var to = i == starts.Length ? lines.Count : starts[i];
                var segment = new List<JsonNode?>();
                for (var j = from; j < to; j++)
                    if (!(lines[j] is JsonObject o && IsStructureRow(o))) segment.Add(lines[j]);
                segments.Add(Reorder(segment));
            }

            var columns = ColumnNames(sheet);
            lines.Clear();
            separators.Clear();
            for (var i = 0; i < segments.Count; i++)
            {
                if (i > 0) separators.Add(lines.Count);
                foreach (var line in segments[i])
                {
                    if (line is JsonObject o)
                        foreach (var key in HelperKeys)
                            if (!columns.Contains(key)) o.Remove(key);
                    lines.Add(line);
                }
            }
            return true;
        }

        private static List<JsonNode?> Reorder(List<JsonNode?> segment)
        {
            var slots = new JsonNode?[segment.Count];
            var taken = new bool[segment.Count];
            var rest = new List<JsonNode?>();
            foreach (var line in segment)
            {
                if (TryGetOriginalIndex(line, out var i) && i < slots.Length && !taken[i])
                {
                    slots[i] = line;
                    taken[i] = true;
                }
                else rest.Add(line);
            }

            var next = 0;
            for (var i = 0; i < slots.Length; i++)
                if (!taken[i]) slots[i] = rest[next++];
            return [.. slots];
        }

        private static bool TryReadStarts(JsonArray separators, int lineCount, out int[] starts)
        {
            starts = new int[separators.Count];
            for (var i = 0; i < starts.Length; i++)
            {
                if (separators[i] is not JsonValue v || !v.TryGetValue(out int start)) return false;
                if (start < (i == 0 ? 0 : starts[i - 1]) || start > lineCount) return false;
                starts[i] = start;
            }
            return true;
        }

        private static bool TryGetOriginalIndex(JsonNode? line, out int index)
        {
            index = -1;
            return line is JsonObject o && o[OriginalIndexKey] is JsonValue v
                && v.TryGetValue(out index) && index >= 0;
        }

        private static bool IsStructureRow(JsonObject row) =>
            row.ContainsKey(StructureColumnsKey)
            && row.All(p => p.Key.StartsWith("__", StringComparison.Ordinal));

        private static HashSet<string> ColumnNames(JsonObject sheet)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (sheet["columns"] is JsonArray columns)
                foreach (var column in columns)
                    if (column?["name"] is JsonValue v && v.TryGetValue(out string? name)) names.Add(name);
            return names;
        }
    }
}
