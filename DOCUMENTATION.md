# EnergyUsageManager.dll (API reference)

This document describes the public API surface for the backend of the `EnergyUsageManager` project. It documents each public class, its public properties and methods,
expected inputs/outputs and notable behavior. The console UI in `..\EnergyUsageManager.Test\Frontend.cs` demonstrates common usage patterns and can be used as a 
reference.

---

## High-level overview

- `UserManager` — user accounts, authentication, sessions and user-facing actions (create unit, pay bills, admin settings).
- `UnitManager` — create and manage units (homes, buildings, apartments), query units and perform unit actions.
- `MonthTicker` — advances the simulated month, generates monthly reports and issues bills.
- `DataStore.Settings` — application-wide numeric settings persisted to disk (prices, thresholds, current date and global money).

Only backend types are documented here; frontend / console UI usage is shown in `..\EnergyUsageManager.Test\Frontend.cs`.

---

# `UserManager`

Namespace: `EnergyUsageManager`

Purpose: manage user accounts, authentication sessions, and actions performed by users (creating units, payments, admin settings).

Public members

- `UserManager.UserType` (static class of string constants)
  - `Owner` — value: `"owner"` (homeowner account)
  - `Renter` — value: `"renter"` (renter / apartment tenant)
  - `Manager` — value: `"manager"` (building manager)
  - `Admin` — value: `"admin"` (system administrator)

- `UserManager.AdminSettings` (static class of string keys)
  - `PricePerKWh` — key: `"PricePerKWh"`
  - `OveruseThreshold` — key: `"OveruseThreshold"`
  - `PenaltyPricePerKWh` — key: `"OverusePricePerKWh"`
  - These are string keys used when calling `User.SetSetting(...)`.

- `UserManager.User` (represents a logged-in user)
  - Properties
    - `int SessionID` (readonly) — a unique session identifier assigned when a `User` instance is created (useful to track sessions).
    - `decimal Money` — current account balance. Getter is public; setter is internal (backed by the user database). Use `AddMoney(...)` to add funds.
    - `string Name` — username (get/set). Setting updates the underlying database.
    - `string Type` — user type; must be one of values in `UserManager.UserType`. Setting validates and persists; invalid values will throw.
    - `string MoneyFormatted` — convenience formatted money string like `$123.45`.
    - `UnitManager.Unit? OwnedUnit` — the unit assigned to this user (may be `null`). Assigning a unit updates the unit's manager/owner username in the DB; removing clears links.
  - Methods
    - `UnitManager.Unit AddUnit(string name, string? buildingName = null)`
      - Create and return a new unit owned by this user.
      - Behavior depends on `User.Type`:
        - `Owner` => creates a `home` unit.
        - `Manager` => creates a `building` unit.
        - `Renter` => creates an `apartment` unit and requires `buildingName` to register the apartment under an existing building.
        - `Admin` cannot create units (throws).
      - Throws on name collision or invalid inputs.
    - `bool TryAddUnit(string name, string? buildingName, out UnitManager.Unit? newUnit)` and overload `TryAddUnit(string name, out UnitManager.Unit? newUnit)`
      - Safe wrapper for `AddUnit` that returns `true` on success and sets `newUnit`, otherwise returns `false` and `newUnit` is `null`.
    - `void DeleteUnit()`
      - Deletes the unit currently owned by the user. Throws if user has no unit.
    - `void AddMoney(decimal amt)`
      - Adds funds to the user's account (updates persisted DB value).
    - `bool TryPay(decimal amt)`
      - Attempts to pay up to `amt` toward the user's `OwnedUnit.AmountDue`.
      - Returns `false` if no owned unit or nothing due. Otherwise deducts money and reduces unit's `AmountDue`.
      - If paying an apartment, the manager's user account receives the payment; otherwise payments increment `Settings.GlobalMoney`.
    - `UnitManager.Unit AddApartmentToBuilding(string name)`
      - Manager-only: adds an `apartment` unit to the manager's owned building. Throws if user is not a manager or has no building.
    - `bool TryAddApartmentToBuilding(string name, out UnitManager.Unit? newUnit)`
      - Safe wrapper for `AddApartmentToBuilding`.
    - `void SetSetting(string settingKey, object value)`
      - Admin-only operation to update admin settings; `settingKey` must be one of `UserManager.AdminSettings` values.
      - Expected `value` types:
        - `PricePerKWh` and `OverusePricePerKWh` expect `decimal`.
        - `OveruseThreshold` expects `float`.
      - Throws on invalid key, invalid caller type (non-admin) or invalid value type.
    - `void ResetSettings()`
      - Admin-only. Resets persisted settings file to defaults (keeps `GlobalMoney` and `CurrentDate` values if the file already exists).

- Static account/session APIs
  - `bool TrySignUp(string username, string password, out int? sessionID)`
    - Creates a new user account of type `Owner` by default. On success logs the user in and returns `true` with `sessionID` set.
    - Overload: `TrySignUp(string username, string password, string userType, out int? sessionID)` to specify `userType` (must be `Owner`, `Manager` or `Renter`). Returns `false` if the username exists or userType invalid.
  - `int HashString(string password)`
    - Deterministic integer hash used for storing passwords in the simple CSV DB. Public for testing/inspection.
  - `int GenerateSessionID()`
    - Returns a new random session id.
  - `bool TryLogin(string username, string password, out int? sessionID)`
    - Validates credentials against the stored hash; on success creates a `User` instance, stores it in the in-memory session map, returns `true` and the `sessionID`.
  - `bool TryLogout(int sessionID)`
    - Ends the session and removes the in-memory `User` instance.
  - `UserManager.User? GetUserFromSessionID(int sessionID)`
    - Returns the `User` instance associated with a valid session id or `null` if none.

Notes and behavior

- The library persists users to a CSV-backed database. Many property setters save to disk immediately.
- Caller code (see `Frontend.cs`) typically calls `TrySignUp` / `TryLogin` to obtain a `sessionID` and then uses `GetUserFromSessionID(sessionID)` to access the `User` object and perform actions.

---

# `UnitManager`

Namespace: `EnergyUsageManager`

Purpose: create, query and manage units (homes, buildings, apartments) and perform unit-level operations.

Public members

- `UnitManager.UnitType` (static class of string constants)
  - `Home` — value: `"home"`
  - `Building` — value: `"building"`
  - `Apartment` — value: `"apartment"`
  - Helpers: `IsHome(Unit)`, `IsBuilding(Unit)`, `IsApartment(Unit)`.

- `UnitManager.Unit` (represents a unit)
  - Properties
    - `string Name` — unit name (get/set). Setting updates any linked user records so that their `OwnedUnit` references remain consistent.
    - `string Type` — unit type string (one of `UnitManager.UserType` values). Read-only.
    - `string? ManagerUsername` — name of the user who manages / owns the unit (may be `null`). Setting persists to the units DB.
    - `decimal AmountDue` — current outstanding amount due for the unit. Get/set persisted to DB.
    - `decimal BillTotal` — current bill total (accumulated). Get/set persisted.
    - `int MonthsOverdue` — number of months overdue. Get/set persisted.
    - `Unit? ParentUnit` — the parent building for an apartment. Getting returns `null` if none. Setting is only permitted for `Apartment` type units.
    - `Unit[] ChildUnits` — child units (for buildings) — query-only.
  - Methods
    - `void GetMonthlyKWhUsage()` — placeholder (no-op) in current implementation.
    - `void AddApartment(Unit toAdd)` — adds an apartment `toAdd` to this unit (this unit must be a building and `toAdd` must be an apartment); sets parent relationship.
    - `void Delete()` — convenience method that deletes this unit (calls `UnitManager.DeleteUnit(this)`).

- `UnitManager` static operations
  - `UnitManager.Unit? GetUnitByName(string unitName)`
    - Returns the `Unit` by exact name or `null` if no unit exists. Throws if multiple units share the same name (the DB should not allow this).
  - `UnitManager.Unit? CreateUnit(string unitName, string unitType, UserManager.User? manager = null, Unit? parentUnit = null)`
    - Creates and persists a new unit entry. If a unit with `unitName` already exists returns `null`.
    - `manager` is optional; if provided the new unit's `managerUsername` will be set.
    - `parentUnit` should be specified for apartments; it sets the `parentUnitName`.
    - Returns the created `Unit` object or `null` on name collision.
  - `void DeleteUnit(Unit unit)`
    - Deletes the unit from the DB. If the unit is a building all child units are recursively deleted. Any user that had `unitName` referencing the deleted unit will have that field cleared.

Notes and behavior

- Units are stored in a CSV-backed units DB; changes to unit properties are saved immediately.
- Typical usage (see `Frontend.cs`): a `User` calls `AddUnit(...)` or `AddApartmentToBuilding(...)` (manager) to create units, or `GetUnitByName(...)` to look up and assign to a user.

---

# `MonthTicker`

Namespace: `EnergyUsageManager`

Purpose: simulate time progression (months), generate monthly energy usage reports and issue bills for units.

Public members

- `string Month` — current month name (e.g. `"December"`) derived from settings' `CurrentDate` month value.
- `int Year` — current year (get/set) (backed by `DataStore.Settings.CurrentDate`).
- `bool TimeIsProgressing` — public flag; the ticker loop checks this and will pause progression when `false`.

- `Task Begin(int delayInMinutes = 10)`
  - Starts an asynchronous loop that advances months. The loop delays one minute per iteration and ticks when the elapsed minutes >= `delayInMinutes`. The method returns a `Task` that runs the loop in the background.
  - Example: in `Program.Main`, UI calls `MonthTicker.Begin(1)` to advance months every minute.

- `MonthTicker.MonthlyReport` (nested public class) — represents a single month's generated report
  - Properties
    - `string Month` — month name for the report
    - `int Year` — year for the report
  - Methods
    - `decimal GetBillForUnit(UnitManager.Unit unit)` — returns the bill total for `unit` for that report (0 if unit not present in report).
    - `float GetEnergyUsageForUnit(UnitManager.Unit unit)` — returns kWh usage recorded for `unit` in that report (0 if missing).
  - Static utilities
    - `bool Exists(int month, int year)` — returns whether a report file exists for the specified month/year.
    - `MonthTicker.MonthlyReport? GetCurrent()` — returns a `MonthlyReport` for the current month/year if it already exists; otherwise `null`.
    - `MonthTicker.MonthlyReport GenerateCurrent()` — create a new report for the current month/year and persist it (throws if a report already exists for the current month).
    - `MonthTicker.MonthlyReport GetReport(int month, int year)` — load an existing report (throws if missing).
    - `string[]? GetAllReportNames(bool formatted = true)` — returns filenames (or user-friendly formatted strings) for every report in the `Monthly Reports` folder; returns `null` if none exists.

Notes and behavior

- `Begin(...)` starts the background timer loop. The loop calls `Tick()` which shifts month, generates the month's CSV by running usage calculations, and issues bills (sending them to units' `AmountDue`).
- Report files are stored under the application data folder in `Monthly Reports` (see `DataStore` for paths).

---

# `DataStore.Settings`

Namespace: `EnergyUsageManager`

Purpose: persisted global settings and small global state used by other systems (prices, thresholds, global money and the current simulated date).

Public members

- `decimal PricePerKWh` — price applied per kWh for normal usage. Get/Set persisted into `adminSettings.ini` in the app data folder. Setting expects a `decimal` value; getting throws if the file contains an invalid number.
- `float OveruseThresholdKWh` — kWh threshold above which `OverusePricePerKWh` applies. Get/Set persisted; getting throws on parse error.
- `decimal OverusePricePerKWh` — price applied per kWh when usage >= `OveruseThresholdKWh`.
- `decimal GlobalMoney` — global money pool (receives payments for non-apartment units). Persisted.
- `int[] CurrentDate` — two-element array `[month, year]` persisted as `Month/Year` in settings. Set validates month range and array length.
- `void Reset()` — reset the `adminSettings.ini` file to defaults (default price values are written, while existing `GlobalMoney` and `CurrentDate` are preserved when present).

Storage details

- Settings are stored at `%LOCALAPPDATA%\EnergyUsageManager\adminSettings.ini`.
- The format is simple `key=value` per-line text file. `Reset()` writes defaults when the file does not exist.

---

# Files and persistent storage

- Databases are CSV files placed in `%LOCALAPPDATA%\EnergyUsageManager` (see `DataStore.AppDataPath`).
- Monthly reports are saved in `%LOCALAPPDATA%\EnergyUsageManager\Monthly Reports`.
- `UserManager` and `UnitManager` persist changes immediately when properties are changed. Many operations throw exceptions for invalid inputs — callers should catch exceptions or use the provided `Try*` helper methods where available.

---

# Typical call flow (console UI reference)

- Create account: `UserManager.TrySignUp(username, password, userType, out sessionId)`
- Login: `UserManager.TryLogin(username, password, out sessionId)` then `UserManager.GetUserFromSessionID(sessionId)` to get a `User` object.
- Create a unit as owner/manager: call `User.AddUnit(name)` (manager can create a building; owner creates a home). Managers can add apartments with `User.AddApartmentToBuilding(...)`.
- Advance time: call `MonthTicker.Begin(delay)` on startup to start automatic month ticks (in the test console `Begin(1)` is used).
- Inspect reports: `MonthTicker.MonthlyReport.GetAllReportNames()` and `MonthTicker.MonthlyReport.GetReport(month, year)` to read a report and then `GetBillForUnit` / `GetEnergyUsageForUnit` for unit-specific values.
