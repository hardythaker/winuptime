using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpTimeWin
{
    class Test
    {
        public void InitializeData()
        {
            EventLog logs = new EventLog();
            logs.Log = "System";
            logs.MachineName = ".";

            var entries = logs.Entries.Cast<EventLogEntry>();
            var on = from e in entries
                     where e.InstanceId == 6005 && e.Source.Contains("EventLog") && e.EntryType == EventLogEntryType.Information
                     select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "On" };
            var off = from e in entries
                      where e.InstanceId == 6006 && e.Source.Contains("EventLog") && e.EntryType == EventLogEntryType.Information
                      select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Off" };
            var sleep = from e in entries
                        where e.InstanceId == 42 && e.Source.Contains("Kernel-Power") && e.EntryType == EventLogEntryType.Information
                        select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Sleep" };
            var awake = from e in entries
                        where e.InstanceId == 1 && e.Source.Contains("Kernel-General") && e.EntryType == EventLogEntryType.Information && e.UserName.Equals("N/A")
                        select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Awake" };

            List<OnOffEntry> temp = new List<OnOffEntry>();
            Console.Write(on.ToList());
        }
    }
}
