# Bikontrol 🛠🏍️

**Bikontrol** is a web application designed to help motorcycle owners track and manage maintenance tasks—from basic chain lubrication to advanced inspections.

## 📂 Project Structure

- `Bikontrol/`: ASP.NET Core 8 backend using Clean Architecture
- `bikontrol-web/`: Angular CLI: 17.3.17
Node: 18.18.1

## 🚀 Tech Stack

- ASP.NET Core 8
- Angular 17/
- SQL Server - ADO.NET
- Clean Architecture

## 🛠️ Setup

### 🔄 How to set up Schema Compare (optional)

If you want to use Visual Studio’s **Schema Compare** tool to compare and update the database:

1. Open the `Bikontrol.Database.sqlproj` project in **Visual Studio**.
2. Right-click the project → `Schema Compare...`.
3. In the left panel (Source), select:
   - `Database project`: `Bikontrol.Database`
4. In the right panel (Target), select:
   - `Database`: your local instance (e.g., `(localdb)\MSSQLLocalDB`)
5. Click `Compare` to view differences.
6. (Optional) Save the configuration locally via `Save As...`.

> ⚠️ The `.scmp` file is not included in the repository to avoid machine-specific conflicts
