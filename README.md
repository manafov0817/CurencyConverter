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

### Why This Architecture for a Currency Converter API?

This architecture was specifically chosen for a currency converter API for several key benefits:

1. **Separation of Concerns**: Each layer has a distinct responsibility, making the codebase more maintainable and easier to understand. For a currency converter that needs to handle various exchange rate providers, conversion logic, and API endpoints, this clear separation helps manage complexity.

2. **Dependency Rule**: Dependencies flow inward, with the Core layer having no dependencies on outer layers. This ensures that business logic remains independent of implementation details. This is particularly valuable for a currency converter that might need to switch between different exchange rate providers (like Frankfurter, Open Exchange Rates, etc.) without affecting core conversion logic.

3. **Testability**: The use of interfaces and dependency injection makes the code highly testable, allowing for effective unit testing without external dependencies. For financial applications like currency converters, high test coverage is critical to ensure accurate calculations and reliable service.

4. **Flexibility**: The architecture allows for easy replacement of external components (like the currency provider) without affecting the core business logic. This is essential for a currency converter API that needs to maintain service continuity even if an external provider becomes unavailable.

5. **Scalability**: As the application grows, new features can be added without significant refactoring of existing code. The currency converter might need to add features like historical rate analysis, currency alerts, or support for cryptocurrencies in the future.

6. **Resilience**: By isolating external dependencies in the Infrastructure layer, the application can implement resilience patterns (retry, circuit breaker, fallback) to handle external service failures gracefully. This is crucial for a currency converter that depends on third-party APIs for rate data.

7. **Security**: The layered approach allows for implementing security concerns at appropriate levels - authentication and authorization at the API layer, while keeping business rules protected in the Core layer. For financial data, this separation helps maintain proper security boundaries.

### Key Design Patterns

- **Repository Pattern**: Abstracts data access logic
- **Factory Pattern**: Used for creating appropriate currency providers
- **Dependency Injection**: Used throughout the application for loose coupling
- **Options Pattern**: Used for configuration management (e.g., restricted currencies)

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
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

## Docker Support

### Running with Docker

You can run the Currency Converter API using Docker without installing .NET on your local machine.

#### Prerequisites

- Docker Desktop installed on your machine

#### Building the Docker Image

1. Navigate to the project directory:
   ```
   cd CurrencyConverter
   ```

2. Build the Docker image:
   ```
   docker build -t currencyconverter:latest .
   ```

#### Running the Container

Run the application in a Docker container:

```
docker run -d -p 8080:80 --name currency-converter currencyconverter:latest
```

This command:
- Runs the container in detached mode (`-d`)
- Maps port 8080 on your host to port 80 in the container (`-p 8080:80`)
- Names the container "currency-converter" for easy reference

The API will be available at `http://localhost:8080`.

#### Environment Variables

You can configure the application by passing environment variables:

```
docker run -d -p 8080:80 \
  -e Jwt__Key="your-secret-key" \
  -e Jwt__Issuer="your-issuer" \
  -e Jwt__Audience="your-audience" \
  --name currency-converter currencyconverter:latest
```

#### Viewing Logs

To view application logs:

```
docker logs currency-converter
```

#### Stopping the Container

To stop the running container:

```
docker stop currency-converter
```

To remove the container:

```
docker rm currency-converter
```

### Docker Compose (Development Environment)

For a development environment with multiple services, you can use Docker Compose. Create a `docker-compose.yml` file in the root directory:

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Jwt__Key=dev-secret-key
      - Jwt__Issuer=dev-issuer
      - Jwt__Audience=dev-audience
    volumes:
      - ./logs:/app/logs
```

Run with Docker Compose:

```
docker-compose up -d
```

Stop with Docker Compose:

```
docker-compose down
```

## Testing

The project includes a comprehensive testing strategy with multiple test types:

### Unit Tests

Unit tests focus on testing individual components in isolation, using mocks for dependencies. The project includes:

- **Service Tests**: Verify the business logic in the Core layer
- **Provider Tests**: Ensure correct interaction with external APIs
- **Error Handling Tests**: Validate proper exception handling
- **Edge Case Tests**: Test boundary conditions and input validation

### Integration Tests

Integration tests verify that different components work correctly together:

- **API Provider Tests**: Test the actual integration with the Frankfurter API
- **End-to-End Flows**: Verify complete business processes from request to response

### Test Architecture

The testing approach follows these principles:

1. **Arrange-Act-Assert (AAA)**: Tests are structured with clear setup, execution, and verification phases
2. **Isolation**: Unit tests are isolated from external dependencies using mocking
3. **Comprehensive Coverage**: Tests cover happy paths, error conditions, and edge cases
4. **Maintainability**: Tests are designed to be readable and maintainable

Run the tests using the following command:

```
dotnet test
```

### Test Coverage

The project has extensive test coverage, including:

- Core business logic in CurrencyService
- External API integration in FrankfurterApiProvider
- Input validation and error handling
- Cache behavior
- Restricted currency handling

### Generating Test Coverage Reports

While the project is configured with Coverlet for code coverage collection, you'll need to follow these steps to generate human-readable coverage reports locally:

1. Install the ReportGenerator tool:
   ```
   dotnet tool install -g dotnet-reportgenerator-globaltool
   ```

2. Run tests with coverage collection:
   ```
   dotnet test --collect:"XPlat Code Coverage"
   ```

3. Generate an HTML report from the coverage file:
   ```
   reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
   ```

4. Open the generated report:
   ```
   start coveragereport/index.html   # On Windows
   open coveragereport/index.html    # On macOS
   xdg-open coveragereport/index.html # On Linux
   ```

This will provide a detailed view of which code is covered by tests and help identify areas that may need additional testing.

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

## Assumptions

1. The Frankfurter API is the primary data source for exchange rates
2. Restricted currencies (TRY, PLN, THB, MXN) are managed through configuration rather than hardcoded
3. JWT authentication is sufficient for the API's security requirements
4. In-memory caching is adequate for the current scale of operations
5. The application requires high test coverage to ensure reliability

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