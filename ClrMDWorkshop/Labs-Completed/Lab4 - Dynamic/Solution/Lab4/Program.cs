using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;
using Shared;

namespace Lab4
{
    class Program
    {
        static void Main(string[] args)
        {
            // args[0] is supposed to be the dump filename

            if (args.Length != 1)
            {
                Console.WriteLine("A dump filename must be provided.");
                return;
            }

            var dumpFilename = args[0];
            if (!File.Exists(dumpFilename))
            {
                Console.WriteLine($"{dumpFilename} does not exist.");
                return;
            }

            ProcessDumpFile(args[0]);
        }

        private static void ProcessDumpFile(string dumpFilename)
        {
            using (var target = Utils.GetTarget(dumpFilename))
            {
                ClrRuntime clr = target.ClrVersions[0].CreateRuntime();
                var heap = clr.Heap;

                ShowConcurrentDictionaries(heap);
                ShowConcurrentQueues(heap);
            }
        }

        private static void ShowConcurrentDictionaries(ClrHeap heap)
        {
            var dicos = heap.GetProxies("System.Collections.Concurrent.ConcurrentDictionary<System.Int32,System.String>");
            foreach (var dico in dicos)
            {
                var buckets = dico.m_tables.m_buckets;
                Console.WriteLine($"{buckets.Length} buckets");

                foreach (var bucket in buckets)
                {
                    if (bucket == null) continue;

                    var key = (int)bucket.m_key;
                    var address = (ulong)bucket.m_value;
                    var value = heap.GetObjectType(address).GetValue(address) as string;

                    Console.WriteLine($"{key} = {value}");
                }

                Console.WriteLine();
            }
        }

        private static void ShowConcurrentQueues(ClrHeap heap)
        {
            var queues = heap.GetProxies("System.Collections.Concurrent.ConcurrentQueue<System.Int32>");
            int count = 1;
            foreach (var queue in queues)
            {
                Console.WriteLine($"Queue #{count}");
                // a queue is managing a chained list of segments
                // each segment points to the next via its m_next field
                //              stores the elements in its m_array field
                var segment = queue.m_head;
                while (segment != null)
                {
                    for (int index = segment.m_low; index <= segment.m_high; index++)
                    {
                        Console.WriteLine(segment.m_array[index]);
                    }

                    segment = segment.m_next;
                }

                count++;
                Console.WriteLine();
            }
        }

    }
}
