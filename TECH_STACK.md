# Service Booking SaaS Platform - Technology Stack

## Overview
Multi-tenant SaaS platform for service booking across Fitness, Beauty, Wellness, Education, and Activities industries.

---

## Backend Stack (.NET 8)

### Core Framework
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 LTS | Runtime & SDK |
| ASP.NET Core Web API | 8.0 | REST API Framework |
| C# | 12 | Language |

### Architecture & Patterns
| Pattern | Implementation |
|---------|----------------|
| Clean Architecture | Domain → Application → Infrastructure → API |
| Repository Pattern | Generic + Specific repositories |
| CQRS | MediatR for commands/queries |
| Dependency Injection | Built-in .NET DI |

### Database & ORM
| Technology | Purpose |
|------------|---------|
| PostgreSQL 16 | Primary database |
| Entity Framework Core 8 | ORM |
| Npgsql | PostgreSQL provider |
| Row-Level Security | Tenant isolation via Global Query Filters |

### Authentication & Security
| Technology | Purpose |
|------------|---------|
| ASP.NET Core Identity | User management |
| JWT Bearer Tokens | API authentication |
| Refresh Tokens | Long-lived sessions |
| BCrypt | Password hashing |

### Logging & Monitoring
| Technology | Purpose |
|------------|---------|
| Serilog | Structured logging |
| Seq (or ELK) | Log aggregation |
| CorrelationId | Request tracing |

### Validation & Mapping
| Technology | Purpose |
|------------|---------|
| FluentValidation | Request validation |
| AutoMapper (or Mapster) | DTO mapping |

### Background Jobs (Future)
| Technology | Purpose |
|------------|---------|
| Hangfire | Scheduled jobs, reminders |

---

## Frontend Stack (React)

### Core Framework
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 18.x | UI Framework |
| TypeScript | 5.x | Type safety |
| Vite | 5.x | Build tool & dev server |

### State Management
| Technology | Purpose |
|------------|---------|
| TanStack Query (React Query) | Server state, caching |
| Zustand | Client state (minimal) |

### Routing & Navigation
| Technology | Purpose |
|------------|---------|
| React Router v6 | Client-side routing |

### Styling
| Technology | Purpose |
|------------|---------|
| Tailwind CSS | Utility-first styling |
| tailwind-merge | Class merging |
| clsx | Conditional classes |
| Headless UI | Accessible components |

### Forms & Validation
| Technology | Purpose |
|------------|---------|
| React Hook Form | Form handling |
| Zod | Schema validation |

### UI Components
| Technology | Purpose |
|------------|---------|
| Radix UI | Primitive components |
| Lucide React | Icons |
| date-fns | Date manipulation |
| FullCalendar | Calendar views |

### Development Tools
| Technology | Purpose |
|------------|---------|
| MSW (Mock Service Worker) | API mocking |
| ESLint | Linting |
| Prettier | Formatting |

---

## DevOps & Infrastructure

### Containerization
| Technology | Purpose |
|------------|---------|
| Docker | Containerization |
| Docker Compose | Local development |

### CI/CD (Future)
| Technology | Purpose |
|------------|---------|
| GitHub Actions | CI/CD pipelines |

### Cloud (Future)
| Technology | Purpose |
|------------|---------|
| Azure / AWS | Cloud hosting |
| Azure SQL / RDS | Managed PostgreSQL |

---

## Project Structure

```
BarakMeetings/
├── backend/
│   ├── src/
│   │   ├── BookingPlatform.Domain/           # Entities, Enums, Interfaces
│   │   ├── BookingPlatform.Application/      # Use Cases, DTOs, Services
│   │   ├── BookingPlatform.Infrastructure/   # EF Core, Identity, External
│   │   └── BookingPlatform.API/              # Controllers, Middleware
│   ├── tests/
│   │   ├── BookingPlatform.Domain.Tests/
│   │   ├── BookingPlatform.Application.Tests/
│   │   └── BookingPlatform.API.Tests/
│   └── BookingPlatform.sln
│
├── frontend/
│   ├── src/
│   │   ├── api/                # API client, endpoints
│   │   ├── components/         # Shared UI components
│   │   ├── features/           # Feature modules
│   │   ├── hooks/              # Custom hooks
│   │   ├── layouts/            # Page layouts
│   │   ├── lib/                # Utilities
│   │   ├── routes/             # Route definitions
│   │   ├── stores/             # Zustand stores
│   │   ├── styles/             # Global styles
│   │   └── types/              # TypeScript types
│   ├── public/
│   └── package.json
│
├── contracts/                   # Shared API contracts (OpenAPI/TypeScript)
│   ├── openapi.yaml
│   └── types/
│
├── docker/
│   ├── docker-compose.yml
│   ├── Dockerfile.api
│   └── Dockerfile.frontend
│
└── docs/
    ├── TECH_STACK.md
    ├── API_CONTRACTS.md
    └── ARCHITECTURE.md
```

---

## NuGet Packages (Backend)

```xml
<!-- Domain Layer - No external dependencies -->

<!-- Application Layer -->
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="FluentValidation" Version="11.9.0" />
<PackageReference Include="AutoMapper" Version="13.0.1" />

<!-- Infrastructure Layer -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.1" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.1" />

<!-- API Layer -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="6.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.1" />
```

---

## NPM Packages (Frontend)

```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.22.0",
    "@tanstack/react-query": "^5.17.0",
    "zustand": "^4.5.0",
    "axios": "^1.6.5",
    "react-hook-form": "^7.49.0",
    "@hookform/resolvers": "^3.3.4",
    "zod": "^3.22.4",
    "date-fns": "^3.3.0",
    "@fullcalendar/react": "^6.1.10",
    "@fullcalendar/daygrid": "^6.1.10",
    "@fullcalendar/timegrid": "^6.1.10",
    "@radix-ui/react-dialog": "^1.0.5",
    "@radix-ui/react-dropdown-menu": "^2.0.6",
    "@radix-ui/react-select": "^2.0.0",
    "lucide-react": "^0.312.0",
    "clsx": "^2.1.0",
    "tailwind-merge": "^2.2.0"
  },
  "devDependencies": {
    "typescript": "^5.3.3",
    "vite": "^5.0.12",
    "@types/react": "^18.2.48",
    "@types/react-dom": "^18.2.18",
    "tailwindcss": "^3.4.1",
    "postcss": "^8.4.33",
    "autoprefixer": "^10.4.17",
    "eslint": "^8.56.0",
    "prettier": "^3.2.4",
    "msw": "^2.1.4"
  }
}
```

---

## Environment Setup Requirements

### Prerequisites
- Node.js 20.x LTS
- .NET SDK 8.0
- PostgreSQL 16
- Docker Desktop (optional)
- VS Code or Rider (Backend) + VS Code (Frontend)

### Recommended VS Code Extensions
- C# Dev Kit
- ESLint
- Prettier
- Tailwind CSS IntelliSense
- Thunder Client (API testing)
