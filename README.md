# Service Booking SaaS Platform

Multi-tenant service booking platform for fitness studios, salons, clinics, and more.

## Quick Start

### Prerequisites
- Node.js 20.x
- .NET SDK 9.0+
- PostgreSQL 16 (or Docker)
- Docker Desktop (optional)

### Option 1: Docker Compose (Recommended)

```bash
cd docker
docker-compose up -d
```

This starts:
- PostgreSQL on port 5432
- Backend API on http://localhost:5001
- Frontend on http://localhost:5173
- Seq (logging) on http://localhost:5341

### Option 2: Manual Setup

**1. Start PostgreSQL**

```bash
# Using Docker
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine
```

**2. Run Backend**

```bash
cd backend
dotnet restore
dotnet run --project src/BookingPlatform.API
```

API will be at http://localhost:5001

**3. Run Frontend**

```bash
cd frontend
npm install
npm run dev
```

Frontend will be at http://localhost:5173

## Project Structure

```
BarakMeetings/
├── backend/                    # .NET 9 Web API
│   ├── src/
│   │   ├── BookingPlatform.Domain/        # Entities, Enums, Interfaces
│   │   ├── BookingPlatform.Application/   # Use Cases, DTOs, Services
│   │   ├── BookingPlatform.Infrastructure/# EF Core, Identity, External
│   │   └── BookingPlatform.API/           # Controllers, Middleware
│   └── tests/
│
├── frontend/                   # React + TypeScript + Vite
│   ├── src/
│   │   ├── api/               # API client
│   │   ├── components/        # UI components
│   │   ├── features/          # Feature modules
│   │   ├── hooks/             # Custom hooks
│   │   ├── layouts/           # Page layouts
│   │   ├── stores/            # Zustand stores
│   │   └── types/             # TypeScript types
│   └── public/
│
├── contracts/                  # Shared API contracts
├── docker/                     # Docker configuration
└── docs/                       # Documentation
```

## Team Work Division

### Engineer 1: Infrastructure & Security
- Clean Architecture setup
- JWT Authentication
- Tenant Resolution Middleware
- EF Core with Global Query Filters
- Logging (Serilog)
- Super Admin API

### Engineer 2: Domain Logic (Backend)
- Entity models
- Availability Algorithm
- Booking Manager
- Staff Service Overrides
- Wait List Logic

### Engineer 3: Frontend
- React setup
- Booking Wizard
- Calendar View
- Dashboard
- Dark Mode / Theming

## API Endpoints

### Auth
- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Register
- `POST /api/auth/refresh` - Refresh token

### Services
- `GET /api/services` - List services
- `POST /api/services` - Create service
- `PUT /api/services/{id}` - Update service

### Staff
- `GET /api/staff` - List staff
- `GET /api/staff?serviceId={id}` - Staff for service

### Availability
- `GET /api/availability?serviceId=&staffId=&date=` - Get available slots

### Appointments
- `GET /api/appointments` - List appointments
- `POST /api/appointments` - Create booking
- `PUT /api/appointments/{id}/status` - Update status

## Key Technical Decisions

### Multi-Tenancy
- Single database, shared schema
- Row-level security via `TenantId` on all entities
- Global Query Filters in EF Core
- Tenant resolved from JWT claim or `X-Tenant-Id` header

### Booking Algorithm
1. Get staff working hours for date
2. Get service duration (check staff overrides)
3. Get existing appointments
4. Generate available slots
5. For group classes: allow booking if `CurrentAttendees < Capacity`

### Concurrency Control
- Optimistic concurrency via `Version` property
- Database transactions for critical booking operations

## Environment Variables

### Backend (.NET)
```env
ConnectionStrings__DefaultConnection=Host=localhost;...
Jwt__Secret=your-secret-key
Jwt__Issuer=BookingPlatform
Jwt__Audience=BookingPlatformClients
```

### Frontend (Vite)
```env
VITE_API_URL=http://localhost:5001/api
```
