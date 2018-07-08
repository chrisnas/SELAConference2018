using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Threading.Tasks;

namespace NaiveListener
{
    class Program
    {
        static void Main(string[] args)
        {
            // list ETW sessions
            Console.WriteLine("Current ETW sessions:");
            foreach (var session in TraceEventSession.GetActiveSessionNames())
            {
                Console.WriteLine(session);
            }
            Console.WriteLine("--------------------------------------------");


            string sessionName = "EtwSessionForCLR_" + Guid.NewGuid().ToString();
            Console.WriteLine($"Starting {sessionName}...\r\n");
            using (TraceEventSession userSession = new TraceEventSession(sessionName, TraceEventSessionOptions.Create))
            {
                Task.Run(() =>
                {
                    // register handlers for events on the session source
                    // --> listen to all CLR events
                    userSession.Source.Clr.All += delegate (TraceEvent data)
                    {
                        // skip verbose and unneeded events
                        if (SkipEvent(data))
                            return;

                        // raw dump of the events
                        Console.WriteLine($"{data.ProcessID,7}___[{data.ID} | {data.OpcodeName}] {data.EventName} <| {data.GetType().Name}");
                    };

                    // decide which provider to listen to with filters if needed
                    userSession.EnableProvider(
                        ClrTraceEventParser.ProviderGuid,  // CLR provider
                        TraceEventLevel.Verbose,
                        (ulong)(
                            ClrTraceEventParser.Keywords.Contention |           // thread contention timing
                            ClrTraceEventParser.Keywords.Threading |            // threadpool events
                            ClrTraceEventParser.Keywords.Exception |            // get the first chance exceptions
                            ClrTraceEventParser.Keywords.GCHeapAndTypeNames |   // for finalizer and exceptions type names
                            ClrTraceEventParser.Keywords.Type |                 // for finalizer and exceptions type names
                            ClrTraceEventParser.Keywords.GC                     // garbage collector details
                        )
                    );

                    // this is a blocking call until the session is disposed
                    userSession.Source.Process();
                    Console.WriteLine("End of session");
                });

                // wait for the user to dismiss the session
                Console.WriteLine("Presse ENTER to exit...");
                Console.ReadLine();
            }
        }

        private static bool SkipEvent(TraceEvent data)
        {
            if (data.ProcessID != 13276) return true;

            return
                (data.Opcode == (TraceEventOpcode)10) ||
                (data.Opcode == (TraceEventOpcode)11) ||
                (data.Opcode == (TraceEventOpcode)21) ||
                (data.Opcode == (TraceEventOpcode)22) ||
                (data.Opcode == (TraceEventOpcode)23) ||
                (data.Opcode == (TraceEventOpcode)24) ||
                (data.Opcode == (TraceEventOpcode)25) ||
                (data.Opcode == (TraceEventOpcode)27) ||
                (data.Opcode == (TraceEventOpcode)38) ||
                (data.Opcode == (TraceEventOpcode)32) ||
                (data.Opcode == (TraceEventOpcode)33) ||
                (data.Opcode == (TraceEventOpcode)34) ||
                (data.Opcode == (TraceEventOpcode)36) ||
                (data.Opcode == (TraceEventOpcode)39) ||
                (data.Opcode == (TraceEventOpcode)40) ||
                (data.Opcode == (TraceEventOpcode)82)
                ;
        }
    }
}
