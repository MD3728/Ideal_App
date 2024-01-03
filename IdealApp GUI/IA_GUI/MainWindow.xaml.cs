using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;
using System.Threading;
using System.Runtime.InteropServices;

// Dependencies that are necessary for this to run
// System.Security.Cryptography;
// System.Configuration.ConfigurationManager; (Backend)
// System.Data.SQLite.Core; (Backend)
// Dapper (Backend)
// Extended.Wpf.toolkit   
// microsoft.aspnet.webapi.client (For Time API Request)

// Add these to Properties -> Build -> Output -> prebuild event
// if exist "$(TargetPath).locked" del "$(TargetPath).locked"
// if exist "$(TargetPath)" if not exist "$(TargetPath).locked" move "$(TargetPath)" "$(TargetPath).locked"  

// Name of Pages:
// planMain (Main Screen: Plans tab)
// statsMain (Main Screen )
// settingsMain
// planSettingsMain: First screen of each plan
// planSettingsApp: Second screen of each plan
// planSettings Time: Third screen of each plan
// password: Password Screen

namespace IA_GUI
{

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    // Standard Directories
    //const string blockConfigDirectory = "C:\\Program Files\\Protected_Folder\\Provisional_IdealApp\\PersonalSettings.txt";
    const string intermediateDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\SpawnProxy\\bin\\Debug\\net7.0\\Intermediate.exe";
    const string intermediateBackwardDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\SpawnMain\\bin\\Debug\\net7.0\\Intermediate Backward.exe";
    const string secondaryDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealAppProxy\\bin\\Debug\\net7.0\\Secondary.exe";
    const string primaryDirectory = "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealApp\\bin\\Debug\\net7.0\\Primary.exe";
    const string planInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\plans.db";
    const string statsInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\stats.db";
    const string appInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\root.db";

    // Globals
    public string currentScreen = "None";
    public RootInformation rootSettings = new RootInformation();
    public int currentPlanIndex = 1;
    public PlanInformation tempPlanCache = new PlanInformation();
    public string randomChars = "";
    public bool moveOn = false;

    // Event Suppressor
    public bool canPlanProtectionChange = true; // Prevents selection event from occuring

    // Constants for GUI
    public const int maxSettings = 9;
    public const int maxStats = 11;

    // Property to allow for bindings (Plans)
    public PlanInformation returnTempPlanCache {
      get
      {
        return tempPlanCache;
      }
      set
      {
        tempPlanCache = value;
      }
    }

    // Property to allow for bindings (Settings)
    public RootInformation returnSettings
    {
      get
      {
        return rootSettings;
      }
      set
      {
        rootSettings = value;
      }
    }
   
    public static Random random = new Random();

    public MainWindow()
    {
      InitializeComponent();//Do not touch

      currentScreen = "planMain";
      loadPlansInformation();

      //Example Code
      var definedLabel = (Rectangle)this.FindName("GlobalHeader");
      var wholeGrid = (Grid)this.FindName("total_grid");
      // For hexadecimal
      BrushConverter bc = new BrushConverter();
      //definedLabel.Fill = (Brush)bc.ConvertFrom("#76d7fc");
      //wholeGrid.Background = (Brush)bc.ConvertFrom("#76d7fc");

    }

    //
    // Event Handler Methods
    //

    // Prepares entry into the initial screen (all plans)
    public void loadMainPlanScreen(object sender, RoutedEventArgs e)
    {
      // Attempt to remove eventhandlers
      var submitButton = (Button)this.FindName("PasswordSubmit");
      try { submitButton.Click -= new RoutedEventHandler(transitionToDelete); } catch (Exception ex) { }
      try { submitButton.Click -= new RoutedEventHandler(transitionToEdit); } catch (Exception ex) { }
      try { submitButton.Click -= new RoutedEventHandler(transitionToStop); } catch (Exception ex) { }

      if (currentScreen == "planSettingsMain")
      {
        var planFrame = (Grid)this.FindName($"password_grid");
        planFrame.Visibility = Visibility.Collapsed;
      }
      if (currentScreen == "statsMain")
      {
        var planFrame = (Grid)this.FindName($"stats_grid");
        planFrame.Visibility = Visibility.Collapsed;
      }
      else if (currentScreen == "settingsMain")
      {
        // Update to proper settings
        List<RootInformation> settingData = BackEnd.parseAppData();
        int attributeCounter = 1;
        int buttonCounter = 1;// Correlates with button numbers
        foreach (RootInformation cSetting in settingData)
        {
          if (cSetting.canModify == 0)
          {// Unseen attributes
            attributeCounter++;
            continue;
          }
          var CSButton = (ToggleButton)this.FindName($"settingToggle{buttonCounter}");
          if (CSButton.IsChecked == true)
          {
            cSetting.attributeValue = "1";
          }
          else
          {
            cSetting.attributeValue = "0";
          }
          buttonCounter++;
          attributeCounter++;
          BackEnd.updateEntrySettings(cSetting);
        }
        var planFrame = (Grid)this.FindName($"settings_grid");
        planFrame.Visibility = Visibility.Collapsed;
        var blockFrame3 = (Grid)this.FindName($"blocked_grid");
        blockFrame3.Visibility = Visibility.Collapsed;
      }

      // For blockedStats, blockedSettings, and all other password screens
      var blockFrame = (Grid)this.FindName($"password_grid");
      blockFrame.Visibility = Visibility.Collapsed;
      var blockFrame2 = (Grid)this.FindName($"blocked_grid");
      blockFrame2.Visibility = Visibility.Collapsed;
      loadPlansInformation();
    }

    // Prepares entry into statistics screen
    public void loadStatsScreen(object sender, RoutedEventArgs e)
    {
      if (currentScreen == "planMain")
      {
        var planFrame = (Grid)this.FindName($"plan_grid");
        planFrame.Visibility = Visibility.Collapsed;
      }
      else if (currentScreen == "settingsMain")
      {
        // Update to proper settings
        List<RootInformation> settingData = BackEnd.parseAppData();
        int attributeCounter = 1;
        int buttonCounter = 1;// Correlates with button numbers
        foreach (RootInformation cSetting in settingData)
        {
          if (cSetting.canModify == 0)
          {// Unseen attributes
            attributeCounter++;
            continue;
          }
          var CSButton = (ToggleButton)this.FindName($"settingToggle{buttonCounter}");
          if (CSButton.IsChecked == true)
          {
            cSetting.attributeValue = "1";
          }
          else
          {
            cSetting.attributeValue = "0";
          }
          buttonCounter++;
          attributeCounter++;
          BackEnd.updateEntrySettings(cSetting);
        }
        var planFrame = (Grid)this.FindName($"settings_grid");
        planFrame.Visibility = Visibility.Collapsed;

      }else if (currentScreen == "blockedSettings")
      {
        var blockFrame = (Grid)this.FindName($"blocked_grid");
        blockFrame.Visibility = Visibility.Collapsed;
      }
      displayStatsInformation();
    }

    // Prepares entry into global settings screen
    public void loadSettingsScreen(object sender, RoutedEventArgs e)
    {
      if (currentScreen == "statsMain")
      {
        ApplyTemplate();
        var planFrame = (Grid)this.FindName($"stats_grid");
        planFrame.Visibility = Visibility.Collapsed;
      }
      else if (currentScreen == "planMain")
      {
        var planFrame = (Grid)this.FindName($"plan_grid");
        planFrame.Visibility = Visibility.Collapsed;
      }
      else if (currentScreen == "blockedStats")
      {
        var blockFrame = (Grid)this.FindName($"blocked_grid");
        blockFrame.Visibility = Visibility.Collapsed;
      }
      var blockFrame2 = (Grid)this.FindName($"password_grid");
      blockFrame2.Visibility = Visibility.Collapsed;
      displaySettingsInformation();
    }

    // Event handler for changing global statistics settings (switching between daily and weekly)
    public void changeConfigurationStat(object sender, RoutedEventArgs e)
    {// Make sure not to select any option by default
      var comboBoxOption1 = (ComboBoxItem)this.FindName($"timeChoice1");
      bool firstOption = comboBoxOption1.IsSelected;
      List <StatsInformation> statData = BackEnd.parseStatistics();
      if (firstOption)
      {
        ApplyTemplate();
        int numStats2 = 1;
        foreach (StatsInformation cStat in statData)
        {
          if (numStats2 <= statData.Count())
          {
            var activeStat3 = (Label)this.FindName($"statTime{numStats2}");
            if (activeStat3 == null)
            {
              continue;
            }
            // Read and parse data
            int processCDay = cStat.processCDay;
            double hours = Math.Floor(processCDay / 3600.0);
            double minutes = Math.Floor(processCDay / 60.0);
            int seconds = processCDay % 60;
            activeStat3.Content = $"{hours}H {minutes}M {seconds}S";
          }
          numStats2++;
        }
      }
      else
      {
        showWeekStat(statData);
      }
    }

    // For reloading with different timing methods inside of plans
    public void changeConfigurationTiming(object sender, RoutedEventArgs e)
    {
      var comboBoxOption1 = (ComboBox)this.FindName($"timeSelect");
      int selectedOption = comboBoxOption1.SelectedIndex;
      switch (selectedOption)
      {
        case 0://Scheduled
          tempPlanCache.timingMethod = 0;
          break;
        case 1://Pomodoro
          tempPlanCache.timingMethod = 1;
          break;
        default:
          break;
      }

      showTimingInformation();
    }

    // ComboxBox Trigger: For changing protection in Plan General Screen (planScreen1), changes text boxes
    public void changeConfigurationProtection(object sender, RoutedEventArgs e)
    {
      if (canPlanProtectionChange)
      {
        var comboBoxOption1 = (ComboBox)this.FindName($"protectSelect");
        int selectedOption = comboBoxOption1.SelectedIndex;
        switch (selectedOption)
        {
          case 0://None
            tempPlanCache.protectionActiveType = 0;
            tempPlanCache.protectionActivePwd = "";
            tempPlanCache.protectionActiveChar = 0;
            break;
          case 1://Delaying
            tempPlanCache.protectionActiveChar = 0;//Overload of value
            tempPlanCache.protectionActiveType = 1;
            break;
          case 2://Password
            tempPlanCache.protectionActiveType = 2;
            //tempPlanCache.protectionActivePwd = "";// Set Later
            tempPlanCache.protectionActiveChar = 0;
            break;
          case 3://Random Characters
            tempPlanCache.protectionActiveType = 3;
            tempPlanCache.protectionActiveChar = 0;
            break;
          case 4://Forced
            tempPlanCache.protectionActiveType = 4;
            break;
          default:
            break;
        }
        if (currentScreen != "None")
        {
          showBasicPlanInformation(false);
        }
      }     
    }

    // Prepares for entry into the general plan information screen (#1)
    public void loadFirstPlanScreen(object sender, RoutedEventArgs e)
    {
      Button originButton = sender as Button;
      string contents = (string)originButton.Name;

      List<PlanInformation> planData = BackEnd.parsePlans();
      if (currentScreen == "planSettingsApp")
      {
        var planFrame = (Grid)this.FindName($"plan_grid_2");
        planFrame.Visibility = Visibility.Collapsed;
      }
      else if (currentScreen == "planMain")
      {
        var planFrame = (Grid)this.FindName($"plan_grid");
        planFrame.Visibility = Visibility.Collapsed;
      }

      // Determine whether to create a new plan
      if (contents.Remove(10) == "planCreate")// New Plan (from planMain screen)
      {
        tempPlanCache = new PlanInformation();
        showBasicPlanInformation(true);
        BackEnd.addEntryPlans(tempPlanCache);
        tempPlanCache = BackEnd.findLastEntryPlan();
      }
      else
      {
        int x = tempPlanCache.idPlan;
        showBasicPlanInformation(true);
        BackEnd.updateEntryPlans(tempPlanCache);
      }
    }

    //
    // Preliminary automatic detection for all possible password/char/forced screens
    //
    public void promptUserProtection(object sender, RoutedEventArgs e)
    {
      List<PlanInformation> planData = BackEnd.parsePlans();

      // Determine the entry point of the user, by using button id
      Button originButton = sender as Button;
      string contents = (string)originButton.Name;

      // Parse the name to find where to go next, and current plan
      string finalDirective = "";
      int planNumber = -1;
      int counter = 0;
      foreach (char a in contents)
      {
        int ignoreOutput;
        bool isInt = int.TryParse(a.ToString(), out ignoreOutput);
        if (isInt)
        {
          planNumber = int.Parse(contents.Substring(counter));
          finalDirective = contents.Remove(counter, contents.Length - counter);
          break;
        }
        counter++;
      }

      // Set the new
      currentPlanIndex = planNumber;
      tempPlanCache = planData[currentPlanIndex - 1];

      // Protection not active 
      if ((!isCurrentPlanActive(tempPlanCache))||(tempPlanCache.protectionActiveType == 0))
      {
        switch (finalDirective)
        {
          case "planStop":
            stopPlan();
            break;
          case "planDelete":
            deletePlan();
            break;
          case "planEdit":
            editPlan();
            break;
        }
      }
      else // Protection Active
      {
        Button submitButton = (Button)this.FindName($"PasswordSubmit");
        // Deal with protection evels
        switch (tempPlanCache.protectionActiveType)
        {
          case 0: //No protection (Direct Access, See above if statement)
            break;
          case 2:
          case 3: //Password and Random Characters
                  // Special case with pausing
            if ((tempPlanCache.returnPausingAllowed)&&(finalDirective == "planStop"))
            {
              transitionToStop(sender, e);
              break;
            }
            var inputInstruction = (Label)this.FindName($"TLabel");
            var userInput = (PasswordBox)this.FindName($"PasswordInput");
            userInput.Clear(); // Clear previous password
            var planFrame = (Grid)this.FindName($"password_grid");
            planFrame.Visibility = Visibility.Visible;
            var origScreen = (Grid)this.FindName($"plan_grid");
            origScreen.Visibility = Visibility.Collapsed;
           
            if (tempPlanCache.protectionActiveType == 2)
            {
              inputInstruction.Content = "Please Enter the Password for This Set:\n";
            }
            else
            {
              // Generate random string
              string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";// Set of all characters
              randomChars = new string(Enumerable.Repeat(chars, tempPlanCache.protectionActiveChar).Select(s => s[random.Next(s.Length)]).ToArray());
              inputInstruction.Content = "Please Enter the Following Characters:\n" + randomChars;
            }
            // Determine the final command of the submit button
            switch (finalDirective)
            {
              case "planStop":
                submitButton.Click += new RoutedEventHandler(transitionToStop);
                break;
              case "planDelete":
                submitButton.Click += new RoutedEventHandler(transitionToDelete);
                break;
              case "planEdit":
                submitButton.Click += new RoutedEventHandler(transitionToEdit);
                break;
            }
            break;
          case 1:
          case 4: // Total Block and Delay
                  // Special case with pausing
            if ((tempPlanCache.returnPausingAllowed) && (finalDirective == "planStop"))
            {
              transitionToStop(sender, e);
              break;
            }
            var origScreen1 = (Grid)this.FindName($"plan_grid");
            origScreen1.Visibility = Visibility.Collapsed;
            var tBlockScreen = (Grid)this.FindName($"blocked_grid");
            tBlockScreen.Visibility = Visibility.Visible;
            var labelInstruction = (Label)this.FindName($"BlockLabel");
            labelInstruction.Content = "You cannot perform this action until\n         this plan becomes inactive\n                    or disabled";
            break;
          default:
            break;
        }
      }

    }

    // Delete Button Command
    public void transitionToDelete(object sender, RoutedEventArgs e)
    {
      if (checkLogin())
      {
        Button submitButton = (Button)this.FindName($"PasswordSubmit");
        submitButton.Click -= new RoutedEventHandler(transitionToDelete);
        deletePlan();
      }
    }

    // Stop Button Command
    public void transitionToStop(object sender, RoutedEventArgs e)
    {
      if (checkLogin())
      {
        Button submitButton = (Button)this.FindName($"PasswordSubmit");
        submitButton.Click -= new RoutedEventHandler(transitionToStop);
        stopPlan();
      }
    }

    // Edit Button Command
    public void transitionToEdit(object sender, RoutedEventArgs e)
    {
      if (checkLogin())
      {
        Button submitButton = (Button)this.FindName($"PasswordSubmit");
        submitButton.Click -= new RoutedEventHandler(transitionToEdit);
        editPlan();
      }
    }

    // The following methods below are called from login or can be called standalone
    // Delete Command
    public void deletePlan()
    {
      BackEnd.deleteEntryPlans(tempPlanCache.idPlan);
      loadPlansInformation();//Reload
    }

    // Stop Command
    public void stopPlan()
    {
      tempPlanCache.currentlyActive = 0;
      BackEnd.updateActivePlans(tempPlanCache);
      loadPlansInformation();//Reload
    }

    // Edit Command
    public void editPlan()
    {
      var planFrame = (Grid)this.FindName($"plan_grid");
      planFrame.Visibility = Visibility.Collapsed;
      showBasicPlanInformation(true);
    }

    // Checks the validity of password/chars and returns a boolean
    public bool checkLogin()
    {
      if (tempPlanCache.protectionActiveType == 2)//Password Protection
      {
        var planFrame2 = (PasswordBox)this.FindName($"PasswordInput");
        string inputPwd = planFrame2.Password;
        if (BackEnd.verifyPassword(inputPwd, tempPlanCache.protectionActivePwd))
        {
          var origScreen1 = (Grid)this.FindName($"password_grid");//Remove Screen
          origScreen1.Visibility = Visibility.Collapsed;
          return true;
        }
        else
        {
          System.Windows.MessageBox.Show("Incorrect Password", "Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
          return false;
        }
      }
      else if (tempPlanCache.protectionActiveType == 3)//Random Characters
      {
        var planFrame2 = (PasswordBox)this.FindName($"PasswordInput");
        string inputPwd = planFrame2.Password;
        if (inputPwd == randomChars)
        {
          var origScreen1 = (Grid)this.FindName($"password_grid");//Remove Screen
          origScreen1.Visibility = Visibility.Collapsed;
          return true;
        }
        else
        {
          System.Windows.MessageBox.Show("Your input has errors!", "Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
          return false;
        }
      }
      return true;
    }

    // Prepares for entry into block information screen
    public void loadSecondPlanScreen(object sender, RoutedEventArgs e)
    {
      //Update Password Information
      var descriptionBox1 = (TextBox)this.FindName($"planProtect1");
      var defaultProtectSelect1 = (ComboBox)this.FindName($"protectSelect");
      if (defaultProtectSelect1.SelectedIndex == 2)//Password Input
      {
        if (!string.IsNullOrWhiteSpace(descriptionBox1.Text))
        {
          tempPlanCache.protectionActivePwd = BackEnd.hashPassword(descriptionBox1.Text);
        }
      }

      // General Update
      BackEnd.updateEntryPlans(tempPlanCache);
      //Transition
      if (currentScreen == "planSettingsMain")
      {
        var planFrame = (Grid)this.FindName($"plan_grid_1");
        planFrame.Visibility = Visibility.Collapsed;
      }
      else if (currentScreen == "planSettingsTime")
      {
        // Update the checkboxes from the third screen
        string timeString = "";
        for (int a = 1; a < 8; a++)
        {
          var cCheck = (CheckBox)this.FindName($"day{a}");
          if (cCheck.IsChecked == true)
          {
            if (timeString.Length >= 1)
            {
              timeString += "|";
            }
            timeString += $"{a}";
          }
        }
        var planFrame = (Grid)this.FindName($"plan_grid_3");
        planFrame.Visibility = Visibility.Collapsed;
      }
      
      showAppInformation();      
    }

    // Prepares for entry into timing information screen 
    public void loadThirdPlanScreen(object sender, RoutedEventArgs e)
    {
      BackEnd.updateEntryPlans(tempPlanCache);
      if (currentScreen == "planSettingsApp")
      {
        var planFrame = (Grid)this.FindName($"plan_grid_2");
        planFrame.Visibility = Visibility.Collapsed;
      }

      showTimingInformation();
    }

    // Go back to plan_main screen, from last page of plan editor
    public void returnToMainPlan(object sender, RoutedEventArgs e)
    {
      // Enable the top menu
      var top1 = (Button)this.FindName($"GlobalPlan");
      top1.IsEnabled = true;
      var top2 = (Button)this.FindName($"GlobalStat");
      top2.IsEnabled = true;
      var top3 = (Button)this.FindName($"GlobalSetting");
      top3.IsEnabled = true;
      if (currentScreen == "planSettingsTime")
      {
        // Update the checkboxes from the third screen
        string timeString = "";
        for (int a = 1; a < 8; a++)
        {
          var cCheck = (CheckBox)this.FindName($"day{a}");
          if (cCheck.IsChecked == true)
          {
            if (timeString.Length >= 1)
            {
              timeString += "|";
            }
            timeString += $"{a}";
          }
        }
        tempPlanCache.activeDays = timeString;
        BackEnd.updateEntryPlans(tempPlanCache);//Final Update

        var planFrame = (Grid)this.FindName($"plan_grid_3");
        planFrame.Visibility = Visibility.Collapsed;
      }

      loadPlansInformation();
    }

    // Change between whitelist and blacklist
    public void confirmListType(object sender, RoutedEventArgs e)
    {
      ComboBox selectionBox = (ComboBox)this.FindName($"listingSelectBox");
      tempPlanCache.blockMethod = selectionBox.SelectedIndex;
      showAppInformation();
    }

    // Start Button Command
    public void startSelectedPlan(object sender, RoutedEventArgs e)
    {
      Button buttonType = sender as Button;
      string contents = buttonType.Name;
      currentPlanIndex = int.Parse(contents.Remove(0, 9));
      List<PlanInformation> planData = BackEnd.parsePlans();
      //planData.RemoveAt(planData.Count() - 1);// Remove last placeholder entry
      PlanInformation currentPlan = planData[currentPlanIndex - 1];
      currentPlan.currentlyActive = 1;
      BackEnd.updateActivePlans(currentPlan);
      loadPlansInformation();//Reload
    }

    // Delete all statistics
    public void resetAllStat(object sender, RoutedEventArgs e)
    {
      BackEnd.deleteAllStats();
      displayStatsInformation();
    }

    // Delete selected statistic
    public void deleteCStat(object sender, RoutedEventArgs e)
    {
      Button buttonType = sender as Button;
      string contents = buttonType.Name;
      int currentPlanIndex2 = int.Parse(contents.Remove(0, 9));//statReset#
      List<StatsInformation> statData = BackEnd.parseStatistics();
      //planData.RemoveAt(planData.Count() - 1);// Remove last placeholder entry
      StatsInformation currentStat = statData[currentPlanIndex2 - 1];
      BackEnd.deleteEntryStats(currentStat.idApp);
      displayStatsInformation();//Reload
    }

    //
    // General Methods
    //

    // For ALL Plans, determines if at least one is active and running
    public bool isPlanActive()
    {
      bool activePlan = false;
      List<PlanInformation> planData = BackEnd.parsePlans();
      foreach (PlanInformation cPlan in planData)
      {
        int timingMethod = cPlan.timingMethod;
        if (cPlan.currentlyActive == 1)
        {
          bool active = false;
          if (timingMethod == 0)// Normal Schedule
          {
            active = CheckScheduledTime(cPlan.activeDays.Split("|"), cPlan.activeTime.Split("|"));
          }
          else if (timingMethod == 1)// Pomodoro
          {
            try
            {
              active = CheckPomoTime(cPlan.activeDays.Split("|"), cPlan.pomoAmount.ToString(), cPlan.pomoDuration.ToString(), cPlan.pomoSetAmount.ToString(),
              cPlan.pomoSmallBreak.ToString(), cPlan.pomoLargeBreak.ToString(), cPlan.pomoScheduledStart);
            }
            catch (Exception e)
            {
              active = false;
            }
          }
          if (active)
          {
            activePlan = true;
            break;
          }

        }
      }
      return activePlan;
    }

    // Determine if a specific plan is active
    public bool isCurrentPlanActive(PlanInformation cPlan)
    {
      if (cPlan.currentlyActive == 0)
      {
        return false;
      }
      if (cPlan.timingMethod == 0)//Regular Schedule
      {
        if (CheckScheduledTime(cPlan.activeDays.Split("|"), cPlan.activeTime.Split("|")))
        {
          return true;
        }
        else
        {
          return false;
        }
      }
      else
      {
        if (CheckPomoTime(cPlan.activeDays.Split("|"), cPlan.pomoAmount.ToString(), cPlan.pomoDuration.ToString(), cPlan.pomoSetAmount.ToString(),
              cPlan.pomoSmallBreak.ToString(), cPlan.pomoLargeBreak.ToString(), cPlan.pomoScheduledStart))
        {
          return true;
        }
        return false;
      }

    }

    // Main Plan Page (Home)
    public void loadPlansInformation()
    { 
      currentScreen = "planMain";
      // Get Database Information
      var planFrame = (Grid)this.FindName($"plan_grid");
      planFrame.Visibility = Visibility.Visible;
      List<PlanInformation> planData = BackEnd.parsePlans();
      BrushConverter bc = new BrushConverter();
      Brush greenColor = (Brush)bc.ConvertFrom("#31bf58");
      Brush redColor = (Brush)bc.ConvertFrom("#FFFF6347");

      int numPlans = 1;
      foreach (PlanInformation cPlan in planData)
      {
        // Delete Excess Entries
        if (numPlans > 12)
        {
          BackEnd.deleteEntryPlans(cPlan.idPlan);
          planData.Remove(cPlan);
          continue;
        }

        // Read and parse data
        bool whitelist = (cPlan.blockMethod == 0) ? false : true;
        bool firewall = (cPlan.enforcementMethod == 0) ? false : true;
        int timingMethod = cPlan.timingMethod;
        bool active = false;
        if (timingMethod == 0)// Normal Schedule
        {
          active = CheckScheduledTime(cPlan.activeDays.Split("|"), cPlan.activeTime.Split("|"));
        }
        else if (timingMethod == 1)// Pomodoro
        {
          try
          {
            active = CheckPomoTime(cPlan.activeDays.Split("|"), cPlan.pomoAmount.ToString(), cPlan.pomoDuration.ToString(), cPlan.pomoSetAmount.ToString(),
            cPlan.pomoSmallBreak.ToString(), cPlan.pomoLargeBreak.ToString(), cPlan.pomoScheduledStart);
          } catch (Exception e)
          {
            active = false;
          }
        }

        // Name
        var activeLabel5 = (Label)this.FindName($"planName{numPlans}");
        activeLabel5.Content = cPlan.planName;

        //Active Display
        if ((active))
        {
          var activeLabel2 = (Label)this.FindName($"planActive{numPlans}");
          activeLabel2.Background = greenColor;
          activeLabel2.Content = "Yes";
        }
        else
        {
          var activeLabel2 = (Label)this.FindName($"planActive{numPlans}");
          activeLabel2.Background = redColor;
          activeLabel2.Content = "No";
        }

        // Listing Display
        if (whitelist)
        {
          var activeLabel2 = (Label)this.FindName($"planWhitelist{numPlans}");
          activeLabel2.Background = greenColor;
          activeLabel2.Content = "Whitelisting";
        }
        else
        {
          var activeLabel2 = (Label)this.FindName($"planWhitelist{numPlans}");
          activeLabel2.Background = redColor;
          activeLabel2.Content = "Blacklisting";
        }

        // Enforcement
        if (firewall)
        {
          var activeLabel2 = (Label)this.FindName($"planFirewall{numPlans}");
          activeLabel2.Background = greenColor;
          activeLabel2.Content = "Firewall";
        }
        else // Regular Blocking
        {
          var activeLabel2 = (Label)this.FindName($"planFirewall{numPlans}");
          activeLabel2.Background = redColor;
          activeLabel2.Content = "Kill Process";
        }

        var startButton = (Button)this.FindName($"planStart{numPlans}");
        var stopButton = (Button)this.FindName($"planStop{numPlans}");
        var activeLabel = (Label)this.FindName($"planName{numPlans}");
        var whitelistLabel = (Label)this.FindName($"planWhitelist{numPlans}");
        var firewallLabel = (Label)this.FindName($"planFirewall{numPlans}");

        // Show proper buttons
        if (cPlan.protectionInactiveAddAllow == 0)//Enabled
        {
          // Start or stop the plans
          if ((cPlan.currentlyActive == 0))
          { 
            startButton.Visibility = Visibility.Visible; 
            stopButton.Visibility = Visibility.Collapsed;
          }
          else
          {
            startButton.Visibility = Visibility.Collapsed;
            stopButton.Visibility = Visibility.Visible;
          }
          activeLabel.Visibility = Visibility.Visible;
          whitelistLabel.Visibility = Visibility.Visible;
          firewallLabel.Visibility = Visibility.Visible;
        }
        else//Disabled
        {
          startButton.Visibility = Visibility.Collapsed;
          stopButton.Visibility = Visibility.Collapsed;
          activeLabel.Visibility = Visibility.Collapsed;
          whitelistLabel.Visibility = Visibility.Collapsed;
          firewallLabel.Visibility = Visibility.Collapsed;
        }
        var deleteButton = (Button)this.FindName($"planDelete{numPlans}");
        deleteButton.Visibility = Visibility.Visible;
        var createButton = (Button)this.FindName($"planCreate{numPlans}");
        createButton.Visibility = Visibility.Collapsed;
        var editButton = (Button)this.FindName($"planEdit{numPlans}");
        editButton.Visibility = Visibility.Visible;

        numPlans++;
      }

      // Make unused plans invisible
      for (int a = numPlans; a <= 12; a++)
      {
        var nameLabel = (Label)this.FindName($"planName{a}");
        nameLabel.Content = "Empty";
        var activeLabel = (Label)this.FindName($"planActive{a}");
        activeLabel.Visibility = (System.Windows.Visibility) Enum.Parse(typeof(System.Windows.Visibility), "Collapsed");
        var whitelistLabel = (Label)this.FindName($"planWhitelist{a}");
        whitelistLabel.Visibility = Visibility.Collapsed;
        var firewallLabel = (Label)this.FindName($"planFirewall{a}");
        firewallLabel.Visibility = Visibility.Collapsed;
        var deleteButton = (Button)this.FindName($"planDelete{a}");
        deleteButton.Visibility = Visibility.Collapsed;
        var createButton = (Button)this.FindName($"planCreate{a}");
        createButton.Visibility = Visibility.Visible;
        var editButton = (Button)this.FindName($"planEdit{a}");
        editButton.Visibility = Visibility.Collapsed;
        var startButton = (Button)this.FindName($"planStart{a}");
        startButton.Visibility = Visibility.Collapsed;
        var stopButton = (Button)this.FindName($"planStop{a}");
        stopButton.Visibility = Visibility.Collapsed;
      }

    }
   
    //Plan Page 1 (Description, Method, Kill) Load
    public void showBasicPlanInformation(bool newPlan)
    {
      //int x = tempPlanCache.idPlan;
      // Get Database Information
      var planSettingFrame = (Grid)this.FindName($"plan_grid_1");
      planSettingFrame.Visibility = Visibility.Visible;

      // All Grid Objects
      currentScreen = "planSettingsMain";
      // Get Database Information
      var nameBox = (TextBox)this.FindName($"planNameBox");
      var descriptionBox = (TextBox)this.FindName($"planDescriptionBox");

      var descriptionBox1 = (TextBox)this.FindName($"planProtect1");//Password
      var descriptionBox2 = (TextBox)this.FindName($"planProtect2");//Random Characters
      var descriptionBox3 = (TextBox)this.FindName($"planProtect3");//Delay

      var defaultKillSelect1 = (ComboBox)this.FindName($"killSelect");
      var defaultProtectSelect1 = (ComboBox)this.FindName($"protectSelect");

      var protectSelectAllow = (ToggleButton)this.FindName($"protectSelectAllow");
      var protectSelectPause = (ToggleButton)this.FindName($"protectSelectPause");

      // Disable top menu
      var top1 = (Button)this.FindName($"GlobalPlan");
      top1.IsEnabled = false;
      var top2 = (Button)this.FindName($"GlobalStat");
      top2.IsEnabled = false;
      var top3 = (Button)this.FindName($"GlobalSetting");
      top3.IsEnabled = false;


      // Create default entry or use old entry, then update all of the boxes
      nameBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      descriptionBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      defaultKillSelect1.GetBindingExpression(ComboBox.SelectedIndexProperty).UpdateTarget();
      canPlanProtectionChange = false;
      defaultProtectSelect1.GetBindingExpression(ComboBox.SelectedIndexProperty).UpdateTarget();
      canPlanProtectionChange = true;
      protectSelectAllow.GetBindingExpression(ToggleButton.IsCheckedProperty).UpdateTarget();
      protectSelectPause.GetBindingExpression(ToggleButton.IsCheckedProperty).UpdateTarget();

      // Visibility of various user inputs
      if (descriptionBox1 != null)
      {
        descriptionBox1.Visibility = Visibility.Collapsed;
        descriptionBox2.Visibility = Visibility.Collapsed;
        descriptionBox3.Visibility = Visibility.Collapsed;
        switch (defaultProtectSelect1.SelectedIndex)
        {
          case 0://None
            break;
          case 1://Delaying
            descriptionBox3.Visibility = Visibility.Visible;
            break;
          case 2://Password
            descriptionBox1.Text = "";//Reset to prevent seeing
            descriptionBox1.Visibility = Visibility.Visible;
            break;
          case 3://Random Characters
            descriptionBox2.Visibility = Visibility.Visible;
            break;
          case 4://Forced
            break;
          default:
            break;
        }
      }
    }

    //Plan Page 2 (Apps/Whitelisting/Blacklist)
    public void showAppInformation()
    {
      //int x = tempPlanCache.idPlan;
      currentScreen = "planSettingsApp";
      // Get Database Information
      var planSettingFrame = (Grid)this.FindName($"plan_grid_2");
      planSettingFrame.Visibility = Visibility.Visible;

      // Get Database Information
      var whitelistNameBox = (TextBox)this.FindName($"appNameAllowBox");
      var blacklistNameBox = (TextBox)this.FindName($"appNameBlockBox");
      var whitelistPathBox = (TextBox)this.FindName($"appPathAllowBox");
      var blacklistPathBox = (TextBox)this.FindName($"appPathBlockBox");

      var whitelistNameLabel = (Label)this.FindName($"appNameAllowLabel");
      var blacklistNameLabel = (Label)this.FindName($"appNameBlockLabel");
      var whitelistPathLabel = (Label)this.FindName($"appPathAllowLabel");
      var blacklistPathLabel = (Label)this.FindName($"appPathBlockLabel");

      var listingSelectBox = (ComboBox)this.FindName($"listingSelectBox");

      //tempPlanCache
      bool blockType = (tempPlanCache.blockMethod == 0) ? false : true; // false, means blacklist, true means whitelist

      blacklistNameBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      blacklistPathBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      whitelistNameLabel.Visibility = Visibility.Visible;
      whitelistPathLabel.Visibility = Visibility.Visible;

      if (!blockType)//Blacklisting
      {
        tempPlanCache.allowedNames = "";//Reset
        tempPlanCache.allowedPaths = "";
        listingSelectBox.SelectedIndex = 0;
        whitelistNameBox.Visibility = Visibility.Collapsed;
        whitelistPathBox.Visibility = Visibility.Collapsed;
        whitelistNameLabel.Visibility = Visibility.Collapsed;
        whitelistPathLabel.Visibility = Visibility.Collapsed;
      }
      else// Whitelisting
      {
        listingSelectBox.SelectedIndex = 1;
        whitelistNameBox.Visibility = Visibility.Visible;
        whitelistPathBox.Visibility = Visibility.Visible;
        whitelistNameBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        whitelistPathBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      }

      //Import/Export
      //var activeLabel2 = (Button)this.FindName($"importPlanButton");
      //activeLabel2.Visibility = Visibility.Visible;
      //var activeLabel3 = (Button)this.FindName($"exportPlanButton");
      //activeLabel3.Visibility = Visibility.Visible;
    }

    //Plan Page 3 (Timing)
    public void showTimingInformation()
    {
      currentScreen = "planSettingsTime";
      // Get Database Information
      var planSettingFrame = (Grid)this.FindName($"plan_grid_3");
      planSettingFrame.Visibility = Visibility.Visible;

      // Inputs
      string[] activeDays = tempPlanCache.activeDays.Split("|");

      var timeSelectBox = (ComboBox)this.FindName($"timeSelect");
      var scheduledTimeBox = (TextBox)this.FindName($"timeNormalBox");

      //var timerBox = (TextBox)this.FindName($"timerBox");

      var startTimePomo = (TextBox)this.FindName($"startTimePomoBox");
      var smallBreakPomo = (TextBox)this.FindName($"smallBreakPomoBox");
      var largeBreakPomo = (TextBox)this.FindName($"largeBreakPomoBox");
      var pomoAmount = (TextBox)this.FindName($"pomoAmountBox");
      var pomoDuration = (TextBox)this.FindName($"pomoDurationBox");

      // Labels
      //var timeSelectLabel = (Label)this.FindName($"timeSelectLabel");
      var scheduledTimeLabel = (Label)this.FindName($"timeNormal");
      var startTimePomoLabel = (Label)this.FindName($"startTimePomoLabel");
      var smallBreakPomoLabel = (Label)this.FindName($"smallBreakPomoLabel");
      var largeBreakPomoLabel = (Label)this.FindName($"largeBreakPomoLabel");
      var pomoAmountLabel = (Label)this.FindName($"pomoAmountLabel");
      var pomoDurationLabel = (Label)this.FindName($"pomoDurationLabel");


      //Show Timing Type
      //tempPlanCache.timingMethod = 0;
      //tempPlanCache.activeTime = "2200-2300|0000-2400|1121-2132";
      int timingType = tempPlanCache.timingMethod;

      scheduledTimeBox.Visibility = Visibility.Collapsed;
      //timerBox.Visibility = Visibility.Collapsed;
      startTimePomo.Visibility = Visibility.Collapsed;
      smallBreakPomo.Visibility = Visibility.Collapsed;
      largeBreakPomo.Visibility = Visibility.Collapsed;
      pomoAmount.Visibility = Visibility.Collapsed;
      pomoDuration.Visibility = Visibility.Collapsed;

      startTimePomoLabel.Visibility = Visibility.Collapsed;
      //timeSelectLabel.Visibility = Visibility.Collapsed;
      scheduledTimeLabel.Visibility = Visibility.Collapsed;
      startTimePomo.Visibility = Visibility.Collapsed;
      smallBreakPomoLabel.Visibility = Visibility.Collapsed;
      largeBreakPomoLabel.Visibility = Visibility.Collapsed;
      pomoAmountLabel.Visibility = Visibility.Collapsed;
      pomoDurationLabel.Visibility = Visibility.Collapsed;

      switch (timingType)
      {
        case 0: // Scheduled
          timeSelectBox.SelectedIndex = 0;
          scheduledTimeBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          scheduledTimeBox.Visibility = Visibility.Visible;
          scheduledTimeLabel.Visibility = Visibility.Visible;
          break;
        case 1: //Pomodoro
          timeSelectBox.SelectedIndex = 1;
          startTimePomo.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          smallBreakPomo.GetBindingExpression(TextBox.TextProperty).UpdateTarget(); 
          largeBreakPomo.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          pomoAmount.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          pomoDuration.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          startTimePomo.Visibility = Visibility.Visible;
          smallBreakPomo.Visibility = Visibility.Visible;
          largeBreakPomo.Visibility= Visibility.Visible;
          pomoAmount.Visibility= Visibility.Visible;
          pomoDuration.Visibility= Visibility.Visible;

          startTimePomoLabel.Visibility = Visibility.Visible;          
          startTimePomo.Visibility = Visibility.Visible;
          smallBreakPomoLabel.Visibility = Visibility.Visible;
          largeBreakPomoLabel.Visibility = Visibility.Visible;
          pomoAmountLabel.Visibility = Visibility.Visible;
          pomoDurationLabel.Visibility = Visibility.Visible;
          break;
        case 2:// Timer, not used
          timeSelectBox.SelectedIndex = 2;
          //timerBox.Text = tempPlanCache.activeTime;
          break;
        default:
          timeSelectBox.SelectedIndex = 0;
          break;
      }
      // Show Days Active
      foreach (string cDay in activeDays)
      {
        var cCheck = (CheckBox)this.FindName($"day{cDay}");
        cCheck.IsChecked = true;
      }

      var activeLabel1 = (Button)this.FindName($"backButtonPlanPage3");
      activeLabel1.Visibility = Visibility.Visible;
      var activeLabel4 = (Button)this.FindName($"nextButtonPlanPage3");
      activeLabel4.Visibility = Visibility.Visible;
      //Import/Export
      //var activeLabel2 = (Button)this.FindName($"importPlanButton");
      //activeLabel2.Visibility = Visibility.Visible;
      //var activeLabel3 = (Button)this.FindName($"exportPlanButton");
      //activeLabel3.Visibility = Visibility.Visible;
      //tempPlanCache.pomoDuration = 500;

    }
     
    // Assume that user is on the settings page
    public void displaySettingsInformation()
    {
      // Get Database Information
      // Check for user settings to allow or deny access

      bool canAccessSettings = false;

      List<RootInformation> settingData = BackEnd.parseAppData();

      foreach (RootInformation cSetting in settingData)
      {
        if ((cSetting.attributeName == "Global Protection Enabled") && (cSetting.attributeValue=="0"))
        {
          canAccessSettings = true;
          break;
        }else if ((cSetting.attributeName == "Settings Protection") && (cSetting.attributeValue == "0"))
        {
          canAccessSettings = true;
          break;
        }
      }

      // Setting all on
      if (!canAccessSettings)
      {
        canAccessSettings = !isPlanActive();
      }

      // Various displays
      if (canAccessSettings)// Access Permitted
      {
        currentScreen = "settingsMain";
        var settingFrame = (Grid)this.FindName($"settings_grid");
        settingFrame.Visibility = Visibility.Visible;
        BrushConverter bc = new BrushConverter();
        Brush greenColor = (Brush)bc.ConvertFrom("#31bf58");
        Brush redColor = (Brush)bc.ConvertFrom("#FFFF6347");

        List<int> invisibleAttributes = new List<int>();
        int numSettings = 1;
        int realSettingsIndexer = 0;
        foreach (RootInformation cSetting in settingData)
        {
          realSettingsIndexer++;
          // Delete Excess Entries
          if (numSettings > maxSettings)
          {
            BackEnd.deleteEntrySettings(cSetting.idAttribute);
            settingData.Remove(cSetting);
            continue;
          }

          // Read and parse data
          bool attributeValue = (cSetting.attributeValue == "0") ? false : true;
          bool defaultValue = (cSetting.defaultValue == "0") ? false : true;
          bool canModify = (cSetting.canModify == 0) ? false : true;
          var tButton1 = (ToggleButton)this.FindName($"settingToggle{numSettings}");

          if (!canModify)
          {
            invisibleAttributes.Add(realSettingsIndexer);
            continue;
          }

          // Determine if setting was selected
          if ((attributeValue))
          {
            tButton1.Background = greenColor;
            tButton1.IsChecked = true;
          }
          else // Regular Blocking
          {
            tButton1.Background = redColor;
            tButton1.IsChecked = false;
          }
          tButton1.Visibility = Visibility.Visible;

          //Update Name
          // Show proper buttons
          var activeLabel = (Label)this.FindName($"settingLabel{numSettings}");
          activeLabel.Visibility = Visibility.Visible;
          activeLabel.Content = cSetting.attributeName;

          numSettings++;
        }

        // Make unused settings invisibile
        for (int a = numSettings; a <= maxSettings; a++)
        {
          var activeLabel = (Label)this.FindName($"settingLabel{a}");
          activeLabel.Visibility = Visibility.Collapsed;
          var activeButton = (ToggleButton)this.FindName($"settingToggle{a}");
          activeButton.Visibility = Visibility.Collapsed;
        }
      }
      else // Access Denied
      {
        currentScreen = "blockedSettings";
        var settingFrame = (Grid)this.FindName($"blocked_grid");
        settingFrame.Visibility = Visibility.Visible;
        var activeLabel = (Label)this.FindName($"BlockLabel");
        activeLabel.Content = "You cannot view the Settings page\nuntil all plans are inactive or disabled";
      }
    }

    // Display the statistics screen
    public void displayStatsInformation() {
      // Get Database Information
      // Check for user settings to allow or deny access

      bool canAccessSettings = false;

      List<RootInformation> settingData = BackEnd.parseAppData();

      foreach (RootInformation cSetting in settingData)
      {
        if ((cSetting.attributeName == "Global Protection Enabled") && (cSetting.attributeValue == "0"))
        {
          canAccessSettings = true;
          break;
        }
        else if ((cSetting.attributeName == "Statistics Protection") && (cSetting.attributeValue == "0"))
        {
          canAccessSettings = true;
          break;
        }
      }

      // Setting all on
      if (!canAccessSettings)
      {
        canAccessSettings = !isPlanActive();
      }

      // Various displays
      if (canAccessSettings)
      {
        currentScreen = "statsMain";
        var statFrame = (Grid)this.FindName($"stats_grid");
        statFrame.Visibility = Visibility.Visible;
        var timePeriodSelect = (ComboBoxItem)this.FindName($"timeChoice1");//Select Default
        timePeriodSelect.IsSelected = true;

        List<int> invisibleApps = new List<int>(); // Perhaps add some system apps
        int numStats = 1;
        int realStatsIndexer = 0;
        List<StatsInformation> statData = BackEnd.parseStatistics();
        foreach (StatsInformation cStat in statData)
        {
          realStatsIndexer++;
          // Do not show excess entries
          if (numStats > maxStats)
          {
            //BackEnd.deleteEntryStats(cStat.idApp);
            //statData.Remove(cStat);
            continue;
          }

          // Show proper buttons
          var resetButton = (Button)this.FindName($"statReset{numStats}");
          resetButton.Visibility = Visibility.Visible;
          var activeLabel1 = (Label)this.FindName($"statName{numStats}");
          activeLabel1.Visibility = Visibility.Visible;
          activeLabel1.Content = cStat.processName;
          var activeLabel2 = (Label)this.FindName($"statPath{numStats}");
          activeLabel2.Visibility = Visibility.Visible;
          activeLabel2.Content = cStat.processPath;
          var activeLabel3 = (Label)this.FindName($"statTime{numStats}");
          activeLabel3.Visibility = Visibility.Visible;

          // Update Values for time
          numStats++;
        }
        // Make unused stats invisibile
        for (int a = numStats; a <= maxStats; a++)
        {
          // Show proper buttons
          var resetButton = (Button)this.FindName($"statReset{a}");
          resetButton.Visibility = Visibility.Collapsed;
          var activeLabel1 = (Label)this.FindName($"statName{a}");
          activeLabel1.Visibility = Visibility.Collapsed;
          var activeLabel2 = (Label)this.FindName($"statPath{a}");
          activeLabel2.Visibility = Visibility.Collapsed;
          var activeLabel3 = (Label)this.FindName($"statTime{a}");
          activeLabel3.Visibility = Visibility.Collapsed;
        }

        int numStats2 = 1;
        int maxEnumeration = (statData.Count() > maxStats) ? maxStats : statData.Count();
        foreach (StatsInformation cStat in statData)
        {
          if (numStats2 <= maxEnumeration)
          {
            var activeStat3 = (Label)this.FindName($"statTime{numStats2}");
            // Read and parse data
            int processCDay = cStat.processCDay;
            double hours = Math.Floor(processCDay / 3600.0);
            double minutes = Math.Floor(processCDay / 60.0);
            int seconds = processCDay % 60;
            activeStat3.Content = $"{hours}H {minutes}M {seconds}S";
          }
          numStats2++;
        }
      } else
      { // Access Denied
        currentScreen = "blockedStats";
        var settingFrame = (Grid)this.FindName($"blocked_grid");
        settingFrame.Visibility = Visibility.Visible;
        var activeLabel = (Label)this.FindName($"BlockLabel");
        activeLabel.Content = "You cannot view the Statistics page\nuntil all plans are inactive or disabled";
      }

    }

    // Change time view to one week instead of one day
    public void showWeekStat(List<StatsInformation> statData)
    {
      int numStats = 1;
      foreach (StatsInformation cStat in statData)
      {
        if (numStats > maxStats)//Ignore excess entries
        {
          continue;
        }
        var activeStat = (Label)this.FindName($"statTime{numStats}");
        if (activeStat.Visibility == Visibility.Visible)
        {
          // Read and parse data
          int totalTime = 0;
          string[] processCWeek = cStat.processPastTimes.Split("|");
          foreach (string b in processCWeek)
          {
            totalTime += Int32.Parse(b);
          }
          double hours = Math.Floor(totalTime / 3600.0);
          double minutes = Math.Floor((totalTime - hours*3600) / 60.0);
          int seconds = totalTime % 60;
          activeStat.Content = $"{hours}H {minutes}M {seconds}S";

        }
        numStats++;
      }
    }

    //
    //Timing Methods
    //

    // Check if within current time limit (Scheduled Times)
    public static bool CheckScheduledTime(string[] scheduledDays, string[] scheduledTime)
    {
      try
      {
        // Get time and parse
        bool withinTime = false;
        bool withinDay = false;

        // Look at day
        int currentDay = (int)DateTime.Today.DayOfWeek;
        if (currentDay == 0) { currentDay = 7; }

        Console.WriteLine(currentDay);

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

        string currentTime = DateTime.Now.ToString("HHmmss");

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
      }catch(Exception e)
      {
        Console.WriteLine(e.ToString());
        return false;
      }
    }

    // Check if within current time limit
    public static bool CheckPomoTime(string[] scheduledDays, string pomoAmount, string pomoDuration, string pomoSetNumber, string pomoSmallBreak, string pomoLargeBreak, string pomoScheduledStart)
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

      string currentTime = DateTime.Now.ToString("HHmmss");

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
    public static string parseTime(int originalTime, double timeOffset)
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


    // Unrelated Event Handlers
    // Toggle Button
    public void HandleCheck(object sender, RoutedEventArgs e)
    {
      ToggleButton tButton = sender as ToggleButton;
      tButton.Content = "On";
    }

    // Toggle Button
    public void HandleUnchecked(object sender, RoutedEventArgs e)
    {
      ToggleButton tButton = sender as ToggleButton;
      tButton.Content = "Off";
    }

  }
}

