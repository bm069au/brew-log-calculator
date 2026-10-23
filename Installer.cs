using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

public class Setup : Form {
    readonly string target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Brew Log Calculator");
    Button install = new Button();
    Label status = new Label();
    CheckBox desktop = new CheckBox();
    bool completed;
    public Setup() {
        AppBranding.SetIcon(this);
        Text="Install Brew Log & Calculator";Size=new Size(650,390);MinimumSize=Size;MaximumSize=Size;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.White;AutoScaleMode=AutoScaleMode.Dpi;
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22),ColumnCount=1,RowCount=6};Controls.Add(panel);
        panel.Controls.Add(new Label{Text="Brew Log & Calculator",Font=new Font("Segoe UI",18,FontStyle.Bold),Dock=DockStyle.Fill,AutoSize=true});
        panel.Controls.Add(new Label{Text="A small, offline recipe and hydrometer log for ginger beer, cider, beer, wine, mead and other brews.",Dock=DockStyle.Fill,AutoSize=true});
        panel.Controls.Add(new Label{Text="Install to:\n"+target+"\n\nBatch data stays beside the program in BrewLog.txt. Existing batch data is kept when reinstalling.",Dock=DockStyle.Fill,AutoSize=true,Padding=new Padding(0,12,0,8)});
        desktop.Text="Create a desktop shortcut";desktop.Checked=true;desktop.AutoSize=true;panel.Controls.Add(desktop);
        status.AutoSize=true;status.Dock=DockStyle.Fill;panel.Controls.Add(status);
        install.Text="Install";install.Size=new Size(140,36);install.Anchor=AnchorStyles.Right;install.Click+=delegate{Install();};panel.Controls.Add(install);
    }
    static void GrantDataWrite(string file) {
        if(!File.Exists(file))File.WriteAllText(file,"",new UTF8Encoding(false));
        FileSecurity acl=File.GetAccessControl(file);
        acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid,null),FileSystemRights.Read|FileSystemRights.Write|FileSystemRights.Synchronize,AccessControlType.Allow));
        File.SetAccessControl(file,acl);
    }
    static void Shortcut(string path,string exe,string description) {
        Type t=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(t);
        object link=t.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});
        Type lt=link.GetType();lt.InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{exe});lt.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,link,new object[]{Path.GetDirectoryName(exe)});lt.InvokeMember("Description",BindingFlags.SetProperty,null,link,new object[]{description});lt.InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);
    }
    void Install() {
        if(completed){Close();return;}
        install.Enabled=false;
        try {
            Directory.CreateDirectory(target);
            using(var payload=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            using(var zip=new ZipArchive(payload,ZipArchiveMode.Read))foreach(var e in zip.Entries){
                if(e.FullName!="BrewLog.exe"&&e.FullName!="README.txt")throw new InvalidDataException("Unexpected installer entry");
                using(var input=e.Open())using(var output=File.Create(Path.Combine(target,e.FullName)))input.CopyTo(output);
            }
            GrantDataWrite(Path.Combine(target,"BrewLog.txt"));GrantDataWrite(Path.Combine(target,"BrewLog.txt.bak"));
            string programs=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),"Brew Log & Calculator.lnk");
            Shortcut(programs,Path.Combine(target,"BrewLog.exe"),"Offline brewing log and alcohol estimate");
            string desk=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),"Brew Log & Calculator.lnk");
            if(desktop.Checked)Shortcut(desk,Path.Combine(target,"BrewLog.exe"),"Offline brewing log and alcohol estimate");
            string uninstall=@"$ErrorActionPreference = 'Stop'
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process -FilePath (Join-Path $PSHOME 'powershell.exe') -Verb RunAs -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('""' + $PSCommandPath + '""'))
    exit
}
$root = $PSScriptRoot
$exe = Join-Path $root 'BrewLog.exe'
if (Get-Process BrewLog -ErrorAction SilentlyContinue) { throw 'Close Brew Log before uninstalling.' }
foreach ($name in @('BrewLog.exe','README.txt')) { $file = Join-Path $root $name; if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file } }
foreach ($folder in @([Environment]::GetFolderPath('CommonPrograms'),[Environment]::GetFolderPath('CommonDesktopDirectory'))) { $link = Join-Path $folder 'Brew Log & Calculator.lnk'; if (Test-Path -LiteralPath $link) { Remove-Item -LiteralPath $link } }
Remove-Item -LiteralPath 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\BrewLogCalculator' -ErrorAction SilentlyContinue
Write-Host ('Removed the app. Your BrewLog.txt and .bak are kept in ' + $root)
Remove-Item -LiteralPath $PSCommandPath
";
            string script=Path.Combine(target,"Uninstall.ps1");File.WriteAllText(script,uninstall,new UTF8Encoding(true));
            using(RegistryKey key=Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BrewLogCalculator")) {
                key.SetValue("DisplayName","Brew Log & Calculator");key.SetValue("DisplayVersion","1.0.1");key.SetValue("Publisher","Brett Martin Moore");key.SetValue("InstallLocation",target);key.SetValue("DisplayIcon",Path.Combine(target,"BrewLog.exe"));
                string ps=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe");key.SetValue("UninstallString","\""+ps+"\" -NoProfile -ExecutionPolicy Bypass -File \""+script+"\"");key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);
            }
            status.Text="Installed. Open Brew Log & Calculator from Start or your desktop.\nNormal use does not require administrator rights.";completed=true;install.Text="Close";install.Enabled=true;
        }catch(Exception e){status.Text="Install did not finish: "+e.Message;install.Enabled=true;}
    }
    [STAThread] public static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Setup());}
}
