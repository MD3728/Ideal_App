// Intermediate file for spawning IdealApp Main Process
using System.Diagnostics;
using System.Configuration;

// Locations

string path = ConfigurationManager.AppSettings["IA_Directory"];

Process.Start(path);
