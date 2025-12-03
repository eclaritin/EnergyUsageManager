using System;
using System.Collections.Generic;
using static EnergyUsageManager.DataStore;
using static EnergyUsageManager.UnitManager;

namespace EnergyUsageManager
{
    internal class CalculationHelper
    {
        /// Properties ///
        private static Random Randomness { get => new Random(System.Environment.TickCount); }
        
        /// Bill Calculation Methods ///
        internal static decimal CalculateUnitBill(Unit unit, out float kWhUsage) 
        {
            kWhUsage = GenerateUnitKWhUsage(unit);
            if (unit.Type == UnitType.Home || unit.Type == UnitType.Apartment)
            {
                if (kWhUsage >= Settings.OveruseThresholdKWh)
                {
                    return (decimal)kWhUsage * Settings.OverusePricePerKWh;
                }
                else
                {
                    return (decimal)kWhUsage * Settings.PricePerKWh;
                }
            }else if (unit.Type == UnitType.Building)
            {
                // calculate bill of each unit
                decimal sum = 0;
                foreach(Unit child in unit.ChildUnits)
                {
                    sum += CalculateUnitBill(child, out float thisUsage);
                }

                return sum;
            }
            else
            {
                throw new Exception($"Invalid unit type '{unit.Type}'.");
            }
        }

        private static float GenerateUnitKWhUsage(Unit unit)
        {
            if (unit.Type == UnitType.Apartment)
            {
                // Apartment: 600-1500 kwh/month
                float min = 600;
                float max = 1500;
                return min + (float)Randomness.NextDouble() * (max - min);
            }
            else if (unit.Type == UnitType.Home)
            {
                // Home: 1100-3400 kwh/month
                float min = 1100;
                float max = 3400;
                return min + (float)Randomness.NextDouble() * (max - min);
            }
            else if (unit.Type == UnitType.Building)
            {
                // Building: sums usage of each child unit
                float total = 0;
                foreach (var child in unit.ChildUnits)
                {
                    total += GenerateUnitKWhUsage(child);
                }
                return total;
            }
            else
            {
                // unexpected type
                throw new Exception($"Unexpected unit type '{unit.Type}', please use a value in the UnitManager.UnitType enum.");
            }

        }

        internal static CSVDataBase GenerateMonthlyData(string filename)
        {
            // create new database from filename
            var db = new CSVDataBase(new string[] { "unitName", "energyUsage", "energyOveruse", "billTotal" }, filename);

            // generate monthly data for each unit entry
            UnitDB.ForEachEntry(unitEntry =>
            {
                // get unit object
                var unit = new Unit(unitEntry);

                // create new entry to generate
                var entry = new DBEntry();

                // set properties to calculations
                entry.Set("unitName", unit.Name);
                entry.Set("billTotal", CalculateUnitBill(unit, out float thisUsage).ToString());
                entry.Set("energyUsage", thisUsage.ToString());
                entry.Set("energyOveruse", thisUsage > Settings.OveruseThresholdKWh ? "yes" : "no");

                // add entry to db
                db.AddEntry(entry);
            });

            return db;
        }

    }
}
