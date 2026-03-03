# RequestHub

Sistema de Solicitudes Internas (Mesa de Servicios) con backend ASP.NET Core + SQL Server/EF Core + JWT y frontend Vue 3.

## Backend

Proyecto: `RequestHub/`

### Credenciales sembradas
- Admin: `admin` / `Admin123*`
- Solicitante: `solicitante` / `Solicitante123*`
- Gestor TI: `gestor-ti` / `Gestor123*`

### Endpoints principales
- `POST /api/auth/login`
- `GET/POST /api/catalogs/areas`
- `GET/POST /api/catalogs/request-types`
- `GET/POST /api/catalogs/priorities`
- `GET /api/servicerequests` (filtros por query)
- `GET /api/servicerequests/{id}`
- `POST /api/servicerequests` (multipart/form-data)
- `PUT /api/servicerequests/{id}`
- `POST /api/servicerequests/{id}/take`
- `POST /api/servicerequests/{id}/assign`
- `POST /api/servicerequests/{id}/status`
- `POST /api/servicerequests/{id}/comments`

## Frontend

Proyecto: `frontend/` (Vue 3 + Vite)

Pantallas:
- Login
- Mis Solicitudes
- Bandeja del Área
- Detalle de Solicitud
- Administración de Catálogos

## Ejecutar

Backend:
```bash
cd RequestHub
dotnet restore
dotnet run
```

Frontend:
```bash
cd frontend
npm install
npm run dev
```
