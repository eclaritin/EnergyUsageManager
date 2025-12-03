using System;
using System.Data;
using System.Threading.Tasks;
using EnergyUsageManager; 

// I originally wrote EnergyUsageManager in a separate project than the Frontend.css program. The backend code would be compiled into a .dll class library,
// but because that made it hard to set up the project on each group members computer, I decided to just include it as a folder in one big console-app project.

using static EnergyUsageManager.Test.Render;

namespace EnergyUsageManager.Test
{
    internal class Render
    {
        private static void WriteHorizontalLine()
        {
            int width = Console.BufferWidth; // maximum amount of characters that can be displayed on one line for the console

            string toDisplay = ""; // this string will be written to console when this is done

            while (toDisplay.Length < width) // loops until the line is as long as window
            {
                toDisplay += "-"; // adds a "-" character every iteration
            }

            Console.WriteLine(toDisplay); // write the horizontal line that was just generated to the console
        }

        public static void WriteHeader()
        {
            bool loggedIn = !(Program.CurrentUser == null); // if sessionID is null, then user is not logged in, therefore false, otherwise true if logged in

            Console.WriteLine(MonthTicker.Month + ", " + MonthTicker.Year); // display month & year

            if (loggedIn) // display user information if logged in
            {
                var user = Program.CurrentUser;

                Console.WriteLine(user.Type + ": " + user.Name); // [User type]: [username]
                Console.WriteLine("Money: " + user.MoneyFormatted); // use MoneyFormatted field for displaying purposes :)
            }

            // write a horizontal line & newline to visually separate data
            WriteHorizontalLine();
            Console.WriteLine();
        }

        public static async void StartTimeDrawer()
        {
            while (true)
            {
                // wait
                await Task.Delay(15 * 1000); // every 15 seconds (15*1000 milliseconds)

                // get file data before writing anything so there's less chance of main rendering from being interrupted
                string dateText = MonthTicker.Month + ", " + MonthTicker.Year + "      "; // extra space at end for clearing last text

                int[] originalCursorPos = new[] {Console.CursorLeft, Console.CursorTop}; // save current position
                
                // update date text
                Console.SetCursorPosition(0, 0);
                Console.Write(dateText); 

                Console.SetCursorPosition(originalCursorPos[0], originalCursorPos[1]); // go back to current position
            }
        }

        // Prompting Methods //
        public static string Prompt(string message, bool allowBlank = false)
        {
            while (true)
            {
                if (message != "") Console.Write(message + ": "); // if message isn't empty write a default formatted message
                string input = Console.ReadLine() ?? "";


                if (input.Trim() == "" && !allowBlank)
                {
                    Console.WriteLine("Please enter something.\n");
                    continue; // prevent code after this from running
                }

                return input;
            }
        }

        public static int PromptForInt(string message, int[]? withinRange = null)
        {
            // validate that the withinRange argument is valid
            if (withinRange != null)
            {
                if (withinRange.Length != 2) throw new Exception($"Range must have inclusive lower bound in index 0, and inclusive upper bound in index 1. Got an array with {withinRange.Length} ints.");
                if (withinRange[0] > withinRange[1]) throw new Exception($"Lower bound ({withinRange[0]}) was greater than upper bound ({withinRange[1]}). Did you mean `new []{{{withinRange[1]},{withinRange[0]}}}`?");
            }

            // looping variables 
            int toReturn = 0;
            bool success = false;

            // loop until int input validation allows a value to pass through
            while (!success)
            {
                // get raw text input
                string textInput = Prompt(message);

                // test if input is integer
                success = int.TryParse(textInput, out toReturn);

                // message to be displayed if not integer
                if (!success)
                {
                    Console.WriteLine("Please enter a whole number!\n");
                    continue;
                }

                // is range specified? if so, do range validation
                if (withinRange != null)
                {
                    // test if input is within range
                    success = (toReturn >= withinRange[0] && toReturn <= withinRange[1]);

                    // display message if out of range
                    if (!success)
                    {
                        Console.WriteLine($"Please enter a number between {withinRange[0]} and {withinRange[1]}!\n");
                        continue;
                    }
                }
            }

            // return value if program has made it this far
            return toReturn;
        }

        public static float PromptForFloat(string message, float[]? withinRange = null)
        {
            // validate that the withinRange argument is valid
            if (withinRange != null)
            {
                if (withinRange.Length != 2) throw new Exception($"Range must have inclusive lower bound in index 0, and inclusive upper bound in index 1. Got an array with {withinRange.Length} floats.");
                if (withinRange[0] > withinRange[1]) throw new Exception($"Lower bound ({withinRange[0]}) was greater than upper bound ({withinRange[1]}). Did you mean `new []{{{withinRange[1]},{withinRange[0]}}}`?");
            }

            // looping variables 
            float toReturn = 0;
            bool success = false;

            // loop until int input validation allows a value to pass through
            while (!success)
            {
                // get raw text input
                string textInput = Prompt(message);

                // test if input is integer
                success = float.TryParse(textInput, out toReturn);

                // message to be displayed if not integer
                if (!success)
                {
                    Console.WriteLine("Please enter a number!\n");
                    continue;
                }

                // is range specified? if so, do range validation
                if (withinRange != null)
                {
                    // test if input is within range
                    success = (toReturn >= withinRange[0] && toReturn <= withinRange[1]);

                    // display message if out of range
                    if (!success)
                    {
                        Console.WriteLine($"Please enter a number between {withinRange[0]} and {withinRange[1]}!\n");
                        continue;
                    }
                }
            }

            // return value if program has made it this far
            return toReturn;
        }

        public static decimal PromptForMoney(string message, decimal[]? withinRange = null)
        {
            // validate that the withinRange argument is valid
            if (withinRange != null)
            {
                if (withinRange.Length != 2) throw new Exception($"Range must have inclusive lower bound in index 0, and inclusive upper bound in index 1. Got an array with {withinRange.Length} decimal types.");
                if (withinRange[0] > withinRange[1]) throw new Exception($"Lower bound ({withinRange[0]}) was greater than upper bound ({withinRange[1]}). Did you mean `new []{{{withinRange[1]},{withinRange[0]}}}`?");
            }

            // looping variables 
            decimal toReturn = 0;
            bool success = false;

            // loop until int input validation allows a value to pass through
            while (!success)
            {
                // prompt message
                if (message != "") Console.Write(message + ": $");
                else Console.Write("$");

                // get raw text input
                string textInput = Prompt("");

                // test if input is integer
                success = decimal.TryParse(textInput, out toReturn);

                // message to be displayed if not integer
                if (!success)
                {
                    Console.WriteLine("Please enter a number!\n");
                    continue;
                }

                // is range specified? if so, do range validation
                if (withinRange != null)
                {
                    // test if input is within range
                    success = (toReturn >= withinRange[0] && toReturn <= withinRange[1]);

                    // display message if out of range
                    if (!success)
                    {
                        Console.WriteLine($"Please enter a number between {withinRange[0]} and {withinRange[1]}!\n");
                        continue;
                    }
                }
            }

            // return value if program has made it this far
            return toReturn;
        }

        public static int PromptOptions(string message, string[] options)
        {
            // display beginning messages
            Console.Clear();
            Render.WriteHeader(); // header always
            Console.WriteLine(message);
            Console.WriteLine("");

            // loop through options displaying each one with their index
            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine($"{i + 1}) {options[i]}");
            }
            Console.WriteLine("");

            // return index (zero-based; hence the "- 1") that the user chooses
            // (this method automatically handles type & range validation)
            return PromptForInt("Choose an item", new[] { 1, options.Length }) - 1;

        }

    }

    public class Program
    {
        // Runtime Global Vars //
        internal static int? MySessionID = null;

        internal static UserManager.User? CurrentUser
        {
            get // every time Program.CurrentUser is referred to,
                // it runs the following code to return the user automatically
                // for convenience! :D
            {
                if (MySessionID == null) return null;

                // casting is required to convert the nullable, int?, datatype into the expected int type
                return UserManager.GetUserFromSessionID((int)MySessionID);
            }

            // (the value of Program.CurrentUser cannot be set, only "read")
        }

        // Entry-Point //
        public static void Main(string[] args)
        {
            MonthTicker.Begin(2); // begins a loop that ticks the months every 2 minutes
            // rest of code still progresses because the task that handles the loop is asynchronous from the UI.

            StartTimeDrawer(); // also an async function

            MainMenu(); // finally go to main menu
        }

        // Screens //
        public static void MainMenu()
        {
            while (true)
            {
                int choice = PromptOptions("Main Menu", new[] { "Log in", "Create account", "Exit program" });

                switch (choice)
                {
                    case 0: LoginScreen(); break;
                    case 1: SignupScreen(); break;
                    case 2: return;
                }
            }
        }

        public static void LoginScreen()
        {
            Console.Clear();
            Console.WriteLine("Login screen\n\n");

            string uname = Prompt("Username");
            string pword = Prompt("Password");

            bool success = UserManager.TryLogin(uname, pword, out MySessionID);
            if (!success)
            {
                Console.WriteLine("\nYour username or password is incorrect!\nPress any key to return to the menu.");
                Console.ReadKey(true);
                return;
            }

            UnitManagerScreen();
        }

        public static void SignupScreen()
        {
            Console.Clear();
            Console.WriteLine("Account creation screen\n\n");

            string uname = Prompt("Username");
            string pword = Prompt("Password");
            string confirmpword = Prompt("Confirm password");

            if (confirmpword != pword)
            {
                Console.WriteLine("\nYour passwords don't match! Please try again!\nPress any key to return to menu.");
                Console.ReadKey(true);
                return;
            }

            // ask user which non-admin account type they want
            int typeChoice = PromptOptions("Select account type", new[] { "Homeowner", "Renter", "Building manager" });
            string selectedType = typeChoice switch
            {
                0 => UserManager.UserType.Owner,
                1 => UserManager.UserType.Renter,
                2 => UserManager.UserType.Manager,
                _ => UserManager.UserType.Owner
            };

            bool success = UserManager.TrySignUp(uname, pword, selectedType, out MySessionID);
            if (!success)
            {
                Console.WriteLine("\nThere was an error creating your account!\nDo you already have one?\nPress any key to return to menu.");
                Console.ReadKey(true);
                return;
            }

            UnitManagerScreen();
        }

        public static void UnitManagerScreen()
        {
            // if user is admin, call AdminScreen, then return

            var user = CurrentUser;
            if (user == null) return; // safeguard

            if (user.Type == UserManager.UserType.Admin)
            {
                AdminScreen();
                return;
            }

            while (true)
            {
                // build options dynamically depending on user type & owned unit
                var options = new System.Collections.Generic.List<string>();

                options.Add("View my unit");

                if (user.Type == UserManager.UserType.Owner || user.Type == UserManager.UserType.Manager)
                    if (user.OwnedUnit == null) options.Add("Create a unit");

                if (user.Type == UserManager.UserType.Manager)
                    options.Add("Add apartment to my building");

                if (user.OwnedUnit != null)
                    options.Add("Delete my unit");

                if (user.Type == UserManager.UserType.Renter) options.Add("Assign myself to an existing unit");
                options.Add("Add money to my account");
                options.Add("Pay bill");
                options.Add("Logout");

                int choice = PromptOptions("Unit Management", options.ToArray());
                string chosen = options[choice];

                try
                {
                    if (chosen == "View my unit")
                    {
                        Console.Clear();
                        Render.WriteHeader();

                        var unit = user.OwnedUnit;
                        if (unit == null)
                        {
                            Console.WriteLine("You don't have a unit assigned.");
                        }
                        else
                        {
                            Console.WriteLine($"Name: {unit.Name}");
                            Console.WriteLine($"Type: {unit.Type}");
                            Console.WriteLine($"Amount due: ${Math.Round(unit.AmountDue, 2)}");
                            Console.WriteLine($"Bill total: ${Math.Round(unit.BillTotal, 2)}");
                            Console.WriteLine($"Months overdue: {unit.MonthsOverdue}");

                            if (unit.Type == UnitManager.UnitType.Building)
                            {
                                Console.WriteLine("\nChild units:");
                                foreach (var child in unit.ChildUnits)
                                {
                                    Console.WriteLine($" - {child.Name} ({child.Type}) | Due: ${Math.Round(child.AmountDue, 2)}");
                                }
                            }
                        }

                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey(true);
                    }
                    else if (chosen == "Create a unit")
                    {
                        Console.Clear();
                        Render.WriteHeader();

                        string name = Prompt("Unit name");

                        if (user.Type == UserManager.UserType.Owner)
                        {
                            var created = user.AddUnit(name);
                            user.OwnedUnit = created;
                            Console.WriteLine($"Unit '{name}' created and assigned to you.");
                        }
                        else // manager
                        {
                            var created = user.AddUnit(name);
                            user.OwnedUnit = created;
                            Console.WriteLine($"Building '{name}' created and assigned to you.");
                        }

                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey(true);
                    }
                    else if (chosen == "Add apartment to my building")
                    {
                        Console.Clear();
                        Render.WriteHeader();

                        if (user.OwnedUnit == null || user.OwnedUnit.Type != UnitManager.UnitType.Building)
                        {
                            Console.WriteLine("You must own a building to add apartments to it.");
                        }
                        else
                        {
                            string aname = Prompt("Apartment name");
                            var success = user.TryAddApartmentToBuilding(aname, out var newUnit);
                            if (success)
                                Console.WriteLine($"Apartment '{aname}' added to building '{user.OwnedUnit.Name}'.");
                            else
                                Console.WriteLine("Failed to add apartment. Does a unit with that name already exist?");
                        }

                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey(true);
                    }
                    else if (chosen == "Assign myself to an existing unit")
                    {
                        Console.Clear();
                        Render.WriteHeader();

                        string name = Prompt("Enter the exact unit name to assign to your account");
                        var unit = UnitManager.GetUnitByName(name);
                        if (unit == null)
                        {
                            Console.WriteLine($"Unit '{name}' not found.");
                        }
                        else
                        {
                            user.OwnedUnit = unit;
                            Console.WriteLine($"You are now assigned to unit '{unit.Name}'.");
                        }

                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey(true);
                    }
                    else if (chosen == "Add money to my account")
                    {
                        Console.Clear();
                        Render.WriteHeader();

                        decimal amt = PromptForMoney("Amount to add");
                        user.AddMoney(amt);
                        Console.WriteLine($"Added ${Math.Round(amt, 2)} to your account. New balance: {user.MoneyFormatted}");

                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey(true);
                    }
                    else if (chosen == "Pay bill")
                    {
                        Console.Clear();
                        Render.WriteHeader();

                        if (user.OwnedUnit == null)
                        {
                            Console.WriteLine("You don't have a unit to pay for.");
                        }
                        else if (user.OwnedUnit.AmountDue == 0)
                        {
                            Console.WriteLine("You have no outstanding bill.");
                        }
                        else
                        {
                            Console.WriteLine($"Amount due: ${Math.Round(user.OwnedUnit.AmountDue, 2)}");
                            decimal pay = PromptForMoney("Amount to pay", new decimal[] { 0, user.Money });

                            bool paid = user.TryPay(pay);
                            if (paid) Console.WriteLine("Payment processed.");
                            else Console.WriteLine("Payment failed.");
                        }

                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey(true);
                    }
                    else if (chosen == "Logout")
                    {
                        UserManager.TryLogout(user.SessionID);
                        MySessionID = null;
                        Console.WriteLine("Logged out. Press any key to continue...");
                        Console.ReadKey(true);
                        return;
                    }
                    else if (chosen == "Delete my unit")
                    {
                        Console.Clear();
                        Render.WriteHeader();

                        if (user.OwnedUnit == null)
                        {
                            Console.WriteLine("You don't have a unit to delete.");
                        }
                        else
                        {
                            Console.WriteLine($"Are you sure you want to delete your unit '{user.OwnedUnit.Name}'? This action cannot be undone. (y/n)");
                            string confirm = Console.ReadLine() ?? "";
                            if (confirm.ToLower() == "y")
                            {
                                user.DeleteUnit();
                                Console.WriteLine("Your unit has been deleted.");
                            }
                            else
                            {
                                Console.WriteLine("Deletion canceled.");
                            }
                        }

                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey(true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}\nPress any key to continue...");
                    Console.ReadKey(true);
                }
            }
        }

        public static void AdminScreen()
        {
            var user = CurrentUser;
            if (user == null) return;

            while (true)
            {
                int choice = PromptOptions("Admin Panel", new[] { "Set price per kWh", "Set overuse threshold (kWh)", "Set overuse price per kWh", "Reset settings to defaults", "View current settings", "View monthly report", "Logout" });

                try
                {
                    switch (choice)
                    {
                        case 0:
                            {
                                decimal val = PromptForMoney("New price per kWh");
                                user.SetSetting(UserManager.AdminSettings.PricePerKWh, val);
                                Console.WriteLine($"Price per kWh set to ${val}");
                                break;
                            }
                        case 1:
                            {
                                float th = PromptForFloat("New overuse threshold (kWh)");
                                user.SetSetting(UserManager.AdminSettings.OveruseThreshold, th);
                                Console.WriteLine($"Overuse threshold set to {th} kWh");
                                break;
                            }
                        case 2:
                            {
                                decimal val = PromptForMoney("New overuse price per kWh");
                                user.SetSetting(UserManager.AdminSettings.PenaltyPricePerKWh, val);
                                Console.WriteLine($"Overuse price per kWh set to ${val}");
                                break;
                            }
                        case 3:
                            {
                                user.ResetSettings();
                                Console.WriteLine("Settings reset to defaults.");
                                break;
                            }
                        case 4:
                            {
                                Console.Clear();
                                Render.WriteHeader();
                                Console.WriteLine($"PricePerKWh: ${DataStore.Settings.PricePerKWh}");
                                Console.WriteLine($"OveruseThreshold: {DataStore.Settings.OveruseThresholdKWh} kWh");
                                Console.WriteLine($"OverusePricePerKWh: ${DataStore.Settings.OverusePricePerKWh}");
                                Console.WriteLine($"GlobalMoney: ${DataStore.Settings.GlobalMoney}");
                                Console.WriteLine($"CurrentDate: {DataStore.Settings.CurrentDate[0]}/{DataStore.Settings.CurrentDate[1]}");
                                break;
                            }
                        case 5:
                            {
                                // view monthly report
                                var reportsRaw = MonthTicker.MonthlyReport.GetAllReportNames(false);
                                if (reportsRaw == null)
                                {
                                    Console.WriteLine("No monthly reports found.");
                                    break;
                                }

                                // build display names from raw filenames
                                var displayNames = new System.Collections.Generic.List<string>();
                                foreach (var raw in reportsRaw)
                                {
                                    // raw is like "Monthly Reports\\2025-12-EnergyUsage"
                                    var parts = raw.Split('\\');
                                    var fname = parts[parts.Length - 1]; // e.g. "2025-12-EnergyUsage"
                                    var p = fname.Split('-');
                                    int y = int.Parse(p[0]);
                                    int m = int.Parse(p[1]);
                                    string monthName = new DateTime(y, m, 1).ToString("MMMM");
                                    displayNames.Add($"Energy usage report for {monthName}, {y}");
                                }

                                int sel = PromptOptions("Select report to view", displayNames.ToArray());

                                // parse selected raw filename to get month & year
                                var selectedRaw = reportsRaw[sel];
                                var selectedFname = selectedRaw.Split('\\')[selectedRaw.Split('\\').Length - 1];
                                var partsSel = selectedFname.Split('-');
                                int year = int.Parse(partsSel[0]);
                                int month = int.Parse(partsSel[1]);

                                // get report instance
                                var report = MonthTicker.MonthlyReport.GetReport(month, year);

                                // prompt for unit name to view report entry for
                                Console.Clear();
                                Render.WriteHeader();
                                string unitName = Prompt("Enter exact unit name to view report for (leave blank to cancel)", true);
                                if (unitName.Trim() == "")
                                {
                                    Console.WriteLine("Cancelled.");
                                    break;
                                }

                                var unit = UnitManager.GetUnitByName(unitName);
                                if (unit == null)
                                {
                                    Console.WriteLine($"Unit '{unitName}' not found.");
                                    break;
                                }

                                // display report values for this unit
                                decimal bill = report.GetBillForUnit(unit);
                                float kwh = report.GetEnergyUsageForUnit(unit);

                                Console.WriteLine($"Report for {unit.Name} ({unit.Type}) - {new DateTime(year, month, 1).ToString("MMMM yyyy")}\n");
                                Console.WriteLine($"Energy usage: {Math.Round(kwh, 2)} kWh");
                                Console.WriteLine($"Bill total: ${Math.Round(bill, 2)}");

                                break;
                            }
                        case 6:
                            {
                                UserManager.TryLogout(user.SessionID);
                                MySessionID = null;
                                Console.WriteLine("Logged out. Press any key to continue...");
                                Console.ReadKey(true);
                                return;
                            }
                    }

                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}\nPress any key to continue...");
                    Console.ReadKey(true);
                }
            }
        }
    }
}