using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// Description: 
// Keep Alive Process for IdealApp
// Checks and restarts IdealApp process

//Locations:
//Intermediate            "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\SpawnProxy\\bin\\Debug\\net7.0\\Intermediate.exe"
//Intermediate Backward   "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\SpawnMain\\bin\\Debug\\net7.0\\Intermediate Backward.exe"
//Secondary               "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealAppProxy\\bin\\Debug\\net7.0\\Secondary.exe"
//Primary                 "C:\\Users\\MD3728\\Documents\\Programming\\C#\\Ideal_App\\IdealApp Processes\\IdealApp\\bin\\Debug\\net7.0\\Primary.exe"

namespace Primary
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Started");
      FindHostedProcess placeholder = new FindHostedProcess();
      Console.WriteLine("Ended");
    }
  }

  //Class for getting ApplicationFrameHost (UWP) application information
  public class WinAPIFunctions
  {
    //Used to get Handle for Foreground Window
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetForegroundWindow();

    //Used to get ID of any Window
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

    public static int GetWindowProcessId(IntPtr hwnd)
    {
      int pid;
      GetWindowThreadProcessId(hwnd, out pid);
      return pid;
    }

    public static IntPtr GetforegroundWindow()
    {
      return GetForegroundWindow();
    }
  }

  //Class where things are actually done
  class FindHostedProcess
  {
    public Timer MyTimer { get; set; }
    private Process _realProcess;

    // Default Directories
    const string rootDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\IdealApp Processes\";
    const string intermediateDirectory = rootDirectory + "SpawnProxy\\bin\\Debug\\net7.0\\Intermediate.exe";
    const string intermediateBackwardDirectory = rootDirectory + "SpawnMain\\bin\\Debug\\net7.0\\Intermediate Backward.exe";
    const string secondaryDirectory = rootDirectory + "IdealAppProxy\\bin\\Debug\\net7.0\\Secondary.exe";
    const string primaryDirectory = rootDirectory + "IdealApp\\bin\\Debug\\net7.0\\Primary.exe";
    const string planInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\plans.db";
    const string statsInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\stats.db";
    const string appInfoDirectory = @"C:\Users\MD3728\Documents\Programming\C#\Ideal_App\Plans\root.db";

    public FindHostedProcess()
    {
      /* Sleep Method (Perpetual) */
      while (true)
      {
        checkProcesses();
        Thread.Sleep(3000); // Sleep for 1 Second
      }
    }

    // Primary Function For Checking if other IdealApp process are running and restarts
    private void checkProcesses()
    {
      // Check for other processes
      try
      {
        Process[] primaryProcess = Process.GetProcessesByName("Primary");
        Process[] intermediate1 = Process.GetProcessesByName("Intermediate");
        Process[] intermediate2 = Process.GetProcessesByName("Intermediate Backward");
        if ((primaryProcess.Length == 0) && (intermediate1.Length == 0) && (intermediate2.Length == 0))
        {
          Console.WriteLine("Compatible App Not Detected");
          Process.Start(intermediateBackwardDirectory);
        }
        else
        {
          bool processExists = false;
          // Check for second process
          foreach (Process p in primaryProcess)
          {
            if (processExists) { break; }
            string auxFilePath = p.MainModule.FileName;
            if (auxFilePath == primaryDirectory)
            {
              processExists = true;
              break;
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
              break;
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
              break;
            }
          }
          // Start if no process is found
          if (!processExists)
          {
            Console.WriteLine("Compatible App Not Detected");
            Process.Start(intermediateBackwardDirectory);
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.ToString());
      }
    }

    // Function for getting proper information on UWP Apps
    private Process getRealProcess(Process foregroundProcess)
    {
      WinAPIFunctions.EnumChildWindows(foregroundProcess.MainWindowHandle, ChildWindowCallback, IntPtr.Zero);
      return _realProcess;
    }

    //Part of function above
    private bool ChildWindowCallback(IntPtr hwnd, IntPtr lparam)
    {
      var process = Process.GetProcessById(WinAPIFunctions.GetWindowProcessId(hwnd));
      if (process.ProcessName != "ApplicationFrameHost")
      {
        _realProcess = process;
      }
      return true;
    }
  }
}

