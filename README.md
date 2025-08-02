# AEM Energy Solutions - Data Sync Application

This is a .NET Core console application that synchronizes platform and well data from the AEM Energy Solutions REST API to a local SQL Server database.

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

### Part 2: SQL Query
- ✅ Returns the last updated well for each platform
- ✅ Uses window functions for optimal performance
- ✅ Includes both platform and well information

## Database Schema

### Platforms Table
- `Id` (int, PK) - Platform ID from API
- `Name` (nvarchar(255)) - Platform name (maps from `uniqueName`)
- `Code` (nvarchar(50)) - Platform code
- `CreatedAt` (datetime2) - Record creation timestamp
- `UpdatedAt` (datetime2) - Last update timestamp

### Wells Table
- `Id` (int, PK) - Well ID from API  
- `Name` (nvarchar(255)) - Well name (maps from `uniqueName`)
- `Code` (nvarchar(50)) - Well code
- `PlatformId` (int, FK) - Reference to Platform
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

## Data Processing Logic

1. **Authentication:** Login with username/password, receive JWT token
2. **Platform Processing:** Extract platform data from API response
3. **Well Processing:** Process nested well arrays for each platform
4. **Database Operations:** 
   - Check if records exist by ID
   - Update existing records with new data
   - Insert new records if they don't exist
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
Response is a direct array
Found 10 items to process
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
- Different field names between API (`uniqueName`) and database (`Name`) required mapping
- Extensive testing was needed with both actual and dummy data endpoints
- Robust error handling was implemented for production readiness

## Troubleshooting

### Common Issues:

1. **Connection String:** Ensure your SQL Server instance name is correct
2. **Database Permissions:** Make sure the application can create databases
3. **API Access:** Verify internet connectivity to the test API
4. **SSL Certificate:** The `TrustServerCertificate=true` setting handles SSL issues

### Database Creation:
The application uses `EnsureCreatedAsync()` to automatically create the database and tables. No manual migrations are required.

## SQL Query (Part 2)

See `SQLQuery.sql` for the complete query that returns the last updated well for each platform using window functions for optimal performance. the console output for detailed progress information.
