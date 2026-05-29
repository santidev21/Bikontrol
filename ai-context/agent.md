# BIKONTROL - FULL PROJECT ANALYSIS

## GENERAL OVERVIEW
**Bikontrol** is a web application that helps motorcycle owners manage and track maintenance tasks.

### Tech Stack
- **Frontend:** Angular 18 (Standalone Components) + SCSS + Tailwind CSS
- **Backend:** .NET 8 (Clean Architecture) + Entity Framework Core
- **Database:** PostgreSQL
- **Authentication:** JWT (JSON Web Tokens)
- **Testing:** Jest (Frontend), xUnit (Backend)

---

## BACKEND ARCHITECTURE (.NET)

### Layer Structure

```
Bikontrol.API (Presentation Layer)
├── Controllers (AuthController, MotorcyclesController, MaintenanceController)
└── Middleware (ExceptionHandlingMiddleware)

Bikontrol.Application (Business Logic Layer)
├── Interfaces (IAuthService, IMotorcycleService, IMaintenanceService, ICurrentUserService)
├── DTOs (Data Transfer Objects)
└── Validators

Bikontrol.Infrastructure (Service Implementation)
├── Services (AuthService, MotorcycleService, MaintenanceService, KmHistoryService, CurrentUserService)
├── Authentication (JwtTokenGenerator)
└── Mapping (AutoMapper MappingProfile)

Bikontrol.Persistence (Data Access Layer)
├── AppDbContext
├── Repositories (UserRepository, MotorcycleRepository, MaintenanceRepository, UserMaintenanceRepository, KmHistoryRepository)
└── Configurations (Entity configurations)

Bikontrol.Domain (Domain Layer)
└── Entities (User, Motorcycle, Maintenance, UserMaintenance, MotorcycleKmHistory)

Bikontrol.Shared (Cross-cutting Concerns)
└── Exceptions (AuthException, NotFoundException, ForbiddenAccessException, ValidationException)
```

### Domain Models

#### User
```csharp
- Id (Guid)
- Email (string)
- PasswordHash (string)
- FullName (string)
- CreatedAt (DateTime)
- Motorcycles (IList<Motorcycle>)
```

#### Motorcycle
```csharp
- Id (Guid)
- Name, Brand, Year, Nickname, Displacement, Plate (strings/ints)
- Image (string, default: "default.png")
- IsEnabled (bool, soft delete)
- UserId (Guid) - FK to User
- KmHistory (ICollection<MotorcycleKmHistory>)
```

#### MotorcycleKmHistory
```csharp
- Id (Guid)
- MotorcycleId (Guid) - FK
- Km (int)
- RecordedAt (DateTime)
```

#### Maintenance
```csharp
- Id (Guid)
- Name, Description (strings)
- DefaultKmInterval, DefaultTimeIntervalWeeks (int?)
- TrackingType (string: "Km" or "Time")
- IsEnabled (bool)
- UserMaintenanceTypes (ICollection<UserMaintenance>)
```

#### UserMaintenance
```csharp
- Id (Guid)
- UserId (Guid) - FK
- BaseTypeId (Guid?) - FK to Maintenance
- Name, Description (strings)
- KmInterval, TimeIntervalWeeks (int?)
- TrackingType (string: "Km" or "Time")
- IsEnabled (bool)
```

---

## REST API ENDPOINTS

### Authentication
- **POST** `/api/auth/register` - User registration
  - Request: { email, password, fullName }
  - Response: RegisterResponse { id, email, fullName, createdAt, token }

- **POST** `/api/auth/login` - Login
  - Request: { email, password }
  - Response: LoginResponse { id, email, fullName, token }

### Motorcycles (Authenticated)
- **POST** `/api/motorcycles` - Create motorcycle
  - Request: SaveMotorcycleDTO { name, brand, year, nickname, km, displacement, plate }
  - Response: MotorcycleDTO { id, name, brand, year, nickname, km, image, displacement, plate }

- **GET** `/api/motorcycles/mine` - Get my motorcycles
  - Response: MotorcycleDTO[]

- **GET** `/api/motorcycles/{id}` - Get motorcycle by ID

- **PUT** `/api/motorcycles/{id}` - Update motorcycle
  - Request: SaveMotorcycleDTO

- **DELETE** `/api/motorcycles/{id}` - Soft delete motorcycle

### Maintenance (Authenticated)
- **GET** `/api/maintenances/defaults` - Get default maintenance types
  - Response: MaintenanceDTO[]

- **GET** `/api/maintenances/mine` - Get my maintenance items
  - Response: MaintenanceDTO[]

- **GET** `/api/maintenances/{id}` - Get maintenance by ID

- **POST** `/api/maintenances/mine` - Create custom maintenance
  - Request: SaveMaintenanceDTO { baseTypeId?, name, description?, kmInterval?, timeIntervalWeeks?, trackingType }

- **POST** `/api/maintenances/follow` - Follow default maintenance
  - Request: FollowDefaultRequest { defaultId, kmInterval?, timeIntervalWeeks?, trackingType }

- **PUT** `/api/maintenances/{id}` - Update maintenance

- **DELETE** `/api/maintenances/mine/{id}` - Delete maintenance

---

## FRONTEND ARCHITECTURE (ANGULAR)

### Folder Structure
```text
src/
├── app/
│   ├── app.config.ts (Angular configuration)
│   ├── app.routes.ts (Routes)
│   ├── app.component.ts
│   ├── shared/
│   │   ├── services/
│   │   │   └── swal.service.ts (Notifications)
│   │   ├── components/
│   │   │   ├── top-nav/
│   │   │   └── bottom-nav/
│   │   └── interceptors/
│   │       └── auth.interceptor.ts (Adds JWT token)
│   ├── modules/
│   │   ├── auth/
│   │   │   ├── pages/
│   │   │   │   ├── login/
│   │   │   │   └── register/
│   │   │   ├── services/
│   │   │   │   └── auth.service.ts
│   │   │   ├── interfaces/
│   │   │   │   └── auth.model.ts
│   │   │   └── auth-imports.ts
│   │   └── dashboard/
│   │       ├── pages/
│   │       │   ├── home/
│   │       │   ├── motorcycles/
│   │       │   │   ├── motorcycle-summary/
│   │       │   │   └── save-motorcycle/
│   │       │   └── maintenance/
│   │       │       ├── maintenance-page/
│   │       │       ├── save-maintenance/
│   │       │       └── components/
│   │       ├── services/
│   │       │   ├── motorcycles.service.ts
│   │       │   └── maintenance.service.ts
│   │       ├── components/
│   │       │   ├── dashboard-layout/
│   │       │   ├── motorcycle-card/
│   │       │   └── maintenance-info-card/
│   │       └── interfaces/
│   │           ├── motorcycle.interface.ts
│   │           └── maintenance.interface.ts
├── environments/
├── styles.scss
├── main.ts
└── index.html
```

### Main Routes
```typescript
- /login -> LoginComponent (public)
- /register -> RegisterComponent (public)
- /dashboard -> DashboardLayoutComponent
  - /dashboard/home -> HomeComponent
  - /dashboard/motorcycles/summary -> MotorcycleSummaryComponent
  - /dashboard/motorcycles/add -> SaveMotorcycleComponent (create)
  - /dashboard/motorcycles/edit/:id -> SaveMotorcycleComponent (edit)
  - /dashboard/maintenance -> MaintenancePageComponent
  - /dashboard/maintenance/add -> SaveMaintenanceComponent (create)
  - /dashboard/maintenance/edit/:id -> SaveMaintenanceComponent (edit)
```

---

## DATA FLOW AND SERVICES

### AuthService (Frontend)
```typescript
- login(email, password): Observable<LoginResponse>
  -> POST /api/auth/login
  -> Stores token in localStorage

- register(data): Observable<RegisterResponse>
  -> POST /api/auth/register
  -> Stores token in localStorage

- logout(): void
  -> Removes token

- isAuthenticated(): boolean
  -> Checks whether a token exists
```

### AuthInterceptor
- Intercepts every HTTP request
- Adds header: `Authorization: Bearer ${token}`

### MotorcyclesService (Frontend)
```typescript
- getMyMotorcycles(): Observable<Motorcycle[]>
  -> GET /api/motorcycles/mine

- getById(id): Observable<Motorcycle>
  -> GET /api/motorcycles/{id}

- addMotorcycle(dto): Observable<Motorcycle>
  -> POST /api/motorcycles

- updateMotorcycle(id, dto): Observable<void>
  -> PUT /api/motorcycles/{id}

- deleteMotorcycle(id): Observable<void>
  -> DELETE /api/motorcycles/{id}
```

### MaintenanceService (Frontend)
```typescript
- getDefaultMaintenance(): Observable<Maintenance[]>
  -> GET /api/maintenances/defaults

- getUserMaintenance(): Observable<Maintenance[]>
  -> GET /api/maintenances/mine

- getById(id): Observable<Maintenance>
  -> GET /api/maintenances/{id}

- createUserMaintenance(maintenance): Observable<Maintenance>
  -> POST /api/maintenances/mine

- followDefaultMaintenance(payload): Observable<Maintenance>
  -> POST /api/maintenances/follow

- updateMaintenance(id, dto): Observable<void>
  -> PUT /api/maintenances/{id}

- deleteMaintenance(id): Observable<void>
  -> DELETE /api/maintenances/mine/{id}
```

### Backend Services

**AuthService**
- RegisterAsync: validates unique email, hashes password, creates user, generates JWT
- LoginAsync: validates credentials, generates JWT

**MotorcycleService**
- CreateAsync: creates a motorcycle and records the initial km in history
- GetByIdAsync: gets a motorcycle (validates ownership)
- GetByCurrentUserAsync: gets all motorcycles for the current user
- UpdateAsync: updates a motorcycle (validates ownership)
- SoftDeleteAsync: marks a motorcycle as disabled (validates ownership)

**MaintenanceService**
- GetDefaultsAsync: gets default maintenance types
- GetUserMaintenanceAsync: gets the current user's custom maintenance items
- CreateUserMaintenanceAsync: creates custom maintenance
- FollowDefaultAsync: user follows a default maintenance type
- UpdateAsync: updates custom maintenance
- DeleteUserMaintenanceAsync: soft deletes custom maintenance

**CurrentUserService**
- Extracts the userId from the JWT (HttpContext) via claims
- Injected into services that need the current user

---

## AUTHENTICATION AND SECURITY

### JWT Tokens
```text
Claims:
- sub: userId (Guid)
- email: user's email
- fullName: full name
- jti: unique token identifier

Configuration:
- Key: "LACLAVEDELBIKONTROL_ESTA_NO_ME_LA_HACKEA_NADIENN" (development)
- Issuer: "BikontrolAPI"
- Audience: "BikontrolClient"
- Expires: 30 minutes
```

### Token Validation
- Algorithm: HMAC SHA256
- Validation in Program.cs:
  - ValidateIssuer
  - ValidateAudience
  - ValidateLifetime
  - ValidateIssuerSigningKey

### Exception Middleware
```csharp
Maps exceptions to HTTP status codes:
- AuthException -> 401/409
- ValidationException -> 400
- NotFoundException -> 404
- ForbiddenAccessException -> 403
- Everything else -> 500 (Internal Server Error)
```

### Access Control
- All controllers except Auth use the `[Authorize]` attribute
- CurrentUserService validates resource ownership
- Soft deletes are used for users and maintenance items (IsEnabled flag)

---

## DATABASE

### Main Tables
1. **Users**
   - Id (UUID Primary Key)
   - Email (Unique)
   - PasswordHash
   - FullName
   - CreatedAt

2. **Motorcycles**
   - Id (UUID Primary Key)
   - Name, Brand, Year, Nickname, Displacement, Plate
   - Image (default: "default.png")
   - IsEnabled (Soft Delete)
   - UserId (FK -> Users)

3. **MotorcycleKmHistories**
   - Id (UUID Primary Key)
   - MotorcycleId (FK -> Motorcycles)
   - Km
   - RecordedAt

4. **Maintenances**
   - Id (UUID Primary Key)
   - Name, Description
   - DefaultKmInterval, DefaultTimeIntervalWeeks
   - TrackingType ("Km" or "Time")
   - IsEnabled

5. **UserMaintenances**
   - Id (UUID Primary Key)
   - UserId (FK -> Users)
   - BaseTypeId (FK -> Maintenances, nullable)
   - Name, Description
   - KmInterval, TimeIntervalWeeks
   - TrackingType
   - IsEnabled

### Connection
- Provider: PostgreSQL
- Connection String: `Host=localhost;Port=5432;Database=bikontrol_db;Username=postgres;Password=sa123456`

---

## KEY FRONTEND COMPONENTS

### LoginComponent
- Reactive form: email, password
- Validation: email format, password minLength(6)
- Error handling: displays server error messages
- Navigation: redirects to /dashboard on success

### RegisterComponent
- Reactive form: fullName, email, password, confirmPassword
- Custom validator: passwordMatchValidator
- Same flow as login

### HomeComponent
- Loads the user's motorcycles
- Displays motorcycle cards (MotorcycleCardComponent)
- Button to add a new motorcycle

### SaveMotorcycleComponent
- Create/edit mode (detects :id in the route)
- Reactive form with validations
- Fields: name, brand, year, nickname, km, displacement, plate
- SweetAlert2 for notifications
- On create: records the initial km in history

### SaveMaintenanceComponent
- Create/edit mode
- Monitoring type selector: Km or Time
- Time unit conversion (weeks, months, years)
- SweetAlert2 for notifications

---

## MAIN DEPENDENCIES

### Backend (.NET 8)
- AutoMapper: DTO-to-entity mapping
- Entity Framework Core: ORM
- Npgsql: PostgreSQL driver
- Microsoft.AspNetCore.Identity: password hashing
- System.IdentityModel.Tokens.Jwt: JWT handling

### Frontend (Angular 18)
- @angular/core, @angular/common, @angular/forms
- @angular/router: routing
- @angular/platform-browser: HTTP client
- rxjs: reactive programming
- sweetalert2: notifications
- tailwindcss: styles
- jest: testing

---

## IMPORTANT CONFIGURATION

### appsettings.json (Backend)
```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bikontrol_db;..."
  },
  "Jwt": {
    "Key": "clave-super-secreta",
    "Issuer": "BikontrolAPI",
    "Audience": "BikontrolClient"
  }
}
```

### CORS (Backend)
- Allowed origin: `http://localhost:4200`
- Methods: All
- Headers: All

### Frontend package.json scripts
```bash
npm start -> ng serve (port 4200)
npm run build -> ng build
npm test -> jest
npm run test:watch -> jest watch mode
```

---

## AUTO MAPPINGS

```csharp
User ↔ RegisterRequest/RegisterResponse/LoginResponse
Motorcycle ↔ SaveMotorcycleDTO/MotorcycleDTO
Maintenance ↔ MaintenanceDTO (maps DefaultKmInterval -> KmInterval, etc.)
UserMaintenance ↔ MaintenanceDTO/SaveMaintenanceDTO
```

---

## TYPICAL OPERATION FLOW

### Register/Login
1. User opens `/login` or `/register`
2. Completes the form and submits
3. AuthService -> POST /api/auth/login|register
4. Backend validates, creates the user, generates JWT
5. Frontend stores the token in localStorage
6. AuthInterceptor adds it to every request
7. Redirects to `/dashboard`

### Create Motorcycle
1. User opens `/dashboard/motorcycles/add`
2. Completes the SaveMotorcycleComponent form
3. MotorcyclesService -> POST /api/motorcycles
4. Backend creates Motorcycle + records the initial KmHistory
5. Frontend redirects to home
6. HomeComponent reloads the list

### Create Custom Maintenance
1. User opens `/dashboard/maintenance/add`
2. Completes the SaveMaintenanceComponent form
3. Chooses type: Km or Time
4. MaintenanceService -> POST /api/maintenances/mine
5. Backend creates UserMaintenance
6. Frontend redirects to `/dashboard/maintenance`

---

## VALIDATIONS

### Client-side (Angular)
- Reactive forms with Validators
- Field validations in HTML
- SweetAlert2 for confirmations and errors

### Server-side (.NET)
- DataAnnotations in DTOs
- Validation logic in domain entities
- Exception middleware for consistent responses
- Resource ownership validation

---

## POTENTIAL IMPROVEMENTS

- [ ] Implement refresh tokens
- [ ] Add roles/permissions (Admin, User)
- [ ] Pagination in lists
- [ ] Filters and search
- [ ] Change history (audit log)
- [ ] Upcoming maintenance notifications
- [ ] External API integrations (weather, traffic)
- [ ] Export reports (PDF)
- [ ] WhatsApp/Email reminders

---

## CONCLUSION

**Bikontrol** is a well-structured application following Clean Architecture in the backend and Angular best practices in the frontend. Authentication is secure with JWT, validation happens in both layers, and responsibility boundaries are clear. The project is ready to scale with features like roles, notifications, and reports.

---

**Generated:** May 13, 2026  
**Version:** 1.0

## Root Project Commands

These commands live in `C:\Dev\Bikontrol\package.json` and let you work from the repository root:

- `npm run bikontrol` -> starts the frontend and backend together.
- `npm run bikontrol-ui` -> starts only the Angular frontend on `http://localhost:4201`.
- `npm run bikontrol-api` -> starts only the .NET backend on `https://localhost:7179`.
- `npm run build` -> builds frontend and backend.
- `npm run test` -> runs frontend and backend tests.
- `npm run db:update` -> applies EF Core migrations.
- `npm run db:migration:add -- MigrationName` -> creates a new migration.
- `npm run db:migration:remove` -> removes the last migration.
- `npm run db:drop` -> drops the database with EF Core.
- The backend runner ignores `launchSettings.json` and forces `https://localhost:7179` to avoid conflicts with `5202`.
- The frontend runner forces `http://localhost:4201` to avoid conflicts with `4200`.

## Rules for the AI
- Always review the specs before implementing.
- Do not invent requirements outside the specs.
- Document every relevant change in this source of truth.

## AI Guidelines
- Do not use comments with emojis.
- Do not fill the project with comments; only add the necessary ones.
- Keep code and comments in English.
- Always ask before implementing.
- Do not run git commands unless the user explicitly asks for them.
- For any large change, present 3 possible options and let the user decide; show pros and cons for each.
- Do not make large changes without asking first.

---

## MAY 22, 2026 UPDATE (KILOMETER EDITING AND ROLLBACK)

### UX and Rules
- In `motorcycle-summary`, the `Current Mileage` card now includes:
  - an edit button (pencil icon) to open the update modal.
  - a subtle rollback action: `Did you enter the wrong mileage? Roll it back here.`
- UI rule: you cannot record a km value lower than the current one.
- Rollback scope: only the last change (returns to the previous state).

### Backend
- Existing km history endpoints were reused:
  - `GET /api/motorcycles/{id}/km/current`
  - `POST /api/motorcycles/{id}/km-history`
  - `DELETE /api/motorcycles/{id}/km-history/last`
- Adjustment in `KmHistoryService.RollbackLastKmAsync`:
  - if `newKm == last.Km`, remove the last record and finish (real undo without duplicates).

### Frontend
- `motorcycle-summary`:
  - modal to edit km with a numeric input.
  - success swal and summary refresh.
  - confirmation to roll back the last change.
- `top-nav`:
  - `showBackButton` now includes the summary route.
  - back from summary navigates to `/dashboard/home`.
- `motorcycle-card`:
  - shows the current km fetched from the endpoint instead of the static field that was staying at `0`.

### Testing Added/Updated
- Backend (`xUnit`):
  - `AddKmAsync` greater than current: adds.
  - `AddKmAsync` lower than current: `ValidationException`.
  - rollback when `newKm == last`: undoes the last change.
  - rollback when `newKm == previous`: does not duplicate.
- Frontend (`Jest`, unit specs by class):
  - motorcycle service contracts for `km/current`, `km-history`, `km-history/last`.
  - summary: modal, km validation, save and rollback.
  - top-nav: back in summary goes home.
  - motorcycle-card: real km from endpoint.

## MAY 22, 2026 UPDATE (MOTORCYCLE-SCOPED MAINTENANCE)

### Domain Model Changes

#### UserMaintenance (updated)
```csharp
- Id (Guid)
- UserId (Guid) - FK to User
- MotorcycleId (Guid) - FK to Motorcycle
- BaseTypeId (Guid?) - FK to Maintenance
- Name, Description (strings)
- KmInterval, TimeIntervalWeeks (int?)
- TrackingType (string: "Km" or "Time")
- IsEnabled (bool)
```

#### MotorcycleMaintenanceRecord (new)
```csharp
- Id (Guid)
- MotorcycleId (Guid) - FK
- UserMaintenanceId (Guid) - FK
- PerformedAt (DateTime)
- PerformedKm (int?)
- CreatedAt (DateTime)
```

### Database Changes
- `UserMaintenanceTypes` now includes a required `MotorcycleId`.
- New table: `MotorcycleMaintenanceRecords`.
- New indexes for upcoming maintenance and history queries.

### New/Updated API Endpoints

#### Motorcycles
- **GET** `/api/motorcycles/{id}/km/current`
  - Response: `{ km }`
- **POST** `/api/motorcycles/{id}/km-history`
  - Request: `{ km }`
- **DELETE** `/api/motorcycles/{id}/km-history/last`
  - Request: `{ newKm }`

#### Maintenance
- **GET** `/api/maintenances/mine/motorcycle/{motorcycleId}`
  - Gets the user's maintenance items for a specific motorcycle.

- **POST** `/api/maintenances/follow` (updated)
  - Request: `{ motorcycleId, defaultId, trackingType, kmInterval, timeIntervalWeeks }`

- **POST** `/api/maintenances/records`
  - Request: `{ motorcycleId, userMaintenanceId, performedAt, performedKm? }`

- **GET** `/api/maintenances/motorcycle/{motorcycleId}/records`
  - Maintenance history for a motorcycle.

- **GET** `/api/maintenances/motorcycle/{motorcycleId}/upcoming`
  - Upcoming maintenance calculated by the backend (life cycle, overdue, remaining).

### Implemented Business Rules
- Strict ownership: every motorcycle operation validates that the motorcycle belongs to the authenticated user.
- Maintenance recording:
  - Date cannot be later than today.
  - If it is km-based, it cannot be lower than the last record for that maintenance item.
- When recording maintenance with km:
  - Updates `KmHistory` only if the km is greater than the current one.
  - If it is equal, no duplicate record is created.
- KmHistory rollback:
  - The initial record cannot be deleted.
- Life cycle calculation:
  - Calculated in the endpoint, not stored.
  - Clamped between 0% and 100%.
  - Upcoming items are ordered from most overdue (0%) to most recent (100%).
- Time-based maintenance without records:
  - Uses the motorcycle's initial date (the first `KmHistory.RecordedAt`) as the baseline.

### Updated Frontend Routes
```typescript
- /dashboard/motorcycles/summary
- /dashboard/motorcycles/add
- /dashboard/motorcycles/edit/:id
- /dashboard/motorcycles/:motorcycleId/maintenance
- /dashboard/motorcycles/:motorcycleId/maintenance/add
- /dashboard/motorcycles/:motorcycleId/maintenance/edit/:id
- /dashboard/motorcycles/:motorcycleId/register-maintenance
```

### Key UI Changes
- In `motorcycle-summary`:
  - `Register maintenance` button above upcoming maintenance.
  - `Add maintenance` button below the upcoming grid.
  - Upcoming cards use a responsive grid (2 columns on mobile, 3 on desktop).
  - Register button is disabled if there are no associated maintenance items.
  - Visual separator before `Maintenance history`.
- In top nav:
  - Home shows the hamburger menu.
  - Maintenance and motorcycle creation/registration routes show a back button.

### Testing Added
- Backend (`xUnit`):
  - Km history rollback validations.
  - Life cycle recalculation when frequency changes.
  - Time-based maintenance without records (baseline from the motorcycle's initial date).
- Frontend (`Jest`):
  - Summary disables registration when there are no maintenance items.
  - Top nav shows a back button on maintenance routes and navigates correctly to the summary.
