## Task Management API

A fully-featured Task Management API System built with ASP.NET Core, providing comprehensive task management capabilities with user authentication, authorization

- API Link: [API](https://sm-planner.runasp.net/)

## 🚀 Features

- ***User Authentication & Authorization***: Secure user registration, login, logout, and JWT-based authentication
- ***Task Management***: Complete CRUD operations for tasks with Redis caching
- ***User Management***: User profile management and role-based access control
- ***Admin Management***: Automated admin user creation with configurable settings
- ***Status Tracking***: Track task progress with different status levels
- ***Due Date Management***: Set deadlines and manage task scheduling
- ***Pagination***: Efficient data retrieval with customizable page sizes and navigation
- ***RESTful API***: Clean and intuitive API endpoints
- ***Swagger Documentation***: Interactive API documentation
- ***Entity Framework Core***: Robust data persistence layer
- ***Database Integration***: Support for SQL Server and SQLite

## 🛠 Technologies Used

- ASP.NET Core 8.0
- Entity Framework Core
- ASP.NET Core Identity
- JWT Authentication
- SQL Server / SQLite
- Swagger/OpenAPI
- AutoMapper
- FluentValidation

## 📋 Prerequisites

Before running this application, make sure you have the following installed:

- .NET 8.0 SDK
- SQL Server (or SQL Server Express)
- Visual Studio 2022 or Visual Studio Code

## ⚡ Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/MohamedAtef72/TaskManagementAPI.git
cd TaskManagementAPI
```

### 3. Configure Application Settings

Create or update your `appsettings.json` file with the following configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "AdminSettings": {
    "DefaultAdminPassword": "Admin@123456",
    "Admins": [
      {
        "UserName": "admin1",
        "Email": "",
        "PhoneNumber": "",
        "Country": "",
        "ImagePath": ""
      }
    ]
  },
  "JWT": {
    "SecritKey": "Your SecritKey",
    "AudienceIP": "Task",
    "IssuerIP": "https://sm-planner.runasp.net/"
  }
}

```

***Configuration Sections Explained:***

***ConnectionStrings:***
- `DefaultConnection`: Your SQL Server database connection string

***AdminSettings:***
- `AdminEmails`: List of email addresses that will be automatically created as admin users
- `DefaultAdminPassword`: Default password for admin accounts (change in production!)

***JWT Settings:***
- `SecretKey`: JWT signing key (must be at least 32 characters) - ***Fixed typo from SecritKey***
- `AudienceIP`: JWT audience identifier
- `IssuerIP`: JWT issuer URL

### 4. Run Database Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Run the Application

```bash
dotnet run
```

## 📚 API Endpoints

### Authentication

```http
POST /api/Account/Register # User registration
POST /api/Account/Login    # User login
POST /api/Account/Refresh    # User login
```

### Tasks (with Redis Caching)

```http (Some Of EndPoint)
GET /api/Task/GetAllTasks            # Get all tasks for authenticated user 
GET /api/Task/MyTasks                # Get specific tasks related to user
GET /api/Task/SpecificTask/{id}      # Get specific task by ID
POST /api/Task/Add                   # Create new task 
PUT /api/Task/{id}                   # Update existing task 
DELETE /api/Task/{id}                # Delete task 
```

### Users (with Redis Caching)

```http (Some Of EndPoint)
GET /api/User/Userprofile                  # Get User Profile
GET /api/User/GetAllUsers                  # Get All Users
PUT /api/User/Update                       # Update user profile 
DELETE /api/User/Delete                    # Delete user account
DELETE /api/User/AdminDelete/{userId}      # Delete user from Admin Profile 

```

## 📄 Pagination

The API implements efficient pagination for endpoints that return multiple records.

### Pagination Parameters

| Parameter    | Type | Default | Description                           |
|-------------|------|---------|---------------------------------------|
| pageNumber  | int  | 1       | The page number to retrieve (1-based) |
| pageSize    | int  | 10      | Number of items per page (max: 100)   |

### Pagination Response Format

```json
{
  "data": [
    {
      // ... actual data items
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 50
  }
}
```

### Example Requests

```http
GET /api/tasks?pageNumber=1&pageSize=20
```

## 📊 Data Models

### Task Model

```json
{
  "id": 1,
  "title": "Complete API Documentation",
  "description": "Write comprehensive API documentation",
  "status": "InProgress",
  "dueDate": "2024-12-31T23:59:59",
  "userId": "user-guid"
}
```

### User Model

```json
{
  "id": "user-guid",
  "UserName": "John",
  "Email": "john.doe@example.com",
  "Country": "Egypt",
  "PhoneNumber": "",
  "ImagePath": "",
}
```

## 🔐 Authentication

This API uses ***JWT (JSON Web Tokens)*** for authentication. To access protected endpoints:

1. Register a new user or login with existing credentials
2. Include the JWT token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Logout Functionality

In Front Remove Token From Session

## 👑 Admin Features

The application automatically creates admin users based on the ***AdminSettings*** configuration:

- Admin users are created on application startup
- Default admin accounts use the emails specified in `AdminSettings:AdminEmails`
- All admin accounts use the password from `AdminSettings:DefaultAdminPassword`

⚠️ ***Important***: Change the default admin password in production environments

## 🗄 Database Schema

The application uses ***Entity Framework Core*** with the following main entities:

- ***Users***: Store user information and authentication data
- ***Tasks***: Store task details and relationships
- ***UserRoles***: Manage user roles and permissions

## 🚀 Deployment

### Docker Deployment

```dockerfile
# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "TaskManagementAPI.dll"]
```
### Production Considerations

***1. Security:***
- Change the default admin password
- Use a strong, unique JWT secret key
- Enable HTTPS in production
- Secure your Redis instance

***2. Performance:***
- Configure Redis with appropriate memory limits
- Monitor cache hit rates
- Adjust cache expiration times based on usage patterns

***3. Monitoring:***
- Set up logging for cache operations
- Monitor Redis performance
- Track API response times
- 
## 📄 API Documentation

Interactive API documentation is available via ***Swagger UI*** at `/swagger` when running the application.

## 🔧 Troubleshooting

### Common Issues:

***1. Redis Connection Issues:***
- Ensure Redis Docker container is running: `docker ps`
- Restart Redis container: `docker restart redis-cache`
- Check Redis logs: `docker logs redis-cache`
- Verify Redis is accessible: `docker exec -it redis-cache redis-cli ping`

***2. Database Connection Issues:***
- Verify SQL Server is running
- Check connection string format
- Ensure database exists after running migrations

***3. JWT Authentication Issues:***
- Ensure JWT secret key is at least 32 characters
- Check token expiration settings
- Verify issuer and audience configurations

***4. Docker Issues:***
- Make sure Docker Desktop is running
- Check if port 6379 is available: `netstat -an | findstr :6379`
- Remove and recreate Redis container if needed:
  ```bash
  docker stop redis-cache
  docker rm redis-cache
  docker run -d --name redis-cache -p 6379:6379 redis
  ```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 👨‍💻 Author

***Mohamed Atef***
- GitHub: [@MohamedAtef72](https://github.com/MohamedAtef72)
- LinkedIn: [Mohamed Atef](https://www.linkedin.com/in/mohamed-atef-088a55272/)
- Email: ateefmohamed832@gmail.com

## 🙏 Acknowledgments

- ASP.NET Core team for the excellent framework
- Entity Framework Core for robust data access
- Swagger for API documentation
- JWT for secure authentication
