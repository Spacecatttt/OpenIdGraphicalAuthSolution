# OpenIdGraphicalAuthSolution


Some start up scripts:

```
docker run -d   --name postgres  -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=password  -e POSTGRES_DB=postgres  -p 5432:5432   postgres

postgres
password

docker run -d --name pgadmin  -p 8080:80    -e PGADMIN_DEFAULT_EMAIL=admin@admin.com  -e PGADMIN_DEFAULT_PASSWORD=admin   --network pgnetwork  dpage/pgadmin4

admin@admin.com
admin
```

```bash
dotnet ef migrations add InitialIdentityServerConfigurationMigration --context ConfigurationDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Web -o Migrations/IdentityServer/Configuration

dotnet ef migrations add InitialIdentityServerPersistedGrantDbMigration --context PersistedGrantDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Web -o Migrations/IdentityServer/PersistedGrant

dotnet ef migrations add InitialApplicationMigration   --context ApplicationDbContext   --project OpenIdProvider.Data  --startup-project OpenIdProvider.Web   -o Migrations/Application
```

```bash
dotnet ef database update --context ConfigurationDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Web

dotnet ef database update --context PersistedGrantDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Web

dotnet ef database update --context ApplicationDbContext --project OpenIdProvider.Data --startup-project OpenIdProvider.Web
```

```bash
dotnet ef database update
```

```bash
ef migrations remove
```
