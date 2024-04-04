using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Reflection;

// Non-default dependencies that are necessary for this to run:
// System.Security.Cryptography;
// System.Configuration.ConfigurationManager; (Backend)
// System.Data.SQLite.Core; (Backend)
// Dapper (Backend)
// microsoft.aspnet.webapi.client (For Time API Request)

// Add these to Properties -> Build -> Output -> prebuild event if "file cannot be moved" error is seen during compilation
// if exist "$(TargetPath).locked" del "$(TargetPath).locked"
// if exist "$(TargetPath)" if not exist "$(TargetPath).locked" move "$(TargetPath)" "$(TargetPath).locked"  

namespace IA_GUI
{

  public partial class MainWindow : Window
  {
    // Globals
    public string currentScreen = "None";
    public RootInformation rootSettings = new RootInformation();
    public int currentPlanIndex = 1; // Keeps track of current plan
    public PlanInformation tempPlanCache = new PlanInformation(); // Most up to date plan infromation
    public string randomChars = "";

    // Screen Transition Indicators
    public bool inScreenTransition = false; // Prevents multiple screen changes at once
    public Storyboard fadeOut;
    public Storyboard fadeIn;
    public String previousScreen, nextScreen;//Keeps track of previous and next screen for proper transitioning

    // Event Suppressor (During Initial App Launch)
    public bool canPlanProtectionChange = true; // Prevents selection event from occuring

    // Constants for GUI Settings
    public const int maxSettings = 9;
    public const int maxStats = 11;

    // Constants for Color
    Brush greenColor;
    Brush redColor;

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

    // Constructor
    public MainWindow()
    {
      // Boilerplate
      InitializeComponent();
    }

    // Sets up red and green colors for main plan screen
    public void setUpColors()
    {
      // Set up colors
      BrushConverter bc = new BrushConverter();
      greenColor = (Brush)bc.ConvertFrom("#31bf58");
      redColor = (Brush)bc.ConvertFrom("#FFFF6347");
    }

    // Run on window load
    public void defaultTransition(object sender, RoutedEventArgs e)
    {
      fadeOut = Resources["FadeOut"] as Storyboard;
      fadeIn = Resources["FadeIn"] as Storyboard;
      fadeOut.Completed += finishFade;
      fadeIn.Completed += transitionComplete;
      // Set up entry into main plan screen
      all_plans_grid.Visibility = Visibility.Collapsed;
      currentScreen = "all_plans_grid";
      preloadMainPlanScreen(null, null);
    }


    ///
    /// Screen Transition Animation
    ///


    // fadeIn Event Handler
    public void transitionComplete(object sender, EventArgs e)
    {
      inScreenTransition = false;
      GlobalHeader.IsEnabled = true;
    }

    // Half-way Event Handler (fadeOut)
    public void finishFade(object sender, EventArgs e)
    {
      var originalFrame = (Grid)FindName(previousScreen);
      var finalFrame = (Grid)FindName(nextScreen);
      if (nextScreen == "all_plans_grid")
      {
        // Enable the top menu
        GlobalPlan.IsEnabled = true;
        GlobalStat.IsEnabled = true;
        GlobalSetting.IsEnabled = true;
        GlobalStat.Visibility = Visibility.Visible;
        GlobalSetting.Visibility = Visibility.Visible;
      }
      else if (nextScreen == "plan_grid_1")
      {
        // Disable top menu
        GlobalPlan.IsEnabled = false;
        GlobalStat.IsEnabled = false;
        GlobalSetting.IsEnabled = false;
        GlobalStat.Visibility = Visibility.Hidden;
        GlobalSetting.Visibility = Visibility.Hidden;
      }
      originalFrame.Visibility = Visibility.Collapsed;
      finalFrame.Visibility = Visibility.Visible;
      fadeIn.Begin(finalFrame);
    }

    // Transition from one screen to another (fade out, fade in)
    public void transitionScreenFade(String initialScreen, String finalScreen)
    {
      if (!inScreenTransition)
      {
        inScreenTransition = true;
        GlobalHeader.IsEnabled = false;
        var originalFrame = (Grid)FindName(initialScreen);
        previousScreen = initialScreen;
        nextScreen = finalScreen;
        fadeOut.Begin(originalFrame);
      }
    }


    ///
    /// Preliminary Loading Process
    ///


    // Prepares entry into the initial screen (all plans)
    public void preloadMainPlanScreen(object sender, RoutedEventArgs e)
    {
      if (inScreenTransition) { return; }
      // Attempt to remove event handlers from password submission
      try { PasswordSubmit.Click -= new RoutedEventHandler(transitionToDelete); } catch (Exception ex) { }
      try { PasswordSubmit.Click -= new RoutedEventHandler(transitionToEdit); } catch (Exception ex) { }
      try { PasswordSubmit.Click -= new RoutedEventHandler(transitionToStop); } catch (Exception ex) { }

      // Screen transition
      switch (currentScreen)
      {
        case "settings_grid":
          saveSettingsData();
          transitionScreenFade("settings_grid", "all_plans_grid");
          break;
        default:
          transitionScreenFade(currentScreen, "all_plans_grid"); break;

      }
      showAllPlans();
    }

    // Prepares entry into statistics screen
    public void preloadStatsScreen(object sender, RoutedEventArgs e)
    {
      if (inScreenTransition) { return; }
      
      switch (currentScreen)
      {
        case "settings_grid":
          saveSettingsData();
          transitionScreenFade("settings_grid", "stats_grid");
          break;
        default:
          transitionScreenFade(currentScreen, "stats_grid"); break;
      }
      showStatsInformation();
    }

    // Prepares entry into global settings screen
    public void preloadSettingsScreen(object sender, RoutedEventArgs e)
    {
      if (inScreenTransition) { return; }
      
      switch (currentScreen)
      {
        case "settings_grid":
          saveSettingsData();
          transitionScreenFade("settings_grid", "settings_grid");
          break;
        default:
          transitionScreenFade(currentScreen, "settings_grid"); break;
      }
      showSettingsInformation();
    }

    // Prepares for entry into the general plan information screen (#1)
    public void preloadFirstPlanScreen(object sender, RoutedEventArgs e)
    {
      
      if (inScreenTransition) { return; }
      // Necessary for startup
      Button originButton = sender as Button;
      string contents = (string)originButton.Name;

      // Transition to screen
      switch (currentScreen)
      {
        default:
          transitionScreenFade(currentScreen, "plan_grid_1"); break;
      }

      // Determine whether to create a new plan
      try
      {
        if (contents.Remove(10) == "planCreate")// New Plan (from all_plans_grid screen)
        {
          tempPlanCache = new PlanInformation();
          showBasicPlanInformation(true);
          BackEnd.addEntryPlans(tempPlanCache);
          tempPlanCache = BackEnd.findLastEntryPlan();
        }
        else
        {
          showBasicPlanInformation(true);
          BackEnd.updateEntryPlans(tempPlanCache);
        }
      }
      catch (Exception ex)
      {
        showBasicPlanInformation(true);
        BackEnd.updateEntryPlans(tempPlanCache);
      }
    }

    // Prepares for entry into block information screen
    public void preloadSecondPlanScreen(object sender, RoutedEventArgs e)
    {
      if (inScreenTransition) { return; }
      //Update Password Information
      var descriptionBox1 = (TextBox)FindName($"planProtect1");
      var defaultProtectSelect1 = (ComboBox)FindName($"protectSelect");
      if (defaultProtectSelect1.SelectedIndex == 2)//Password Input
      {
        if (!string.IsNullOrWhiteSpace(descriptionBox1.Text))
        {
          tempPlanCache.protectionActivePwd = BackEnd.hashPassword(descriptionBox1.Text);
        }
      }

      // General Update
      BackEnd.updateEntryPlans(tempPlanCache);

      // Transition to screen
      switch (currentScreen)
      {
        case "plan_grid_3":
          savePlanTimingSchedule();
          transitionScreenFade(currentScreen, "plan_grid_2");
          break;
        default:
          transitionScreenFade(currentScreen, "plan_grid_2"); break;
      }
      showAppInformation();
    }

    // Prepares for entry into timing information screen 
    public void preloadThirdPlanScreen(object sender, RoutedEventArgs e)
    {
      if (inScreenTransition) { return; }
      BackEnd.updateEntryPlans(tempPlanCache);
      // Transition to screen
      switch (currentScreen)
      {
        default:
          transitionScreenFade(currentScreen, "plan_grid_3"); break;
      }
      showTimingInformation();
    }

    // Prepares for main plan (from third plan screen)
    public void preloadReturnToMainPlan(object sender, RoutedEventArgs e)
    {
      if (inScreenTransition) { return; }
      BackEnd.updateEntryPlans(tempPlanCache);
      // Transition to screen
      switch (currentScreen)
      {
        case "plan_grid_3":
          savePlanTimingSchedule();
          transitionScreenFade(currentScreen, "all_plans_grid");
          break;
        default:
          transitionScreenFade(currentScreen,"all_plans_grid"); break;
      }

      showAllPlans();
    }

    // From plan screen 3
    public void savePlanTimingSchedule()
    {
      // Update the checkboxes from the third screen
      string timeString = "";
      for (int a = 1; a < 8; a++)
      {
        var cCheck = (CheckBox)FindName($"day{a}");
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
    }

    // Saves settings state after leaving
    public void saveSettingsData()
    {
      // Save settings information
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
        var CSButton = (ToggleButton)FindName($"settingToggle{buttonCounter}");
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
    }


    ///
    /// Screen Loading Process
    ///


    // Main Plan Page (Home)
    public void showAllPlans()
    {
      GlobalPlan.IsSelected = true;
      currentScreen = "all_plans_grid";
      // Get Database Information
      List<PlanInformation> planData = BackEnd.parsePlans();
      setUpColors();//Colors must load in time

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
          }
          catch (Exception e)
          {
            active = false;
          }
        }

        // Name
        var activeLabel = (Label)FindName($"planActive{numPlans}");
        var nameLabel = (Label)FindName($"planName{numPlans}");
        var conditionLabel = (Label)FindName($"planWhitelist{numPlans}");
        var actionLabel = (Label)FindName($"planFirewall{numPlans}");
        var startButton = (Button)FindName($"planStart{numPlans}");
        var stopButton = (Button)FindName($"planStop{numPlans}");
        var whitelistLabel = (Label)FindName($"planWhitelist{numPlans}");
        var firewallLabel = (Label)FindName($"planFirewall{numPlans}");

        nameLabel.Content = cPlan.planName;

        //Active Display
        if (active)
        {
          activeLabel.Background = greenColor;
          activeLabel.Content = "Yes";
        }
        else
        {
          activeLabel.Background = redColor;
          activeLabel.Content = "No";
        }

        // Listing Display
        conditionLabel.Content = whitelist ? "Whitelisting" : "Blacklisting";

        // Enforcement
        actionLabel.Content = firewall ? "Firewall" : "Kill Process";

        // Show proper buttons
        if (cPlan.protectionInactiveAllow == 0)//Enabled
        {
          // Start or stop the plans
          if (cPlan.currentlyActive == 0)
          {
            startButton.Visibility = Visibility.Visible;
            stopButton.Visibility = Visibility.Collapsed;
          }
          else
          {
            startButton.Visibility = Visibility.Collapsed;
            stopButton.Visibility = Visibility.Visible;
          }
        }
        else//Disabled
        {
          startButton.Visibility = Visibility.Collapsed;
          stopButton.Visibility = Visibility.Collapsed;
        }
        activeLabel.Visibility = Visibility.Visible;
        whitelistLabel.Visibility = Visibility.Visible;
        firewallLabel.Visibility = Visibility.Visible;
        var deleteButton = (Button)FindName($"planDelete{numPlans}");
        deleteButton.Visibility = Visibility.Visible;
        var createButton = (Button)FindName($"planCreate{numPlans}");
        createButton.Visibility = Visibility.Collapsed;
        var editButton = (Button)FindName($"planEdit{numPlans}");
        editButton.Visibility = Visibility.Visible;

        numPlans++;
      }

      // Make unused plans invisible
      bool createShown = false;
      for (int a = numPlans; a <= 12; a++)
      {
        var nameLabel = (Label)FindName($"planName{a}");
        nameLabel.Content = "Empty";
        var activeLabel = (Label)FindName($"planActive{a}");
        activeLabel.Visibility = (System.Windows.Visibility)Enum.Parse(typeof(System.Windows.Visibility), "Collapsed");
        var whitelistLabel = (Label)FindName($"planWhitelist{a}");
        whitelistLabel.Visibility = Visibility.Collapsed;
        var firewallLabel = (Label)FindName($"planFirewall{a}");
        firewallLabel.Visibility = Visibility.Collapsed;
        var deleteButton = (Button)FindName($"planDelete{a}");
        deleteButton.Visibility = Visibility.Collapsed;
        if (!createShown)
        {
          var createButton = (Button)FindName($"planCreate{a}");
          createButton.Visibility = Visibility.Visible;
          createShown = true;
        }
        else
        {
          var createButton = (Button)FindName($"planCreate{a}");
          createButton.Visibility = Visibility.Collapsed;
        }
        var editButton = (Button)FindName($"planEdit{a}");
        editButton.Visibility = Visibility.Collapsed;
        var startButton = (Button)FindName($"planStart{a}");
        startButton.Visibility = Visibility.Collapsed;
        var stopButton = (Button)FindName($"planStop{a}");
        stopButton.Visibility = Visibility.Collapsed;
      }

    }

    //Plan Page 1 (Description, Method, Kill) Load
    public void showBasicPlanInformation(bool newPlan)
    {
      GlobalPlan.IsSelected = true;
      // All Grid Objects
      currentScreen = "plan_grid_1";

      // Create default entry or use old entry, then update all of the boxes
      planNameBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      planDescriptionBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      killSelect.GetBindingExpression(ComboBox.SelectedIndexProperty).UpdateTarget();
      canPlanProtectionChange = false;
      protectSelect.GetBindingExpression(ComboBox.SelectedIndexProperty).UpdateTarget();
      canPlanProtectionChange = true;
      protectSelectAllow.GetBindingExpression(ToggleButton.IsCheckedProperty).UpdateTarget();
      protectSelectPause.GetBindingExpression(ToggleButton.IsCheckedProperty).UpdateTarget();

      // Visibility of various user inputs
      if (planProtect1 != null)
      {
        planProtect1.Visibility = Visibility.Collapsed;
        planProtect2.Visibility = Visibility.Collapsed;
        planProtect3.Visibility = Visibility.Collapsed;
        switch (protectSelect.SelectedIndex)
        {
          case 0://None
            break;
          case 1://Delaying
            planProtect3.Visibility = Visibility.Visible;
            break;
          case 2://Password
            planProtect1.Text = "";//Reset to prevent seeing
            planProtect1.Visibility = Visibility.Visible;
            break;
          case 3://Random Characters
            planProtect2.Visibility = Visibility.Visible;
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
      GlobalPlan.IsSelected = true;
      currentScreen = "plan_grid_2";

      // Determine Visibility
      bool blockType = (tempPlanCache.blockMethod == 0) ? false : true; // false, means blacklist, true means whitelist

      appNameBlockBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      appPathBlockBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      appNameAllowLabel.Visibility = Visibility.Visible;
      appPathAllowLabel.Visibility = Visibility.Visible;

      if (!blockType)//Blacklisting
      {
        tempPlanCache.allowedNames = "";//Reset
        tempPlanCache.allowedPaths = "";
        listingSelectBox.SelectedIndex = 0;
        appNameAllowBox.Visibility = Visibility.Collapsed;
        appPathAllowBox.Visibility = Visibility.Collapsed;
        appNameAllowLabel.Visibility = Visibility.Collapsed;
        appPathAllowLabel.Visibility = Visibility.Collapsed;
        chooseNameWhitelist.Visibility = Visibility.Collapsed;
      }
      else// Whitelisting
      {
        listingSelectBox.SelectedIndex = 1;
        appNameAllowBox.Visibility = Visibility.Visible;
        appPathAllowBox.Visibility = Visibility.Visible;
        chooseNameWhitelist.Visibility = Visibility.Visible;
        appNameAllowBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        appPathAllowBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
      }

      //Import/Export
      //var activeLabel2 = (Button)FindName($"importPlanButton");
      //activeLabel2.Visibility = Visibility.Visible;
      //var activeLabel3 = (Button)FindName($"exportPlanButton");
      //activeLabel3.Visibility = Visibility.Visible;
    }

    //Plan Page 3 (Timing)
    public void showTimingInformation()
    {
      GlobalPlan.IsSelected = true;
      currentScreen = "plan_grid_3";

      // Inputs
      string[] activeDays = tempPlanCache.activeDays.Split("|");

      //Show Timing Type
      //tempPlanCache.timingMethod = 0;
      //tempPlanCache.activeTime = "2200-2300|0000-2400|1121-2132";
      int timingType = tempPlanCache.timingMethod;

      timeNormalBox.Visibility = Visibility.Collapsed;
      //timerBox.Visibility = Visibility.Collapsed;
      startTimePomoBox.Visibility = Visibility.Collapsed;
      smallBreakPomoBox.Visibility = Visibility.Collapsed;
      largeBreakPomoBox.Visibility = Visibility.Collapsed;
      pomoAmountBox.Visibility = Visibility.Collapsed;
      pomoDurationBox.Visibility = Visibility.Collapsed;

      startTimePomoLabel.Visibility = Visibility.Collapsed;
      //timeSelectLabel.Visibility = Visibility.Collapsed;
      timeNormal.Visibility = Visibility.Collapsed;
      startTimePomoBox.Visibility = Visibility.Collapsed;
      smallBreakPomoLabel.Visibility = Visibility.Collapsed;
      largeBreakPomoLabel.Visibility = Visibility.Collapsed;
      pomoAmountLabel.Visibility = Visibility.Collapsed;
      pomoDurationLabel.Visibility = Visibility.Collapsed;

      switch (timingType)
      {
        case 0: // Scheduled
          timeSelect.SelectedIndex = 0;
          timeNormalBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          timeNormalBox.Visibility = Visibility.Visible;
          timeNormal.Visibility = Visibility.Visible;
          break;
        case 1: //Pomodoro
          timeSelect.SelectedIndex = 1;
          startTimePomoBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          smallBreakPomoBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          largeBreakPomoBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          pomoAmountBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          pomoDurationBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
          startTimePomoBox.Visibility = Visibility.Visible;
          smallBreakPomoBox.Visibility = Visibility.Visible;
          largeBreakPomoBox.Visibility = Visibility.Visible;
          pomoAmountBox.Visibility = Visibility.Visible;
          pomoDurationBox.Visibility = Visibility.Visible;

          startTimePomoLabel.Visibility = Visibility.Visible;
          smallBreakPomoLabel.Visibility = Visibility.Visible;
          largeBreakPomoLabel.Visibility = Visibility.Visible;
          pomoAmountLabel.Visibility = Visibility.Visible;
          pomoDurationLabel.Visibility = Visibility.Visible;
          break;
        case 2:// Timer, not used
          timeSelect.SelectedIndex = 2;
          //timerBox.Text = tempPlanCache.activeTime;
          break;
        default:
          timeSelect.SelectedIndex = 0;
          break;
      }
      // Show Days Active
      for (int cDay = 1; cDay <= 7; cDay++)
      {
        var cCheck = (CheckBox)FindName($"day{cDay}");
        cCheck.IsChecked = activeDays.Contains(cDay.ToString());
      }

      // Show navigation buttons
      backButtonPlanPage3.Visibility = Visibility.Visible;
      nextButtonPlanPage3.Visibility = Visibility.Visible;

      //Import/Export
      //var activeLabel2 = (Button)FindName($"importPlanButton");
      //activeLabel2.Visibility = Visibility.Visible;
      //var activeLabel3 = (Button)FindName($"exportPlanButton");
      //activeLabel3.Visibility = Visibility.Visible;

      //tempPlanCache.pomoDuration = 500;
    }

    // Assume that user is on the settings page
    public void showSettingsInformation()
    {
      GlobalSetting.IsSelected = true;
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
        else if ((cSetting.attributeName == "Settings Protection") && (cSetting.attributeValue == "0"))
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
        currentScreen = "settings_grid";

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
          var tButton1 = (ToggleButton)FindName($"settingToggle{numSettings}");

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
          var activeLabel = (Label)FindName($"settingLabel{numSettings}");
          activeLabel.Visibility = Visibility.Visible;
          activeLabel.Content = cSetting.attributeName;

          numSettings++;
        }

        // Make unused settings invisibile
        for (int a = numSettings; a <= maxSettings; a++)
        {
          var activeLabel = (Label)FindName($"settingLabel{a}");
          activeLabel.Visibility = Visibility.Collapsed;
          var activeButton = (ToggleButton)FindName($"settingToggle{a}");
          activeButton.Visibility = Visibility.Collapsed;
        }
      }
      else // Access Denied
      {
        currentScreen = "blockedSettings";
        var settingFrame = (Grid)FindName($"blocked_grid");
        settingFrame.Visibility = Visibility.Visible;
        var activeLabel = (Label)FindName($"BlockLabel");
        activeLabel.Content = "You cannot view the Settings page\nuntil all plans are inactive or disabled";
      }
    }

    // Display the statistics screen
    public void showStatsInformation()
    {
      GlobalStat.IsSelected = true;
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
        currentScreen = "stats_grid";
        var timePeriodSelect = (ComboBoxItem)FindName($"timeChoice1");//Select Default
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
          var resetButton = (Button)FindName($"statReset{numStats}");
          resetButton.Visibility = Visibility.Visible;
          var activeLabel1 = (Label)FindName($"statName{numStats}");
          activeLabel1.Visibility = Visibility.Visible;
          activeLabel1.Content = cStat.processName;
          var activeLabel2 = (Label)FindName($"statPath{numStats}");
          activeLabel2.Visibility = Visibility.Visible;
          activeLabel2.Content = cStat.processPath;
          var activeLabel3 = (Label)FindName($"statTime{numStats}");
          activeLabel3.Visibility = Visibility.Visible;

          // Update Values for time
          numStats++;
        }
        // Make unused stats invisibile
        for (int a = numStats; a <= maxStats; a++)
        {
          // Show proper buttons
          var resetButton = (Button)FindName($"statReset{a}");
          resetButton.Visibility = Visibility.Collapsed;
          var activeLabel1 = (Label)FindName($"statName{a}");
          activeLabel1.Visibility = Visibility.Collapsed;
          var activeLabel2 = (Label)FindName($"statPath{a}");
          activeLabel2.Visibility = Visibility.Collapsed;
          var activeLabel3 = (Label)FindName($"statTime{a}");
          activeLabel3.Visibility = Visibility.Collapsed;
        }

        int numStats2 = 1;
        int maxEnumeration = (statData.Count() > maxStats) ? maxStats : statData.Count();
        foreach (StatsInformation cStat in statData)
        {
          if (numStats2 <= maxEnumeration)
          {
            var activeStat3 = (Label)FindName($"statTime{numStats2}");
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
      { // Access Denied
        currentScreen = "blockedStats";
        var settingFrame = (Grid)FindName($"blocked_grid");
        settingFrame.Visibility = Visibility.Visible;
        var activeLabel = (Label)FindName($"BlockLabel");
        activeLabel.Content = "You cannot view the Statistics page\nuntil all plans are inactive or disabled";
      }

    }


    ///
    /// Helper Methods
    ///


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
        var activeStat = (Label)FindName($"statTime{numStats}");
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
          double minutes = Math.Floor((totalTime - hours * 3600) / 60.0);
          int seconds = totalTime % 60;
          activeStat.Content = $"{hours}H {minutes}M {seconds}S";

        }
        numStats++;
      }
    }

    // Delete Command (From user protection)
    public void deletePlan(object sender, RoutedEventArgs e)
    {
      BackEnd.deleteEntryPlans(tempPlanCache.idPlan);
      preloadMainPlanScreen(sender, e); // Transition to screen
    }

    // Stop Command (From user protection)
    public void stopPlan(object sender, RoutedEventArgs e)
    {
      tempPlanCache.currentlyActive = 0;
      BackEnd.updateActivePlans(tempPlanCache);
      preloadMainPlanScreen(sender, e); // Transition to screen
    }

    // Edit Command (From user protection)
    public void editPlan(object sender, RoutedEventArgs e)
    {
      preloadFirstPlanScreen(sender, e); // Transition to screen
    }

    // Change between whitelist and blacklist
    public void confirmListType(object sender, RoutedEventArgs e)
    {
      ComboBox selectionBox = (ComboBox)FindName($"listingSelectBox");
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
      PlanInformation currentPlan = planData[currentPlanIndex - 1];
      currentPlan.currentlyActive = 1;
      BackEnd.updateActivePlans(currentPlan);
      showAllPlans();//Reload
    }

    // Delete all statistics
    public void resetAllStat(object sender, RoutedEventArgs e)
    {
      BackEnd.deleteAllStats();
      showStatsInformation();
    }

    // Delete selected statistic
    public void deleteCStat(object sender, RoutedEventArgs e)
    {
      Button buttonType = sender as Button;
      string contents = buttonType.Name;
      int currentPlanIndex2 = int.Parse(contents.Remove(0, 9));//statReset#
      List<StatsInformation> statData = BackEnd.parseStatistics();
      StatsInformation currentStat = statData[currentPlanIndex2 - 1];
      BackEnd.deleteEntryStats(currentStat.idApp);
      showStatsInformation();//Reload
    }

    // Adds path/name to whitelist boxes
    public void addPhraseToWhitelist(object sender, RoutedEventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = "All files (*.*)|*.*"; // You can customize the file filter if needed
      if (openFileDialog.ShowDialog() == true)
      {
        string selectedFilePath = openFileDialog.FileName;
        selectedFilePath = selectedFilePath.Substring(selectedFilePath.LastIndexOf("\\") + 1);
        appNameAllowBox.Text = appNameAllowBox.Text + $"\n{selectedFilePath}";
      }
    }

    // Adds path/name to blacklist boxes
    public void addPhraseToBlacklist(object sender, RoutedEventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = "All files (*.*)|*.*"; // You can customize the file filter if needed
      if (openFileDialog.ShowDialog() == true)
      {
        string selectedFilePath = openFileDialog.FileName;
        selectedFilePath = selectedFilePath.Substring(selectedFilePath.LastIndexOf("\\") + 1);
        appNameBlockBox.Text = appNameAllowBox.Text + $"\n{selectedFilePath}";
      }
    }

    // Future Additions
    // Adds path/name to whitelist boxes
    //public void addPathToWhitelist(object sender, RoutedEventArgs e)
    //{
    //  using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
    //  {
    //    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
    //  }
    //}

    //// Adds path/name to blacklist boxes
    //public void addPathToBlacklist(object sender, RoutedEventArgs e)
    //{
    //  DialogResult result = dialog.ShowDialog();
    //  if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
    //  {
    //    string selectedFolder = dialog.SelectedPath;
    //    // Do something with the selected folder path
    //    // For example, display it in a text box:
    //    // textBoxFolderPath.Text = selectedFolder;
    //  }
    //}


    ///
    /// Preliminary automatic detection for all possible password/char/forced screens
    ///


    // Asks for user protection, always called before loading in first plan screen when editing
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
      if ((!isCurrentPlanActive(tempPlanCache)) || (tempPlanCache.protectionActiveType == 0))
      {
        switch (finalDirective)
        {
          case "planStop":
            stopPlan(sender, e);
            break;
          case "planDelete":
            deletePlan(sender, e);
            break;
          case "planEdit":
            editPlan(sender, e);
            break;
        }
      }
      else // Protection Active
      {
        Button submitButton = (Button)FindName($"PasswordSubmit");
        // Deal with protection evels
        switch (tempPlanCache.protectionActiveType)
        {
          case 0: //No protection (Direct Access, See above if statement)
            break;
          case 2: case 3: //Password and Random Characters
            
            // Special case with pausing
            if ((tempPlanCache.returnPausingAllowed) && (finalDirective == "planStop"))
            {
              transitionToStop(sender, e);
              break;
            }
            var inputInstruction = (Label)FindName($"TLabel");
            var userInput = (PasswordBox)FindName($"PasswordInput");
            userInput.Clear(); // Clear previous password

            // Transition Screens
            transitionScreenFade("all_plans_grid", "password_grid");
            currentScreen = "password_grid";

            // Password Case
            if (tempPlanCache.protectionActiveType == 2)
            {
              inputInstruction.Content = "Please Enter the Password for This Set:\n";
            }
            else // Generate random string
            {
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
            // Transition Screen
            transitionScreenFade("all_plans_grid", "blocked_grid");
            currentScreen = "blocked_grid";
            BlockLabel.Content = "You cannot perform this action until\n         this plan becomes inactive\n                    or disabled";
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
        Button submitButton = (Button)FindName($"PasswordSubmit");
        submitButton.Click -= new RoutedEventHandler(transitionToDelete);
        deletePlan(sender, e);
      }
    }

    // Stop Button Command
    public void transitionToStop(object sender, RoutedEventArgs e)
    {
      if (checkLogin())
      {
        Button submitButton = (Button)FindName($"PasswordSubmit");
        submitButton.Click -= new RoutedEventHandler(transitionToStop);
        stopPlan(sender, e);
      }
    }

    // Edit Button Command
    public void transitionToEdit(object sender, RoutedEventArgs e)
    {
      if (checkLogin())
      {
        Button submitButton = (Button)FindName($"PasswordSubmit");
        submitButton.Click -= new RoutedEventHandler(transitionToEdit);
        editPlan(sender, e);
      }
    }

    // Checks the validity of password/chars and returns a boolean
    public bool checkLogin()
    {
      if (tempPlanCache.protectionActiveType == 2)//Password Protection
      {
        var planFrame2 = (PasswordBox)FindName($"PasswordInput");
        string inputPwd = planFrame2.Password;
        if (BackEnd.verifyPassword(inputPwd, tempPlanCache.protectionActivePwd))
        {
          var origScreen1 = (Grid)FindName($"password_grid");//Remove Screen
          origScreen1.Visibility = Visibility.Collapsed;
          return true;
        }
        else
        {
          MessageBox.Show("Incorrect Password", "Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
          return false;
        }
      }
      else if (tempPlanCache.protectionActiveType == 3)//Random Characters
      {
        var planFrame2 = (PasswordBox)FindName($"PasswordInput");
        string inputPwd = planFrame2.Password;
        if (inputPwd == randomChars)
        {
          var origScreen1 = (Grid)FindName($"password_grid");//Remove Screen
          origScreen1.Visibility = Visibility.Collapsed;
          return true;
        }
        else
        {
          MessageBox.Show("Your input has errors!", "Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
          return false;
        }
      }
      return true;
    }


    ///
    /// Timing Methods
    ///


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


    ///
    /// Other Event Handlers
    ///


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

    // Event handler for changing global statistics settings (switching between daily and weekly)
    public void changeConfigurationStat(object sender, RoutedEventArgs e)
    {// Make sure not to select any option by default
      var comboBoxOption1 = (ComboBoxItem)FindName($"timeChoice1");
      bool firstOption = comboBoxOption1.IsSelected;
      List<StatsInformation> statData = BackEnd.parseStatistics();
      if (firstOption)
      {
        ApplyTemplate();
        int numStats2 = 1;
        foreach (StatsInformation cStat in statData)
        {
          if (numStats2 <= statData.Count())
          {
            var activeStat3 = (Label)FindName($"statTime{numStats2}");
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
      var comboBoxOption1 = (ComboBox)FindName($"timeSelect");
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
        var comboBoxOption1 = (ComboBox)FindName($"protectSelect");
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

  }
}

