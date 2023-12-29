using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Data.SQLite;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using System.Numerics;
using Dapper;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
//using Microsoft.AspNet.WebApi.Client;
using System.Reflection;
using System.Timers;

// Database Structure
//Install-Package WindowsFirewallHelper    Install-Package Microsoft.AspNet.WebApi.Client
/* stats.db (Statistics)
// Description of Columns (UserStatistics Table)

// idApp: Unique ID for each application
// processName: Name of the process (eg. firefox.exe)
// processPath: Path of the process (eg. C:\Program Files\Mozilla Firefox\firefox.exe)
// processCDay: Current time spent on the process (seconds)
// processPastTimes: Past times spent on the process (eg. 900|1500|232|etc.) (seconds)
*/

/* plans.db (User Configuration)

// Be sure to start front with lower case

// General Information
PlanName
BlockMethod   (0 for blacklist, 1 for whitelist)
PlanDescription 
Kill   
Firewall

// Content 

BlockedPaths  eg. (C:\Program Files\AMD\|C:\Program Files\Google\|C:\Program Files\Internet Explorer\) split on |
BlockedNames 
AllowedPaths  eg. (C:\Program Files\|C:\Program Files(x86)\| C:\Windows\) split on |
AllowedNames

// Protection

ProtectionActiveType (Types are as following: 0 (None), 1 (Password), 2 (Random Characters), 3 (Completely Blocked))
ProtectionActivePwd (Hash of password)
ProtectionActiveChar (Number of Random Input)
ProtectionActiveAddAllow (TBD)
ProtectionInactiveType (Same as protectionactivetype)
ProtectionInactivePwd (Hash of password)    
ProtectionInactiveChar (Number of Random Input) 
ProtectionInactiveAddAllow (TBD)

// Timing

TimingMethod  (0 or 1, 0 for standard time, 1 for pomodoro)
ActiveDays   (Days where plan is active) delimited by |    eg. 1|2|3|4|5|6|7 
ActiveTime   (Active Times) delimited by |    eg. 0000-2400
PomoAmount   (Pomodoro Cycles) (any integer)
PomoDuration (in minutes)
PomoSetAmount  (number of sets per round)
PomoSmallBreak   (time between sets)
PomoLargeBreak   (time between rounds)
PomoScheduledStart   (starting times) follows as    0000|1600|etc.
*/

/* root.db (General Application Configuration)
// Description of Columns (AppData Table)

// currentVersion (eg 1.0.0)
// lastUpdate  (eg. 8-24-2022)
// protectionEnabled (bool, FBD)
// directoryProtection (bool, FBD)
// dateTimeEnabled = false (bool, FBD)
// showNotification = false (bool, FBD)
// showTimer = false (bool, FBD)
// uninstallProtection (bool, FBD)
// disableTaskmgr = true (bool, FBD)
// disableTaskkill (TBD) 
// masterPassword (hash of password)
*/

namespace Primary
{
  // Constructor
  class Program
  {
    static void Main(string[] args)
    {//Program Entry Point
      Console.WriteLine("Program Start");//Debugging
      ProcessParser newobj = new ProcessParser();
      Console.WriteLine("Program End");//Debugging
    }
  }

  // Database Reference/Object Classes
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

  public class StatsInformation
  {
    public int idApp { get; set; }
    public string processName { get; set; }
    public string processPath { get; set; }
    public int processCDay { get; set; }
    public int processCHour { get; set; }
    public string processPastTimes { get; set; }

  }

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
        string tActiveTime = this.activeTime.Replace("|", "\n");
        return tActiveTime;
      }
      set
      {
        activeTime = value.Replace("\n", "|");
      }
    }

    // Special string and box (Apps)
    public string returnNameBlacklist
    {
      get
      {
        string tActiveTime = this.blockedNames.Replace("|", "\n");
        return tActiveTime;
      }
      set
      {
        blockedNames = value.Replace("\n", "|");
      }
    }

    // Special string and test
    public string returnNameWhitelist
    {
      get
      {
        string tActiveTime = this.allowedNames.Replace("|", "\n");
        return tActiveTime;
      }
      set
      {
        allowedNames = value.Replace("\n", "|");
      }
    }

    // Special string and test
    public string returnPathBlacklist
    {
      get
      {
        string tActiveTime = this.blockedPaths.Replace("|", "\n");
        return tActiveTime;
      }
      set
      {
        blockedPaths = value.Replace("\n", "|");
      }
    }

    // Special string and test
    public string returnPathWhitelist
    {
      get
      {
        string tActiveTime = this.allowedNames.Replace("|", "\n");
        return tActiveTime;
      }
      set
      {
        allowedPaths = value.Replace("\n", "|");
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
    public int enforcementMethod { get; set; } // Firewall/Kill
    public string planName { get; set; }
    public string planDescription { get; set; }
    public string blockedPaths { get; set; }
    public string blockedNames { get; set; }
    public string allowedPaths { get; set; }
    public string allowedNames { get; set; }
    public int protectionActiveType { get; set; }// Use this for both types of protection
    public string protectionActivePwd { get; set; }
    public int protectionActiveChar { get; set; }
    public int protectionActiveAddAllow { get; set; }//Substitute for pausing
    public int protectionInactiveType { get; set; }
    public string protectionInactivePwd { get; set; }
    public int protectionInactiveChar { get; set; }
    public int protectionInactiveAddAllow { get; set; } // Only one actually used
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

  // API Template Class
  public class TimeInformation
  {
    public TimeInformation() { }
    public DateTime datetime { get; set;  } // Do not change this name
  }

  // Time API Handler
  public class TimeProcessor
  {
    public static HttpClient client { get; set; }

    // Database Commands
    public static string loadConnString(string id = "Default")
    {
      return ConfigurationManager.ConnectionStrings[id].ConnectionString;
    }

    // Send request to API and return response
    public static async Task<TimeInformation> loadTimeInformation()
    {
      client = new HttpClient();
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      string url = "http://worldtimeapi.org/api/ip";

      using (HttpResponseMessage response = await client.GetAsync(url))
      {
        if (response.IsSuccessStatusCode)
        {
          TimeInformation result = await response.Content.ReadAsAsync<TimeInformation>();
          // Update Database with new time
          using (IDbConnection connection = new SQLiteConnection(loadConnString("Root")))
          {
            connection.Query<RootInformation>($"UPDATE AppData SET attributeValue = '{result.datetime.ToString("yyyy MM dd HH mm ss")}' WHERE idAttribute = 12", new DynamicParameters());
          }
          return result;
        }
        else
        {
          throw new Exception(response.ReasonPhrase);
        }
      }
    }
  }

  //Primary Class
  class ProcessParser
  {
    private Process _realProcess { get; set; } // Holds value of (real, eg. UWP and not ApplicationFrameHost) current process
    private bool isParserProcessing = false; //Determines if the program is in the middle of running and skips the next instance until it is
    private DateTime cTime = new DateTime();

    // Directory Shortcuts
    const string rootDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\IdealApp Processes\";
    const string intermediateDirectory = rootDirectory + "SpawnProxy\\bin\\Debug\\net7.0\\Intermediate.exe";
    const string intermediateBackwardDirectory = rootDirectory + "SpawnMain\\bin\\Debug\\net7.0\\Intermediate Backward.exe";
    const string secondaryDirectory = rootDirectory + "IdealAppProxy\\bin\\Debug\\net7.0\\Secondary.exe";
    const string primaryDirectory = "IdealApp\\bin\\Debug\\net7.0\\Primary.exe";
    const string planInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\plans.db";
    const string statsInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\stats.db";
    const string appInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\root.db";

    public ProcessParser()
    {
      /* Final Version */

      // Create ~3 second cycle
      System.Timers.Timer timer = new System.Timers.Timer();
      timer.Interval = 3000; // 3 seconds
      timer.Elapsed += new ElapsedEventHandler(this.actionCycle);
      timer.Start();

      // Keep program running
      while (true)
      {
        Thread.Sleep(100000);
      }

    }

    // Actions performed at consistent intervals
    public void actionCycle(object sender, ElapsedEventArgs args)
    {
      //var watch = Stopwatch.StartNew();

      // Skips cycle if previous cycle is still executing
      if (!isParserProcessing)
      {
        isParserProcessing = true;
        Task.Run(() => TimeProcessor.loadTimeInformation());//Run the process
        keepProcessAlive();
        killProcesses();
        checkProcesses();
        Console.WriteLine("Finished One Cycle");
        isParserProcessing = false;
      }
      else
      {
        Console.WriteLine("Current Cycle Skipped");
      }

      //  watch.Stop();
      //  var elapsedMs = watch.ElapsedMilliseconds;
      //  Console.WriteLine($"Elapsed {elapsedMs} ms");
    }

    // Database Commands
    public static string loadConnString(string id = "Default")
    {
      return ConfigurationManager.ConnectionStrings[id].ConnectionString;
    }

    private List<PlanInformation> parsePlans()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("PlanRead")))
      {
        var output = connection.Query<PlanInformation>("SELECT * FROM Plans", new DynamicParameters()).ToList();
        return output;
      }
    }

    private List<RootInformation> parseAppData()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("RootRead")))
      {
        var output = connection.Query<RootInformation>("SELECT * FROM AppData", new DynamicParameters()).ToList();
        return output;
      }
    }

    private List<StatsInformation> parseStatistics()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("StatRead")))
      {
        var output = connection.Query<StatsInformation>("SELECT * FROM UserStatistics", new DynamicParameters()).ToList();
        return output;
      }
    }

    // Primary Method (Called perpetually)
    private void killProcesses()
    {
      // Deal with user settings
      List<RootInformation> appInformation = parseAppData();
      foreach (var cSetting in appInformation)
      {
        switch (cSetting.idAttribute)
        {
          case 4://Global Switch
            if (cSetting.attributeValue == "0")// If off, immediately break program
            {
              return;
            }
            break;
          case 12:
            string[] timeValues = cSetting.attributeValue.Split(" ");
            cTime = new DateTime(int.Parse(timeValues[0]), int.Parse(timeValues[1]), int.Parse(timeValues[2]), int.Parse(timeValues[3]), int.Parse(timeValues[4]), int.Parse(timeValues[5]));
            break;
          default: 
            break;
        }
      }

      List<PlanInformation> planInformation = parsePlans();
      //Parse and execute plans
      foreach (PlanInformation currentInfo in planInformation)
      {
        if ((currentInfo.currentlyActive == 0)||(currentInfo.protectionInactiveAddAllow == 1))//Ensure that plan is active and not disabled
        {
          continue;
        }
        //
        // Plan Presets (From Database File)
        //
        //Timing
        string timingMethod = currentInfo.timingMethod.ToString(); // 1 Means Scheduled, 2 Means Pomodoro
        string[] scheduledDays = currentInfo.activeDays.Split("|");
        string[] scheduledTime = currentInfo.activeTime.Split("|"); //"1200-1300", "1400-1500"
        string pomoAmount = currentInfo.pomoAmount.ToString();
        string pomoDuration = currentInfo.pomoDuration.ToString();
        string pomoSetNumber = currentInfo.pomoSetAmount.ToString();
        string pomoSmallBreak = currentInfo.pomoSmallBreak.ToString();
        string pomoLargeBreak = currentInfo.pomoLargeBreak.ToString();
        string pomoScheduledStart = currentInfo.pomoScheduledStart;
        //Blocks all apps with specified names (of EXEs)
        string[] blockAppName = currentInfo.blockedNames.Split("|"); // "iexplore"
        //Allows all apps with specified names (of EXEs)
        string[] allowAppName = currentInfo.allowedNames.Split("|"); //"notepad", "firefox"
        //Blocks all apps with specified paths
        string[] blockAppPath = currentInfo.blockedPaths.Split("|"); //"C:\\Program Files\\Internet Explorer\\", 
        //Allows all apps with specified paths
        string[] allowAppPath = currentInfo.allowedPaths.Split("|"); //{ "C:\\Program Files\\WindowsApps\\Microsoft.Windows.Photos", "C:\\Program Files\\WindowsApps\\Microsoft.MSPaint", "C:\\Program Files\\WindowsApps\\microsoft.windowscommunicationsapps","C:\\Program Files\\WindowsApps\\AD2F1837.BOAudioControl", "C:\\Program Files\\WindowsApps\\Microsoft.WindowsCamera", "C:\\Program Files\\WindowsApps\\Microsoft.WindowsCalculator", "C:\\Program Files\\WindowsApps\\Microsoft.Todos"};

        bool blockAllApps = currentInfo.blockMethod == 0 ? false:true; // If whitelist is on or not

        //
        // Determine Time
        //
        bool withinTime = true;
        if (timingMethod == "0")// Regular Timing
        {
          withinTime = checkScheduledTime(scheduledDays, scheduledTime);
        }
        else if (timingMethod == "1")// Pomodoro
        {
          withinTime = checkPomoTime(scheduledDays, pomoAmount, pomoDuration, pomoSetNumber, pomoSmallBreak, pomoLargeBreak, pomoScheduledStart);
       }

        // If time period is within block period of set, valid times
        if (withinTime)
        {
          //
          //Name blocking
          //

          // Blacklist Mode (Always On)
          foreach (string cAppName in blockAppName)
          {
            if (String.IsNullOrEmpty(cAppName))// If not empty (eg. not a blank line)
            {
              continue;
            }
            try
            {
              Process[] processlist2 = Process.GetProcessesByName(cAppName);
              foreach (Process process in processlist2)
              {
                getActualProcess(process);
                try
                {
                  _realProcess.Kill();
                  Console.WriteLine($"Blacklist Name Now blocking: {_realProcess.MainModule.FileName} ");
                }
                catch (Exception ex)
                {

                }
              }
            }
            catch (Exception ex)
            {

            }
          }
          
          // Path blocking 
          //
          // Determine whether to kill apps based on paths
          // Both whitelisting and blacklisting are accounted for
          // Exceptions occur with whitelisted (exceptions) app names

          Process[] processlist = Process.GetProcesses();
          foreach (Process process in processlist)
          {// Implement Path and/or Process Name
            try
            {
              // Get Real Process
              string appPath = "";
              try
              {
                getActualProcess(process);
                appPath = process.MainModule.FileName.ToLower();
              }
              catch(System.ComponentModel.Win32Exception e)
              {
                //Console.WriteLine(e);
                continue;
              }

              // Path Whitelisting (Only when enabled)
              if (blockAllApps) 
              {
                bool willBlock = true;
                foreach (string cAppPath in allowAppPath)
                {
                  if (cAppPath.ToLower() == appPath.Substring(0, cAppPath.Length))
                  {//Kill application if conditions are not true
                    willBlock = false;
                    break;
                  }
                }
                if (willBlock)
                {
                  Console.WriteLine($"Whitelist Path Now blocking: {appPath} ");
                  _realProcess.Kill();
                }
              }

              // Path Blacklisting (Always Enabled)
              foreach (string cAppPath in blockAppPath)
              {
                if (cAppPath.ToLower() == appPath.Substring(0, cAppPath.Length))
                {//Kill application if conditions are true
                  bool canKill = true;

                  //
                  // Name Blocking Exceptions
                  //
                  // Limited Whitelist (Exception) Mode
                  if (blockAllApps)
                  {
                    canKill = !allowAppName.Contains(process.ProcessName); // Is in exceptions
                  }
                  if (canKill)
                  {
                    Console.WriteLine($"Blacklist Path Now blocking: {cAppPath}    {appPath.Substring(0, cAppPath.Length)} ");
                    _realProcess.Kill();
                  }
                  break;
                }
              }
            }
            catch (Exception ex)
            {

            }
          }
        }
      }

      //Debugging
      //Console.WriteLine("Process: {0} ID: {1} Window title: {2}\nPath: {3}\nName: {4}", _realProcess.ProcessName, _realProcess.Id, _realProcess.MainWindowTitle, _realProcess.MainModule.FileName, _realProcess.MainModule.FileVersionInfo.FileDescription);
      //Console.WriteLine("Process Listed Above Killed");
    }

    // For Statistics: Checks for processes and logs them for up to a week
    private void checkProcesses()
    {
      // Create tables if it doesn't exist
      int sampleTimeDuration = 3; // Sampling interval of time for each cycle

      // Database Connection
      List<StatsInformation> appStatistics = parseStatistics();
      string[] lastCheckTime = parseAppData()[9].attributeValue.Split(" ");//10th in database

      // Create new 
      if (appStatistics.Count() == 0)
      {
        StatsInformation tNew = new StatsInformation();
        tNew.processPath = @"C:\";
        tNew.processName = $"placeholder";
        tNew.processPastTimes = $"0|0|0|0|0|0|0"; 
        appStatistics.Add(tNew);
      }

      // Time Checker
      var cDateTime = cTime;
      DateTime originalTime = cDateTime;

      //Format of the time in database is: [2023 7 31 23 59 59]
      foreach (string cString in lastCheckTime)
      {
        try
        {
          originalTime = new DateTime(cString[0], cString[1], cString[2]);
          originalTime.AddHours(cString[4]); originalTime.AddMinutes(cString[5]); originalTime.AddSeconds(cString[6]);
        }
        catch (Exception ex) // There is no time listed in the database (theoretically not possible)
        {
          originalTime = cDateTime;
          break;
        }
      }
      Console.WriteLine(originalTime);

      // Log all processes
      // Update logs

      int dayDifference = (cDateTime - originalTime).Days;  // Change in time

      using (IDbConnection connection = new SQLiteConnection(loadConnString("Stat")))
      {
        //var output = connection.Query<StatsInformation>("SELECT * FROM UserStatistics", new DynamicParameters()).ToList();

        // Update (Increase) Time (Determine if new day has started and update previous times)
        // Check individual plans
        Process[] processlist = Process.GetProcesses();
        foreach (Process cProcess in processlist)
        {
          if (!String.IsNullOrEmpty(cProcess.MainWindowTitle))// Ensure application is valid (mostly)
          {
            string cProcessName = "";
            string cProcessPath = "";
            try
            {
              cProcessName = cProcess.ProcessName;
              cProcessPath = cProcess.MainModule.FileName;
            }
            catch (System.ComponentModel.Win32Exception e)
            {
              continue;
            }

            bool found = false;
            // Check each existing process for existence and update if necessary
            foreach (StatsInformation cEntry in appStatistics)
            {
              string NPName = cEntry.processName.Trim();
              string NPPath = cEntry.processPath.Trim();
              List<string> previousRecTimes = new List<string>(cEntry.processPastTimes.Trim().Split("|"));
              if ((NPName == cProcessName) || (NPPath == cProcessPath)) // Encountering a process not yet documented
              {
                string currentRecordedDuration = cEntry.processCDay.ToString();
                previousRecTimes[0] = currentRecordedDuration;
                // Remove old times (only stores 1 week)
                for (int b = 1; ((b <= dayDifference) && (b < 7)); b++)
                {
                  previousRecTimes.Remove(previousRecTimes[7 - b]);// Previous Time (Now stored as list)
                }
                //Extra Cases
                if (dayDifference > 0) // Day has switched
                {
                  previousRecTimes.Insert(0, currentRecordedDuration);
                }
                else
                {
                  currentRecordedDuration = (int.Parse(currentRecordedDuration) + sampleTimeDuration).ToString();
                }
                for (int c = previousRecTimes.Count(); c < 7; c++)// Add "0" time for days not listed (or before first run)
                {
                  previousRecTimes.Add("0");
                }
                // Modify the Data
                string test = string.Join("|", previousRecTimes);
                //Console.WriteLine(test);
                var result2 = connection.Query<StatsInformation>($"UPDATE UserStatistics SET processCDay = {currentRecordedDuration}, processPastTimes = '{test}' WHERE idApp = {cEntry.idApp}", new DynamicParameters()).ToList();
                found = true;
                break;
              }
            }

            // Not Existing entries
            if (!found)
            {
              var result = connection.Query<StatsInformation>($"INSERT INTO UserStatistics (processName, processPath, processCDay, processPastTimes) VALUES ('{cProcessName}', '{cProcessPath}' , {sampleTimeDuration}, '0|0|0|0|0|0|0')", new DynamicParameters()).ToList();
            }
           
          }
        }
        // Update last check time
        string newLastCheckTime = cDateTime.ToString("yyyy MM dd HH mm ss");
        // Read database values (Root)
        using (IDbConnection connection2 = new SQLiteConnection(loadConnString("Root")))
        {
          var output = connection2.Query<StatsInformation>($"UPDATE AppData SET attributeValue = '{newLastCheckTime}' WHERE idAttribute = {10}", new DynamicParameters()).ToList();
        }
        
      }

    }

    // Check if within current time limit (Scheduled Times)
    private bool checkScheduledTime(string[] scheduledDays, string[] scheduledTime)
    {
      // Get time and parse
      bool withinTime = false;
      bool withinDay = false;

      // Look at day
      int currentDay = (int)DateTime.Today.DayOfWeek;
      if (currentDay == 0) { currentDay = 7; }

      //Console.WriteLine(currentDay);

      foreach (string currentSchedDay in scheduledDays)
      {
        if (currentDay == Int32.Parse(currentSchedDay))
        {
          withinDay = true;
          break;
        }
      }

      // If no days apply
      if (!withinDay)
      {
        return false;
      }

      string currentTime = cTime.ToString("HHmmss");

      foreach (string setDuration in scheduledTime)
      {
        string[] totalInterval = setDuration.Split('-');
        string startingInterval = totalInterval[0];
        string endingInterval = totalInterval[1];
        if ((Int32.Parse(currentTime.Substring(0, 4)) > Int32.Parse(startingInterval)) && (Int32.Parse(currentTime.Substring(0, 4)) < Int32.Parse(endingInterval)))
        {
          withinTime = true;
          break;
        }
      }
      return withinTime;
    }

    // Check if within current time limit
    private bool checkPomoTime(string[] scheduledDays, string pomoAmount, string pomoDuration, string pomoSetNumber, string pomoSmallBreak, string pomoLargeBreak, string pomoScheduledStart)
    {
      // Get time and parse
      bool withinTime = false;
      bool withinDay = false;

      // Look at day
      int currentDay = (int)DateTime.Today.DayOfWeek;
      if (currentDay == 0) { currentDay = 7; }

      //Console.WriteLine(currentDay);

      foreach (string currentSchedDay in scheduledDays)
      {
        if (currentDay == Int32.Parse(currentSchedDay))
        {
          withinDay = true;
          break;
        }
      }

      // If no days apply
      if (!withinDay)
      {
        return false;
      }

      string currentTime = cTime.ToString("HHmmss");

      // Calculate acceptable times

      List<string> scheduledTime = new List<string>();

      int timeOffset = 0;
      for (int a = 1; a < Int32.Parse(pomoAmount) + 1; a++)
      {
        string startingTime = parseTime(int.Parse(pomoScheduledStart), timeOffset);
        timeOffset += Int32.Parse(pomoDuration);
        string endingTime = parseTime(int.Parse(pomoScheduledStart), timeOffset);
        scheduledTime.Add($"{startingTime}-{endingTime}");
        if (a % int.Parse(pomoSetNumber) == 0) // Large break
        {
          timeOffset += int.Parse(pomoLargeBreak);
        }
        else
        {
          timeOffset += int.Parse(pomoSmallBreak);
        }
      }

      foreach (string setDuration in scheduledTime)
      {
        string[] totalInterval = setDuration.Split('-');
        string startingInterval = totalInterval[0];
        string endingInterval = totalInterval[1];
        if ((Int32.Parse(currentTime.Substring(0, 4)) > Int32.Parse(startingInterval)) && (Int32.Parse(currentTime.Substring(0, 4)) < Int32.Parse(endingInterval)))
        {
          withinTime = true;
          break;
        }
      }
      return withinTime;
    }

    // Method for adding time to an existing time
    private string parseTime(int originalTime, double timeOffset)
    {
      int remainingTimeInHour = 60 - originalTime % 100;
      if (timeOffset < remainingTimeInHour) // Will not flip an hour
      {
        return (originalTime + timeOffset).ToString();
      }
      else
      {
        int finalTime;
        int initialHours = (int)Math.Floor((double)originalTime / 100);
        int initialMinutes = originalTime % 100;
        int minutesAdded = (int)timeOffset % 60; // Extra minutes
        int hoursAdded = (int)Math.Floor((double)timeOffset / 60); // Extra hours
        int unparsedMinutes = minutesAdded + initialMinutes; // Value for unparsed (>= 60 minutes)
        if (unparsedMinutes >= 60)// Correct the minutes number
        {
          unparsedMinutes -= 60;
        }
        int finalHours = initialHours + hoursAdded;
        int finalMinutes = unparsedMinutes;

        finalTime = finalHours * 100 + finalMinutes;

        if (finalTime > 2400) // Go into a new day
        {
          finalTime -= 2400;
        }
        Console.WriteLine($"Final Time: {finalTime}");
        return finalTime.ToString();
      }
    }

    // Utility Methods

    // Shortcut for obtaining information from UWP (Microsoft Store) applications
    private void getActualProcess(Process originalProcess)
    {
      _realProcess = originalProcess;//Stores real originalProcess information (mostly for UWP apps)
      if (!String.IsNullOrEmpty(originalProcess.MainWindowTitle))
      {
        if (originalProcess.ProcessName == "ApplicationFrameHost")//Get Real Process Data for UWP apps
        {
          _realProcess = getUWPProcess(originalProcess);
        }
      }
    }

    // Keeps process and secondary process running
    private void keepProcessAlive()
    {
      //
      // Keep Alive
      //

      // Check for other program processes and start if nonexistent
      try
      {
        Process[] secondaryProcess = Process.GetProcessesByName("Secondary");
        Process[] intermediate1 = Process.GetProcessesByName("Intermediate");
        Process[] intermediate2 = Process.GetProcessesByName("Intermediate Backward");
        if ((secondaryProcess.Length == 0) && (intermediate1.Length == 0) && (intermediate2.Length == 0))
        {
          Console.WriteLine("Compatible App Not Detected");
          Process.Start(intermediateDirectory);
        }
        else
        {
          bool processExists = false;
          // Check for second process
          foreach (Process p in secondaryProcess)
          {
            if (processExists) { break; }
            string auxFilePath = p.MainModule.FileName;
            if (auxFilePath == secondaryDirectory)
            {
              processExists = true;
            }
          }
          // Check for intermediate process (primary to secondary)
          foreach (Process p in intermediate1)
          {
            if (processExists) { break; }
            string auxFilePath = p.MainModule.FileName;
            if (auxFilePath == intermediateDirectory)
            {
              processExists = true;
            }
          }
          // Check for backward intermediate process (secondary to primary)
          foreach (Process p in intermediate2)
          {
            if (processExists) { break; }
            string auxFilePath = p.MainModule.FileName;
            if (auxFilePath == intermediateBackwardDirectory)
            {
              processExists = true;
            }
          }
          // Start if no process is found
          if (!processExists)
          {
            Console.WriteLine("Compatible App Not Detected #2");
            Process.Start(intermediateDirectory);
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.ToString());
      }
    }

    // Function for getting proper app information on UWP (Microsoft Store) Apps
    private Process getUWPProcess(Process foregroundProcess)
    {
      WinAPIFunctions.enumChildWindows(foregroundProcess.MainWindowHandle, childWindowCallback, IntPtr.Zero);
      return _realProcess;
    }

    // Part of method above
    private bool childWindowCallback(IntPtr hwnd, IntPtr lparam)
    {
      var process = Process.GetProcessById(WinAPIFunctions.getWindowProcessId(hwnd));
      if (process.ProcessName != "ApplicationFrameHost")
      {
        _realProcess = process;
      }
      return true;
    }
  }

  // Class for getting ApplicationFrameHost (UWP/Microsoft Store) detailed information
  public class WinAPIFunctions
  {
    // Foreground Window Getter (Only way to get MS Store Apps)
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr getForegroundWindow();

    // ID of any window
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int getWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool enumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);//Parameters are: window handle, all top level windows, 

    public static int getWindowProcessId(IntPtr hwnd)
    {
      int windowpid;
      getWindowThreadProcessId(hwnd, out windowpid);
      return windowpid;
    }

    public static IntPtr getforegroundWindow()
    {
      return getForegroundWindow();
    }
  }

}


