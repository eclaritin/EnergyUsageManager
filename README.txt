[ Overview ]
EnergyUsageManager is a system that keeps track of the monthly energy usage of homes & buildings (in kWh), and 
disincentivizes overconsumption of energy which could put a strain on power lines. Users log in and register their home 
if they're a homeowner, or their apartment building's units if they're a building manager. These units are all billed
monthly based on power consumption. If the power usage goes over a certain threshold, they are billed at a higher rate.
These variables: energy price per kWh, overusage threshold, & overuse energy price per kWh are all managed by the 
administrator, who's account is registered automatically in Frontend.cs. A .csv report is generated each month detailing 
the energy usage, whether the unit went over their threshold for energy usage, and the resulting bill.

This project is divided into two parts, the backend API & the frontend console application. The API is a class library 
which can be referenced by projects that wish to implement its functionality. This is why you'll see a
"using EnergyUsageManager;" statement in the Frontend.cs file. The frontend project contains a Render class and the 
Program class. The Monthticker is initialized in the entrypoint, the date updates dynamically, and helper methods handle
input validation gracefully.

[ How to Use ]
You will first be met with the main menu screen, you can enter the number corresponding to whichever option you'd like 
to choose, and press enter when you're done typing. You can either log in, create an account, or exit on this screen.
If it's the first time you're using this program, you should select Create account. This takes you to the account 
creation screen, where you enter your username, password, & confirmation password. Then, you can select your account type.
The homeowner, manages their own unit and directly pays to the global account. The building manager collects payments from
renters which they then pay to the global account, and the renter pays their building manager. Once your account has been
created, you can register a new unit (unless you're a renter), view your unit's details & bill once it's created, add
money to your account, & pay your bill. If you're a renter you can assign a unit to yourself with the name of the unit
given to you by your building manager. If you're a building manager, you can add a unit to your building. If you log in as
admin, you are instead shown the admin panel. This presents you with the options to: set the monthly energy price, the
overuse threshold, & the overuse energy price, as well as reset the settings to their defaults, view current setting
configuration, & view the monthly report for any unit.
