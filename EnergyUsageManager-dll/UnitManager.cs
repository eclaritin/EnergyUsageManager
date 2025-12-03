using System;
using System.Collections.Generic;
using System.Linq;

using static EnergyUsageManager.DataStore;
using static EnergyUsageManager.UserManager;

namespace EnergyUsageManager
{
    public class UnitManager
    {
        /// Enums ///
        
        public static class UnitType
        {
            public static string Home = "home";
            public static string Building = "building";
            public static string Apartment = "apartment";

            public static bool IsHome(Unit unit) => unit.Type == UnitType.Home;
            public static bool IsBuilding(Unit unit) => unit.Type == UnitType.Building;
            public static bool IsApartment(Unit unit) => unit.Type == UnitType.Apartment;
        }

        /// Properties ///
        internal static CSVDataBase UnitDB;
        private static Random Randomness = new Random(System.Environment.TickCount); // mostly unique seed for extra security

        /// Classes ///
        
        public class Unit
        {
            // fields
            private DBEntry _ref;

            public string Name 
            { 
                get => _ref.Get("unitName");
                set
                {
                    // modify unit name in linked user
                    var entries = UserDB.GetEntriesByFieldValue("unitName", _ref.Get("unitName"));
                    foreach (var entry in entries) entry.Set("unitName", value);

                    // set value in unit db
                    _ref.Set("unitName", value);
                    UnitDB.SaveToDisk();
                }
            }
            public string Type
            {
                get => _ref.Get("unitType");
            }
            public string? ManagerUsername
            {

                get 
                { 
                    var name = _ref.Get("managerUsername");

                    // handle null
                    if (name == "null") return null;

                    // otherwise return name
                    return name;
                }
                set
                {
                    _ref.Set("managerUsername", value ?? "null");
                    UnitDB.SaveToDisk();
                }
            }
            public decimal AmountDue
            {
                get => decimal.Parse(_ref.Get("amountDue"));
                set
                {
                    _ref.Set("amountDue", value.ToString());
                    UnitDB.SaveToDisk();
                }
            }
            public decimal BillTotal
            {

                get => decimal.Parse(_ref.Get("billTotal"));
                set
                {
                    _ref.Set("billTotal", value.ToString());
                    UnitDB.SaveToDisk();
                }
            }
            public int MonthsOverdue
            {
                get => int.Parse(_ref.Get("monthsOverdue"));
                set
                {
                    _ref.Set("monthsOverdue", value.ToString());
                    UnitDB.SaveToDisk();
                }
            }

            public Unit? ParentUnit
            {
                get
                {
                    string name = _ref.Get("parentUnitName");

                    // handle null
                    if (name == "null") return null;

                    //otherwise get unit from name
                    return GetUnitByName(name);
                }
                set
                {
                    // can't set parent if unit is not an apartment
                    if (!(Type == UnitManager.UnitType.Apartment)) throw new Exception($"Unit type of '{Type}' cannot have a parent element.");

                    _ref.Set("parentUnitName", value.Name ?? "null");
                    UnitDB.SaveToDisk();
                }
            }

            public Unit[] ChildUnits
            {
                get
                {
                    // get entries in db
                    var entries = UnitDB.GetEntriesByFieldValue("parentUnitName", Name);

                    // list to append converted objects to
                    var unitList = new List<Unit>();

                    foreach(var entry in entries)
                    {
                        // add new unit objects created 
                        unitList.Add(new Unit(entry));
                    }

                    // convert to array
                    return unitList.ToArray();
                }
            }

            // constructor
            internal Unit(DBEntry UnitEntry)
            {
                _ref = UnitEntry;
            }

            // methods

            public void GetMonthlyKWhUsage() { }

            internal void SendBill(decimal amt)
            {
                if (AmountDue != 0)
                {
                    // if last month's bill is overdue
                    MonthsOverdue += 1;
                    BillTotal += amt; // add new bill
                }
                else
                {
                    BillTotal = amt; // otherwise set new bill
                    MonthsOverdue = 0; // also reset overdue months
                }

                // add amount due
                AmountDue += amt;
            }

            public void AddApartment(Unit toAdd)
            {
                // guard clauses
                if (Type != UnitType.Building) throw new Exception($"Cannot add unit '{toAdd.Name}' to unit '{Name}' of type '{Type}'.");
                if (toAdd.Type != UnitType.Apartment) throw new Exception($"Cannot add unit '{toAdd.Name}' of type '{toAdd.Type}' to building '{Name}'.");

                // set parent unit
                toAdd.ParentUnit = this;
            }

            public void Delete() => DeleteUnit(this);
        }

        /// Static Constructor ///
        
        static UnitManager()
        {
            UnitDB = CSVDataBase.CreateOrLoadDatabase(new[] { "unitName", "unitType", "managerUsername", "amountDue", "billTotal", "monthsOverdue", "parentUnitName" }, "units");
        }

        /// Unit Searching Methods ///

        public static Unit? GetUnitByName(string unitName)
        {
            var entries = UnitDB.GetEntriesByFieldValue("unitName", unitName);

            if (entries.Length > 1) throw new Exception($"There are multiple unit entries with the same name as '{unitName}'.");
            if (entries.Length == 0) return null;

            return new Unit(entries[0]);

        }

        public static Unit? CreateUnit(string unitName, string unitType, UserManager.User? manager = null, Unit? parentUnit = null)
        {
            // search for unit with the same name
            // if results.Lenght > 0, return null
            if (UnitDB.GetEntriesByFieldValue("unitName",unitName).Length > 0) return null;

            // create a db entry assigning specified values, and default values
            var entry = new DBEntry();
            entry.Set("unitName", unitName);
            entry.Set("unitType", unitType);
            entry.Set("managerUsername", manager?.Name ?? "null");
            entry.Set("parentUnitName", (unitType==UnitType.Apartment && parentUnit!=null) ? parentUnit.Name : "null");
            entry.Set("amountDue", "0.00");
            entry.Set("billTotal", "0.00");
            entry.Set("monthsOverdue", "0");

            // add db entry to UnitDB
            UnitDB.AddEntry(entry);

            // return Unit object created from the entry object
            return new Unit(entry);
        }

        public static void DeleteUnit(Unit unit)
        {
            // handle if unit is of type Building
            if (unit.Type == UnitType.Building)
                foreach (var child in unit.ChildUnits) DeleteUnit(child); // delete child units as well

            // handle if unit is owned by someone
            if (unit.ManagerUsername != null)
            {
                UserDB.GetEntriesByFieldValue("username", unit.ManagerUsername)[0].Set("unitName", "null"); // set owned unit name to null
                UserDB.SaveToDisk(); // save
            }

            UnitDB.DeleteEntriesByFieldValue("unitName", unit.Name); // delete in db
        }

    }
}
