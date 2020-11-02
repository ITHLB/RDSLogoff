using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Management.Infrastructure;

namespace RDS_Abmelder
{
    public class RemoteSession
    {
        public string ServerName { get; set; }
        public string SessionID { get; set; }
        public IEnumerable<CimInstance> Processes {
            get {
                IEnumerable<CimInstance> UserProcesses = null;
                CimSession cimSession = CimSession.Create(ServerName);
                UserProcesses =
                    cimSession.QueryInstances(@"root\cimv2",
                        "WQL",
                        $"SELECT * FROM Win32_Process WHERE SessionID={SessionID}").ToList();

                return UserProcesses;
            }
        }
        
        public RemoteSession(string initServerName, string initSessionID)
        {
            ServerName = initServerName;
            SessionID = initSessionID;
        }

        private ButtonCommands.KillProcessCommand m_KillProcessCommand = new ButtonCommands.KillProcessCommand();
        public ButtonCommands.KillProcessCommand CmdKillProcess
        {
            get { return m_KillProcessCommand; }
        }
    }
}
