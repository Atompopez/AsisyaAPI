# Asisya API — Prueba técnica Desarrollador II

API REST de productos y categorías en **ASP.NET Core (.NET 10) + PostgreSQL**, con carga masiva
asíncrona de 100.000 productos, autenticación JWT, pruebas unitarias y de integración, Docker,
CI en GitHub Actions y una SPA en **React**.

[![CI](https://github.com/Atompopez/AsisyaAPI/actions/workflows/ci.yml/badge.svg)](https://github.com/Atompopez/AsisyaAPI/actions/workflows/ci.yml)

---

## Contenido

1. [Inicio rápido con Docker](#1-inicio-rápido-con-docker)
2. [Ejecución local sin Docker](#2-ejecución-local-sin-docker)
3. [Endpoints](#3-endpoints)
4. [Arquitectura](#4-arquitectura)
5. [Decisiones arquitectónicas](#5-decisiones-arquitectónicas)
6. [Carga masiva de 100.000 productos](#6-carga-masiva-de-100000-productos)
7. [Escalabilidad horizontal en la nube](#7-escalabilidad-horizontal-en-la-nube)
8. [Seguridad](#8-seguridad)
9. [Pruebas](#9-pruebas)
10. [CI/CD](#10-cicd)
11. [Frontend y adaptación Angular → React](#11-frontend-y-adaptación-angular--react)

---

## 1. Inicio rápido con Docker

Requisitos: Docker con Docker Compose v2, `bash` y `curl` (Git Bash o WSL en Windows).

```bash
git clone https://github.com/Atompopez/AsisyaAPI.git
cd AsisyaAPI

cp .env.example .env          # edita JWT_KEY y ADMIN_PASSWORD
docker compose up --build -d  # levanta PostgreSQL 16 + API en http://localhost:8080
```

Al arrancar, la API aplica las migraciones de EF Core y crea el usuario administrador definido en
`.env`. Swagger queda en <http://localhost:8080/swagger>.

Crea las categorías **SERVIDORES** y **CLOUD** con `POST /Category` y, opcionalmente, carga los
100.000 productos:

```bash
ADMIN_PASSWORD='Admin123!' ./scripts/seed.sh                     # solo categorías
ADMIN_PASSWORD='Admin123!' BULK_COUNT=100000 ./scripts/seed.sh   # categorías + 100k productos
```

Variables de `scripts/seed.sh`: `API_URL` (por defecto `http://localhost:8080`), `ADMIN_USER`
(por defecto `admin`), `ADMIN_PASSWORD` (obligatoria) y `BULK_COUNT` (opcional). Se puede ejecutar
varias veces: si una categoría ya existe, la API responde 409 y el script la omite.

Para levantar el frontend contra esta API:

```bash
cd frontend
echo "VITE_API_URL=http://localhost:8080" > .env
npm install && npm run dev    # http://localhost:5173
```

## 2. Ejecución local sin Docker

Requisitos: .NET SDK 10, Node.js 20.19 o superior (probado con 24) y un PostgreSQL accesible
(por defecto `localhost:5432`, usuario y contraseña `postgres`, base `asisya`; la API la crea si
no existe).

```bash
# Secretos en user-secrets: nunca en el repositorio
dotnet user-secrets --project src/AsisyaApi.Api set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet user-secrets --project src/AsisyaApi.Api set "SeedAdmin:Password" "Admin123!"
# Si tu PostgreSQL usa otras credenciales:
# dotnet user-secrets --project src/AsisyaApi.Api set "ConnectionStrings:DefaultConnection" "Host=...;Username=...;Password=..."

dotnet build AsisyaAPI.slnx
dotnet run --project src/AsisyaApi.Api     # http://localhost:5152/swagger

API_URL=http://localhost:5152 ADMIN_PASSWORD='Admin123!' ./scripts/seed.sh

cd frontend
cp .env.example .env                        # VITE_API_URL=http://localhost:5152
npm install
npm run dev                                 # http://localhost:5173
```

> Si falta `Jwt:Key` (o tiene menos de 32 bytes), la API se detiene al arrancar con un mensaje
> explícito, en lugar de firmar tokens con una clave débil.

Migraciones (herramienta local declarada en `dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add <Nombre> --project src/AsisyaApi.Infrastructure --output-dir Persistence/Migrations
```

## 3. Endpoints

| Método | Ruta                    | Auth    | Descripción |
|--------|-------------------------|---------|-------------|
| POST   | `/api/auth/login`       | pública | usuario/contraseña → JWT |
| POST   | `/Category`             | JWT     | crea categoría (`name`, `photoUrl`) |
| GET    | `/Category`             | JWT     | lista categorías (la usa el formulario del frontend) |
| POST   | `/Product`              | JWT     | crea un producto |
| POST   | `/Product/bulk`         | JWT     | encola la generación de N productos → `202 Accepted` + `jobId` |
| GET    | `/Product/bulk/{jobId}` | JWT     | estado del job: `status`, `totalRecords`, `processedRecords`, fechas y error |
| GET    | `/Products`             | JWT     | listado paginado: `page`, `pageSize` (máx. 100), `search`, `categoryId`, `minPrice`, `maxPrice`, `sortBy` |
| GET    | `/Products/{id}`        | JWT     | detalle con nombre y **foto de la categoría** |
| PUT    | `/Products/{id}`        | JWT     | actualiza el producto |
| DELETE | `/Products/{id}`        | JWT     | elimina el producto (`204`) |
| GET    | `/health`               | pública | health check para el orquestador o balanceador |

`sortBy` acepta: `id`, `name`, `name_desc`, `price`, `price_desc`, `createdAt`, `createdAt_desc`.
`search` busca, sin distinguir mayúsculas, en el nombre y la descripción.

Los errores se devuelven como **ProblemDetails (RFC 7807)**: 400 validación o regla de negocio,
401 credenciales o token inválidos, 404 recurso inexistente, 409 nombre de categoría duplicado.
`src/AsisyaApi.Api/AsisyaApi.Api.http` tiene ejemplos de todas las peticiones.

Ejemplo de carga masiva:

```http
POST /Product/bulk
{ "count": 100000 }                         // o { "count": 100000, "categoryIds": [1, 2] }

HTTP/1.1 202 Accepted
Location: /Product/bulk/72339bea-ce56-4e3c-ad71-4ce244b745e5
{ "jobId": "72339bea-...", "status": "Pending", "totalRecords": 100000, "processedRecords": 0, ... }
```

Si se omite `categoryIds`, los productos se reparten en round-robin entre todas las categorías
existentes, que tras el seed son SERVIDORES y CLOUD.

## 4. Arquitectura

Arquitectura limpia por capas: las dependencias apuntan hacia el dominio.

```
┌──────────────────────────────────────────────────────────────┐
│ AsisyaApi.Api            Controllers, Program.cs, Swagger,   │
│                          manejo de excepciones → ProblemDetails
└───────────────┬───────────────────────────┬──────────────────┘
                │                           │ (composición DI)
┌───────────────▼──────────────┐  ┌─────────▼──────────────────┐
│ AsisyaApi.Application        │◄─┤ AsisyaApi.Infrastructure   │
│ DTOs + mapeo explícito,      │  │ EF Core + Npgsql, repos,   │
│ servicios (casos de uso),    │  │ migraciones, JWT, COPY     │
│ interfaces (puertos),        │  │ binario, Channel +         │
│ PagedResult, filtros LINQ    │  │ BackgroundService          │
└───────────────┬──────────────┘  └────────────────────────────┘
                │
┌───────────────▼──────────────┐
│ AsisyaApi.Domain             │  Entidades (Category, Product, User,
│ (sin dependencias)           │  BulkJob) y enum BulkJobStatus
└──────────────────────────────┘
```

| Capa | Responsabilidad | Carpetas |
|------|-----------------|----------|
| **Domain** | Modelo del negocio, sin dependencias de frameworks | `Entities/`, `Enums/` |
| **Application** | Casos de uso y reglas: validación de filtros, unicidad, existencia de categorías, orquestación del job masivo. Define las interfaces que implementa Infrastructure. No conoce EF Core. | `DTOs/`, `Interfaces/`, `Services/`, `Common/` |
| **Infrastructure** | Detalles técnicos: `AppDbContext`, configuraciones y migraciones EF, repositorios, `NpgsqlBinaryImporter`, `Channel<T>`, `BackgroundService`, generación y validación de JWT | `Persistence/`, `Repositories/`, `BulkLoad/`, `Auth/` |
| **Api** | HTTP: rutas, `[Authorize]`, códigos de estado, Swagger. Solo recibe y devuelve DTOs. | `Controllers/`, `Extensions/`, `Middleware/` |

Las entidades nunca cruzan la frontera HTTP: los controllers reciben y devuelven DTOs, y el mapeo
se hace a mano en `Application/DTOs/Mappings.cs`. Así el contrato de la API es explícito y
queda desacoplado del esquema de BD.

## 5. Decisiones arquitectónicas

| Decisión | Justificación |
|----------|---------------|
| **.NET 10** en lugar de .NET 7 | .NET 7 está fuera de soporte desde mayo de 2024 (sin parches de seguridad). .NET 10 es la versión LTS vigente y el código no usa nada que impida bajarlo de versión cambiando `TargetFramework`. |
| **PostgreSQL** | Relacional, open source, disponible como servicio gestionado en cualquier nube (RDS, Azure Database, Cloud SQL) y con `COPY` binario para cargas masivas. |
| **Arquitectura por capas** (no hexagonal puro) | Separación clara y testeable sin la ceremonia de puertos y adaptadores para cada detalle. Application define interfaces (`IProductRepository`, `IBulkProductWriter`, `IJwtTokenGenerator`, `IBulkLoadQueue`…) que Infrastructure implementa. |
| **EF Core Migrations** aplicadas al arrancar (`Database.MigrateAsync()`, nunca `EnsureCreated`) | El esquema queda versionado en `Infrastructure/Persistence/Migrations` y evoluciona sin perder datos. El arranque reintenta mientras la BD aún no acepta conexiones. |
| **Categorías vía `POST /Category`** (`scripts/seed.sh`) y no con data seeding | Respeta la letra del enunciado y de paso ejercita la API real, JWT incluido. |
| **Rutas literales del enunciado** (`/Category`, `/Product`, `/Products/{id}`) | Compatibles con una colección Postman escrita contra esas rutas exactas, aunque mezclen singular y plural. |
| **Usuarios en tabla `Users`** con `PasswordHasher<User>` de ASP.NET Core Identity | PBKDF2 con sal e iteraciones, estándar y auditado. El usuario admin se crea al arrancar a partir de variables de entorno. |
| **Validación en dos niveles** | DataAnnotations en los DTOs (400 automático de `[ApiController]`) y reglas de negocio en los servicios (`minPrice ≤ maxPrice`, `sortBy` válido, categoría existente, nombre único). |
| **Filtros en LINQ puro** (`ProductQueryExtensions`) | El repositorio EF los traduce a SQL y las pruebas unitarias los ejecutan en memoria sin base de datos. |
| **Índices** en `Products(CategoryId)`, `Products(Price)`, `Products(Name)` y únicos en `Categories(Name)` y `Users(Username)` | Cubren los filtros y ordenamientos de `GET /Products` con 100.000+ filas. |

## 6. Carga masiva de 100.000 productos

```
POST /Product/bulk ──► BulkLoadService.StartAsync
                         │ valida, crea BulkJob (Pending) en BD
                         │ encola BulkLoadWorkItem en Channel<T>  ──► 202 + jobId (inmediato)
                         ▼
           BulkLoadBackgroundService (IHostedService, lector único)
                         │ scope DI nuevo por job
                         ▼
           BulkLoadService.ProcessAsync
             ├─ Status = Processing
             ├─ por cada lote de 5.000:
             │    genera productos → BulkProductWriter (COPY ... FROM STDIN BINARY)
             │    ProcessedRecords += 5.000  (persistido → visible en el polling)
             └─ Status = Completed | Failed (+ ErrorMessage)

GET /Product/bulk/{jobId} ──► estado actual del job
```

- **Inserción con `NpgsqlBinaryImporter`** (protocolo `COPY` binario de PostgreSQL): cada lote de
  5.000 filas viaja en streaming en un único comando, sin change tracking ni un `INSERT` por fila.
  En local, **100.000 productos se insertan en unos 2–3 segundos**.
- **Cada lote hace commit por separado**, así `ProcessedRecords` refleja filas realmente persistidas.
  Si un lote falla, el job queda `Failed` con el mensaje de error y el conteo exacto de lo insertado.
- **La cola es un `Channel<T>` acotado (100 jobs)**, singleton compartido entre el productor
  (controller) y el consumidor (`BackgroundService`). Si se llena, el productor espera: hay
  backpressure y no se agota la memoria.
- **Robustez ante reinicios:** la cola vive en memoria, así que al arrancar el servicio marca como
  `Failed` («Interrumpido por un reinicio de la API») los jobs que quedaron `Pending` o
  `Processing`. Así ningún cliente hace polling de un job que nunca terminará.
- La lógica del job está en Application y se prueba con mocks (`BulkLoadServiceTests`); una prueba
  de integración ejecuta el COPY real contra PostgreSQL.

## 7. Escalabilidad horizontal en la nube

**Implementado hoy**

- **API stateless:** la autenticación es JWT sin sesión de servidor, así que cualquier réplica
  atiende cualquier request. Se puede escalar con N contenedores detrás de un balanceador
  (Azure Container Apps, AWS ECS/Fargate, Cloud Run o Kubernetes con HPA), usando `/health` como
  probe.
- **Configuración por variables de entorno** (12-factor): connection string, clave JWT y
  credenciales de admin llegan por entorno o por un secret manager (Key Vault, Secrets Manager).
- **Pool de conexiones único** (`NpgsqlDataSource`) compartido por EF Core y el COPY.
- **Paginación obligatoria** (`pageSize` máximo 100), consultas `AsNoTracking` e índices en las
  columnas filtradas.
- **Imagen Docker multi-stage** con runtime ASP.NET mínimo y usuario sin privilegios.

**Evolución propuesta (documentada, no implementada por el margen de tiempo)**

1. **Redis como caché distribuida para `GET /Products` y `GET /Products/{id}`**
   (`IDistributedCache` o `HybridCache`). La clave sería el hash de los parámetros de consulta,
   con TTL corto (30–60 s) e invalidación por prefijo o versión en cada `POST/PUT/DELETE` y al
   completar un job masivo. Al ser distribuida, todas las réplicas comparten la caché.
2. **RabbitMQ (o Azure Service Bus / SQS) como evolución del `Channel<T>`.** `IBulkLoadQueue` ya
   abstrae la cola: bastaría otra implementación que publique el `BulkLoadWorkItem` en un exchange
   durable, y un **worker independiente** (el mismo `BulkLoadService.ProcessAsync` dentro de un
   `BackgroundService` en otro contenedor) que consuma con `prefetch=1` y ack manual. Con esto se
   gana:
   - desacoplar la carga pesada de las réplicas que atienden HTTP y escalar los workers por
     separado según la longitud de la cola (KEDA);
   - durabilidad: un job no se pierde si un pod se reinicia, porque el mensaje vuelve a la cola;
   - reintentos con dead-letter queue.
   Hoy, con varias réplicas de la API, cada una procesa los jobs que recibió, y eso funciona, pero
   no reparte la carga ni sobrevive a un reinicio. Ese es el límite que resuelve el broker.
3. **Base de datos:** PostgreSQL gestionado con **réplicas de lectura** para `GET /Products`
   (connection string de solo lectura en el repositorio de consultas) y PgBouncer si crece el
   número de réplicas de la API. Para búsqueda de texto a gran escala, índice `pg_trgm` (GIN) o
   un motor dedicado.
4. **Observabilidad:** OpenTelemetry (trazas y métricas) exportado a Application Insights,
   CloudWatch o Grafana.

## 8. Seguridad

- Todos los endpoints de negocio llevan `[Authorize]`; solo `/api/auth/login` y `/health` son
  anónimos. Hay una prueba de integración que lo verifica endpoint por endpoint.
- JWT HMAC-SHA256 con validación de emisor, audiencia, firma y expiración (60 min por defecto,
  30 s de tolerancia de reloj).
- **El secreto no está en el repositorio:** `appsettings.json` lleva `Jwt:Key` vacío. En desarrollo
  se usan user-secrets y en Docker el archivo `.env`, que está en `.gitignore` (hay una plantilla
  `.env.example`). La API no arranca con una clave de menos de 32 bytes.
- Contraseñas con `PasswordHasher<User>` (PBKDF2). Los errores 500 no exponen detalles internos.
- CORS restringido a los orígenes configurados (`Cors:AllowedOrigins`).
- En producción, TLS termina en el balanceador o ingress; el contenedor escucha HTTP en 8080.

## 9. Pruebas

```bash
dotnet test AsisyaAPI.slnx
```

| Proyecto | Qué cubre | Herramientas |
|----------|-----------|--------------|
| `tests/AsisyaApi.UnitTests` (43 pruebas) | Servicios de Application con repositorios mockeados: normalización de paginación, parseo de `sortBy`, validación del rango de precios, mapeo a DTOs, 404/409/400, hashing y login, ciclo de vida del `BulkJob` (lotes de 5.000, progreso, `Completed`/`Failed`), reparto round-robin y filtros LINQ en memoria | xUnit, Moq |
| `tests/AsisyaApi.IntegrationTests` (9 pruebas) | API real (`WebApplicationFactory<Program>`) contra PostgreSQL 16 en contenedor. **Flujo punta a punta: `POST /Category` → `POST /Product` → `GET /Products` devuelve el producto con su categoría** y `GET /Products/{id}` con la foto; PUT/DELETE; carga masiva real por COPY con polling hasta `Completed`; 401 en endpoints protegidos y en login inválido | xUnit, Testcontainers.PostgreSql |

Las pruebas de integración necesitan Docker. Sin Docker se pueden correr contra un PostgreSQL
existente:

```bash
TEST_POSTGRES_CONNECTION="Host=localhost;Port=5432;Database=asisya_it;Username=postgres;Password=postgres" dotnet test
```

## 10. CI/CD

`.github/workflows/ci.yml` corre en cada push a `main`, en cada pull request y a mano
(*workflow_dispatch*). Los resultados se ven en la pestaña **Actions**.

| Job | Pasos |
|-----|-------|
| `backend` | checkout → setup-dotnet 10 → restore → build (Release) → test (unitarias + integración; Testcontainers usa el Docker del runner) → publica los `.trx` como artifact → `dotnet format --verify-no-changes` |
| `frontend` | checkout → setup-node 22 → `npm ci` → `npm run lint` → `npm run build` |
| `docker` | tras `backend` y `frontend`: `docker build` de la imagen de la API (con caché de GitHub Actions) |

## 11. Frontend y adaptación Angular → React

SPA en `frontend/` (React 19 + Vite). Ver también [`frontend/README.md`](frontend/README.md).

- **Login** (`pages/Login.jsx`): el JWT se guarda en `localStorage`.
- **Interceptor** (`api/axiosInstance.js`): agrega `Authorization: Bearer <token>` a cada request.
  Ante un 401 limpia el token y la app vuelve a `/login`.
- **AuthGuard** (`components/AuthGuard.jsx`): protege las rutas de productos y, sin un token
  vigente (revisa `exp`), redirige a `/login` recordando la ruta pedida.
- **Listado** (`pages/ProductList.jsx`): búsqueda, filtro por categoría y rango de precio, orden
  y paginación del servidor. Los filtros viven en la URL, así que se pueden compartir; la
  eliminación pide confirmación en línea.
- **Formularios** (`pages/ProductForm.jsx`): crear y editar con validaciones (obligatorios,
  longitudes, precio ≥ 0 con 2 decimales, stock entero ≥ 0, categoría). Al editar se muestra la
  foto de la categoría que devuelve `GET /Products/{id}`.
- **Enrutamiento modular** (`routes/AppRouter.jsx` + `routes/productRoutes.jsx`).

**Adaptación consciente de terminología.** El enunciado pide React, pero describe el frontend con
términos de Angular. Se usaron los equivalentes idiomáticos de React:

| Término del enunciado (Angular) | Implementación en React |
|---------------------------------|-------------------------|
| Reactive Forms | **React Hook Form** (`useForm`, `register` con reglas de validación) |
| AppRoutingModule / módulos de rutas | **React Router**: `createBrowserRouter` en `AppRouter.jsx` y un módulo `productRoutes.jsx` |
| HttpInterceptor | interceptor de request/response de **axios** |
| CanActivate (guard) | componente **`AuthGuard`** que envuelve las rutas protegidas con `<Outlet />` |
| Servicio de autenticación inyectable | **Context** (`AuthProvider`) + hook `useAuth` |
