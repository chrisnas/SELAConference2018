using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;

namespace ConsoleListener
{
    public class ClrEventsManager
    {
        private readonly TraceEventSession _session;
        private readonly int _processId;
        private readonly TypesInfo _types;
        private readonly ContentionInfoStore _contentionStore;

        public event EventHandler<ExceptionArgs> FirstChanceException;
        public event EventHandler<FinalizeArgs> Finalize;
        public event EventHandler<ContentionArgs> Contention;
        public event EventHandler<GCHeapStatsTraceData> GCStats;
        public event EventHandler<GCEndArgs> GCEnd;


        public ClrEventsManager(TraceEventSession session, int processId)
        {
            _session = session;
            _processId = processId;
            _types = new TypesInfo();
            _contentionStore = new ContentionInfoStore();
            _contentionStore.AddProcess(processId);
        }


        public void ProcessEvents()
        {
            // setup process filter if any
            TraceEventProviderOptions options = null;
            if (_processId != -1)
            {
                options = new TraceEventProviderOptions()
                {
                    ProcessIDFilter = new List<int>() { _processId },
                };
            }

            // register handlers for events on the session source
            // --------------------------------------------------

            // get exceptions
            _session.Source.Clr.ExceptionStart += OnExceptionStart;

            // get finalizers
            _session.Source.Clr.TypeBulkType += OnTypeBulkType;
            _session.Source.Clr.GCFinalizeObject += OnGCFinalizeObject;

            // get thread contention time
            _session.Source.Clr.ContentionStart += OnContentionStart;
            _session.Source.Clr.ContentionStop += OnContentionStop;

            // get GC details
            _session.Source.Clr.GCHeapStats += OnGCHeapStats;
            _session.Source.Clr.GCStop += OnGCStop;
            _session.Source.Clr.GCAllocationTick += ClrOnGcAllocationTick;

            // thread creation and run (but no exit)
            _session.Source.Clr.ThreadCreating += ClrOnThreadCreating;
            _session.Source.Clr.ThreadRunning += ClrOnThreadRunning;

            // thread pool
            _session.Source.Clr.ThreadPoolWorkingThreadCountStart += ClrOnThreadPoolWorkingThreadCountStart;
            _session.Source.Clr.ThreadPoolWorkerThreadStart += ClrOnThreadPoolWorkerThreadStart;
            _session.Source.Clr.ThreadPoolWorkerThreadStop += ClrOnThreadPoolWorkerThreadStop;
            _session.Source.Clr.ThreadPoolEnqueue += ClrOnThreadPoolEnqueue;
            _session.Source.Clr.ThreadPoolDequeue += ClrOnThreadPoolDequeue;

            // decide which provider to listen to with filters if needed
            _session.EnableProvider(
                ClrTraceEventParser.ProviderGuid,  // CLR provider
                TraceEventLevel.Verbose,
                (ulong)(
                ClrTraceEventParser.Keywords.Contention |           // thread contention timing
                ClrTraceEventParser.Keywords.Threading |            // threadpool events
                ClrTraceEventParser.Keywords.Exception |            // get the first chance exceptions
                ClrTraceEventParser.Keywords.GCHeapAndTypeNames |   // for finalizer type names
                ClrTraceEventParser.Keywords.Type |                 // for TypeBulkType definition of types
                ClrTraceEventParser.Keywords.GC                     // garbage collector details
                ),
                options
            );

            // this is a blocking call until the session is disposed
            _session.Source.Process();
        }

        private void ClrOnGcAllocationTick(GCAllocationTickTraceData data)
        {
            Console.WriteLine($"{data.AllocationKind.ToString()} : {data.TypeName} ({data.AllocationAmount})");
        }

        private void ClrOnThreadPoolDequeue(ThreadPoolWorkTraceData data)
        {
            Console.WriteLine("ThreadPoolDequeue");
        }

        private void ClrOnThreadPoolEnqueue(ThreadPoolWorkTraceData data)
        {
            Console.WriteLine("ThreadPoolEnqueue");
        }

        private void ClrOnThreadPoolWorkerThreadStop(ThreadPoolWorkerThreadTraceData data)
        {
            Console.WriteLine($"ThreadPoolWorkerThreadStop {data.ActiveWorkerThreadCount} - {data.RetiredWorkerThreadCount}");
        }

        private void ClrOnThreadPoolWorkerThreadStart(ThreadPoolWorkerThreadTraceData data)
        {
            Console.WriteLine($"ThreadPoolWorkerThreadStart {data.ActiveWorkerThreadCount} - {data.RetiredWorkerThreadCount}");
        }

        private void ClrOnThreadPoolWorkingThreadCountStart(ThreadPoolWorkingThreadCountTraceData data)
        {
            Console.WriteLine($"ThreadPoolWorkingThreadCountStart {data.Count}");
        }

        private void ClrOnThreadRunning(ThreadStartWorkTraceData data)
        {
            Console.WriteLine("ThreadRunning");
        }

        private void ClrOnThreadCreating(ThreadStartWorkTraceData data)
        {
            Console.WriteLine("ThreadCreating");
        }

        private void OnExceptionStart(ExceptionTraceData data)
        {
            if (data.ProcessID != _processId)
                return;

            NotifyFirstChanceException(data.TimeStamp, data.ProcessID, data.ExceptionType, data.ExceptionMessage);
        }

        private void OnTypeBulkType(GCBulkTypeTraceData data)
        {
            if (data.ProcessID != _processId)
                return;

            // keep track of the id/name type associations
            for (int currentType = 0; currentType < data.Count; currentType++)
            {
                GCBulkTypeValues value = data.Values(currentType);
                _types[value.TypeID] = value.TypeName;
            }
        }
        private void OnGCFinalizeObject(FinalizeObjectTraceData data)
        {
            if (data.ProcessID != _processId)
                return;

            // the type id should have been associated to a name via a previous TypeBulkType event
            NotifyFinalize(data.TimeStamp, data.ProcessID, data.TypeID, _types[data.TypeID]);
        }

        private void OnContentionStart(ContentionTraceData data)
        {
            ContentionInfo info = _contentionStore.GetContentionInfo(data.ProcessID, data.ThreadID);
            if (info == null)
                return;

            info.TimeStamp = data.TimeStamp;
            info.ContentionStartRelativeMSec = data.TimeStampRelativeMSec;
        }
        private void OnContentionStop(ContentionTraceData data)
        {
            ContentionInfo info = _contentionStore.GetContentionInfo(data.ProcessID, data.ThreadID);
            if (info == null)
                return;

            // unlucky case when we start to listen just after the ContentionStart event
            if (info.ContentionStartRelativeMSec == 0)
                return;

            var contentionDurationMSec = data.TimeStampRelativeMSec - info.ContentionStartRelativeMSec;
            var isManaged = (data.ContentionFlags == ContentionFlags.Managed);
            NotifyContention(data.TimeStamp, data.ProcessID, data.ThreadID, TimeSpan.FromMilliseconds(contentionDurationMSec), isManaged);
        }

        private void OnGCHeapStats(GCHeapStatsTraceData data)
        {
            if (data.ProcessID != _processId)
                return;

            NotifyGarbageCollection(data);
        }

        private void OnGCStop(GCEndTraceData data)
        {
            if (data.ProcessID != _processId)
                return;

            NotifyEndGC(data.TimeStamp, data.ProcessID, data.Depth, data.Count);
        }



        private void NotifyFirstChanceException(DateTime timestamp, int processId, string typeName, string message)
        {
            var listeners = FirstChanceException;
            listeners?.Invoke(this, new ExceptionArgs(timestamp, processId, typeName, message));
        }

        private void NotifyFinalize(DateTime timeStamp, int processId, ulong typeId, string typeName)
        {
            var listeners = Finalize;
            listeners?.Invoke(this, new FinalizeArgs(timeStamp, processId, typeId, typeName));
        }

        private void NotifyContention(DateTime timeStamp, int processId, int threadId, TimeSpan duration, bool isManaged)
        {
            var listeners = Contention;
            listeners?.Invoke(this, new ContentionArgs(timeStamp, processId, threadId, duration, isManaged));
        }

        private void NotifyGarbageCollection(GCHeapStatsTraceData stats)
        {
            var listeners = GCStats;
            listeners?.Invoke(this, stats);
        }
        private void NotifyEndGC(DateTime timestamp, int processId, int generation, int count)
        {
            var listeners = GCEnd;
            listeners?.Invoke(this, new GCEndArgs(timestamp, processId, generation, count));
        }
    }
}
