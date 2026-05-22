# 🏍️ BIKONTROL - ANÁLISIS COMPLETO DEL PROYECTO

## 📋 DESCRIPCIÓN GENERAL
**Bikontrol** es una aplicación web para que propietarios de motocicletas gestionen y tracken tareas de mantenimiento.

### Stack Tecnológico
- **Frontend:** Angular 18 (Standalone Components) + SCSS + Tailwind CSS
- **Backend:** .NET 8 (Clean Architecture) + Entity Framework Core
- **Base de Datos:** PostgreSQL
- **Autenticación:** JWT (JSON Web Tokens)
- **Testing:** Jest (Frontend), xUnit (Backend)

---

## 🏛️ ARQUITECTURA DEL BACKEND (.NET)

### Estructura de Capas

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
├── Entities (User, Motorcycle, Maintenance, UserMaintenance, MotorcycleKmHistory)

Bikontrol.Shared (Cross-cutting Concerns)
└── Exceptions (AuthException, NotFoundException, ForbiddenAccessException, ValidationException)
```

### Modelos de Dominio

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
- UserId (Guid) - FK a User
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
- TrackingType (string: "Km" o "Time")
- IsEnabled (bool)
- UserMaintenanceTypes (ICollection<UserMaintenance>)
```

#### UserMaintenance
```csharp
- Id (Guid)
- UserId (Guid) - FK
- BaseTypeId (Guid?) - FK a Maintenance
- Name, Description (strings)
- KmInterval, TimeIntervalWeeks (int?)
- TrackingType (string: "Km" o "Time")
- IsEnabled (bool)
```

---

## 🔌 API REST ENDPOINTS

### Autenticación
- **POST** `/api/auth/register` - Registro de usuario
  - Request: { email, password, fullName }
  - Response: RegisterResponse { id, email, fullName, createdAt, token }
  
- **POST** `/api/auth/login` - Login
  - Request: { email, password }
  - Response: LoginResponse { id, email, fullName, token }

### Motocicletas (Autorizadas)
- **POST** `/api/motorcycles` - Crear motocicleta
  - Request: SaveMotorcycleDTO { name, brand, year, nickname, km, displacement, plate }
  - Response: MotorcycleDTO { id, name, brand, year, nickname, km, image, displacement, plate }

- **GET** `/api/motorcycles/mine` - Obtener mis motocicletas
  - Response: MotorcycleDTO[]

- **GET** `/api/motorcycles/{id}` - Obtener motocicleta por ID

- **PUT** `/api/motorcycles/{id}` - Actualizar motocicleta
  - Request: SaveMotorcycleDTO

- **DELETE** `/api/motorcycles/{id}` - Soft delete de motocicleta

### Mantenimiento (Autorizadas)
- **GET** `/api/maintenances/defaults` - Obtener tipos de mantenimiento por defecto
  - Response: MaintenanceDTO[]

- **GET** `/api/maintenances/mine` - Obtener mis mantenimientos
  - Response: MaintenanceDTO[]

- **GET** `/api/maintenances/{id}` - Obtener mantenimiento por ID

- **POST** `/api/maintenances/mine` - Crear mantenimiento personalizado
  - Request: SaveMaintenanceDTO { baseTypeId?, name, description?, kmInterval?, timeIntervalWeeks?, trackingType }

- **POST** `/api/maintenances/follow` - Seguir mantenimiento por defecto
  - Request: FollowDefaultRequest { defaultId, kmInterval?, timeIntervalWeeks?, trackingType }

- **PUT** `/api/maintenances/{id}` - Actualizar mantenimiento

- **DELETE** `/api/maintenances/mine/{id}` - Eliminar mantenimiento

---

## 🎨 ARQUITECTURA DEL FRONTEND (ANGULAR)

### Estructura de Carpetas
```
src/
├── app/
│   ├── app.config.ts (Configuración Angular)
│   ├── app.routes.ts (Rutas)
│   ├── app.component.ts
│   ├── shared/
│   │   ├── services/
│   │   │   └── swal.service.ts (Notificaciones)
│   │   ├── components/
│   │   │   ├── top-nav/
│   │   │   └── bottom-nav/
│   │   └── interceptors/
│   │       └── auth.interceptor.ts (Agrega token JWT)
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
├── index.html
```

### Rutas Principales
```typescript
- /login -> LoginComponent (público)
- /register -> RegisterComponent (público)
- /dashboard -> DashboardLayoutComponent
  - /dashboard/home -> HomeComponent
  - /dashboard/motorcycles/summary -> MotorcycleSummaryComponent
  - /dashboard/motorcycles/add -> SaveMotorcycleComponent (crear)
  - /dashboard/motorcycles/edit/:id -> SaveMotorcycleComponent (editar)
  - /dashboard/maintenance -> MaintenancePageComponent
  - /dashboard/maintenance/add -> SaveMaintenanceComponent (crear)
  - /dashboard/maintenance/edit/:id -> SaveMaintenanceComponent (editar)
```

---

## 📡 FLUJO DE DATOS Y SERVICIOS

### AuthService (Frontend)
```typescript
- login(email, password): Observable<LoginResponse>
  → POST /api/auth/login
  → Guarda token en localStorage

- register(data): Observable<RegisterResponse>
  → POST /api/auth/register
  → Guarda token en localStorage

- logout(): void
  → Elimina token

- isAuthenticated(): boolean
  → Verifica si existe token
```

### AuthInterceptor
- Intercepta todas las peticiones HTTP
- Agrega header: `Authorization: Bearer ${token}`

### MotorcyclesService (Frontend)
```typescript
- getMyMotorcycles(): Observable<Motorcycle[]>
  → GET /api/motorcycles/mine

- getById(id): Observable<Motorcycle>
  → GET /api/motorcycles/{id}

- addMotorcycle(dto): Observable<Motorcycle>
  → POST /api/motorcycles

- updateMotorcycle(id, dto): Observable<void>
  → PUT /api/motorcycles/{id}

- deleteMotorcycle(id): Observable<void>
  → DELETE /api/motorcycles/{id}
```

### MaintenanceService (Frontend)
```typescript
- getDefaultMaintenance(): Observable<Maintenance[]>
  → GET /api/maintenances/defaults

- getUserMaintenance(): Observable<Maintenance[]>
  → GET /api/maintenances/mine

- getById(id): Observable<Maintenance>
  → GET /api/maintenances/{id}

- createUserMaintenance(maintenance): Observable<Maintenance>
  → POST /api/maintenances/mine

- followDefaultMaintenance(payload): Observable<Maintenance>
  → POST /api/maintenances/follow

- updateMaintenance(id, dto): Observable<void>
  → PUT /api/maintenances/{id}

- deleteMaintenance(id): Observable<void>
  → DELETE /api/maintenances/mine/{id}
```

### Backend Services

**AuthService**
- RegisterAsync: Valida email único, hashea contraseña, crea usuario, genera JWT
- LoginAsync: Valida credenciales, genera JWT

**MotorcycleService**
- CreateAsync: Crea moto, registra km inicial en historial
- GetByIdAsync: Obtiene moto (validar propiedad del usuario)
- GetByCurrentUserAsync: Obtiene todas las motos del usuario
- UpdateAsync: Actualiza moto (validar propiedad)
- SoftDeleteAsync: Marca como deshabilitada (validar propiedad)

**MaintenanceService**
- GetDefaultsAsync: Obtiene tipos de mantenimiento por defecto
- GetUserMaintenanceAsync: Obtiene mantenimientos personalizados del usuario
- CreateUserMaintenanceAsync: Crea mantenimiento personalizado
- FollowDefaultAsync: Usuario sigue tipo de mantenimiento predeterminado
- UpdateAsync: Actualiza mantenimiento personalizado
- DeleteUserMaintenanceAsync: Soft delete de mantenimiento personalizado

**CurrentUserService**
- Extrae userId del JWT (HttpContext) mediante claims
- Se inyecta en servicios que necesitan saber el usuario actual

---

## 🔐 AUTENTICACIÓN Y SEGURIDAD

### JWT Tokens
```
Claims:
- sub: userId (Guid)
- email: email del usuario
- fullName: nombre completo
- jti: Identificador único del token

Configuración:
- Key: "LACLAVEDELBIKONTROL_ESTA_NO_ME_LA_HACKEA_NADIENN" (desarrollo)
- Issuer: "BikontrolAPI"
- Audience: "BikontrolClient"
- Expires: 30 minutos
```

### Validación de Tokens
- Firma: HMAC SHA256
- Validaciones en Program.cs:
  - ValidateIssuer
  - ValidateAudience
  - ValidateLifetime
  - ValidateIssuerSigningKey

### Middleware de Excepciones
```csharp
Mapea excepciones a HTTP Status Codes:
- AuthException → 401/409
- ValidationException → 400
- NotFoundException → 404
- ForbiddenAccessException → 403
- Resto → 500 (Internal Server Error)
```

### Control de Acceso
- Todos los controladores excepto Auth tienen atributo `[Authorize]`
- CurrentUserService valida pertenencia de recursos
- Soft deletes para usuarios y mantenimientos (IsEnabled flag)

---

## 🗄️ BASE DE DATOS

### Tablas Principales
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
   - UserId (FK → Users)

3. **MotorcycleKmHistories**
   - Id (UUID Primary Key)
   - MotorcycleId (FK → Motorcycles)
   - Km
   - RecordedAt

4. **Maintenances**
   - Id (UUID Primary Key)
   - Name, Description
   - DefaultKmInterval, DefaultTimeIntervalWeeks
   - TrackingType ("Km" o "Time")
   - IsEnabled

5. **UserMaintenances**
   - Id (UUID Primary Key)
   - UserId (FK → Users)
   - BaseTypeId (FK → Maintenances, nullable)
   - Name, Description
   - KmInterval, TimeIntervalWeeks
   - TrackingType
   - IsEnabled

### Conexión
- Provider: PostgreSQL
- Connection String: `Host=localhost;Port=5432;Database=bikontrol_db;Username=postgres;Password=sa123456`

---

## 🎯 COMPONENTES CLAVE DEL FRONTEND

### LoginComponent
- Form reactivo: email, password
- Validaciones: email format, password minLength(6)
- Manejo de errores: Muestra mensajes de error del servidor
- Navegación: Redirige a /dashboard en éxito

### RegisterComponent
- Form reactivo: fullName, email, password, confirmPassword
- Validador personalizado: passwordMatchValidator
- Mismo flujo que login

### HomeComponent
- Carga lista de motocicletas del usuario
- Muestra tarjetas de motocicletas (MotorcycleCardComponent)
- Botón para agregar nueva motocicleta

### SaveMotorcycleComponent
- Modo edición/creación (detecta :id en ruta)
- Form reactivo con validaciones
- Campos: name, brand, year, nickname, km, displacement, plate
- SweetAlert2 para notificaciones
- En creación: registra km inicial en historial

### SaveMaintenanceComponent
- Modo edición/creación
- Selector de tipo de monitoreo: Km o Tiempo
- Conversión de unidades de tiempo (semanas, meses, años)
- SweetAlert2 para notificaciones

---

## 📦 DEPENDENCIAS PRINCIPALES

### Backend (.NET 8)
- AutoMapper: Mapeo de DTOs a Entities
- Entity Framework Core: ORM
- Npgsql: Driver PostgreSQL
- Microsoft.AspNetCore.Identity: Password hashing
- System.IdentityModel.Tokens.Jwt: Manejo de JWT

### Frontend (Angular 18)
- @angular/core, @angular/common, @angular/forms
- @angular/router: Routing
- @angular/platform-browser: HTTP Client
- rxjs: Reactive programming
- sweetalert2: Notificaciones
- tailwindcss: Estilos
- jest: Testing

---

## ⚙️ CONFIGURACIÓN IMPORTANTE

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
- Origen permitido: `http://localhost:4200`
- Métodos: Todos
- Headers: Todos

### package.json Scripts (Frontend)
```bash
npm start -> ng serve (puerto 4200)
npm run build -> ng build
npm test -> jest
npm run test:watch -> jest watch mode
```

---

## 🔄 MAPEOS AUTOMÁTICOS (AutoMapper)

```csharp
User ↔ RegisterRequest/RegisterResponse/LoginResponse
Motorcycle ↔ SaveMotorcycleDTO/MotorcycleDTO
Maintenance ↔ MaintenanceDTO (mapea DefaultKmInterval → KmInterval, etc)
UserMaintenance ↔ MaintenanceDTO/SaveMaintenanceDTO
```

---

## 🚀 FLUJO TÍPICO DE OPERACIÓN

### Registro/Login
1. Usuario entra en `/login` o `/register`
2. Completa formulario y envía
3. AuthService → POST /api/auth/login|register
4. Backend valida, crea usuario, genera JWT
5. Frontend guarda token en localStorage
6. AuthInterceptor lo agrega a todas las peticiones
7. Redirige a `/dashboard`

### Crear Motocicleta
1. Usuario en `/dashboard/motorcycles/add`
2. Completa SaveMotorcycleComponent form
3. MotorcyclesService → POST /api/motorcycles
4. Backend crea Motorcycle + registra KmHistory inicial
5. Frontend redirige a home
6. HomeComponent recarga lista

### Crear Mantenimiento Personalizado
1. Usuario en `/dashboard/maintenance/add`
2. Completa SaveMaintenanceComponent form
3. Elige tipo: Km o Time
4. MaintenanceService → POST /api/maintenances/mine
5. Backend crea UserMaintenance
6. Frontend redirige a /dashboard/maintenance

---

## 📝 VALIDACIONES

### Cliente (Angular)
- Formularios reactivos con Validators
- Validaciones de campo en HTML
- SweetAlert2 para confirmaciones y errores

### Servidor (.NET)
- DataAnnotations en DTOs
- Lógica de validación en Domain Entities
- Middleware de excepciones para respuestas uniformes
- Validaciones de pertenencia de recursos

---

## 🔧 MEJORAS POTENCIALES

- [ ] Implementar refresh tokens
- [ ] Agregar roles/permisos (Admin, User)
- [ ] Paginación en listas
- [ ] Filtros y búsqueda
- [ ] Historial de cambios (audit log)
- [ ] Notificaciones de próximos mantenimientos
- [ ] Integración con APIs externas (clima, tráfico)
- [ ] Exportar reportes (PDF)
- [ ] Integración con WhatsApp/Email para recordatorios

---

## 🏁 CONCLUSIÓN

**Bikontrol** es una aplicación bien estructurada siguiendo Clean Architecture en el backend y mejores prácticas de Angular en el frontend. El flujo de autenticación es seguro con JWT, la validación ocurre en ambas capas, y la separación de responsabilidades es clara. El proyecto está listo para escalarse agregando features como roles, notificaciones y reportes.

---

**Generado:** Mayo 13, 2026
**Versión:** 1.0

## Guias para la IA
- No uses comentarios con emojis
- No llenes el proyecto de comentarios, solo los necesarios
- El codigo y comentario ponlo en ingles.
- Siempre pregunta antes de implementar.
- No ejecutes comandos git a no ser que el usuario explicitamente lo pida.
- Cada cambio que grande que te pida, dame 3 posibles opciones y yo decido cual sera mejor, tu presentame pros y contras de cada una.
- No decidas cambios grandes sin preguntarme
---

## ACTUALIZACION 2026-05-22 (EDICION Y REVERSIÓN DE KILOMETRAJE)

### UX y reglas
- En `motorcycle-summary` la tarjeta de `Kilometraje Actual` ahora incluye:
  - botón editar (ícono lápiz) para abrir modal de actualización.
  - acción discreta de reversión: `¿Te equivocaste con el kilometraje? Reviértelo aquí.`
- Regla UI: no se permite registrar km menor al actual.
- Alcance de reversión: solo último cambio (vuelve al penúltimo estado).

### Backend
- Se reusaron endpoints existentes de km history:
  - `GET /api/motorcycles/{id}/km/current`
  - `POST /api/motorcycles/{id}/km-history`
  - `DELETE /api/motorcycles/{id}/km-history/last`
- Ajuste en `KmHistoryService.RollbackLastKmAsync`:
  - si `newKm == last.Km`, elimina último registro y finaliza (undo real sin duplicados).

### Frontend
- `motorcycle-summary`:
  - modal para editar km con input numérico.
  - success swal y refresco de datos del summary.
  - confirmación para revertir último cambio.
- `top-nav`:
  - `showBackButton` ahora incluye ruta summary.
  - back desde summary navega a `/dashboard/home`.
- `motorcycle-card`:
  - muestra km actual consultado por endpoint en lugar del campo estático que quedaba en `0`.

### Testing agregado/actualizado
- Backend (`xUnit`):
  - `AddKmAsync` mayor al actual: agrega.
  - `AddKmAsync` menor al actual: `ValidationException`.
  - rollback cuando `newKm == last`: deshace último cambio.
  - rollback cuando `newKm == previous`: no duplica.
- Frontend (`Jest`, unit specs por clase):
  - servicio de motos para contratos `km/current`, `km-history`, `km-history/last`.
  - summary: modal, validación de km, guardado y reversión.
  - top-nav: back en summary a home.
  - motorcycle-card: km real desde endpoint.

## ACTUALIZACION 2026-05-22 (MANTENIMIENTOS POR MOTOCICLETA)

### Cambios de Modelo de Dominio

#### UserMaintenance (actualizado)
```csharp
- Id (Guid)
- UserId (Guid) - FK a User
- MotorcycleId (Guid) - FK a Motorcycle
- BaseTypeId (Guid?) - FK a Maintenance
- Name, Description (strings)
- KmInterval, TimeIntervalWeeks (int?)
- TrackingType (string: "Km" o "Time")
- IsEnabled (bool)
```

#### MotorcycleMaintenanceRecord (nuevo)
```csharp
- Id (Guid)
- MotorcycleId (Guid) - FK
- UserMaintenanceId (Guid) - FK
- PerformedAt (DateTime)
- PerformedKm (int?)
- CreatedAt (DateTime)
```

### Cambios de Base de Datos
- `UserMaintenanceTypes` ahora incluye `MotorcycleId` obligatorio.
- Nueva tabla: `MotorcycleMaintenanceRecords`.
- Nuevos indices para consultas de proximos mantenimientos e historial.

### Endpoints API nuevos/actualizados

#### Motocicletas
- **GET** `/api/motorcycles/{id}/km/current`
  - Response: `{ km }`
- **POST** `/api/motorcycles/{id}/km-history`
  - Request: `{ km }`
- **DELETE** `/api/motorcycles/{id}/km-history/last`
  - Request: `{ newKm }`

#### Mantenimientos
- **GET** `/api/maintenances/mine/motorcycle/{motorcycleId}`
  - Obtiene mantenimientos del usuario para una moto especifica.

- **POST** `/api/maintenances/follow` (actualizado)
  - Request: `{ motorcycleId, defaultId, trackingType, kmInterval, timeIntervalWeeks }`

- **POST** `/api/maintenances/records`
  - Request: `{ motorcycleId, userMaintenanceId, performedAt, performedKm? }`

- **GET** `/api/maintenances/motorcycle/{motorcycleId}/records`
  - Historial de mantenimientos realizados por moto.

- **GET** `/api/maintenances/motorcycle/{motorcycleId}/upcoming`
  - Proximos mantenimientos calculados por backend (vida util, vencido, faltantes).

### Reglas de Negocio Implementadas
- Ownership estricto: todas las operaciones por moto validan que la moto pertenezca al usuario autenticado.
- Registro de mantenimiento:
  - Fecha no puede ser posterior a hoy.
  - Si es por Km, no puede ser menor al ultimo registro de ese mantenimiento.
- Al registrar mantenimiento con Km:
  - Actualiza `KmHistory` solo si el km es mayor al actual.
  - Si es igual, no duplica registro.
- Rollback de KmHistory:
  - No se permite borrar el registro inicial.
- Calculo de vida util:
  - Se calcula en endpoint, no se persiste.
  - Clamp en 0%-100%.
  - Orden de proximos: mas vencido (0%) a mas reciente (100%).
- Mantenimiento por tiempo sin registros:
  - Usa como baseline la fecha inicial de la moto (primer `KmHistory.RecordedAt`).

### Rutas Frontend actualizadas
```typescript
- /dashboard/motorcycles/summary
- /dashboard/motorcycles/add
- /dashboard/motorcycles/edit/:id
- /dashboard/motorcycles/:motorcycleId/maintenance
- /dashboard/motorcycles/:motorcycleId/maintenance/add
- /dashboard/motorcycles/:motorcycleId/maintenance/edit/:id
- /dashboard/motorcycles/:motorcycleId/register-maintenance
```

### Cambios UI clave
- En `motorcycle-summary`:
  - Boton `Registrar mantenimiento` arriba de proximos mantenimientos.
  - Boton `Agregar mantenimientos` debajo del grid de proximos.
  - Tarjetas de proximos en formato grid responsive (2 columnas mobile, 3 desktop).
  - Boton de registrar deshabilitado si no hay mantenimientos asociados.
  - Separador visual antes de `Registro de mantenimientos`.
- En top nav:
  - Home muestra hamburguesa.
  - Rutas de mantenimiento/registro por moto y crear moto muestran boton volver.

### Testing agregado
- Backend (xUnit):
  - Validaciones de rollback de km history.
  - Recalculo de vida util al cambiar frecuencia.
  - Caso de mantenimiento por tiempo sin registros (baseline por fecha inicial de moto).
- Frontend (Jest):
  - Summary deshabilita registrar cuando no hay mantenimientos.
  - Top nav muestra boton volver en rutas de mantenimiento y navega al summary correcto.

---
