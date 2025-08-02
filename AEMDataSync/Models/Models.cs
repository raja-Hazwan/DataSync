using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AEMDataSync.Models
{
    // Database Models
    public class Platform
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        // Updated to accommodate larger coordinate values
        [Column(TypeName = "decimal(18,10)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        public decimal? Longitude { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<Well> Wells { get; set; } = new List<Well>();
    }

    public class Well
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [ForeignKey("Platform")]
        public int PlatformId { get; set; }

        // Updated to accommodate larger coordinate values
        [Column(TypeName = "decimal(18,10)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(18,10)")]
        public decimal? Longitude { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public virtual Platform Platform { get; set; } = null!;
    }

    // API Response Models - Updated to match actual API structure
    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("expiration")]
        public DateTime? Expiration { get; set; }
    }

    public class ApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public List<PlatformWellData>? Data { get; set; }
    }

    // Updated to match the actual API response structure
    public class PlatformWellData
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; } // This is Platform ID

        [JsonPropertyName("uniqueName")]
        public string? UniqueName { get; set; } // This is Platform Name

        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lastUpdate")] // For dummy data
        public DateTime? LastUpdate { get; set; }

        [JsonPropertyName("well")]
        public List<WellData>? Wells { get; set; }

        // Additional properties that might be present in API responses
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }

    public class WellData
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("platformId")]
        public int? PlatformId { get; set; }

        [JsonPropertyName("uniqueName")]
        public string? UniqueName { get; set; }

        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("lastUpdate")] // For dummy data
        public DateTime? LastUpdate { get; set; }

        // Additional properties that might be present in API responses
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }
}