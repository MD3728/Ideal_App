namespace ServiceTest
{
  partial class ProjectInstaller
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
      this.aiservice = new System.ServiceProcess.ServiceInstaller();
      // 
      // serviceProcessInstaller1
      // 
      this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
      this.serviceProcessInstaller1.Password = null;
      this.serviceProcessInstaller1.Username = null;
      // 
      // aiservice
      // 
      this.aiservice.Description = "Service for IdealApp";
      this.aiservice.DisplayName = "AppIdealService";
      this.aiservice.ServiceName = "ServList";
      this.aiservice.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller1_AfterInstall);
      // 
      // ProjectInstaller
      // 
      this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.aiservice});

    }

    #endregion

    private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
    private System.ServiceProcess.ServiceInstaller aiservice;
  }
}