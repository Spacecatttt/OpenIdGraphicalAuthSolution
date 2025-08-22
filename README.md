# OpenIdGraphicalAuthSolution 🔐

![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet) ![Blazor](https://img.shields.io/badge/Blazor-Web_App-blue) ![Duende IdentityServer](https://img.shields.io/badge/Duende-IdentityServer-orange) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-relational-blue)


## About the Project
This project is a complete, modern identity provider built on .NET 8, Blazor, and Duende IdentityServer. It serves as a centralized authentication and authorization service for various client applications.

The solution is designed with a multi-tenant architecture, allowing different organizations to manage their own users, groups, and clients. A key feature of this project is the implementation of a custom graphical password authentication method using steganography, where a user's password is securely embedded within an image file.

---

## Key Features ✨
- **OIDC Provider**: Fully functional OpenID Connect and OAuth 2.0 provider using Duende IdentityServer.
- **Multi-Tenancy**: Data is isolated by Organizations, allowing administrators to manage their own resources.
- **Role-Based Access Control**: A granular permission system with roles like Owner, Admin, and Member within each organization.
- **Graphical Password Authentication**: A custom authentication flow where a user can log in by providing an image that contains their securely embedded password.
- **User & Client Management**: A comprehensive Blazor-based UI for administrators to manage users, groups, and OIDC clients.
---

## Getting Started
Follow these steps to get the project up and running on your local machine.

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Docker](https://www.docker.com/products/docker-desktop/) and Docker Compose
* [OpenSSL](https://www.openssl.org/) or a similar tool for generating certificates.


---

### Installation & Setup

#### 1. Clone the Repository
```bash
git clone https://github.com/Spacecatttt/OpenIdGraphicalAuthSolution
cd OpenIdGraphicalAuthSolution
```

#### 2. Generate Signing Certificates
IdentityServer requires a certificate for signing tokens.

Create a `keys` folder in the root of the repository.
```bash
mkdir -p ./keys
```

`Note`: Update the `SERVER_CN="server domain name"` field in `generate-certs.sh` with your server's domain name.

Generate the necessary certificate files using the provided script.
```bash
chmod +x generate-certs.sh
./generate-certs.sh
```
You will be prompted to create a password for the `.p12` file. Remember it, as you must update the `Kestrel__Certificates__Default__Password` value in your `docker-compose.yaml` to match it.

---

### Running the Application
You have two options for running the project.

#### **Option 1: Using Docker Compose (Recommended)**

This is the simplest way to get everything running, as it handles the database and the web application automatically.
```bash
docker-compose up --build -d
```

`Note!` After running for the first time, you will need to set up the database schema by [applying the database migrations](#applying-migrations) and [seeding the database](#seeding-data).

The application will be available at `https://localhost:9331`.

---

#### **Option 2: Running Locally**

**Step 1: Run PostgreSQL Database**

You can run a PostgreSQL instance in any way you prefer. The recommended way is with Docker.
```bash
docker run -d --name postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=password -e POSTGRES_DB=OpenIdProviderDb -p 5432:5432 postgres
```

This will start a PostgreSQL server with the following credentials:
- **User**: `postgres`
- **Password**: `password`

**Step 2: Update Connection String**

Ensure the `"DefaultConnection"` string in `OpenIdProvider.Blazor/appsettings.json` matches your PostgreSQL setup.

**Step 3: Apply Database Migrations**

Before running the app for the first time, you need to create the database schema by [applying the database migrations](#applying-migrations) and [seeding the database](#seeding-data).

**Step 4: Run the Project**
```bash
cd OpenIdProvider.Blazor
dotnet run
```
The application will be available at `https://localhost:9331`.


## Database Migrations

### Applying Migrations
To apply all pending migrations, you must run the `update` command for each `DbContext` individually from the root directory:

```bash
# For Application data
dotnet ef database update --context ApplicationDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Blazor

# For IdentityServer Configuration data
dotnet ef database update --context ConfigurationDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Blazor

# For IdentityServer Operational data
dotnet ef database update --context PersistedGrantDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Blazor
```

## Seeding Data

### Initial Database Seeding
If you are running the application for the first time, you need to add some standard data. This can be done easily by running:
```bash
cd DatabaseSeederTool
dotnet run
```

### Additional Seeding or Data Management
For seeding or modifying data, you can use the following utilities:

- **`DatabaseSeeder.cs`**: Performs a full, large-scale seed of an empty database.
- **`DataHelperService.cs`**: A helper service with methods to add specific data (e.g., create an organization for a specific user).

The initial seeding process is configured in `DatabaseSeederTool/Program.cs`.
To use the database seeder, simply uncomment the relevant code in `Program.cs` and run:
```bash
dotnet run
```

After the seeding process is complete, you can log in with the following credentials:
```bash
Email: owner1@example.com
Password: Password123!
```

### Creating New Migrations
If you change any of the data models in the `OpenIdProvider.Data` project, you will need to create a new migration. The project uses three separate `DbContexts`, so you must specify which context the changes apply to.

Run these commands from the root directory (`OpenIdGraphicalAuthSolution`):

```bash
# For Application data (Users, Organizations, etc.)
dotnet ef migrations add <MigrationName> --context ApplicationDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Blazor -o Migrations/Application

# For IdentityServer Configuration data (Clients, Scopes, etc.)
dotnet ef migrations add <MigrationName> --context ConfigurationDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Blazor -o Migrations/IdentityServer/Configuration

# For IdentityServer Operational data (Grants, Tokens, etc.)
dotnet ef migrations add <MigrationName> --context PersistedGrantDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Blazor -o Migrations/IdentityServer/PersistedGrant
```
