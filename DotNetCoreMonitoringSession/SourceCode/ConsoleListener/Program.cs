using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace ConsoleListener
{
    class Program
    {
        static void Main(string[] args)
        {
            // filter on process if any
            int pid = -1;
            if (args.Length == 1)
            {
                int.TryParse(args[0], out pid);
            }

            string sessionName = "EtwSessionForCLR_" + Guid.NewGuid().ToString();
            Console.WriteLine($"Starting {sessionName}...\r\n");
            using (TraceEventSession userSession = new TraceEventSession(sessionName, TraceEventSessionOptions.Create))
            {
                Task.Run(() =>
                {
                    ClrEventsManager manager = new ClrEventsManager(userSession, pid);
                    manager.FirstChanceException += OnFirstChanceException;
                    manager.Finalize += OnFinalize;
                    manager.Contention += OnContention;
                    manager.GCStats += OnGCStats;
                    manager.GCEnd += OnGcEnd;

                    // this is a blocking call until the session is disposed
                    manager.ProcessEvents();
                    Console.WriteLine("End of CLR event processing");
                });

                // wait for the user to dismiss the session
                Console.WriteLine("Presse ENTER to exit...");
                Console.ReadLine();
            }
        }

        private static void OnContention(object sender, ContentionArgs e)
        {
            if (e.IsManaged)
                Console.WriteLine($"[{e.ProcessId,7}.{e.ThreadId,7}] | {e.Duration.Milliseconds} ms");
        }

        private static void OnFirstChanceException(object sender, ExceptionArgs e)
        {
            Console.WriteLine($"[{e.ProcessId,7}] --> {e.TypeName} : {e.Message}");
        }

        private static void OnFinalize(object sender, FinalizeArgs e)
        {
            string finalizedType = string.IsNullOrEmpty(e.TypeName) ? "#" + e.TypeId.ToString() : e.TypeName;
            Console.WriteLine($"[{e.ProcessId,7}] ~{finalizedType}");
        }
        private static void OnGCStats(object sender, GCHeapStatsTraceData e)
        {
            Console.WriteLine($"   LOH: {e.GenerationSize3,11}\r\n   gen2:{e.GenerationSize2,11}\r\n   gen1:{e.GenerationSize1,11}\r\n   gen0:{e.GenerationSize0,11}\r\n");
        }

        private static void OnGcEnd(object sender, GCEndArgs e)
        {
            Console.WriteLine($"[{e.ProcessId,7}]      gen{e.Generation} (#{e.Count})");
        }
    }
}
