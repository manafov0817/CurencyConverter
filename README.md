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

```
POST /api/auth/login
```

Request body:
```json
{
  "username": "user",
  "password": "user123"
}
```

Response:
```json
{
  "username": "user",
  "token": "your-jwt-token",
  "roles": ["User"]
}
```

### Currency Operations

#### Get Latest Exchange Rates

```
GET /api/currency/rates?baseCurrency=EUR
```

#### Convert Currency

```
GET /api/currency/convert?amount=100&fromCurrency=USD&toCurrency=EUR
```

#### Get Historical Rates

```
GET /api/currency/historical?baseCurrency=EUR&startDate=2020-01-01&endDate=2020-01-31&page=1&pageSize=10
```

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