
namespace ConsoleListener
{
    internal abstract class InfoBase
    {
        protected InfoBase(int processId)
        {
            ProcessId = processId;
        }

        public int ProcessId { get; }
    }
}
