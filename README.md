# AEM Energy Solutions - Data Sync Application

This is a .NET 8.0 console application that synchronizes platform and well data from the AEM Energy Solutions REST API to a local SQL Server database.

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server Express (or any SQL Server instance)
- Internet connection to access the API

## Project Structure

```
AEMDataSync/
├── Program.cs              # Main application logic
├── Models/
│   └── Models.cs          # Data models and API response models
├── Data/
│   └── AEMDbContext.cs    # Entity Framework DbContext
├── AEMDataSync.csproj     # Project file with package references
├── appsettings.json       # Configuration file
└── SQLQuery.sql           # SQL query for Part 2 of assessment
```

## Setup Instructions

### 1. Create the project directory:
```bash
mkdir AEMDataSync
cd AEMDataSync
```

### 2. Create all project files:
- Copy `AEMDataSync.csproj` to the root directory
- Copy `Program.cs` to the root directory
- Copy `appsettings.json` to the root directory
- Create `Models/` folder and add `Models.cs`
- Create `Data/` folder and add `AEMDbContext.cs`

### 3. Install packages:
```bash
dotnet restore
```

### 4. Configure your database:
Update the connection string in `appsettings.json` to match your SQL Server instance:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=AEMDataSync;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;"
  },
  "ApiSettings": {
    "BaseUrl": "http://test-demo.aemenersol.com",
    "Username": "user@aemenersol.com",
    "Password": "Test@123"
  }
}
```

### 5. Run the application:
```bash
dotnet run
```

## Features

### Part 1: Data Synchronization
- ✅ Authenticates with the API using JWT bearer tokens
- ✅ Handles the actual API response format (direct array structure)
- ✅ Calls `GetPlatformWellActual` endpoint to fetch real data
- ✅ Calls `GetPlatformWellDummy` endpoint to test error handling
- ✅ Updates existing records by ID or inserts new records
- ✅ Handles nested well data within platform objects
- ✅ Robust error handling for individual record processing
- ✅ Uses Entity Framework Code First approach
- ✅ Automatically creates database and tables
- ✅ Supports both `updatedAt` and `lastUpdate` timestamp fields for compatibility

### Part 2: SQL Query
- ✅ Returns the last updated well for each platform
- ✅ Uses window functions for optimal performance
- ✅ Includes both platform and well information

## Database Schema

### Platforms Table
- `Id` (int, PK) - Platform ID from API
- `UniqueName` (nvarchar(255)) - Platform name (maps from `uniqueName`)
- `Latitude` (decimal(19,10)) - Platform latitude coordinates
- `Longitude` (decimal(19,10)) - Platform longitude coordinates
- `CreatedAt` (datetime2) - Record creation timestamp
- `UpdatedAt` (datetime2) - Last update timestamp

### Wells Table
- `Id` (int, PK) - Well ID from API  
- `UniqueName` (nvarchar(255)) - Well name (maps from `uniqueName`)
- `PlatformId` (int, FK) - Reference to Platform
- `Latitude` (decimal(19,10)) - Well latitude coordinates
- `Longitude` (decimal(19,10)) - Well longitude coordinates
- `CreatedAt` (datetime2) - Record creation timestamp
- `UpdatedAt` (datetime2) - Last update timestamp

## API Integration

- **Base URL:** `http://test-demo.aemenersol.com`
- **Authentication:** JWT bearer token via username/password login
- **Login Format:** `{"username": "user@aemenersol.com", "password": "Test@123"}`
- **Token Response:** Plain JWT string (e.g., `"eyJhbGciOiJIUzI1NiIs..."`)

### API Endpoints Used:
- `POST /api/Account/Login` - Authentication
- `GET /api/PlatformWell/GetPlatformWellActual` - Real data
- `GET /api/PlatformWell/GetPlatformWellDummy` - Test data

### API Response Format:
The API returns a direct array of platform objects with nested well arrays:
```json
[
  {
    "id": 11,
    "uniqueName": "Platform1",
    "latitude": 1.012000,
    "longitude": 0.123100,
    "createdAt": "2010-01-12T01:10:24.123",
    "updatedAt": "2010-01-12T01:10:24.123",
    "well": [
      {
        "id": 1,
        "platformId": 11,
        "uniqueName": "Well11",
        "latitude": 37.062570,
        "longitude": 18.406885,
        "createdAt": "2017-11-01T02:41:00",
        "updatedAt": "2018-08-04T02:16:42"
      }
    ]
  }
]
```

**Note:** The API uses "well" (singular) as the property name for the wells array.

## Data Models

The application uses two separate model types:

### Database Entities
- `Platform` - Entity Framework model for database operations
- `Well` - Entity Framework model for database operations

### API Response Models
- `PlatformWellData` - Deserializes API platform responses
- `WellData` - Deserializes API well responses
- `LoginResponse` - Handles authentication responses
- `ApiResponse` - Generic wrapper for API responses

## Package Dependencies

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.7" />
<PackageReference Include="System.Text.Json" Version="9.0.7" />
```

## Error Handling

The application includes robust error handling for:
- API authentication failures (handles plain JWT token response)
- Network connectivity issues
- Different API response structures between actual and dummy endpoints
- Missing or malformed data properties
- Database connection problems
- Individual record processing errors (continues with other records)
- Missing platform IDs for wells
- Null or invalid datetime values

## Data Processing Logic

1. **Authentication:** Login with username/password, receive JWT token
2. **API Response Parsing:** 
   - Handles both array and object response formats
   - Supports multiple data wrapper property names (`data`, `result`, `items`)
   - Falls back to single item parsing if array parsing fails
3. **Platform Processing:** Extract platform data from API response using `PlatformWellData` model
4. **Well Processing:** Process nested well arrays for each platform using `WellData` model
5. **Database Operations:** 
   - Check if records exist by ID
   - Update existing records with new data
   - Insert new records if they don't exist
   - Handle both `updatedAt` and `lastUpdate` timestamp fields
   - Maintain creation and update timestamps

## Expected Console Output

```
AEM Energy Solutions - Data Sync Application
============================================
1. Authenticating...
Attempting login to: http://test-demo.aemenersol.com/api/Account/Login
Request body: {"username":"user@aemenersol.com","password":"Test@123"}
Response status: OK
✓ Authentication successful
✓ Token received and set: eyJhbGciOiJIUzI1NiIs...

2. Syncing actual platform and well data...
API Response Status for GetPlatformWellActual: OK
Raw JSON Response for GetPlatformWellActual: [{"id":11,...}]
Response is a direct array
Found 10 items to process
Updated platform: Platform1 (ID: 11) - Lat: 1.012000, Lon: 0.123100
Updated well: Well11 (ID: 1) - Lat: 37.062570, Lon: 18.406885
✓ Saved 20 changes to database from GetPlatformWellActual
✓ GetPlatformWellActual data processed successfully

3. Testing with dummy data...
API Response Status for GetPlatformWellDummy: OK
Response is a direct array
Found 10 items to process
✓ GetPlatformWellDummy data processed successfully

Data synchronization completed successfully!
```

## Time Estimation

**Actual Time Required: 6-8 hours**

**Breakdown:**
- Initial project setup and API exploration: 1 hour
- Authentication implementation and JWT handling: 1.5 hours
- Entity Framework models and database setup: 1.5 hours
- API response analysis and data mapping: 2 hours
- Data synchronization logic implementation: 1.5 hours
- Error handling and testing with both endpoints: 1 hour
- SQL query development and documentation: 30 minutes

**Why this timeframe:**
- The API returned different response formats than initially expected
- JWT authentication required plain string handling instead of JSON objects
- The API response structure was a direct array with nested wells, requiring careful data extraction
- Separate models needed for API responses vs database entities
- Different field names between API (`uniqueName`) and initial expectations
- Extensive testing was needed with both actual and dummy data endpoints
- Robust error handling was implemented for production readiness

## Troubleshooting

### Common Issues:

1. **Connection String:** Ensure your SQL Server instance name is correct
2. **Database Permissions:** Make sure the application can create databases
3. **API Access:** Verify internet connectivity to the test API
4. **SSL Certificate:** The `TrustServerCertificate=true` setting handles SSL issues
5. **JSON Parsing:** The application handles various response formats automatically
6. **Missing Wells:** Some platforms may not have associated wells - this is handled gracefully

### Database Creation:
The application uses `EnsureCreatedAsync()` to automatically create the database and tables. No manual migrations are required.

## SQL Query (Part 2)

See `SQLQuery.sql` for the complete query that returns the last updated well for each platform using window functions for optimal performance. The query uses `UniqueName` fields to match the current database schema.

## Architecture Notes

- **Separation of Concerns:** API response models (`PlatformWellData`, `WellData`) are separate from database entities (`Platform`, `Well`)
- **Flexible JSON Handling:** Uses `JsonExtensionData` to capture additional API properties
- **Timestamp Compatibility:** Supports both `updatedAt` and `lastUpdate` field names for different API endpoints
- **High-Precision Coordinates:** Uses `decimal(19,10)` for latitude/longitude to maintain precision
- **Entity Framework Code First:** Database schema is generated from model classes
