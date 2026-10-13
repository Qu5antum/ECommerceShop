# ECommerceShop

A complete e-commerce project with a React + TypeScript frontend and an ASP.NET Core Web API backend. It combines customer workflows, seller operations, admin management, payment handling, order processing, notifications, analytics, and caching in a single full-stack application.

## Overview

`ECommerceShop` is an online store with the following user roles:

- Customer / buyer
- Seller
- Administrator / manager / moderator
- Order, payment, and notification management system

The project is built as a monolithic backend API plus a separate frontend application with role-based pages and navigation.

## Key Features

### Customer Features

- User registration and login
- JWT-based authentication
- Product catalog and category browsing
- Product search by name and sorting by price
- Product detail pages
- Shopping cart
- Order placement
- Order history
- Notifications panel
- User profile management

### Seller Features

- Seller profile creation and update
- Product management and stock tracking
- Viewing seller orders
- Access to order details and customer information
- Product availability and stock checks
- Seller status management

### Admin / Management Features

- User management
- Role assignment and removal
- User activation/deactivation
- Category management
- Product moderation and deletion
- Order management and status updates
- Payment management
- Store analytics dashboard
- Seller status management

### Additional Features

- PostgreSQL as the main database
- Redis for caching and faster analytical queries
- Swagger / OpenAPI documentation for the API
- Frontend CORS configuration
- Image storage for products and seller profiles
- Notification system based on database models
- FluentValidation-based DTO validation

## Tech Stack

### Backend

- C# / ASP.NET Core
- .NET SDK 10
- Entity Framework Core
- PostgreSQL
- Redis
- JWT + ASP.NET Core Authentication
- Swagger / Swashbuckle
- FluentValidation
- BCrypt.Net-Next

### Frontend

- React 19
- TypeScript
- Vite
- React Router
- Axios
- ESLint

### Infrastructure

- Docker Compose
- PostgreSQL container
- Redis container
- Frontend served via nginx

## Project Structure

```text
ECommerceShop/
├── backend/
│   └── src/
│       └── App/
│           ├── Controllers/
│           ├── Services/
│           ├── Repositories/
│           ├── Models/
│           ├── DTOs/
│           ├── Database/
│           ├── Enum/
│           ├── Validators/
│           ├── Migrations/
│           ├── Program.cs
│           └── appsettings*.json
├── frontend/
│   ├── src/
│   ├── public/
│   ├── package.json
│   ├── vite.config.ts
│   └── README.md
├── docker-compose.yml
├── README.md
└── .gitignore
```

### Backend Architecture

The backend is organized into layers:

- Controllers — HTTP endpoints
- Services — business logic
- Repositories — data access and CRUD operations
- Models — database entities
- DTOs — request/response contracts
- Validators — request validation
- Transactions — Unit of Work pattern
- Database — DbContext and EF configuration

### Frontend Architecture

- `src/pages` — application pages
- `src/api` — API requests
- `src/types` — shared TypeScript types
- `src/components` — reusable UI components
- `src/App.tsx` — route configuration

## Main Domain Entities

The database contains the following main entities:

- User
- Category
- Product
- SellerProfile
- Cart
- CartItem
- Order
- OrderItem
- Payment
- Review
- Notification

User roles include:

- DefaultUser
- Seller
- Manager
- Moderator
- Admin

## Frontend Routes

The app includes dedicated pages for major flows:

- `/` — home page
- `/main` — main storefront page
- `/login` — login
- `/register` — registration
- `/product/:id` — product details
- `/cart` — shopping cart
- `/orders` — user orders
- `/notifications` — notifications
- `/seller/profile` — seller profile
- `/seller/products` — seller products
- `/user/profile` — user profile
- `/admin/*` — admin pages

## API and Swagger

The backend runs Swagger UI and exposes OpenAPI documentation.

By default, the Swagger UI is available at:

```text
http://localhost:8080/swagger
```

If the API is launched locally without Docker, check the port in the launch configuration (`launchSettings.json`).

## Requirements

To run the project locally, you need:

- Docker Desktop / Docker Compose
- .NET SDK 10
- Node.js 20+
- npm
- PostgreSQL (if not using Docker)
- Redis (if not using Docker)

## Quick Start with Docker Compose

The root project already contains `docker-compose.yml`, which starts:

- backend
- PostgreSQL
- Redis
- frontend

### Start the project

```bash
docker compose up --build
```

After startup:

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

### Stop the project

```bash
docker compose down
```

To remove database and Redis data as well:

```bash
docker compose down -v
```

## Local Run Without Docker

### 1. Start PostgreSQL and Redis

You can run the database and cache using Docker:

```bash
docker run --name shop-db -e POSTGRES_DB=shop_db -e POSTGRES_USER=admin -e POSTGRES_PASSWORD=shop1234 -p 5432:5432 -d postgres:18
docker run --name redis-cache -p 6379:6379 -d redis:7.4-alpine redis-server --appendonly yes --requirepass StrongSecurePassword123
```

### 2. Start the backend

```bash
cd backend/src/App
dotnet restore
dotnet ef database update
dotnet run
```

If `dotnet ef` is not available, restore the local tools first:

```bash
dotnet tool restore
```

### 3. Start the frontend

```bash
cd frontend
npm install
npm run dev
```

The frontend will run at:

```text
http://localhost:5173
```

## Configuration

### Backend

The file `backend/src/App/appsettings.json` includes the main server settings:

```json
{
  "Jwt": {
    "Key": "super-secret-key-for-ecommerce-api-2026",
    "Issuer": "EcommerceApi",
    "Audience": "EcommerceClient"
  },
  "ConnectionStrings": {
    "Redis": "redis:6379,******",
    "DefaultConnection": "Host=localhost;Port=5432;Database=shop_db;Username=admin;******; Include Error Detail=true"
  }
}
```

Docker Compose passes these values via environment variables so the API can connect to PostgreSQL and Redis.

### Frontend

The frontend uses this environment variable:

```bash
VITE_API_URL=http://localhost:8080/api
```

If it is not set, the default value is:

```text
http://localhost:8080/api
```

## Role-Based Access System

The application uses JWT and role claims to restrict access. The main flows are:

- Customers can browse products, place orders, update their profile, and manage the cart.
- Sellers can create and edit products, view their orders, and manage their store profile.
- Managers, administrators, and moderators can manage users, payments, orders, and analytics.

This is implemented through `[Authorize]` and `[Authorize(Roles = "...")]` attributes on controllers and API methods.

## Main Business Flows

### Registration and Login

1. A user registers through `/register`
2. The API creates a new user and hashes the password
3. After login, the backend returns a JWT token
4. The token is saved in `localStorage` and attached to subsequent API requests

### Cart and Order Checkout

1. User adds products to the cart
2. API creates and updates cart items
3. User places an order
4. The system creates the order and associated order items
5. Payment and status updates can then occur

### Seller Product Management

1. Seller creates a seller profile
2. Adds products with price, SKU, description, and image
3. Reviews stock levels and order status
4. Manages the full list of products and availability

### Admin Dashboard

1. Admin reviews overall store statistics
2. Views users and sellers
3. Updates roles and statuses
4. Manages product and category data
5. Reviews payment and analytics information

## Security and Quality

The project includes basic security measures:

- JWT authentication
- Role-based authorization
- BCrypt password hashing
- FluentValidation request validation
- CORS configuration
- Access control enforced at the API method level

## Current Notes

The project is already fairly complete and includes a robust e-commerce workflow, but it is important to keep in mind that:

- it is a monolithic application rather than a microservices architecture
- a portion of the business logic is placed in services and repositories without a strict DDD structure
- production secrets, passwords, and connection settings should be replaced before deployment
- `docker-compose.yml` contains development-oriented values and should be hardened for a real production environment

## Directory Structure in Detail

```text
backend/
├── src/App/
│   ├── Controllers/
│   │   ├── AuthContoller.cs
│   │   ├── ProductController.cs
│   │   ├── OrderController.cs
│   │   ├── UserContoller.cs
│   │   ├── CartController.cs
│   │   ├── CategoryController.cs
│   │   ├── SellerProfileController.cs
│   │   ├── PaymentController.cs
│   │   ├── NotificationController.cs
│   │   └── GeneralAnalyticController.cs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── ProductService.cs
│   │   ├── OrderService.cs
│   │   ├── PaymentService.cs
│   │   ├── SellerProfileService.cs
│   │   └── Analytics/
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── ProductRepository.cs
│   │   ├── OrderRepository.cs
│   │   └── ...
│   ├── Models/
│   ├── DTOs/
│   ├── Enum/
│   ├── Exceptions/
│   ├── Transactions/
│   ├── Database/
│   └── Program.cs
└── ...

frontend/
├── src/
│   ├── api/
│   ├── components/
│   ├── pages/
│   ├── types/
│   ├── App.tsx
│   └── main.tsx
├── package.json
├── vite.config.ts
├── tsconfig.json
└── eslint.config.js
```

## Useful Commands

### Backend

```bash
cd backend/src/App
dotnet restore
dotnet build
dotnet run
```

### Frontend

```bash
cd frontend
npm install
npm run build
npm run dev
```

### Docker

```bash
docker compose up --build
docker compose logs -f backend
docker compose down
```

## Common Issues and Fixes

### 1. Swagger does not open

Check that the backend is running on port `8080` and that the application built successfully without errors.

### 2. Frontend cannot reach the backend

Verify the `VITE_API_URL` environment variable or use the default value:

```text
http://localhost:8080/api
```

### 3. PostgreSQL connection errors

Check the connection values in `appsettings.json` and the environment variables in Docker Compose.

### 4. Authentication errors

Make sure the JWT secret key and token are passed correctly in the `Authorization` header.

## License

There is no explicit license file in the repository. If the project is used publicly, it is recommended to add a `LICENSE` file and choose an appropriate license.

## Conclusion

`ECommerceShop` is a complete e-commerce project that includes both user-facing and admin-facing workflows. It combines a modern React frontend, an ASP.NET Core backend, PostgreSQL storage, and Redis-based caching, making it a strong base for further enhancement and adaptation to real business requirements.

If you want, I can also prepare:

- a more detailed API controller-by-controller guide;
- an ERD/database diagram;
- a role-and-permission matrix;
- a shorter GitHub-ready version of the README;
- a README in Russian as well.
