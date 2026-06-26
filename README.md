# GestionEmpresarialApp

Sistema de control de acceso empresarial con manejo de roles y permisos, construido en ASP.NET Core MVC (.NET 8) y PostgreSQL.

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL corriendo localmente (puerto 5432)

## Configuración inicial

### 1. Crear la base de datos

Conéctate a PostgreSQL y ejecuta:

```sql
CREATE DATABASE gestionempresarialdb;
```

Luego ejecuta el siguiente script para crear las tablas e insertar los datos de prueba:

```sql
CREATE TABLE roles (
    rol_id      SERIAL PRIMARY KEY,
    rol_name    VARCHAR(50) NOT NULL,
    description TEXT
);

CREATE TABLE usuarios (
    usuario_id    SERIAL PRIMARY KEY,
    username      VARCHAR(100) NOT NULL UNIQUE,
    password      VARCHAR(255) NOT NULL,
    rol_id        INT NOT NULL REFERENCES roles(rol_id),
    full_name     VARCHAR(100),
    email         VARCHAR(150),
    status        VARCHAR(20) DEFAULT 'activo',
    created_at    TIMESTAMPTZ DEFAULT NOW(),
    last_login_at TIMESTAMPTZ
);

CREATE TABLE role_permissions (
    id         SERIAL PRIMARY KEY,
    rol_id     INT NOT NULL REFERENCES roles(rol_id) ON DELETE CASCADE,
    permission VARCHAR(100) NOT NULL,
    UNIQUE (rol_id, permission)
);

CREATE TABLE audit_logs (
    log_id     SERIAL PRIMARY KEY,
    user_id    INT REFERENCES usuarios(usuario_id) ON DELETE SET NULL,
    username   VARCHAR(100) NOT NULL,
    action     VARCHAR(50)  NOT NULL,
    target     TEXT,
    ip_address VARCHAR(45),
    result     VARCHAR(20)  NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

create table categories (
    category_id SERIAL primary key,
    name       varchar(100) not null unique
);

create table clients (
    client_id SERIAL primary key,
    first_name     varchar(100) not null,
    last_name   varchar(100) not null,
    phone   varchar(20),
    email      varchar(150),
    address  text,
    status     varchar(20) default 'activo',
    created_at timestamptz default now()
);

create table products (
    product_id  SERIAL primary key,
    code       varchar(50)    not null unique,
    name       varchar(150)   not null,
    price       NUMERIC(10,2)  not null default 0,
    stock        int            not null default 0,
    category_id int            references categories(category_id),
    status       varchar(20)    default 'activo',
    created_at   timestamptz    default now()
);


-- Roles
INSERT INTO roles (rol_name, description) VALUES
    ('Administrador', 'Acceso total al sistema'),
    ('Supervisor',    'Puede consultar y modificar'),
    ('Ejecutor',      'Puede consultar y agregar');

-- Usuarios (contraseña: 12345)
INSERT INTO usuarios (username, password, rol_id) VALUES
    ('admin',      '12345', 1),
    ('supervisor', '12345', 2),
    ('ejecutor',   '12345', 3);
    
INSERT INTO categories (name) VALUES ('Electrónica'), ('Ropa'), ('Alimentos'), ('Hogar'), ('Otros');
```

### 2. Configurar el connection string

El connection string **no está en el código** — se guarda como secreto local con el siguiente comando (reemplaza los valores con los de tu PostgreSQL):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=gestionempresarialdb;Username=postgres;Password=TU_PASSWORD"
```

### 3. Ejecutar la aplicación

```bash
dotnet run
```

La app estará disponible en `http://localhost:5146`. Abre esa URL en el navegador — redirige automáticamente al login.

## Credenciales de acceso

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `12345` | Administrador |
| `supervisor` | `12345` | Supervisor |
| `ejecutor` | `12345` | Ejecutor |

## Estructura del proyecto

```
Controllers/   → Lógica de cada módulo (Account, Home, Users, Roles, Audit)
Models/        → Clases que representan las tablas de la BD
Data/          → ApplicationDbContext (conexión con PostgreSQL via EF Core)
Helpers/       → SessionPermissions (verificación de permisos en sesión)
Views/         → Pantallas Razor (.cshtml)
wwwroot/css/   → Estilos del sistema
```

## Módulos disponibles

| Módulo | Ruta | Descripción |
|---|---|---|
| Login | `/Account/Login` | Autenticación con bloqueo por intentos |
| Dashboard | `/Home/Index` | Estadísticas en tiempo real |
| Usuarios | `/Users/Index` | CRUD de cuentas de usuario |
| Roles | `/Roles/Index` | Asignación de permisos por rol |
| Auditoría | `/Audit/Index` | Historial de accesos y operaciones |
