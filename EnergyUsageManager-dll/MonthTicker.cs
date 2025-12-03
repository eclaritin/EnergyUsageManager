using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static EnergyUsageManager.DataStore;
using static EnergyUsageManager.UserManager;
using static EnergyUsageManager.UnitManager;
using static EnergyUsageManager.CalculationHelper;

namespace EnergyUsageManager
{
    public class MonthTicker
    {
        /// Classes ///
        
        public class MonthlyReport
        {
            // Fields //
            private readonly int rMonth;
            private readonly int rYear;
            private CSVDataBase db;

            public string Month { get => GetMonthName(rMonth); }
            public int Year { get => rYear; }

            // Constructor //
            private MonthlyReport(int month, int year)
            {
                // set basic fields
                rMonth = month;
                rYear = year;

                // generate file path
                string fname = GenerateReportFileName(month, year);

                // get file if it exists, otherwise generate new data

                if (Exists(month, year)) 
                    db = new CSVDataBase(fname);
                else
                    db = GenerateMonthlyData(fname);

            }

            // Data Retrieval //

            internal DBEntry? GetEntryByUnit(Unit unit)
            {
                var entries = db.GetEntriesByFieldValue("unitName", unit.Name);

                // handle units that have joined within the month
                if (entries.Length == 0) return null;

                return entries[0];
            }

            public decimal GetBillForUnit(Unit unit)
            {
                var entry = GetEntryByUnit(unit);
                return decimal.Parse(entry?.Get("billTotal") ?? "0");
            }

            public float GetEnergyUsageForUnit(Unit unit)
            {
                var entry = GetEntryByUnit(unit);
                return float.Parse(entry?.Get("energyUsage") ?? "0");
            }

            // Send out bills to get paid!! //

            internal void SendAllBills()
            {
                // loop through every entry
                db.ForEachEntry(entry =>
                {
                    var unit = GetUnitByName(entry.Get("unitName"));
                    unit.SendBill(GetBillForUnit(unit));
                });
            }

            // Static Methods //

            private static string GenerateReportFileName(int month, int year) => $"Monthly Reports\\{year}-{month}-EnergyUsage";

            public static bool Exists(int month, int year) => CSVDataBase.Exists(GenerateReportFileName(month, year));
            public static bool Exists(string month, int year) => Exists(GetMonthNumber(month), year);
            private static void ThrowIfExists(int month, int year)
            {
                if (Exists(month, year)) throw new Exception($"The energy usage report for {GetMonthName(month)}/{year} already exists!");
            }
            private static void ThrowIfDoesntExist(int month, int year)
            {
                if (!Exists(month,year)) throw new Exception($"The energy usage report for {GetMonthName(month)}/{year} doesn't exist!");
            }

            public static MonthlyReport? GetCurrent()
            {
                if (!Exists(_month, MonthTicker.Year)) return null; // return null if report hasn't been created yet
                return new MonthlyReport(_month, MonthTicker.Year); // otherwise create CSVDatabase object from path
            }

            public static MonthlyReport GenerateCurrent()
            {
                ThrowIfExists(MonthTicker._month, MonthTicker.Year); // for debugging purposes (program shouldn't be expecting to generate a new report if it already exists)
                return new MonthlyReport(MonthTicker._month, MonthTicker.Year);
            }

            public static MonthlyReport GetReport(int month, int year)
            {
                ThrowIfDoesntExist(month, year); // ensure file exists
                return new MonthlyReport(month, year);
            }
            public static MonthlyReport GetReport(string month, int year) => GetReport(GetMonthNumber(month), year);

            public static string[]? GetAllReportNames(bool formatted = true)
            {
                var fnames = GetFolderFileNames("Monthly Reports");

                if (fnames.Length == 0) return null;

                // return csvdatabase-ready filenames if not formatted
                if (!formatted) return fnames.Select(value => $"Monthly Reports\\{value}").ToArray();

                // otherwise format as "Energy usage report for {MonthName} {Year}"
                return fnames.Select(value =>
                {
                    // get month & year by taking the first two values between dashes
                    string month = GetMonthName(int.Parse(value.Split('-')[1]));
                    int year = int.Parse(value.Split('-')[0]);

                    // return formatted string for this entry
                    return $"Energy usage report for {month}, {year}";
                }).ToArray(); // perform this operation on all file names, return the result as an array
            }

        }

        /// Properties ///
        private static int _month
        {
            get => Settings.CurrentDate[0];
            set => Settings.CurrentDate = new int[] { value, Year };
        }
        
        public static string Month { get => GetMonthName(_month); }

        public static int Year
        {
            get => Settings.CurrentDate[1];
            set => Settings.CurrentDate = new int[] { _month, value };
        }

        public static bool TimeIsProgressing = false;

        /// Helper Methods ///
        
        private static void ShiftMonth()
        {
            if (_month+1 == 13)
            { 
                _month = 1;
                Year++;
            }
            else
            {
                _month++;
            }
            if (_month > 12 || _month < 1) throw new Exception($"Month number is somehow {_month} ?????");
        }

        internal static string GetMonthName(int month)
        {
            switch (month)
            {
                case 1: return "January";
                case 2: return "February";
                case 3: return "March";
                case 4: return "April";
                case 5: return "May";
                case 6: return "June";
                case 7: return "July";
                case 8: return "August";
                case 9: return "September";
                case 10: return "October";
                case 11: return "November";
                case 12: return "December";
                default: throw new Exception($"Invalid month number {_month} cannot be converted to string");
            }
        }
        internal static int GetMonthNumber(string month)
        {
            switch (month.Trim().ToLower())
            {
                case "january":
                case "jan":
                    return 1;
                case "february":
                case "feb":
                    return 2;
                case "march":
                case "mar":
                    return 3;
                case "april":
                case "apr":
                    return 4;
                case "may":
                    return 5;
                case "june":
                case "jun":
                    return 6;
                case "july":
                case "jul":
                    return 7;
                case "august":
                case "aug":
                    return 8;
                case "september":
                case "sep":
                case "sept":
                    return 9;
                case "october":
                case "oct":
                    return 10;
                case "november":
                case "nov":
                    return 11;
                case "december":
                case "dec":
                    return 12;
                default: throw new Exception($"Invalid string '{month}' cannot be converted to its corresponding int");
            }
        }

        /// Main Method ///

        internal static void Tick()
        {
            ShiftMonth(); // go to next month

            // create this month's CSV
            var report = MonthlyReport.GenerateCurrent();

            // send bills
            report.SendAllBills();
        }

        internal static async void TickLoop(int delay = 10)
        {
            int minsPassed = 0; // keeps track of elapsed time
            TimeIsProgressing = true;

            while (true)
            {
                // wait
                await Task.Delay(60000);

                // allow dev to pause time if they please
                if (!TimeIsProgressing) { continue; }

                // if time isn't paused, add to minutes passed
                minsPassed++;

                // Tick if minsPassed >= delay
                if (minsPassed >= delay)
                {
                    Tick();
                    minsPassed = 0;
                }
            }
        }

        // starts an asynchronous task that progresses time, returns the task
        public static Task Begin(int delayInMinutes = 10) => Task.Run(() => TickLoop(delayInMinutes));
    }
}
