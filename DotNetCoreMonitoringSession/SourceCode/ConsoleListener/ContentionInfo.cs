using System;

namespace ConsoleListener
{
    internal class ContentionInfo : InfoBase
    {
        public ContentionInfo(int processId, int threadId)
            : base(processId)
        {
            ThreadId = threadId;
        }


        public int ThreadId { get; set; }

        public DateTime TimeStamp { get; set; }

        public double ContentionStartRelativeMSec { get; set; }
    }
}
