using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using AEMDataSync.Models;
using AEMDataSync.Data;

namespace AEMDataSync
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string? bearerToken;
        private static IConfiguration? configuration;

        static async Task Main(string[] args)
        {
            Console.WriteLine("AEM Energy Solutions - Data Sync Application");
            Console.WriteLine("============================================");

            try
            {
                // Load configuration from appsettings.json
                configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                // Initialize database
                using var context = new AEMDbContext(connectionString!);
                await context.Database.EnsureCreatedAsync();

                // Step 1: Login and get bearer token
                Console.WriteLine("1. Authenticating...");
                await LoginAsync();

                // Step 2: Fetch and sync actual data
                Console.WriteLine("2. Syncing actual platform and well data...");
                await SyncDataAsync("GetPlatformWellActual");

                // Step 3: Test with dummy data (to test error handling)
                Console.WriteLine("3. Testing with dummy data...");
                await SyncDataAsync("GetPlatformWellDummy");

                Console.WriteLine("\nData synchronization completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task LoginAsync()
        {
            var apiSettings = configuration!.GetSection("ApiSettings");

            // Use "username" field as required by the API
            var loginRequest = new
            {
                username = apiSettings["Username"],  // Changed from "email" to "username"
                password = apiSettings["Password"]
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = apiSettings["BaseUrl"];
            Console.WriteLine($"Attempting login to: {baseUrl}/api/Account/Login");
            Console.WriteLine($"Request body: {json}");

            var response = await httpClient.PostAsync($"{baseUrl}/api/Account/Login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✓ Authentication successful");

                // The API returns a plain JWT token string, not a JSON object
                // Remove any quotes if present and use as bearer token
                bearerToken = responseContent.Trim().Trim('"');

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                Console.WriteLine($"✓ Token received and set: {bearerToken.Substring(0, Math.Min(20, bearerToken.Length))}...");
            }
            else
            {
                Console.WriteLine($"Response content: {responseContent}");
                throw new Exception($"Login failed: {response.StatusCode}");
            }
        }

        private static async Task SyncDataAsync(string endpoint)
        {
            try
            {
                var apiSettings = configuration!.GetSection("ApiSettings");
                var baseUrl = apiSettings["BaseUrl"];
                var response = await httpClient.GetAsync($"{baseUrl}/api/PlatformWell/{endpoint}");

                Console.WriteLine($"API Response Status for {endpoint}: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw JSON Response for {endpoint}: {jsonContent}");

                    try
                    {
                        // First, try to determine if it's an array or object
                        using var document = JsonDocument.Parse(jsonContent);
                        var root = document.RootElement;

                        List<PlatformWellData>? dataList = null;

                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            // Direct array of platform/well data
                            Console.WriteLine("Response is a direct array");
                            dataList = JsonSerializer.Deserialize<List<PlatformWellData>>(jsonContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                        }
                        else if (root.ValueKind == JsonValueKind.Object)
                        {
                            // Object wrapper - look for common data field names
                            Console.WriteLine("Response is an object, looking for data array...");

                            if (root.TryGetProperty("data", out var dataElement))
                            {
                                dataList = JsonSerializer.Deserialize<List<PlatformWellData>>(dataElement.GetRawText(), new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                            }
                            else if (root.TryGetProperty("result", out var resultElement))
                            {
                                dataList = JsonSerializer.Deserialize<List<PlatformWellData>>(resultElement.GetRawText(), new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                            }
                            else if (root.TryGetProperty("items", out var itemsElement))
                            {
                                dataList = JsonSerializer.Deserialize<List<PlatformWellData>>(itemsElement.GetRawText(), new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                            }
                            else
                            {
                                Console.WriteLine("Available properties in response:");
                                foreach (var property in root.EnumerateObject())
                                {
                                    Console.WriteLine($"  - {property.Name}: {property.Value.ValueKind}");
                                }

                                // Try to deserialize the whole object as a single item if it has platform/well properties
                                if (root.TryGetProperty("platformId", out _) || root.TryGetProperty("wellId", out _))
                                {
                                    var singleItem = JsonSerializer.Deserialize<PlatformWellData>(jsonContent, new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });
                                    dataList = new List<PlatformWellData> { singleItem! };
                                }
                            }
                        }

                        if (dataList != null && dataList.Count > 0)
                        {
                            Console.WriteLine($"Found {dataList.Count} items to process");
                            await ProcessDataAsync(dataList, endpoint);
                            Console.WriteLine($"✓ {endpoint} data processed successfully");
                        }
                        else
                        {
                            Console.WriteLine($"⚠ No data found in {endpoint} response");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON parsing error for {endpoint}: {ex.Message}");
                        Console.WriteLine($"Raw content: {jsonContent}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"⚠ Failed to fetch {endpoint}: {response.StatusCode}");
                    Console.WriteLine($"Error content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error processing {endpoint}: {ex.Message}");
            }
        }

        private static async Task ProcessDataAsync(List<PlatformWellData> data, string source)
        {
            var connectionString = configuration!.GetConnectionString("DefaultConnection");
            using var context = new AEMDbContext(connectionString!);

            foreach (var platformData in data)
            {
                try
                {
                    // Process Platform data
                    if (platformData.Id.HasValue)
                    {
                        var existingPlatform = await context.Platforms
                            .FirstOrDefaultAsync(p => p.Id == platformData.Id.Value);

                        if (existingPlatform != null)
                        {
                            // Update existing platform
                            if (!string.IsNullOrEmpty(platformData.UniqueName))
                                existingPlatform.Name = platformData.UniqueName;
                            existingPlatform.Code = platformData.UniqueName ?? ""; // Using uniqueName as code since no separate code field

                            // Use the appropriate date field based on source
                            if (platformData.UpdatedAt.HasValue)
                                existingPlatform.UpdatedAt = platformData.UpdatedAt.Value;
                            else if (platformData.LastUpdate.HasValue)
                                existingPlatform.UpdatedAt = platformData.LastUpdate.Value;
                            else
                                existingPlatform.UpdatedAt = DateTime.UtcNow;

                            Console.WriteLine($"Updated platform: {existingPlatform.Name} (ID: {existingPlatform.Id})");
                        }
                        else
                        {
                            // Insert new platform
                            var platform = new Platform
                            {
                                Id = platformData.Id.Value,
                                Name = platformData.UniqueName ?? "",
                                Code = platformData.UniqueName ?? "", // Using uniqueName as code
                                CreatedAt = platformData.CreatedAt ?? DateTime.UtcNow,
                                UpdatedAt = platformData.UpdatedAt ?? platformData.LastUpdate ?? DateTime.UtcNow
                            };
                            context.Platforms.Add(platform);
                            Console.WriteLine($"Added new platform: {platform.Name} (ID: {platform.Id})");
                        }
                    }

                    // Process Well data - the API returns wells as a nested array
                    if (platformData.Wells != null && platformData.Wells.Count > 0)
                    {
                        foreach (var wellData in platformData.Wells)
                        {
                            if (wellData.Id.HasValue)
                            {
                                var existingWell = await context.Wells
                                    .FirstOrDefaultAsync(w => w.Id == wellData.Id.Value);

                                if (existingWell != null)
                                {
                                    // Update existing well
                                    if (!string.IsNullOrEmpty(wellData.UniqueName))
                                        existingWell.Name = wellData.UniqueName;
                                    existingWell.Code = wellData.UniqueName ?? ""; // Using uniqueName as code
                                    if (wellData.PlatformId.HasValue)
                                        existingWell.PlatformId = wellData.PlatformId.Value;

                                    // Use the appropriate date field
                                    if (wellData.UpdatedAt.HasValue)
                                        existingWell.UpdatedAt = wellData.UpdatedAt.Value;
                                    else if (wellData.LastUpdate.HasValue)
                                        existingWell.UpdatedAt = wellData.LastUpdate.Value;
                                    else
                                        existingWell.UpdatedAt = DateTime.UtcNow;

                                    Console.WriteLine($"Updated well: {existingWell.Name} (ID: {existingWell.Id})");
                                }
                                else
                                {
                                    // Insert new well
                                    var well = new Well
                                    {
                                        Id = wellData.Id.Value,
                                        Name = wellData.UniqueName ?? "",
                                        Code = wellData.UniqueName ?? "", // Using uniqueName as code
                                        PlatformId = wellData.PlatformId ?? platformData.Id ?? 0,
                                        CreatedAt = wellData.CreatedAt ?? DateTime.UtcNow,
                                        UpdatedAt = wellData.UpdatedAt ?? wellData.LastUpdate ?? DateTime.UtcNow
                                    };
                                    context.Wells.Add(well);
                                    Console.WriteLine($"Added new well: {well.Name} (ID: {well.Id})");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Platform {platformData.UniqueName} has no wells");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Error processing platform {platformData.UniqueName} from {source}: {ex.Message}");
                    // Continue processing other items
                }
            }

            var savedChanges = await context.SaveChangesAsync();
            Console.WriteLine($"✓ Saved {savedChanges} changes to database from {source}");
        }
    }
}