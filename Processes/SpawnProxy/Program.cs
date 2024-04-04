// Intermediate file for spawning IdealApp (Proxy) Keep Alive Process
using System.Diagnostics;
using System.Configuration;

// Locations

string path = ConfigurationManager.AppSettings["IA_Proxy_Directory"];

Process.Start(path);