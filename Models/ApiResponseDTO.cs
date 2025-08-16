using System.Text.Json.Serialization;

namespace StorySpoilerTests.Models
{
    public class ApiResponseDTO
    {
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("storyId")]
        public string? StoryId { get; set; }
    }
}