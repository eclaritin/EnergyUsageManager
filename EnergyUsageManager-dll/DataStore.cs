using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using static EnergyUsageManager.DataStore;
using static EnergyUsageManager.UserManager;

namespace EnergyUsageManager
{
    public class DataStore
    {
        /// Classes ///
        internal class DBEntry
        {
            private Dictionary<string, string> Data;
            public string[] Fields { get => Data.Keys.ToArray(); }

            public DBEntry(Dictionary<string, string> data)
            {
                Data = data;
            }
            public DBEntry()
            {
                Data = new Dictionary<string, string>();
            }

            public void Set(string key, string value)
            {
                if (Data.ContainsKey(key)) Data[key] = value;
                else Data.Add(key, value);
            }
            public string? Get(string key) => Data.GetValueOrDefault(key);

        }

        internal class CSVDataBase
        {
            // properties

            private List<DBEntry> db;
            public readonly string[] Fields;
            public readonly string path;

            // constructors

            public CSVDataBase(string[] fields, string filename = "Untitled")
            {
                db = new List<DBEntry>();
                Fields = fields.ToArray();
                path = AppDataPath + "\\" + filename + ".csv";

                SaveToDisk();
            }

            public CSVDataBase(string filename = "Untitled")
            {
                path = AppDataPath + "\\" + filename + ".csv";

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"File '{filename}.csv' not found in appdata. If you are initializing a new database, please specify database fields in your first argument.");
                }

                db = new List<DBEntry>();

                var workingFields = new List<string>();

                bool firstline = true; // first line flag true by default, set to false after first line iteration
                foreach (string line in File.ReadLines(path))
                {
                    if (line.Trim() == "") continue; // ignore empty lines

                    var cols = line.Split(',');
                    var entry = new DBEntry();

                    // enumerate over each column
                    for (int i = 0; i < cols.Length; i++)
                    {
                        string col = cols[i];

                        if (firstline) // set up dictionary keys
                        {
                            workingFields.Add(col);

                            // prevent flow from reaching non-firstline code
                            continue;
                        }

                        // get key from column index
                        string key = workingFields[i];

                        // set key in entry to value
                        entry.Set(key, col);
                    }

                    // disable first line and continue to normal looping
                    if (firstline) { firstline = false; continue; }

                    db.Add(entry);
                }

                // set fields array's final value
                Fields = workingFields.ToArray();
            }

            // static methods //
            ////////////////////
            
            public static CSVDataBase LoadFromDisk(string filename = "Untitled") => new CSVDataBase(filename);

            public static string PrintEntry(DBEntry entry, bool writeToConsole = true)
            {
                var printList = new List<string>();

                foreach (var key in entry.Fields)
                {
                    printList.Add($"{key}: {entry.Get(key)}");
                }

                string toPrint = string.Join("\n", printList);
                if (writeToConsole) Console.WriteLine(toPrint);

                return toPrint;
            }

            public static bool Exists(string filename)
            {
                // construct csv database path
                string path = AppDataPath + "\\" + filename + ".csv";

                // check if it exists
                return File.Exists(path);
            }

            public static CSVDataBase CreateOrLoadDatabase(string[] expectedFields, string filename = "Untitled")
            {
                // delcare
                CSVDataBase db;

                // try to load existing database
                try
                {
                    db = new CSVDataBase(filename);
                }
                // otherwise, create it (autosaves, so this only applies to first run)
                catch
                {
                    db = new CSVDataBase(expectedFields, filename);
                }

                return db;
            }

            // methods //
            /////////////

            // validation
            public bool FieldExists(string field)
            {
                return Fields.Contains(field);
            }
            
            public void ThrowIfNonexistentField(string field)
            {
                if (FieldExists(field)) return;
                throw new Exception($"Field {field} doesn't exist!");
            }

            // selectors
            public DBEntry[] GetEntriesByFieldValue(string field, string value)
            {
                ThrowIfNonexistentField(field);

                // declare working list
                var foundEntries = new List<DBEntry>();

                // loop through every entry & test if field is equal to value
                foreach(var entry in db)
                {
                    if (entry.Get(field) == value) foundEntries.Add(entry);
                }

                // return final entry array
                return foundEntries.ToArray();
            }

            public bool ContainsFieldValue(string field, string value)
            {
                ThrowIfNonexistentField(field);

                // search each entry for entry.Get(field)==value
                foreach (var entry in db)
                {
                    // return true if found
                    if (entry.Get(field) == value) return true;
                }

                // return false otherwise
                return false;

            }

            // operations
            public void AddEntry(DBEntry entry, bool setMissingFieldsToNull = false)
            {
                // check each key for invalid fields
                int matchingFields = 0;
                foreach (var key in entry.Fields)
                {
                    if (!Fields.Contains(key)) throw new Exception("DBEntry contains field that doesn't exist in database");
                    matchingFields++;
                }

                if (matchingFields < Fields.Length)
                {
                    if (!setMissingFieldsToNull) throw new Exception("DBEntry doesn't contain every field required to be in this database");

                    foreach(var field in Fields)
                    {
                        if (entry.Get(field) != null) continue;
                        entry.Set(field, "null");
                    }
                }

                db.Add(entry);
                SaveToDisk();
            }

            public void DeleteEntriesByFieldValue(string field, string value) { 
                ThrowIfNonexistentField(field);

                // remove all matching entries
                db.RemoveAll(entry => entry.Get(field) == value);

                // weird memory thing that i'ma try in an effort to debug this
                db = db.ToArray().ToList();

                // save changes
                SaveToDisk();
            }

            public void ForEachEntry(Action<DBEntry> action)
            {
                foreach (var entry in db)
                {
                    action.Invoke(entry);
                }
            }

            // filesystem methods
            internal void SaveToDisk()
            {

                // initialize writer
                FileStream writeStream = File.Create(path);

                // write header
                var headerStr = string.Join(",", Fields)+"\n";
                var headerBytes = Encoding.UTF8.GetBytes(headerStr);
                writeStream.Write(headerBytes);

                // loop through database, writing each entry to file
                foreach (var entry in db)
                {
                    var line = new List<string>();

                    // get each field's value, adding to line list
                    foreach (var field in Fields)
                    {
                        line.Add(entry.Get(field) ?? "");
                    }

                    // join & write to file
                    var lineStr = string.Join(",", line)+"\n";
                    var lineBytes = Encoding.UTF8.GetBytes(lineStr);
                    writeStream.Write(lineBytes);
                    
                }

                writeStream.Close();
            }

            private void CheckForUpdates()
            {
                var diskDB = LoadFromDisk(path);

                // compare number of entries
                if (diskDB.db.Count != db.Count)
                {
                    db = diskDB.db; // update memory copy
                    return;
                }

                // compare each entry
                for (int i = 0; i < db.Count; i++)
                {
                    // get entries
                    var entry = db[i];
                    var diskEntry = diskDB.db[i];

                    // compare each field, if any differ, update memory copy
                    foreach (var field in Fields)
                    {
                        // get values in field
                        var val = entry.Get(field);
                        var dval = diskEntry.Get(field);

                        // compare
                        if (val != dval)
                        {
                            db = diskDB.db; // copy entire database from disk
                            return;
                        }
                    }
                }
            }

            // debug
            public string PrintAllEntries(bool writeToConsole = true)
            {
                var printList = new List<string>();

                // loop through entries adding all to print list, formatted with my PrintEntry method :)
                foreach(var entry in db)
                {
                    printList.Add(PrintEntry(entry, false));
                    printList.Add("--------------------\n"); // make it less visually cluttered
                }

                // construct string
                string printable = string.Join("\n", printList);

                // write to console
                if (writeToConsole) Console.WriteLine(printable);

                return printable;
            }

        }

        public static class Settings
        {
            // properties
            internal static string SettingsPath { get => AppDataPath+"\\adminSettings.ini"; }

            public static decimal PricePerKWh
            {
                get
                {
                    string key = "PricePerKWh"; // using a var because there's multiple references to this & I gotta copy and paste for each property

                    // parse value from settings
                    bool success = decimal.TryParse(GetValue(key), out decimal parsed);

                    // throw error message for debugging if it couldn't be parsed
                    if (!success) throw new Exception($"Unable to parse value in {SettingsPath}['{key}']!");

                    return parsed;
                }
                set
                {
                    SetValue("PricePerKWh",value.ToString());
                }
            }

            public static float OveruseThresholdKWh
            {
                get
                {
                    string key = "OveruseThreshold"; 

                    // parse value from settings
                    bool success = float.TryParse(GetValue(key), out float parsed);

                    // throw error message for debugging if it couldn't be parsed
                    if (!success) throw new Exception($"Unable to parse value in {SettingsPath}['{key}']!");

                    return parsed;
                }
                set
                {
                    SetValue("OveruseThreshold", value.ToString());
                }
            }

            public static decimal OverusePricePerKWh
            {
                get
                {
                    string key = "OverusePricePerKWh"; 

                    // parse value from settings
                    bool success = decimal.TryParse(GetValue(key), out decimal parsed);

                    // throw error message for debugging if it couldn't be parsed
                    if (!success) throw new Exception($"Unable to parse value in {SettingsPath}['{key}']!");

                    return parsed;
                }
                set
                {
                    SetValue("OverusePricePerKWh", value.ToString());
                }
            }

            public static decimal GlobalMoney
            {
                get
                {
                    string key = "GlobalMoney";

                    // parse value from settings
                    bool success = decimal.TryParse(GetValue(key), out decimal parsed);

                    // throw error message for debugging if it couldn't be parsed
                    if (!success) throw new Exception($"Unable to parse value in {SettingsPath}['{key}']!");

                    return parsed;
                }
                set
                {
                    SetValue("GlobalMoney", value.ToString());
                }
            }

            public static int[] CurrentDate
            {
                get
                {
                    string raw = GetValue("CurrentDate");

                    return raw.Split('/').Select(value =>
                    {
                        bool success = int.TryParse(value.Trim(), out int num);

                        // handle incorrectly formatted CurrentDate
                        if (!success) throw new Exception($"Expected 'int/int' in settings.ini['CurrentDate'], got '{raw}' instead. Is the settings.ini file formatted incorrectly?");

                        return num;
                    }).ToArray();
                }
                set
                {
                    if (value.Length != 2) throw new Exception($"settings.ini['CurrentDate'] array must have 2 values, got {value.Length} instead.");
                    if (value[0] < 1 || value[0] > 12) throw new Exception($"Month in settings.ini['CurrentDate'] must be between 1-12 (inclusive), instead got {value[0]}");

                    SetValue("CurrentDate", $"{value[0]}/{value[1]}"); // formatted as 'Month/Year'
                }
            }

            // static constructor
            static Settings()
            {
                if (!File.Exists(SettingsPath)) Reset();
            }

            // methods
            public static void Reset()
            {
                // starting values
                decimal GM = 0;
                int[] CD = new[] { 12, 2025 };
                
                // read file for global values if it exists
                if (File.Exists(SettingsPath))
                {
                    GM = GlobalMoney;
                    CD = CurrentDate;
                }

                // keep global money & current date the same because they are global values and not necessarily meant to be settings
                File.WriteAllText(SettingsPath, $"PricePerKWh=0.1762\nOveruseThreshold=15000\nOverusePricePerKWh=0.389\n" +
                    $"GlobalMoney={GM}\n" +
                    $"CurrentDate={CD[0]}/{CD[1]}"); 
            }

            private static string? GetValue(string key)
            {
                // read data, split into lines
                string data = File.ReadAllText(SettingsPath);
                string[] lines = data.Split('\n');

                // loop through lines
                foreach (string line in lines)
                {
                    // skip empty lines
                    if (line.Trim() == "") continue;

                    // split line by = to get an array where [0] = key & [1] = value
                    string[] keyValPair = line.Trim().Split("=");
                    string thisKey = keyValPair[0].Trim();
                    string thisValue = keyValPair[1].Trim();

                    // if key doesn't match the search key, skip
                    if (thisKey != key) continue;

                    // otherwise, we have the answer
                    return thisValue;
                }

                // if code reaches this, that means there's no value, throw exception for debugging
                throw new Exception($"'{key}' not found in settings file! Did you misspell the search key?");
            }

            private static void SetValue(string key, string value)
            {
                string PPKWH = key.ToLower() == "priceperkwh" ? value : PricePerKWh.ToString();
                string OUTH = key.ToLower() == "overusethreshold" ? value : OveruseThresholdKWh.ToString();
                string OUPPKWH = key.ToLower() == "overusepriceperkwh" ? value : OverusePricePerKWh.ToString();
                string GM = key.ToLower() == "globalmoney" ? value : GlobalMoney.ToString();
                string CD = key.ToLower() == "currentdate" ? value : $"{CurrentDate[0]}/{CurrentDate[1]}";

                string toWrite = $"PricePerKWh={PPKWH}\nOveruseThreshold={OUTH}\nOverusePricePerKWh={OUPPKWH}\nGlobalMoney={GM}\nCurrentDate={CD}";
                File.WriteAllText(SettingsPath, toWrite);
            }
        }

        /// Properties ///
        internal static string AppDataPath { get => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\EnergyUsageManager"; }
        internal static string MonthlyReportsPath { get => AppDataPath + "\\Monthly Reports"; }

        /// Static Constructor ///

        static DataStore()
        {
            // ensure folder in app data exists
            if (!Directory.Exists(AppDataPath)) Directory.CreateDirectory(AppDataPath);

            // ensure monthly reports folder exists
            if (!Directory.Exists(MonthlyReportsPath)) Directory.CreateDirectory(MonthlyReportsPath);
        }

        /// Directory Functions ///

        internal static string[] GetFolderFileNames(string folderName = "")
        {
            string path = AppDataPath + (folderName == "" ? "" : "\\") + folderName;
            string[] paths = Directory.GetFiles(path);
            var pathList = new List<string>();

            foreach (string thisPath in paths)
            {
                // skip over system files & hidden files
                var attr = File.GetAttributes(thisPath);
                if (attr.HasFlag(FileAttributes.System) || attr.HasFlag(FileAttributes.Hidden)) { continue; }

                // separate path by slashes
                var splitDir = thisPath.Split('\\');

                // add file name without extension
                pathList.Add(splitDir[splitDir.Length - 1].Split('.')[0]);
            }

            return pathList.ToArray();
        }
    }
}
