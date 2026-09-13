# XAMPP MySQL database

IWAS now reads XAMPP MySQL through `MySqlWarehouseReadStore`. The verified local server is MariaDB 10.4.32 on `127.0.0.1:3306`, database `iwas`. ASP.NET Core still runs through .NET; Apache does not execute this application.

## Setup and run

Start **MySQL** in XAMPP Control Panel. From the repository root, with the .NET 8 SDK:

```powershell
./database/setup.ps1
dotnet run --project src/Presentation/Iwas.Presentation.csproj --urls http://127.0.0.1:5080
```

This workspace also has an ignored local SDK at `.tools/dotnet/dotnet.exe`; substitute that path for `dotnet` if the system has only a runtime. The setup script detects it automatically.

Setup creates five InnoDB tables from [schema.sql](../database/schema.sql), seeds an entirely empty database with the existing demonstration dataset, creates `iwas_reader` restricted to SELECT on `iwas.*`, and generates `src/Presentation/appsettings.Local.json` with a random password. Existing rows and local settings are preserved on reruns. The tool never resets an existing unmanaged account; resolve any account conflict explicitly. DDL/account creation is not transactional; a failed run can leave created tables or an account, while seed rows are committed together.

The default setup identity is XAMPP's local `root` with no password. If yours differs, set `IWAS_SETUP_CONNECTION` in the setup process to your administrative connection string. Do not commit credentials. The web application never uses this setup variable or root account. `ConnectionStrings__Warehouse` can override the local runtime connection through an environment variable. See `appsettings.Local.example.json` for the format. Local settings are ignored by Git and must not be distributed with a deployment. `SslMode=None` is for this loopback XAMPP installation only.

Open the app at http://127.0.0.1:5080. With Apache running, browse the tables in http://localhost/phpmyadmin by selecting **iwas**.

## Data and consistency

The expanded V2 seed contains **25 items, 287 stock movements, 165 delivery records across 9 suppliers, and 52 requisitions**, plus one metadata row. Use **8 July 2026**, item **IT-1108**, requisition **RQ-0871**, and supplier period **1 January to 30 June 2026**. The UI retains its demonstration date limit.

Fresh setup uses the expanded seed. To append it to an original demo database:

```powershell
./database/setup.ps1 -AddDemoData
```

This adds 20 items (`IT-6001` through `IT-6020`), 274 movements, 140 deliveries and 46 requisitions (`RQ-0900` through `RQ-0945`) in one transaction, preserving original rows and credentials. A locked source-version marker prevents duplicate application; conflicting keys fail and roll back instead of overwriting data. It only applies to the recognized original demo dataset. Subsequent runs do nothing. The canonical expanded dataset is defined in `database/DemoSeedData.cs`; the original fixture remains unchanged.

New categories include Packaging, Safety, Cleaning and Office alongside Stationery and IT. Monthly issues span July 2025 through June 2026; July 8 receipts and issues populate R1. Histories include low stock, zero stock and dead stock. There are 145 completed deliveries and 20 open orders. Of 52 requisitions, 32 are dated July 8; choose **July 1 to July 8** in Clarifications to include the 20 earlier requests. R5 retains its single-day scope.

Matching examples: `RQ-0900` (insufficient stock), `RQ-0912` (zero-stock mouse), `RQ-0917` (missing catalogue price), `RQ-0943` (missing quantity), `RQ-0944` (box/each mismatch), and `RQ-0945` (tied candidates). The A4 paper valuation remains 90 reams / BDT 49,050.00. Deliveries are a separate supplier-performance dataset; the current schema does not link their quantities to individual stock receipts.

Tables map directly to the four existing Model source contracts. IDs use case-insensitive Unicode collation; the Model also validates canonical identities. Quantities/costs use `DECIMAL(18,4)`; descriptions use utf8mb4, including Bengali. Item foreign keys, unique movement order, positive quantity constraints and unique purchase-order identity enforce the v1 source assumptions. This initial schema supports one item per purchase order, not split deliveries. Nullable prices and actual delivery dates remain null. No computed totals, matches or reports are persisted.

The scoped database store opens one read-only REPEATABLE READ transaction per web request, checks that source tables use InnoDB, and materializes a bounded consistent extract before releasing the connection. Existing Model validation/filtering then operates on that immutable request data. Dashboard and multi-section report subcalls share the same extract, while each receives an independently disposable query context. A new web request reads fresh database data. The adapter fails on excess rows rather than truncating financial inputs; this approach is suitable for the current small warehouse dataset, with whole-source caps defined by `QueryLimits`. Larger sources need selective SQL queries. Connection failures reach the existing HTTP 503 error page; there is no silent fixture fallback.

Metadata reports `mysql-v1` and `TransactionSnapshot`. The request ID is not a replayable database version. `source_version` labels the imported dataset; administrators must update metadata and source records together when replacing an extract. Trusted opening history is asserted only for the known seed. Real warehouse history, opening quantities and lead-time assumptions must be verified before treating results as operational decisions.

The adapter uses [MySqlConnector](https://mysqlconnector.net/) and its [read-only transaction API](https://mysqlconnector.net/api/mysqlconnector/mysqlconnection/begintransactionasync/).

## Verification

```powershell
dotnet build src/Presentation/Iwas.Presentation.csproj
dotnet run --project test/Model/Iwas.Model.Tests.csproj
dotnet run --project test/Business/Iwas.Business.Tests.csproj
dotnet run --project test/Database/Iwas.Database.Tests.csproj
```

Database tests expect the unchanged seed and perform only reads: source mappings, Unicode, decimal/FIFO results, limits, cancellation, disposal, metadata and runtime permission inspection. They do not attempt writes against the application database. Setup and data maintenance stay outside the web runtime.


