using System.Text.Json.Serialization;

namespace LocalWorkshop
{
    public sealed class LocalWorkshopInfo
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("hasScripts")]
        public bool HasScripts { get; set; }

        [JsonPropertyName("enabledByDefault")]
        public bool EnabledByDefault { get; set; }

        [JsonPropertyName("pak")]
        public string? Pak { get; set; }
    }

    public sealed class LocalWorkshopItem
    {
        public required long Id { get; init; }

        public required string FolderPath { get; init; }

        public required string PakPath { get; init; }

        public string? PreviewPath { get; init; }

        public required LocalWorkshopInfo Info { get; init; }

        public string DisplayName =>
            !string.IsNullOrWhiteSpace(Info.Title) ? Info.Title!
            : !string.IsNullOrWhiteSpace(Info.Name) ? Info.Name!
            : $"Local #{Id}";

        public override string ToString() => $"{DisplayName} (id={Id}, pak={Path.GetFileName(PakPath)})";
    }
}
