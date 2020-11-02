using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Management.Infrastructure;

namespace RDS_Abmelder
{
    public class functions
    {
        public static IEnumerable<CimInstance> GetSessions(string connectionBroker, string userdomain, string username)
        {
            IEnumerable<CimInstance> queryInstances = null;

            if (String.IsNullOrWhiteSpace(username))
            {
                return queryInstances;
            }

            if (String.IsNullOrWhiteSpace(connectionBroker))
            {
                MessageBox.Show("Ihr RDS Connection Broker Server konnte nicht automatisch gefunden werden.\nBitte legen Sie ihn manuell über das Einstellungsfenster fest.", "Fehler aufgertreten");
                return queryInstances;
            }

            try
            {
                CimSession cimSession = CimSession.Create(connectionBroker);
                queryInstances =
                    cimSession.QueryInstances(@"root\cimv2",
                        "WQL",
                        "SELECT * FROM Win32_SessionDirectorySession WHERE UserName=\"" + username + "\" AND DomainName=\"" + userdomain + "\"").ToList();
            }
            catch (Exception e) {
                MessageBox.Show(e.Message + Environment.NewLine + Environment.NewLine + "Sie müssen ein Administrator auf '" + GUIStateManager.GUIStateInstance.connectionBroker  + "' (dem Connection Broker Server) sein um Sitzungen abfragen zu können.", "Fehler aufgertreten");
            }

            return queryInstances;
        }

        public static string TryGetConnectionBroker()
        {
            // TODO: Error handling
            string ConnectionBroker = null;
            Microsoft.Win32.RegistryKey HKCU = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Default);
            var FeedsList = HKCU.OpenSubKey(@"Software\Microsoft\workspaces\Feeds");
            string[] Feeds = FeedsList.GetSubKeyNames();
            if (Feeds.Count() == 1)
            {
                var RDSKey = FeedsList.OpenSubKey(Feeds[0]);
                ConnectionBroker = RDSKey.GetValue("WorkspaceId").ToString();
            }

            return ConnectionBroker;
        }

        private static string getLogoffExePath()
        {
            // This function finds logoff.exe on 32-bit Windows systems,
            // on 64-bit Windows systems running this program compiled for 32-bit
            // and on 64-bit systems running a 64-bit binary of this program

            string LogoffPathSystem = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\logoff.exe";
            string LogoffPathSysnative = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Sysnative\logoff.exe";

            if (File.Exists(LogoffPathSysnative)) {
                return LogoffPathSysnative;
            } else if (File.Exists(LogoffPathSystem)) {
                return LogoffPathSystem;
            } else {
                MessageBox.Show("Die Windows-Datei logoff.exe konnte auf diesem System nicht gefunden werden.", "Fehler aufgetreten");
                return null;
            }
        }

        public static logoffReturn LogoffRDPSession(string ComputerName, string SessionID)
        {
            string LogoffExe = getLogoffExePath();
            if (LogoffExe != null)
            {
                ProcessStartInfo psi = new ProcessStartInfo(LogoffExe);
                psi.Arguments = SessionID + " /SERVER:" + ComputerName;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Process LogoffProcess = new Process();
                LogoffProcess.StartInfo = psi;

                bool ProcessStarted = LogoffProcess.Start();
                if (ProcessStarted)
                {
                    LogoffProcess.WaitForExit();
                }

                logoffReturn Results = new logoffReturn();
                Results.ExitCode = LogoffProcess.ExitCode;
                Results.StdOut = LogoffProcess.StandardOutput.ReadToEnd();
                Results.StdErr = LogoffProcess.StandardError.ReadToEnd();

                return Results;
            } else {
                return null;
            }
        }
    }
}
