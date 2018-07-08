using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleListener
{
    public class ClrEventArgs : EventArgs
    {
        public DateTime TimeStamp { get; }

        public int ProcessId { get; }

        public ClrEventArgs(DateTime timestamp, int processId)
        {
            TimeStamp = timestamp;
            ProcessId = processId;
        }
    }
}
