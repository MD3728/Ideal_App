# IdealApp

<!-- Top -->
<a name="readme-top"></a>

<!-- Title -->
<br />
<div align="center">
  <h3 align="center">IdealApp</h3>
</div>

<!-- Table of Contents -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#about-the-project">About</a></li>
    <li><a href="#getting-started">Installation</a></li>
    <li><a href="#usage">Usage</a></li>
  </ol>
</details>

<!-- About -->
## About
Project Link: [https://github.com/MD3728/Ideal_App](https://github.com/MD3728/Ideal_App)
A free time management app built for maximum security.  
Version 2.0.0            

# Built With

* .NET 7.0 (Runtime Required)
* WPF

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- GETTING STARTED -->
### Getting Started and Installation
Follow the steps below to run the project locally.    

<h1>NuGet Dependencies:</h1>

1. IdealApp Main: Dapper, System.Data.SQLite.Core, System.Configuration.ConfigurationManager, System.Net.Http.Headers
2. IdealApp Secondary: N/A
3. IdealApp Service: Dapper, System.Data.SQLite.Core, System.Configuration.ConfigurationManager, WindowsFirewallHelper
4. IdealApp GUI (IA_GUI): System.Security.Cryptography, System.Data.SQLite.Core, System.Configuration.ConfigurationManager
<br />

<h1>Setting Up IdealApp:</h1>

1. Download the entire repository and uncompress (unzip)
2. Install the .NET 7.0 runtime, this will be packaged with the app at a later date
3. Move the folder to any location that has only read access for regular users, e.g. C:\Program Files   
4. Go to the service folder and install using your preferred method (e.g. installutil.exe in VS 2022)
5. Create a task scheduler task to run primary on startup and login of your preferred user
<br />

<h1>Databases</h1><br/>

1. This project uses SQLite for all storage, so ensure that the files are stored in a proper location
2. Defaults are included in the 'Plans' folder
3. The database is extra secure in places where the user does not have write access, though this will limit functionality    
<br/>

<h1>Troubleshooting:</h1><br/>   

<strong>Instructions If Service Fails to Install</strong> 

1. Go to services with administrator privileges (services.msc) 
2. Find any service name identical to 'ServList' or 'AppIdealService'  
3. Delete using 'sc.exe delete "ServiceName"' 
4. Restart the computer or manually start the service

<p align="right">(<a href="#readme-top">back to top</a>)</p>

