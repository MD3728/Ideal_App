using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Data.SQLite;
using System.Security.Cryptography;
using Dapper;
using System.Data;
using System.Configuration;
using System.Net.Http.Headers;
using System.Timers;
using System.Web.Http.Results;
//using Microsoft.AspNet.WebApi.Client;


namespace Primary
{
  // Constructor
  class Program
  {
    static void Main(string[] args)
    {//Program Entry Point
      Console.WriteLine("Start Debugging");//Debugging
      new ProcessParser();
      Console.WriteLine("End Debugging");//Debugging
    }
  }

  /// 
  /// Database Object Classes
  /// 

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
      this.protectionInactiveAllow = 0;
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
        bool tActiveTime = protectionInactiveAllow == 0 ? false : true;
        return tActiveTime;
      }
      set
      {
        protectionInactiveAllow = value ? 1 : 0;
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
    public int protectionActiveAddAllow { get; set; }// Pausing Attribute
    public int protectionInactiveType { get; set; }
    public string protectionInactivePwd { get; set; }
    public int protectionInactiveChar { get; set; }
    public int protectionInactiveAllow { get; set; } // Disabled Attribute
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

  public class TimeInformation
  {
    public TimeInformation() { }
    public DateTime datetime { get; set;  } // Do not change this name
  }

  /// 
  /// API Methods
  /// 

  // Time API Handler
  public class TimeProcessor
  {
    public static HttpClient client { get; set; }
    const int timeInRootDbIndex = 11; // idAttribute = 12, Current Time
    public const double pollingInterval = 1.0; // How often actionCycle runs

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
      client.Timeout = TimeSpan.FromSeconds(pollingInterval - 0.5);
      string url = "http://worldtimeapi.org/api/ip";

      try
      {
        using (HttpResponseMessage response = await client.GetAsync(url))
        {
          if (response.IsSuccessStatusCode)
          {
            TimeInformation result = await response.Content.ReadAsAsync<TimeInformation>();
            return result;
          }
          return null;
        }
      }catch(Exception ex)
      {
        return null;
        //throw new Exception(response.ReasonPhrase);
      }
    }
  }

  /// 
  /// Basic Cycle Methods
  /// 

  class ProcessParser
  {
    private Process _realProcess { get; set; } // Holds value of (real, eg. UWP and not ApplicationFrameHost) current process
    private bool isParserProcessing = false; //Determines if the program is in the middle of running and skips the next instance until it is
    private DateTime cTime = new DateTime();
    private double pollingInterval = 1.0; // How often actionCycle runs
    const int timeInRootDbIndex = 11; // idAttribute = 12, Current Time

    // Directory Values
    readonly string rootDirectory = ConfigurationManager.AppSettings["Root_Directory"];
    readonly string secondaryDirectory = ConfigurationManager.AppSettings["IA_Proxy_Directory"];
    readonly string intermediateDirectory = ConfigurationManager.AppSettings["IA_Spawn_Proxy_Directory"];
    readonly string intermediateBackwardDirectory = ConfigurationManager.AppSettings["IA_Spawn_Main_Directory"];
    readonly string primaryDirectory = ConfigurationManager.AppSettings["IA_Directory"];
    readonly string planInfoDirectory = ConfigurationManager.AppSettings["Root_Directory"] + "plans.db";
    readonly string statsInfoDirectory = ConfigurationManager.AppSettings["Root_Directory"] + "stats.db";
    readonly string appInfoDirectory = ConfigurationManager.AppSettings["Root_Directory"] + "root.db";

    public ProcessParser()
    {
      /* Final Version */

      // Create polling interval second cycle
      System.Timers.Timer timer = new System.Timers.Timer();
      timer.Interval = 1000 * pollingInterval;
      timer.Elapsed += new ElapsedEventHandler(this.actionCycle);
      timer.Start();

      // Keep program running
      while (true)
      {
        Thread.Sleep(100000);
      }

    }

    // Actions performed at consistent intervals
    public async void actionCycle(object sender, ElapsedEventArgs args)
    {
      var watch = Stopwatch.StartNew();

      // Skips cycle if previous cycle is still executing
      if (!isParserProcessing)
      {
        isParserProcessing = true;

        // Update Current Time        
        var timeCall = await Task.Run(() => TimeProcessor.loadTimeInformation());

        if (timeCall == null) // No internet access or time out
        {
          DateTime localComputerTime = DateTime.Now;
          localComputerTime = localComputerTime.AddDays(2); // Debugging Purposes
          cTime = localComputerTime;
        }
        else
        {
          cTime = timeCall.datetime;
        }

        // No else statement, since update will be performed inside of TimeProcessor class if connection is found

        keepProcessAlive(); // Keep processes alive
        killProcesses(); // Perform plan duties
        checkProcesses(); // Update statistics

        // Update time in database
        using (var connection = new SQLiteConnection(loadConnString("Root")))
        {
          connection.Query<RootInformation>($"UPDATE AppData SET attributeValue = '{cTime.ToString("yyyy MM dd HH mm ss")}' WHERE idAttribute = {timeInRootDbIndex + 1}", new DynamicParameters());
        }

        Console.WriteLine("Finished One Cycle");
        isParserProcessing = false;
      }
      else
      {
        Console.WriteLine("Current Cycle Skipped");
      }

      watch.Stop();
      var elapsedMs = watch.ElapsedMilliseconds;
      Console.WriteLine($"Elapsed {elapsedMs} ms");
    }

    /// 
    /// Database Methods
    /// 

    public static string loadConnString(string id = "Default")
    {
      return ConfigurationManager.ConnectionStrings[id].ConnectionString;
    }

    private List<PlanInformation> parsePlans()
    {
      // Read database values
      using (var connection = new SQLiteConnection(loadConnString("PlanRead")))
      {
        connection.Open();
        using (var command = new SQLiteCommand("PRAGMA temp_store = MEMORY;", connection))
        {
          command.ExecuteNonQuery();
        }
        var output = connection.Query<PlanInformation>("SELECT * FROM Plans", new DynamicParameters()).ToList();
        return output;
      }
    }

    private List<RootInformation> parseAppData()
    {
      // Read database values
      using (var connection = new SQLiteConnection(loadConnString("RootRead")))
      {
        connection.Open();
        using (var command = new SQLiteCommand("PRAGMA temp_store = MEMORY;", connection))
        {
          command.ExecuteNonQuery();
        }
        var output = connection.Query<RootInformation>("SELECT * FROM AppData", new DynamicParameters()).ToList();
        return output;
      }
    }

    private List<StatsInformation> parseStatistics()
    {
      // Read database values
      try
      {
        using (var connection = new SQLiteConnection(loadConnString("StatRead"))) //StatRead does not work
        {
          connection.Open();
          using (var command = new SQLiteCommand("PRAGMA temp_store = MEMORY;", connection))
          {
            command.ExecuteNonQuery();
          }
          var output = connection.Query<StatsInformation>("SELECT * FROM UserStatistics", new DynamicParameters()).ToList();
          return output;
        }
      }catch(Exception ex) // If the database fails to save due to external errors
      {
        using (var connection = new SQLiteConnection(loadConnString("Stat"))) //StatRead does not work
        {
          connection.Open();
          using (var command = new SQLiteCommand("PRAGMA temp_store = MEMORY;", connection))
          {
            command.ExecuteNonQuery();
          }
          var output = connection.Query<StatsInformation>("SELECT * FROM UserStatistics", new DynamicParameters()).ToList();
          return output;
        }
      }
    }

    /// 
    /// Primary Methods
    /// 

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
          //case 12: //Update Time
          //  string[] timeValues = cSetting.attributeValue.Split(" ");
          //  cTime = new DateTime(int.Parse(timeValues[0]), int.Parse(timeValues[1]), int.Parse(timeValues[2]), int.Parse(timeValues[3]), int.Parse(timeValues[4]), int.Parse(timeValues[5]));
          //  break;
          default:
            break;
        }
      }

      //Parse and execute plans
      List<PlanInformation> planInformation = parsePlans();
      List<List<string[]>> simplifiedPlans = new List<List<string[]>>();
      foreach (PlanInformation currentInfo in planInformation)
      {
        if ((currentInfo.currentlyActive == 0) || (currentInfo.protectionInactiveAllow == 1))//Ensure that plan is active and not disabled
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
        bool blockAllApps = currentInfo.blockMethod == 0 ? false : true; // If whitelist is on or not

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

        // Add active plans to the simplified 
        if (withinTime)
        {
          string[] isWhitelist = new string[1];
          isWhitelist[0] = currentInfo.blockMethod.ToString();
          List<string[]> blockSets = new List<string[]> { blockAppName, allowAppName, blockAppPath, allowAppPath, isWhitelist };
          simplifiedPlans.Add(blockSets);
        }
      }

      //var watch = Stopwatch.StartNew();

      // Path Blocking 
      Process[] processlist = Process.GetProcesses();
      foreach (Process process in processlist)
      {
        bool processKilled = false;
        string windowTitle;
        string appPath;
        string processName;
        try
        {
          windowTitle = process.MainWindowTitle; 
          if ((String.IsNullOrEmpty(windowTitle))) { continue; } // && (appPath.Substring(0, 11).Equals(@"c:\windows\"))
          if (process.ProcessName.Equals("ApplicationFrameHost"))
          {
            getActualProcess(process);// Major CPU Eater
            appPath = _realProcess.MainModule.FileName.ToLower();
            processName = _realProcess.ProcessName;
          }
          else
          {
            appPath = process.MainModule.FileName.ToLower(); // Major CPU Eater
            processName = process.ProcessName;
            _realProcess = process;
          }
        }
        catch(Exception ex) //System.ComponentModel.Win32Exception
        {
          continue;
        }

        // Iterate through all plans and terminate if process is killed
        foreach (List<string[]> cPlan in simplifiedPlans)
        {
          string[] blockAppName = cPlan[0];
          string[] allowAppName = cPlan[1];
          string[] blockAppPath = cPlan[2];
          string[] allowAppPath = cPlan[3];
          bool blockAllApps = cPlan[4][0].Equals("0") ? false : true;
          //
          //Name Blocking
          //
          foreach (string cAppName in blockAppName)
          {
            if (String.IsNullOrEmpty(cAppName))// If not empty (eg. not a blank line)
            { continue; }
            try
            {
              if (processName == cAppName)
              {
                _realProcess.Kill();
                processKilled = true;
                Console.WriteLine($"Blacklist Name Now blocking: {appPath} ");
                break;
              }
            }
            catch (Exception ex) { }
          }
          if (processKilled) { break; }
          //
          // Implement Path and/or Process Name
          //
          try
          {
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
                processKilled = true;
                Console.WriteLine($"Whitelist Path Now blocking: {appPath} ");
                _realProcess.Kill();                
              }
            }
            if (processKilled) { break; }
            // Path Blacklisting (Always Enabled)
            foreach (string cAppPath in blockAppPath)
            {
              //Kill application if conditions are true
              if (cAppPath.ToLower() == appPath.Substring(0, cAppPath.Length))
              {
                processKilled = true;
                Console.WriteLine($"Blacklist Path Now blocking: {cAppPath}    {appPath.Substring(0, cAppPath.Length)} ");
                _realProcess.Kill();
                break;
              }
            }
            if (processKilled) { break; }
          }
          catch (Exception ex) { }
        }
      }

      //watch.Stop();
      //var elapsedMs = watch.ElapsedMilliseconds;
      //Console.WriteLine($"Elapsed {elapsedMs} ms");

      //Debugging
      //Console.WriteLine("Process: {0} ID: {1} Window title: {2}\nPath: {3}\nName: {4}", _realProcess.ProcessName, _realProcess.Id, _realProcess.MainWindowTitle, _realProcess.MainModule.FileName, _realProcess.MainModule.FileVersionInfo.FileDescription);
      //Console.WriteLine("Process Listed Above Killed");
    }

    // For Statistics: Checks for processes and logs them for up to a week
    private void checkProcesses()
    { 
      // Database Connection
      List<StatsInformation> appStatistics = parseStatistics();
      string[] lastCheckTime = parseAppData()[timeInRootDbIndex].attributeValue.Split(" ");

      // Create new line if there are no existing statistics
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
      try
      {
        originalTime = new DateTime(int.Parse(lastCheckTime[0]), int.Parse(lastCheckTime[1]), int.Parse(lastCheckTime[2]), int.Parse(lastCheckTime[3]), int.Parse(lastCheckTime[4]), int.Parse(lastCheckTime[5]));
      }
      catch (Exception ex) // There is no time listed in the database (theoretically not possible)
      {
        originalTime = cDateTime;
      }
      Console.WriteLine("Stats Logged Time: " + originalTime);

      // Log all processes     
      int dayDifference = (cDateTime.Date - originalTime.Date).Days;  // Change in time

      using (var connection = new SQLiteConnection(loadConnString("Stat")))
      {
        // Update weekly statistics
        if (dayDifference > 0) // Day has switched
        {
          foreach (StatsInformation cEntry in appStatistics)
          {
            List<string> previousRecTimes = new List<string>(cEntry.processPastTimes.Trim().Split("|"));
            string currentRecordedDuration = cEntry.processCDay.ToString();
            previousRecTimes.Insert(0, currentRecordedDuration);
            //Extra Cases
            for (int a = 0; a < dayDifference - 1; a++)
            {
              previousRecTimes.Insert(0, "0");
            }
            currentRecordedDuration = "0";

            // Remove old times (only stores the 6 previous days)
            for (int b = 6; b < previousRecTimes.Count; b = b)
            {
              previousRecTimes.RemoveAt(previousRecTimes.Count - 1);// Previous Time (Now stored as list)
            }
            // Fill in the rest of the times
            for (int c = previousRecTimes.Count(); c < 6; c++)// Add "0" time for days not listed (or before first run)
            {
              previousRecTimes.Add("0");
            }
            // Modify the Data
            string pastTimesConcat = string.Join("|", previousRecTimes);
            connection.Query<StatsInformation>($"UPDATE UserStatistics SET processCDay = {currentRecordedDuration}, processPastTimes = '{pastTimesConcat}' WHERE idApp = {cEntry.idApp}", new DynamicParameters()).ToList();
          }
        }

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
            catch (System.ComponentModel.Win32Exception ex)
            {
              continue;
            }

            bool found = false;
            // Check each existing process for existence and update if necessary
            foreach (StatsInformation cEntry in appStatistics)
            {
              string NPName = cEntry.processName.Trim(); // Process Name
              string NPPath = cEntry.processPath.Trim(); // Process Path
              List<string> previousRecTimes = new List<string>(cEntry.processPastTimes.Trim().Split("|"));
              string currentRecordedDuration = cEntry.processCDay.ToString();
              if ((NPName == cProcessName) || (NPPath == cProcessPath)) // Encountering an existing entry
              {
                currentRecordedDuration = (int.Parse(currentRecordedDuration) + pollingInterval).ToString();
                connection.Query<StatsInformation>($"UPDATE UserStatistics SET processCDay = {currentRecordedDuration} WHERE idApp = {cEntry.idApp}", new DynamicParameters()).ToList();
                found = true;
                break;
              }
            }
            // New entries
            if (!found)
            {
              var result = connection.Query<StatsInformation>($"INSERT INTO UserStatistics (processName, processPath, processCDay, processPastTimes) VALUES ('{cProcessName}', '{cProcessPath}' , {pollingInterval}, '0|0|0|0|0|0')", new DynamicParameters()).ToList();
            }
          }
        }
        // Update last check time
        string newLastCheckTime = cDateTime.ToString("yyyy MM dd HH mm ss");
        // Read database values (Root)
        using (var connection2 = new SQLiteConnection(loadConnString("Root")))
        {
          var output = connection2.Query<StatsInformation>($"UPDATE AppData SET attributeValue = '{newLastCheckTime}' WHERE idAttribute = {timeInRootDbIndex + 1}", new DynamicParameters()).ToList();
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

    /// 
    /// Utility Methods
    /// 

    // Shortcut for obtaining information from UWP (Microsoft Store) applications
    private void getActualProcess(Process originalProcess)
    {
      if (originalProcess.ProcessName == "ApplicationFrameHost")//Get Real Process Data for UWP apps
      {
        _realProcess = getUWPProcess(originalProcess);
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
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
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

