// Intermediate file for spawning IdealApp Main Process
using System.Diagnostics;

//Locations:
//Intermediate            "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\SpawnProxy\\bin\\Debug\\net7.0\\Intermediate.exe"
//Intermediate Backward   "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\SpawnMain\\bin\\Debug\\net7.0\\Intermediate Backward.exe"
//Secondary               "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealAppProxy\\bin\\Debug\\net7.0\\Secondary.exe"
//Primary                 "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealApp\\bin\\Debug\\net7.0\\Primary.exe"

string path = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealApp\\bin\\Debug\\net7.0\\Primary.exe";
//Console.WriteLine(path);

Process.Start(path);

