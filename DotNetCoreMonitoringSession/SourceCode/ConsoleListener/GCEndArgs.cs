using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace ConsoleListener
{
    public class GCEndArgs : EventArgs
    {
        public DateTime TimeStamp { get; }

        public int ProcessId { get; }

        public int Generation { get; set; }

        public int Count { get; set; }

        public GCEndArgs(DateTime timestamp, int processId, int generation, int count)
        {
            TimeStamp = timestamp;
            ProcessId = processId;
            Generation = generation;
            Count = count;
        }
    }
}
