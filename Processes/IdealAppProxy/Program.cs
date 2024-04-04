using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Configuration;

// Description: 
// Keep Alive Process for IdealApp
// Checks and restarts IdealApp process

namespace Primary
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Start Debugging");//Debugging
      new FindHostedProcess();
      Console.WriteLine("End Debugging");//Debugging
    }
  }

  //Class where things are actually done
  class FindHostedProcess
  {
    public Timer MyTimer { get; set; }
    private Process _realProcess;

    // Default Directories
    readonly string rootDirectory = ConfigurationManager.AppSettings["Root_Directory"];
    readonly string secondaryDirectory = ConfigurationManager.AppSettings["IA_Proxy_Directory"];
    readonly string intermediateDirectory = ConfigurationManager.AppSettings["IA_Spawn_Proxy_Directory"];
    readonly string intermediateBackwardDirectory = ConfigurationManager.AppSettings["IA_Spawn_Main_Directory"];
    readonly string primaryDirectory = ConfigurationManager.AppSettings["IA_Directory"];
    readonly string planInfoDirectory = ConfigurationManager.AppSettings["Root_Directory"] + "plans.db";
    readonly string statsInfoDirectory = ConfigurationManager.AppSettings["Root_Directory"] + "stats.db";
    readonly string appInfoDirectory = ConfigurationManager.AppSettings["Root_Directory"] + "root.db";

    public FindHostedProcess()
    {
      /* Sleep Method (Perpetual) */
      while (true)
      {
        checkProcesses();
        Thread.Sleep(450); 
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
          else
          {
            Console.WriteLine("Compatible App Detected");
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

