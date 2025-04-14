# Currency Converter API

A robust, scalable, and maintainable currency conversion API built with C# and ASP.NET Core, providing high performance, security, and resilience.

## Features

- **Latest Exchange Rates**: Fetch the latest exchange rates for a specific base currency
- **Currency Conversion**: Convert amounts between different currencies
- **Historical Exchange Rates**: Retrieve historical exchange rates for a given period with pagination
- **Resilience & Performance**:
  - Caching to minimize direct calls to the Frankfurter API
  - Retry policies with exponential backoff
  - Circuit breaker pattern for graceful handling of API outages
- **Security & Access Control**:
  - JWT authentication
  - Role-based access control (RBAC)
  - API throttling to prevent abuse
- **Logging & Monitoring**:
  - Structured logging with Serilog
  - Request/response correlation
  - Detailed request logging (client IP, client ID, method, endpoint, response code, response time)

## Architecture

The project follows Clean Architecture principles with the following layers:

- **API Layer**: Controllers, middleware, and API models
- **Core Layer**: Business logic, domain models, and interfaces
- **Infrastructure Layer**: External service integrations, caching, security, and data access

## Getting Started

### Prerequisites

- .NET 7.0 SDK or later
- Visual Studio 2022 or any preferred IDE

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/CurrencyConverter.git
   ```

2. Navigate to the project directory:
   ```
   cd CurrencyConverter
   ```

3. Restore dependencies:
   ```
   dotnet restore
   ```

4. Build the solution:
   ```
   dotnet build
   ```

5. Run the API:
   ```
   cd src/CurrencyConverter.Api
   dotnet run
   ```

The API will be available at `https://localhost:5001` and `http://localhost:5000`.

## API Endpoints

### Authentication

#### Login

```http
POST /api/v1/auth/login
```

**Request Body:**
```json
{
  "username": "user",
  "password": "user123"
}
```

**Response (200 OK):**
```json
{
  "username": "user",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "roles": ["User"]
}
```

**Response (401 Unauthorized):**
```json
{
  "statusCode": 401,
  "message": "Invalid username or password",
  "traceId": "0HM6Q1KOPHK16:00000001"
}
```

**Available Users:**
- Regular User:
  - Username: `user`
  - Password: `user123`
  - Roles: `User`
- Admin User:
  - Username: `admin`
  - Password: `admin123`
  - Roles: `Admin`

### Currency Operations

All currency endpoints require authentication. Include the JWT token in the Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Get Latest Exchange Rates

Retrieves the latest exchange rates for a specified base currency.

```http
GET /api/currency/rates?baseCurrency={baseCurrency}
```

**Parameters:**
- `baseCurrency` (required): The base currency code (e.g., USD, EUR, GBP)

**Example Request:**
```http
GET /api/currency/rates?baseCurrency=USD
```

**Response (200 OK):**
```json
{
  "amount": 1,
  "baseCurrency": "USD",
  "date": "2025-04-14T00:00:00",
  "rates": {
    "EUR": 0.91,
    "GBP": 0.78,
    "JPY": 153.42,
    "CAD": 1.36,
    "AUD": 1.51
  }
}
```

**Response (400 Bad Request):**
```json
{
  "statusCode": 400,
  "message": "Currency TRY is restricted and cannot be used",
  "traceId": "0HM6Q1KOPHK16:00000002"
}
```

#### Convert Currency

Converts an amount from one currency to another.

```http
GET /api/currency/convert?amount={amount}&fromCurrency={fromCurrency}&toCurrency={toCurrency}
```

**Parameters:**
- `amount` (required): The amount to convert (must be greater than 0)
- `fromCurrency` (required): The source currency code
- `toCurrency` (required): The target currency code

**Example Request:**
```http
GET /api/currency/convert?amount=100&fromCurrency=USD&toCurrency=EUR
```

**Response (200 OK):**
```json
{
  "amount": 100,
  "fromCurrency": "USD",
  "toCurrency": "EUR",
  "convertedAmount": 91.02,
  "date": "2025-04-14T00:00:00",
  "rate": 0.9102
}
```

**Response (400 Bad Request):**
```json
{
  "statusCode": 400,
  "message": "Amount must be greater than zero",
  "traceId": "0HM6Q1KOPHK16:00000003"
}
```

#### Get Historical Rates

Retrieves historical exchange rates for a specified period. This endpoint requires Admin role.

```http
GET /api/currency/historical?baseCurrency={baseCurrency}&startDate={startDate}&endDate={endDate}&page={page}&pageSize={pageSize}
```

**Parameters:**
- `baseCurrency` (required): The base currency code
- `startDate` (required): Start date in ISO format (YYYY-MM-DD)
- `endDate` (required): End date in ISO format (YYYY-MM-DD)
- `page` (optional): Page number for pagination (default: 1)
- `pageSize` (optional): Number of items per page (default: 10)

**Example Request:**
```http
GET /api/currency/historical?baseCurrency=EUR&startDate=2025-01-01&endDate=2025-01-31&page=1&pageSize=5
```

**Response (200 OK):**
```json
{
  "items": [
    {
      "date": "2025-01-01T00:00:00",
      "baseCurrency": "EUR",
      "rates": {
        "USD": 1.09,
        "GBP": 0.86,
        "JPY": 167.23
      }
    },
    {
      "date": "2025-01-02T00:00:00",
      "baseCurrency": "EUR",
      "rates": {
        "USD": 1.10,
        "GBP": 0.85,
        "JPY": 168.05
      }
    },
    {
      "date": "2025-01-03T00:00:00",
      "baseCurrency": "EUR",
      "rates": {
        "USD": 1.08,
        "GBP": 0.87,
        "JPY": 166.89
      }
    },
    {
      "date": "2025-01-04T00:00:00",
      "baseCurrency": "EUR",
      "rates": {
        "USD": 1.09,
        "GBP": 0.86,
        "JPY": 167.45
      }
    },
    {
      "date": "2025-01-05T00:00:00",
      "baseCurrency": "EUR",
      "rates": {
        "USD": 1.10,
        "GBP": 0.85,
        "JPY": 168.12
      }
    }
  ],
  "totalCount": 31,
  "page": 1,
  "pageSize": 5,
  "totalPages": 7
}
```

**Response (401 Unauthorized):**
```json
{
  "statusCode": 401,
  "message": "Unauthorized access",
  "traceId": "0HM6Q1KOPHK16:00000004"
}
```

**Response (403 Forbidden):**
```json
{
  "statusCode": 403,
  "message": "User does not have the required role: Admin",
  "traceId": "0HM6Q1KOPHK16:00000005"
}
```

## Error Handling

The API uses a global exception handling middleware that returns consistent error responses:

```json
{
  "statusCode": 400,
  "message": "Error message describing the issue",
  "traceId": "0HM6Q1KOPHK16:00000006",
  "developerMessage": "Detailed error information (only in development environment)"
}
```

Common HTTP status codes:
- `200 OK`: Request succeeded
- `400 Bad Request`: Invalid input or validation error
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: Authenticated but not authorized for the resource
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Unexpected server error

## API Client Examples

### cURL

**Login:**
```bash
curl -X POST "https://localhost:5001/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"user","password":"user123"}'
```

**Get Latest Rates:**
```bash
curl -X GET "https://localhost:5001/api/currency/rates?baseCurrency=USD" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Convert Currency:**
```bash
curl -X GET "https://localhost:5001/api/currency/convert?amount=100&fromCurrency=USD&toCurrency=EUR" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Get Historical Rates:**
```bash
curl -X GET "https://localhost:5001/api/currency/historical?baseCurrency=EUR&startDate=2025-01-01&endDate=2025-01-31&page=1&pageSize=5" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### C# HttpClient

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

// Login
var loginContent = new StringContent(
    JsonSerializer.Serialize(new { username = "user", password = "user123" }),
    Encoding.UTF8,
    "application/json");

var loginResponse = await client.PostAsync("https://localhost:5001/api/v1/auth/login", loginContent);
var loginResult = await loginResponse.Content.ReadAsStringAsync();
var token = JsonDocument.Parse(loginResult).RootElement.GetProperty("token").GetString();

// Set token for subsequent requests
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

// Get latest rates
var ratesResponse = await client.GetAsync("https://localhost:5001/api/currency/rates?baseCurrency=USD");
var ratesResult = await ratesResponse.Content.ReadAsStringAsync();

// Convert currency
var convertResponse = await client.GetAsync(
    "https://localhost:5001/api/currency/convert?amount=100&fromCurrency=USD&toCurrency=EUR");
var convertResult = await convertResponse.Content.ReadAsStringAsync();

// Get historical rates (requires Admin role)
var historicalResponse = await client.GetAsync(
    "https://localhost:5001/api/currency/historical?baseCurrency=EUR&startDate=2025-01-01&endDate=2025-01-31&page=1&pageSize=5");
var historicalResult = await historicalResponse.Content.ReadAsStringAsync();

## Testing

Run the tests using the following command:

```
dotnet test
```

The project includes:
- Unit tests for services and providers
- Integration tests for API endpoints
- Test coverage reports

## Assumptions

1. The Frankfurter API is the primary data source for exchange rates
2. Restricted currencies (TRY, PLN, THB, MXN) are excluded from all operations
3. JWT authentication is sufficient for the API's security requirements
4. In-memory caching is adequate for the current scale of operations

## Future Enhancements

1. **Multiple Exchange Rate Providers**:
   - Add support for additional providers (e.g., Open Exchange Rates, Fixer.io)
   - Implement provider fallback strategies

2. **Advanced Caching**:
   - Implement distributed caching with Redis
   - Add cache invalidation strategies

3. **Enhanced Security**:
   - Implement OAuth 2.0 / OpenID Connect
   - Add API key management
   - Implement IP-based restrictions

4. **Monitoring & Observability**:
   - Integrate with Application Performance Monitoring (APM) tools
   - Add health checks and readiness probes
   - Implement metrics collection for business KPIs

5. **Deployment & Scalability**:
   - Containerize the application with Docker
   - Set up Kubernetes deployment
   - Implement auto-scaling based on load

## License

This project is licensed under the MIT License - see the LICENSE file for details.