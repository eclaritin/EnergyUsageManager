using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using static EnergyUsageManager.DataStore;
using static EnergyUsageManager.UnitManager;

namespace EnergyUsageManager
{
    public class UserManager
    {
        /// Enums ///
        public static class UserType {
            public static string Owner = "owner";
            public static string Renter = "renter";
            public static string Manager = "manager";
            public static string Admin = "admin";
        }
        
        public static class AdminSettings
        {
            public static string PricePerKWh = "PricePerKWh";
            public static string OveruseThreshold = "OveruseThreshold";
            public static string PenaltyPricePerKWh = "OverusePricePerKWh";
        }

        /// Properties ///
        private static Dictionary<int, User> Sessions = new();
        private static Random Randomness { get => new Random(System.Environment.TickCount); } // mostly unique seed for extra security
        internal static CSVDataBase UserDB;

        /// Classes ///
        
        public class User
        {
            // fields //
            private DBEntry _ref;

            // database fields
            public decimal Money
            {
                get => decimal.Parse(_ref.Get("money"));
                internal set
                {
                    _ref.Set("money", value.ToString());
                    UserDB.SaveToDisk();
                }
            }
            public string Name
            {
                get => _ref.Get("username");
                set
                {
                    _ref.Set("username", value);
                    UserDB.SaveToDisk();
                }
            }
            public string Type
            {
                get => _ref.Get("userType");
                set
                {
                    // validate
                    if (!(value==UserManager.UserType.Admin||value==UserManager.UserType.Owner||value==UserManager.UserType.Manager||value==UserManager.UserType.Renter)) throw new Exception($"UserType must be set to a value in UserManager.UserType! Got '{value}' instead");

                    _ref.Set("userType", value);
                    UserDB.SaveToDisk();
                }
            }
            private string? UnitName
            {
                get 
                { 
                    string name = _ref.Get("unitName");

                    // handle null value
                    if (name == "null") return null;

                    // otherwise return string
                    return name;
                }
                set
                {
                    _ref.Set("unitName", value ?? "null");
                    UserDB.SaveToDisk();
                }
            }
            
            // ease of use fields
            public string MoneyFormatted
            {
                get
                {
                    var str = "$" + Math.Round(Money, 2).ToString();
                    var sides = str.Split('.');

                    if (sides.Length == 1) return sides[0] + ".00";

                    if (sides[1].Length == 1) return sides[0] + '.' + sides[1] + "0";
                    if (sides[1].Length == 2) return str;

                    return "$NaN";
                }
            }
            public Unit? OwnedUnit 
            { 
                get => GetUnitByName(UnitName); 
                set
                {
                    if (UnitName != null)
                    {
                        // if user already has unit, delete that unit's manager name
                        OwnedUnit.ManagerUsername = null;
                    }

                    // handle null value
                    if (value == null) { UnitName = null; return; }

                    if (value.ManagerUsername != null)
                    {
                        // if unit already has a manager, erase it
                        value.ManagerUsername = null;
                    }

                    // set unit/manager names in databases
                    UnitName = value.Name;
                    value.ManagerUsername = Name;
                }
            }

            public readonly int SessionID;

            // constructor //

            internal User(DBEntry userEntry)
            {
                _ref = userEntry;
                SessionID = Randomness.Next();
            }

            // validators //

            private void ThrowIfNoUnit()
            {
                if (OwnedUnit == null)
                    throw new Exception($"User '{Name}' doesn't own a unit to perform operations on.");
            }

            // unit actions //

            public Unit AddUnit(string name, string? buildingName = null)
            {
                // prevent creating a unit if user already owns one
                if (UnitName != null) throw new Exception($"User '{Name}' already owns a unit '{UnitName}'. Delete it before creating a new one.");

                // declare vars
                Unit? parent = null;
                string type = "";

                // determine unit type from user type
                if (Type == UserType.Owner) type = UnitType.Home;
                else if (Type == UserType.Manager) type = UnitType.Building;
                else if (Type == UserType.Renter) type = UnitType.Apartment;
                else if (Type == UserType.Admin) throw new Exception("Admin cannot create units.");
                else throw new Exception($"Got unknown user type '{Type}' when creating unit for user '{Name}'.");

                // throw error if unit of same name already exists
                if (GetUnitByName(name) != null) throw new Exception($"Unit '{name}' already exists.");

                // handle renter conditions
                if (type == UnitType.Apartment)
                {
                    // require specifying building name to add an apartment to
                    if (buildingName == null) 
                        throw new Exception($"Apartment '{name}' cannot be added to null building. Please specify the name of the building to register this apartment to in the arguments.");

                    // set parent to building
                    parent = GetUnitByName(buildingName);

                    // handle null building again
                    if (parent == null)
                        throw new Exception($"Apartment '{name}' cannot be added to null building '{buildingName}'. Please register this building first, in order to register this apartment.");
                }

                return CreateUnit(name, type, this, parent);
            }

            public bool TryAddUnit(string name, string? buildingName, out Unit? newUnit)
            {
                try
                {
                    // try to add unit
                    newUnit = AddUnit(name, buildingName);
                    return true; // returns true if successful
                }
                catch
                {
                    // catch error and return failure
                    newUnit = null;
                    return false;
                }
            }
            public bool TryAddUnit(string name, out Unit? newUnit) => TryAddUnit(name, null, out newUnit);

            public void DeleteUnit()
            {
                var unit = OwnedUnit;
                if (unit == null) throw new Exception($"User '{Name}' doesn't own a unit to delete.");

                // delete the unit
                UnitManager.DeleteUnit(unit);
            }

            // money actions //

            public void AddMoney(decimal amt)
            {
                Money += amt;
            }

            public bool TryPay(decimal amt)
            {
                // return false if user doesn't own a unit or there is no bill to pay
                var unit = OwnedUnit;
                if (unit == null) return false;
                if (unit.AmountDue == 0) return false;

                // modify amount depending on how much the user has & how much is actually due
                decimal amtToPay = amt;
                amtToPay = Math.Min(Money, amtToPay);
                amtToPay = Math.Min(unit.AmountDue, amtToPay);

                // subtract amountDue in unit and money in user account
                Money -= amtToPay;
                unit.AmountDue -= amtToPay;

                // if unit type is apartment, pay manager
                if (unit.Type == UnitType.Apartment)
                {
                    // get manager dbentry
                    var manager = UserDB.GetEntriesByFieldValue("username", unit.ParentUnit.ManagerUsername)[0];

                    // get money amount
                    decimal managerMoney = decimal.Parse(manager.Get("money"));

                    // add & set
                    managerMoney += amtToPay;
                    manager.Set("money", managerMoney.ToString());
                    UserDB.SaveToDisk();
                }
                else
                {
                    // pay global money if any other unit type
                    Settings.GlobalMoney += amtToPay;
                }

                return true;
            }

            // manager only actions //

            private void ThrowIfNotManager()
            {
                if (this.Type != UserType.Manager)
                    throw new Exception($"User '{this.Name}' must be an admin account in order to use this feature.");
            }

            public Unit AddApartmentToBuilding(string name)
            {
                ThrowIfNotManager(); // this method is only for managers
                ThrowIfNoUnit(); // this method cannot be used if manager doesn't own a building

                if (GetUnitByName(name) != null) throw new Exception($"Unit '{name}' already exists.");

                return CreateUnit(name,UnitType.Apartment,null,OwnedUnit);
            }
            
            public bool TryAddApartmentToBuilding(string name, out Unit? newUnit)
            {
                // these errors should still happen because the user should not expect to be able to
                // call this if they don't satisfy the base requirements
                ThrowIfNotManager();
                ThrowIfNoUnit();

                try
                {
                    newUnit = AddApartmentToBuilding(name);
                    return true; // returns if adding was successful
                }
                catch
                {
                    newUnit = null;
                    return false; // returns failure if an error occurred
                }
            }

            // renter only actions //

            private void ThrowIfNotRenter()
            {
                if (this.Type != UserType.Renter)
                    throw new Exception($"User '{this.Name}' must be an renter account in order to use this feature.");
            }

            // admin only actions //

            private void ThrowIfNotAdmin()
            {
                if (this.Type != UserManager.UserType.Admin)
                    throw new Exception($"User '{this.Name}' must be an admin account in order to use this feature!");
            }

            private static void ThrowIfInvalidSetting(string settingKey)
            {
                if (!(settingKey == AdminSettings.PenaltyPricePerKWh || settingKey == AdminSettings.PricePerKWh || settingKey == AdminSettings.OveruseThreshold))
                    throw new Exception($"Expected value in UserManager.AdminSettings to be used as the setting key. Got '{settingKey}' instead.");
            }

            public void SetSetting(string settingKey, object value)
            {
                ThrowIfNotAdmin(); // admin only function
                ThrowIfInvalidSetting(settingKey); // validate setting key

                // do something different for each setting
                // (cannot use switch because the enums are not compile-time consts)
                if (settingKey == AdminSettings.PricePerKWh)
                {
                    // declare converted var
                    decimal toSet;
                    try
                    {
                        // attempt to cast
                        toSet = (decimal)value;
                    }
                    catch (InvalidCastException)
                    {
                        // on casting error, we now know the datatype given was invalid, throw more descriptive error
                        throw new Exception($"Expected decimal type when setting '{settingKey}'");
                    }

                    // set setting
                    Settings.PricePerKWh = toSet;
                }
                else if (settingKey == AdminSettings.PenaltyPricePerKWh)
                {
                    // same deal, time to copy & paste :)
                    // declare converted var
                    decimal toSet;
                    try
                    {
                        // attempt to cast
                        toSet = (decimal)value;
                    }
                    catch (InvalidCastException)
                    {
                        // on casting error, we now know the datatype given was invalid, throw more descriptive error
                        throw new Exception($"Expected decimal type when setting '{settingKey}'");
                    }

                    // set setting
                    Settings.OverusePricePerKWh = toSet;
                }else if (settingKey == AdminSettings.OveruseThreshold)
                {
                    // same deal but datatype should validate as float
                    // declare converted var
                    float toSet;
                    try
                    {
                        // attempt to cast
                        toSet = (float)value;
                    }
                    catch (InvalidCastException)
                    {
                        // on casting error, we now know the datatype given was invalid, throw more descriptive error
                        throw new Exception($"Expected float type when setting '{settingKey}'");
                    }

                    // set setting
                    Settings.OveruseThresholdKWh = toSet;
                }
            }

            public void ResetSettings()
            {
                ThrowIfNotAdmin(); // this is an admin-only method
                Settings.Reset(); // talk to settings class to reset
            }
        }

        /// Static Constructor ///

        static UserManager()
        {
            UserDB = CSVDataBase.CreateOrLoadDatabase(
                new[] { "username", "hashValue", "userType", "unitName", "money" },
                "users"
                );

        }

        /// Account Creation Methods ///

        internal static bool TryCreateAdminAccount(string username, string password)
        {
            bool userAlreadyExists = SearchForUserInDatabase(username) == null ? false : true;

            if (userAlreadyExists) return false;

            var myEntry = new DBEntry();
            myEntry.Set("username", username);
            myEntry.Set("hashValue", HashString(password).ToString());
            myEntry.Set("userType", UserType.Admin);
            myEntry.Set("unitName", "null");
            myEntry.Set("money", "0.00");

            UserDB.AddEntry(myEntry);

            return true;
        }

        public static bool TrySignUp(string username, string password, out int? sessionID)
        {
            return TrySignUp(username, password, UserType.Owner, out sessionID);
        }

        public static bool TrySignUp(string username, string password, string userType, out int? sessionID)
        {
            // validate userType
            if (!(userType == UserType.Owner || userType == UserType.Manager || userType == UserType.Renter))
            {
                sessionID = -1;
                return false;
            }

            bool userAlreadyExists = SearchForUserInDatabase(username) == null ? false : true;

            if (userAlreadyExists)
            {
                sessionID = -1;
                return false;
            }

            var myEntry = new DBEntry();
            myEntry.Set("username", username);
            myEntry.Set("hashValue", HashString(password).ToString());
            myEntry.Set("userType", userType);
            myEntry.Set("unitName", "null");
            myEntry.Set("money", "0.00");

            UserDB.AddEntry(myEntry);

            return TryLogin(username, password, out sessionID);
        }

        /// User Verification Methods ///

        private static DBEntry? SearchForUserInDatabase(string username)
        {
            var entryArray = UserDB.GetEntriesByFieldValue("username", username);

            if (entryArray.Length == 0) return null;
            if (entryArray.Length == 1) return entryArray[0];

            throw new Exception("There are multiple entries with the same username for some reason??????");

        }

        public static int HashString(string password)
        {
            // declare return var
            int hashed = 1;

            // loop through each character
            foreach (char c in password)
            {
                int charHash = c.GetHashCode();
                hashed += charHash;
                hashed *= password.Length;
            }

            hashed = Math.Abs(hashed);

            // return hashed password
            return hashed;
        }

        /// Session Methods ///

        public static int GenerateSessionID() => Randomness.Next();

        public static bool TryLogin(string username, string password, out int? sessionID)
        {
            // get hashed password
            int hashed = HashString(password);

            // placeholder value for session ID
            sessionID = -1;

            // if user doesn't exist in db, return false.
            if (!UserDB.ContainsFieldValue("username", username)) return false;

            // get entry for user in database
            var entry = UserDB.GetEntriesByFieldValue("username", username)[0];

            // get hashValue
            int fileHash = int.Parse(entry.Get("hashValue"));

            // compare fileHash with hashed, if they are different, return false because the password mustve been incorrect
            if (fileHash != hashed) return false;

            // if the program has gotten this far, the user entry exists and the password was correct
            // set session ID and return true
            var user = new User(entry);
            sessionID = user.SessionID;

            Sessions.Add((int)sessionID, user);
            return true;
        }

        public static bool TryLogout(int sessionID) => Sessions.Remove(sessionID);

        public static User? GetUserFromSessionID(int sessionID) => Sessions.GetValueOrDefault(sessionID);

    }
}
