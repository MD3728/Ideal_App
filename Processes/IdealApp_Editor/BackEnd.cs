using System;
using System.Collections.Generic;
using System.Linq;

// Sqlite and Password Dependencies
using System.Security.Cryptography;
using Dapper;
using System.Data;
using System.Configuration;
using System.Data.SQLite;

namespace IA_GUI
{
  // Database Reference Classes
  public class RootInformation
  {
    public int idAttribute {  get; set; }
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

  public static class SecurePasswordHasher
  {
    // Security Constants
    private const int SaltSize = 16;
    private const int HashSize = 20;

    // Creates a hash from a password, one way only (Default 10000 iterations)
    public static string Hash(string password, int iterations)
    {
      // Salt
      var salt = new byte[SaltSize];
      var rng = RandomNumberGenerator.Create();
      rng.GetBytes(salt);
      //string refreshToken = Convert.ToBase64String(randomNumber);

      // Hash (SHA-2)
      var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
      var hash = pbkdf2.GetBytes(HashSize);

      // Combine the two
      var hashBytes = new byte[SaltSize + HashSize];
      Array.Copy(salt, 0, hashBytes, 0, SaltSize);
      Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

      // Convert to base64
      var base64Hash = Convert.ToBase64String(hashBytes);

      // Format hash with extra information
      return string.Format("$MYHASH$V1${0}${1}", iterations, base64Hash);
    }

    // Creates a hash from a password with 10000 iterations
    public static string Hash(string password)
    {
      return Hash(password, 10000);
    }

    // Checks for hash support
    public static bool IsHashSupported(string hashString)
    {
      return hashString.Contains("$MYHASH$V1$");
    }

    // Verifies password against hash
    public static bool Verify(string password, string hashedPassword)
    {
      // Check hash
      if (!IsHashSupported(hashedPassword))
      {
        throw new NotSupportedException("The hashtype is not supported");
      }

      // Extract iteration and Base64 string
      var splittedHashString = hashedPassword.Replace("$MYHASH$V1$", "").Split('$');
      var iterations = int.Parse(splittedHashString[0]);
      var base64Hash = splittedHashString[1];

      // Get hash bytes
      var hashBytes = Convert.FromBase64String(base64Hash);

      // Get salt
      var salt = new byte[SaltSize];
      Array.Copy(hashBytes, 0, salt, 0, SaltSize);

      // Create hash with given salt
      var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
      byte[] hash = pbkdf2.GetBytes(HashSize);

      // Get result
      for (var i = 0; i < HashSize; i++)
      {
        if (hashBytes[i + SaltSize] != hash[i])
        {
          return false;
        }
      }
      return true;
    }
  }

  public class BackEnd
  {

    // Loads connection strings from config file
    public static string loadConnString(string id = "Default")
    {
      return ConfigurationManager.ConnectionStrings[id].ConnectionString;
    }

    public static bool verifyPassword(string password, string enteredPassword)
    {
      // Verify
      //var hash = SecurePasswordHasher.Hash(password);
      //var result = SecurePasswordHasher.Verify(password, hash);
      var result = SecurePasswordHasher.Verify(password, enteredPassword);
      return result;

    }

    public static string hashPassword(string password)
    {
      // Hash
      var hash = SecurePasswordHasher.Hash(password);
      return hash;
    }

    // Database Read Methods
    // Note: All methods use a special connection string to reduce conflicts with other executables
    public static List<PlanInformation> parsePlans(){
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("PlanRead")))
      {
        var output = connection.Query<PlanInformation>("SELECT * FROM Plans", new DynamicParameters()).ToList();
        return output;
      }
    }

    public static List<RootInformation> parseAppData()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("RootRead")))
      {
        var output = connection.Query<RootInformation>("SELECT * FROM AppData", new DynamicParameters()).ToList();
        return output;
      }
    }

    public static List<StatsInformation> parseStatistics()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("StatRead")))
      {
        var output = connection.Query<StatsInformation>("SELECT * FROM UserStatistics", new DynamicParameters()).ToList();
        return output;
      }
    }
  
    // Deleting Entries
    public static void deleteEntryPlans(int entryID)
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Plan")))
      {
        var output = connection.Query<PlanInformation>($"DELETE FROM Plans WHERE idPlan = {entryID}", new DynamicParameters()).ToList();
      }
    }

    public static void deleteEntrySettings(int entryID)
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Root")))
      {
        var output = connection.Query<RootInformation>($"DELETE FROM AppData WHERE id = {entryID}", new DynamicParameters()).ToList();
      }
    }

    public static void deleteEntryStats(int entryID)
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Stat")))
      {
        var output = connection.Query<StatsInformation>($"DELETE FROM UserStatistics WHERE idApp = {entryID}", new DynamicParameters()).ToList();
      }
    }

    // Delete all (for reset all button on statistics screen)
    public static void deleteAllStats()
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Stat")))
      {
        var output = connection.Query<StatsInformation>($"DELETE FROM UserStatistics", new DynamicParameters()).ToList();
      }
    }


    // Adding Entries (Create)
    public static PlanInformation findLastEntryPlan()
    {
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Plan")))
      {
        var output = connection.Query<PlanInformation>("SELECT * FROM Plans ORDER BY idPlan DESC LIMIT 1", new DynamicParameters()).ToList();
        return output[0];
      }
    }

    public static void addEntryPlans(PlanInformation newEntry)
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Plan")))
      {
        string insertString = $"INSERT INTO Plans (planName, blockMethod, planDescription, blockedPaths, blockedNames, allowedPaths, allowedNames, protectionInactiveAllow, " +
          $"timingMethod, activeDays, activeTime, pomoAmount, pomoDuration, pomoSetAmount, pomoSmallBreak, pomoLargeBreak, pomoScheduledStart, enforcementMethod, currentlyActive, protectionActiveType, protectionActivePwd, protectionActiveChar, protectionActiveAddAllow) " +
          $"VALUES ('{newEntry.planName}',{newEntry.blockMethod},'{newEntry.planDescription}','{newEntry.blockedPaths}','{newEntry.blockedNames}','{newEntry.allowedPaths}','{newEntry.allowedNames}'," +
          $"{newEntry.protectionInactiveAllow},{newEntry.timingMethod},'{newEntry.activeDays}','{newEntry.activeTime}',{newEntry.pomoAmount},{newEntry.pomoDuration},{newEntry.pomoSetAmount}," +
          $"{newEntry.pomoSmallBreak},{newEntry.pomoLargeBreak},'{newEntry.pomoScheduledStart}',{newEntry.enforcementMethod}," +
          $"{newEntry.currentlyActive},{newEntry.protectionActiveType},'{newEntry.protectionActivePwd}',{newEntry.protectionActiveChar},'{newEntry.protectionActiveAddAllow}');";
        var output = connection.Query<PlanInformation>(insertString, new DynamicParameters());
      }
    }

    // Updating Entries (Update)
    public static void updateEntryPlans(PlanInformation newEntry)
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Plan")))
      {
        string updateString = $"UPDATE Plans SET planName = '{newEntry.planName}', blockMethod = {newEntry.blockMethod},planDescription='{newEntry.planDescription}',blockedPaths='{newEntry.blockedPaths}'" +
          $",blockedNames='{newEntry.blockedNames}',allowedPaths='{newEntry.allowedPaths}',allowedNames='{newEntry.allowedNames}'" +
          $",protectionInactiveAllow={newEntry.protectionInactiveAllow},timingMethod={newEntry.timingMethod},activeDays='{newEntry.activeDays}',activeTime='{newEntry.activeTime}',pomoAmount={newEntry.pomoAmount}," +
          $"pomoDuration={newEntry.pomoDuration},pomoSetAmount={newEntry.pomoSetAmount}," +
          $"pomoSmallBreak={newEntry.pomoSmallBreak},pomoLargeBreak={newEntry.pomoLargeBreak},pomoScheduledStart='{newEntry.pomoScheduledStart}',enforcementMethod={newEntry.enforcementMethod}," +
          $"currentlyActive = {newEntry.currentlyActive}, protectionActiveType = {newEntry.protectionActiveType}, protectionActivePwd = '{newEntry.protectionActivePwd}', protectionActiveChar = {newEntry.protectionActiveChar},protectionActiveAddAllow = {newEntry.protectionActiveAddAllow}" +
          $" WHERE idPlan = {newEntry.idPlan}";
        var output = connection.Query<PlanInformation>(updateString, new DynamicParameters());
      }
    }

    public static void updateActivePlans(PlanInformation newEntry)
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Plan")))
      {
        string updateString = $"UPDATE Plans SET currentlyActive = {newEntry.currentlyActive}" +        
          $" WHERE idPlan = {newEntry.idPlan}";
        var output = connection.Query<PlanInformation>(updateString, new DynamicParameters());
      }
    }

    public static void updateEntrySettings(RootInformation newEntry)
    {
      // Read database values
      using (IDbConnection connection = new SQLiteConnection(loadConnString("Root")))// Only need to update values 
      {
        var output = connection.Query<StatsInformation>($"UPDATE AppData SET attributeValue = '{newEntry.attributeValue}'" +
          $"WHERE idAttribute = {newEntry.idAttribute}", new DynamicParameters());
      }
    }

  }
}
