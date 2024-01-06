using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;

// Personal Dependencies
using Dapper;
using System.Configuration;
using System.Data.SQLite;
using System.Runtime;
using System.Threading;
using WindowsFirewallHelper;

//
// Background Service for IdealApp, provides administrator functionality and reinforces processes
//

namespace ServiceTest
{

  // Service Class
  public partial class ServList : ServiceBase
  {
    // Internal Service Settings
    private const int maxViolationCount = 7; // Maximum violation Amount
    private int violationCount = 0; // Violation amount with regards to process
    private int eventId = 0; // Event Log
    private EventLog globalEventLog;
    private bool canShutdown = true; // Prevents multiple shutdowns and bricking the computer

    // User Settings
    private bool serviceProtectionEnabled = true;
    private bool globalProtectionEnabled = true;
    private bool directoryProtectionEnabled = true;
    private bool methodInProgress = false; // Tracks whether task is complete, and skips current iteration if it isn't

    //
    // Default Directories
    //

    // Final Directories
    //const string blockConfigDirectory = "C:\\Program Files\\Protected_Folder\\Provisional_IdealApp\\PersonalSettings.txt";
    //const string intermediateDirectory = "C:\\Program Files\\Protected_Folder\\Provisional_IdealApp\\Intermediate.exe";
    //const string intermediateBackwardDirectory = "C:\\Program Files\\Protected_Folder\\Provisional_IdealApp\\Intermediate Backward.exe";
    //const string secondaryDirectory = "C:\\Program Files\\Protected_Folder\\Provisional_IdealApp\\Secondary.exe";
    //const string primaryDirectory = "C:\\Program Files\\Protected_Folder\\Provisional_IdealApp\\Primary.exe";

    // Temporary Directories
    const string rootDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\";
    const string blockConfigDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\Plans\\";
    const string intermediateDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\SpawnProxy\\bin\\Debug\\net7.0\\Intermediate.exe";
    const string intermediateBackwardDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\SpawnMain\\bin\\Debug\\net7.0\\Intermediate Backward.exe";
    const string secondaryDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealAppProxy\\bin\\Debug\\net7.0\\Secondary.exe";
    const string primaryDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealApp\\bin\\Debug\\net7.0\\Primary.exe";


    // Constructor
    public ServList()
    {
      InitializeComponent();
      // Create Event Log
      globalEventLog = new EventLog();
      if (!EventLog.SourceExists("MySource"))
      {
        EventLog.CreateEventSource(
            "MySource", "IdealAppServiceLog");
      }
      globalEventLog.Source = "MySource";
      globalEventLog.Log = "IdealAppServiceLog";
    }

    //
    // Determining currently logged in user
    //

    // Definitions
    [DllImport("Wtsapi32.dll")]
    private static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);
    [DllImport("Wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pointer);
    //Default Values
    private enum WtsInfoClass
    {
      WTSUserName = 5,
      WTSDomainName = 7,
    }

    // Final Method
    private static string GetUsername(int sessionId, bool prependDomain = false)// No domain by default
    {
      IntPtr buffer;
      int stringLength;
      string username = "SYSTEM";//Default (If nobody is logged on)
      if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSUserName, out buffer, out stringLength) && stringLength > 1)
      {
        username = Marshal.PtrToStringAnsi(buffer);
        WTSFreeMemory(buffer);
        if (prependDomain)
        {
          if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSDomainName, out buffer, out stringLength) && stringLength > 1)
          {
            username = Marshal.PtrToStringAnsi(buffer) + @"\" + username;
            WTSFreeMemory(buffer);
          }
        }
      }
      return username;
    }

    //
    // Database Actions
    //

    // Database Connections

    // Updates firewall according to plans
    public void UpdateFirewallRules()
    {
      List<PlanInformation> allPlan = DbAccess.parsePlans();
      foreach (PlanInformation cPlan in allPlan)
      {
        // Firewall Blocking and Active
        if ((cPlan.enforcementMethod == 1)&&(cPlan.currentlyActive == 1))
        {
          string[] blockedPaths = cPlan.blockedPaths.Split('|');
          foreach (string cPath in blockedPaths)
          {
            var rule = FirewallManager.Instance.CreateApplicationRule(
              FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
              name: @"IdealBlock: " + cPath,
              FirewallAction.Block,
              cPath
            );
            rule.Direction = FirewallDirection.Outbound;

            FirewallManager.Instance.Rules.Add(rule);
          }
        }
      }
    }

    // Updates registry settings according to user settings
    public void UpdateRules()
    {
      List<RootInformation> applicationSettings = DbAccess.parseAppData();

      // Go through each settings
      foreach (RootInformation rootInformation in applicationSettings)
      {
        if (globalProtectionEnabled == false)//Immediately Stop
        {
          break;
        }
        if (rootInformation.canModify == 0)//Uneditable Options
        {
          continue;
        }
        if (rootInformation.attributeValue == "0")//Disabled Option
        {
          switch (rootInformation.attributeName)
          {
            case "Disable DateTime"://Change Registry Value
              RegistryKey localMachineKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);//Circumvent Virtualization
              RegistryKey key = localMachineKey.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\default\Settings\AllowDateTime", true);
              break;
            case "Service Protection"://Only task enforcement part of service is off
              serviceProtectionEnabled = false;
              break;
            case "Global Protection Enabled"://Everything Off
              globalProtectionEnabled = false;
              break;
            case "Directory Protection":
              directoryProtectionEnabled = false;
              break;
            default:
              break;
          }
        }
        else//Enabled Option
        {
          switch (rootInformation.attributeName)
          {
            case "Disable DateTime"://Change Registry Value
              RegistryKey localMachineKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);//Circumvent Virtualization
              RegistryKey key = localMachineKey.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\default\Settings\AllowDateTime", true);
              key.SetValue("value", 0, RegistryValueKind.DWord);
              break;
            case "Disable Task Manager"://Kill Task Manager
              try
              {
                Process[] taskmgrProcess = Process.GetProcessesByName("taskmgr");
                foreach (Process cProcess in taskmgrProcess)
                {
                  cProcess.Kill();
                }
              }
              catch (Exception ex)
              {

              }
              break;
            case "Service Protection"://Only task enforcement part of service is off
              serviceProtectionEnabled = true;
              break;
            case "Global Protection Enabled"://Everything Off
              globalProtectionEnabled = true;
              break;
            case "Directory Protection":
              directoryProtectionEnabled = true;
              break;
            default:
              break;
          }
        }
      }
    }

    // Checks if any of the primary critical process exists and logs violations
    public bool AppProcessExists()
    {
      // Find Processes
      try
      {
        // Find all processes by name (there are many technical reasons for this, as a service cannot otherwise access)
        Process[] primaryProcess = Process.GetProcessesByName("Primary");
        //Process[] secondaryProcess = Process.GetProcessesByName("Secondary");
        //Process[] intermediate1 = Process.GetProcessesByName("Intermediate");
        //Process[] intermediate2 = Process.GetProcessesByName("Intermediate Backward");
        if ((primaryProcess.Length == 0))// && (secondaryProcess.Length == 0) && (intermediate1.Length == 0) && (intermediate2.Length == 0)
        {
          return false;
        }
        else
        {
          bool processExists = false;
          // Check for primary process (all others can orginate from primary) path to ensure it is the right one
          foreach (Process p in primaryProcess)
          {
            if (processExists) { break; }
            string auxFilePath = p.MainModule.FileName;
            if (auxFilePath == primaryDirectory)
            {
              processExists = true;
            }
          }
          // Start the process if no process is found
          if (!processExists)
          {
            return false;
          }
          else
          {
            return true;
          }
        }
      }
      catch (Exception e)
      {
        globalEventLog.WriteEntry(e.ToString());
      }
      return true;
    }

    // Runs every 5 seconds: Checks state of application and logs issues
    public void OnTimer(object sender, ElapsedEventArgs args)
    {
      if (!methodInProgress){
        methodInProgress = true;
        UpdateRules();
        if ((globalProtectionEnabled) && (serviceProtectionEnabled))// Service Specific Functionality 
        {
          List<string> usernames = new List<string> { "SYSTEM" };
          //var explorer = Process.GetProcessesByName("explorer").FirstOrDefault();
          var explorer = Process.GetProcessesByName("explorer");
          foreach (var cExplorer in explorer)
          {
            if (cExplorer != null)
            {
              var username = GetUsername(cExplorer.SessionId);
              usernames.Add(username);
            }
          }

          if ((canShutdown) && (usernames.Contains("MD3728")))
          {
            if (!AppProcessExists()) // Block process does not exist
            {
              violationCount++;
              eventLog1.WriteEntry($"No processes detected: Violation #{violationCount}\n, max violation count is set at {maxViolationCount}", EventLogEntryType.Information, eventId++);
              if (violationCount >= maxViolationCount) // AKA The primary process did not run for ~30 seconds
              {
                restartComputer();
              }
            }
          }
        }
        methodInProgress = false;
      }
    }

    // Method to instantly (forcefully) restart the computer
    public void restartComputer()
    {
      if (canShutdown)
      {
        // Shutdown the Computer
        canShutdown = false;
        Process pProcess = new Process();
        pProcess.StartInfo.FileName = "CMD.exe";
        pProcess.StartInfo.Arguments = "/c shutdown /r /f /t 0";
        pProcess.StartInfo.UseShellExecute = false;
        pProcess.StartInfo.RedirectStandardOutput = true;
        pProcess.Start();
        pProcess.WaitForExit();
      }
    }

    //
    // On service start and file monitoring
    //

    protected override void OnStart(string[] args)
    {
      UpdateFirewallRules();
      // Set up a timer that triggers every 5 seconds.
      System.Timers.Timer timer = new System.Timers.Timer();
      timer.Interval = 1000; // 5 seconds
      timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
      timer.Start();
      // Create a new FileSystemWatcher and let it run in the background to track important directories settings
      //using (FileSystemWatcher watcher = new FileSystemWatcher())
      //{
      //  watcher.Path = rootDirectory;  // Specify the directory to monitor

      //  // Watch for all changes in LastAccess and LastWrite times, and the renaming of files or directories. 
      //  watcher.NotifyFilter = NotifyFilters.Attributes
      //                  | NotifyFilters.CreationTime
      //                  | NotifyFilters.DirectoryName
      //                  | NotifyFilters.FileName
      //                  | NotifyFilters.LastAccess
      //                  | NotifyFilters.LastWrite
      //                  | NotifyFilters.Security
      //                  | NotifyFilters.Size;
      //  watcher.IncludeSubdirectories = true;

      //  // Add event handlers.
      //  watcher.Changed += OnChanged;
      //  watcher.Created += OnChanged;
      //  watcher.Deleted += OnChanged;
      //  watcher.Renamed += OnRenamed;
      //  watcher.Error += OnError;

      //  // Begin watching.
      //  watcher.EnableRaisingEvents = true;

      //  // Let watcher run until service ends
      //  while (true)
      //  {
      //    Thread.Sleep(10000);
      //  }
      //}
    }

    // On service stop
    protected override void OnStop()
    {

    }

    // Event Handlers for file monitoring
    private void OnChanged(object source, FileSystemEventArgs e)
    {
      if (directoryProtectionEnabled)
      {
        globalEventLog.WriteEntry($"File: {e.FullPath} {e.ChangeType}");
        restartComputer();
      }

    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
      if (directoryProtectionEnabled)
      {
        globalEventLog.WriteEntry($"Rename: {e.OldName} => {e.Name}");
        restartComputer();
      }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {

      globalEventLog.WriteEntry($"Error : {e.GetException().Message}");
    }

    // Event handler for the log
    private void eventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
    {

    }
  }

  //
  // Database Shell Classes
  //

  // Global App Information
  public class RootInformation
  {
    public int idAttribute { get; set; }
    public string attributeName { get; set; }
    public string attributeValue { get; set; }
    public string defaultValue { get; set; }
    public int canModify { get; set; }

    // Special String and box (Timing)
    public bool returnAttributeValue
    {
      get
      {
        bool tActiveTime = this.attributeValue == "0" ? false : true;
        return tActiveTime;
      }
      set
      {
        this.attributeValue = (value == false) ? "0" : "1";
      }
    }

  }

  // Statistics Structure (Not Used)
  public class StatsInformation
  {
    public int idApp { get; set; }
    public string processName { get; set; }
    public string processPath { get; set; }
    public int processCDay { get; set; }
    public int processCHour { get; set; }
    public string processPastTimes { get; set; }

  }

  // Plan Information Structure
  public class PlanInformation
  {
    public PlanInformation()
    {
      this.idPlan = 0;
      this.blockMethod = 0;
      this.enforcementMethod = 0;
      this.planName = "";
      this.planDescription = "";
      this.blockedPaths = "";
      this.blockedNames = "";
      this.allowedPaths = "";
      this.allowedNames = "";
      this.protectionActiveType = 0;
      this.protectionActivePwd = "";
      this.protectionActiveChar = 0;
      this.protectionActiveAddAllow = 0;
      this.protectionInactiveType = 0;
      this.protectionInactivePwd = "";
      this.protectionInactiveChar = 0;
      this.protectionInactiveAddAllow = 0;
      this.timingMethod = 0;
      this.activeDays = "1";
      this.activeTime = "";
      this.pomoAmount = 0;
      this.pomoDuration = 0;
      this.pomoSetAmount = 0;
      this.pomoSmallBreak = 0;
      this.pomoLargeBreak = 0;
      this.pomoScheduledStart = "";
      this.currentlyActive = 0;
    }

    // Special String and box (Timing)
    public string returnActiveTime
    {
      get
      {
        string tActiveTime = this.activeTime.Replace("|", Environment.NewLine);
        return tActiveTime;
      }
      set
      {
        activeTime = value.Replace(Environment.NewLine, "|");
      }
    }

    // Special string and box (Apps)
    public string returnNameBlacklist
    {
      get
      {
        string tActiveTime = this.blockedNames.Replace("|", Environment.NewLine);
        return tActiveTime;
      }
      set
      {
        blockedNames = value.Replace(Environment.NewLine, "|");
      }
    }

    // Special string and test
    public string returnNameWhitelist
    {
      get
      {
        string tActiveTime = this.allowedNames.Replace("|", Environment.NewLine);
        return tActiveTime;
      }
      set
      {
        allowedNames = value.Replace(Environment.NewLine, "|");
      }
    }

    // Special string and test
    public string returnPathBlacklist
    {
      get
      {
        string tActiveTime = this.blockedPaths.Replace("|", Environment.NewLine);
        return tActiveTime;
      }
      set
      {
        blockedPaths = value.Replace(Environment.NewLine, "|");
      }
    }

    // Special string and test
    public string returnPathWhitelist
    {
      get
      {
        string tActiveTime = this.allowedPaths.Replace("|", Environment.NewLine);
        return tActiveTime;
      }
      set
      {
        allowedPaths = value.Replace(Environment.NewLine, "|");
      }
    }

    // Special string and test (Main)
    public bool returnPausingAllowed
    {
      get
      {
        bool tActiveTime = protectionActiveAddAllow == 0 ? false : true;
        return tActiveTime;
      }
      set
      {
        protectionActiveAddAllow = value ? 1 : 0;
      }
    }

    public bool returnStricterAddAllow
    {
      get
      {
        bool tActiveTime = protectionInactiveAddAllow == 0 ? false : true;
        return tActiveTime;
      }
      set
      {
        protectionInactiveAddAllow = value ? 1 : 0;
      }
    }

    // Fields
    public int idPlan { get; set; }
    public int blockMethod { get; set; } // Blacklist or Whitelist
    public int enforcementMethod {  get; set; } // Firewall/Kill
    public string planName { get; set; }
    public string planDescription { get; set; }
    public string blockedPaths { get; set; }
    public string blockedNames { get; set; }
    public string allowedPaths { get; set; }
    public string allowedNames { get; set; }
    public int protectionActiveType { get; set; }// Use this for both types of protection
    public string protectionActivePwd { get; set; }
    public int protectionActiveChar { get; set; }
    public int protectionActiveAddAllow { get; set; }// Pausing Attribute
    public int protectionInactiveType { get; set; }
    public string protectionInactivePwd { get; set; }
    public int protectionInactiveChar { get; set; }
    public int protectionInactiveAddAllow { get; set; } // Disabled Attribute
    public int timingMethod { get; set; }
    public string activeDays { get; set; }
    public string activeTime { get; set; }
    public int pomoAmount { get; set; }
    public int pomoDuration { get; set; }
    public int pomoSetAmount { get; set; }
    public int pomoSmallBreak { get; set; }
    public int pomoLargeBreak { get; set; }
    public string pomoScheduledStart { get; set; }
    public int currentlyActive { get; set; }

  }

  // Database Access Class
  public class DbAccess
  {
    public DbAccess() { }

    // Database Read Methods (Read-Only)


    // Loads connection strings from config file
    public static string loadConnString(string id = "Default")
    {
      return ConfigurationManager.ConnectionStrings[id].ConnectionString;
    }

    // Read Plans
    public static List<PlanInformation> parsePlans()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Plan")))
      {
        var output = connection.Query<PlanInformation>("SELECT * FROM Plans", new DynamicParameters()).ToList();
        return output;
      }
    }

    // Read App Data
    public static List<RootInformation> parseAppData()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Root")))
      {
        var output = connection.Query<RootInformation>("SELECT * FROM AppData ORDER BY idAttribute", new DynamicParameters()).ToList();
        return output;
      }
    }

    // Read Statistics (Not Used)
    public static List<StatsInformation> parseStatistics()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Stat")))
      {
        var output = connection.Query<StatsInformation>("SELECT * FROM UserStatistics", new DynamicParameters()).ToList();
        return output;
      }
    }

  }
}
