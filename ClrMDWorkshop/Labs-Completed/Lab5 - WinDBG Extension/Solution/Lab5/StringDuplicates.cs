using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;

namespace WindbgExtension
{
    public partial class DebuggerExtensions
    {
        [DllExport("sd")]
        public static void sd(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnStringDuplicates(client, args);
        }
        [DllExport("stringduplicates")]
        public static void stringduplicates(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnStringDuplicates(client, args);
        }
        [DllExport("StringDuplicates")]
        public static void StringDuplicates(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnStringDuplicates(client, args);
        }

        private static void OnStringDuplicates(IntPtr client, string args)
        {
            // Must be the first thing in our extension.
            if (!InitApi(client))
                return;

            // Use ClrMD as normal, but ONLY cache the copy of ClrRuntime (this.Runtime).  All other
            // types you get out of ClrMD (such as ClrHeap, ClrTypes, etc) should be discarded and
            // reobtained every run.
            ClrHeap heap = Runtime.Heap;

            // Console.WriteLine now writes to the debugger.


            // extract the threshold (= min number of duplicates from which a string appears in the list)
            int minCountThreshold = 100;
            if (args != null)
            {
                string[] commands = args.Split(' ');
                int.TryParse(commands[0], out minCountThreshold);
            }

            try
            {
                var strings = ComputeDuplicatedStrings(heap);
                if (strings == null)
                {
                    Console.WriteLine("Impossible to enumerate strings...");
                    return;
                }

                int totalSize = 0;

                // sort by size taken by the instances of string
                foreach (var element in strings.Where(s => s.Value > minCountThreshold).OrderBy(s => 2 * s.Value * s.Key.Length))
                {
                    Console.WriteLine(string.Format(
                        "{0,8} {1,12} {2}",
                        element.Value.ToString(),
                        (2 * element.Value * element.Key.Length).ToString(),
                        element.Key.Replace("\n", "## ").Replace("\r", " ##")
                        ));
                    totalSize += 2 * element.Value * element.Key.Length;
                }

                Console.WriteLine("-------------------------------------------------------------------------");
                Console.WriteLine($"         {(totalSize / (1024 * 1024)).ToString(),12} MB");
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }

        public static Dictionary<string, int> ComputeDuplicatedStrings(ClrHeap heap)
        {
            var strings = new Dictionary<string, int>(1024 * 1024);

            // never forget to check if it is possible to walk the heap
            if (!heap.CanWalkHeap)
                return null;

            foreach (var address in heap.EnumerateObjectAddresses())
            {
                try
                {
                    var objType = heap.GetObjectType(address);
                    if (objType == null)
                        continue;

                    if (objType.Name != "System.String")
                        continue;

                    var obj = objType.GetValue(address);
                    var s = obj as string;
                    if (!strings.ContainsKey(s))
                    {
                        strings[s] = 0;
                    }

                    strings[s] = strings[s] + 1;
                }
                catch (Exception x)
                {
                    Console.WriteLine(x);
                    // some InvalidOperationException seems to occur  :^(
                }
            }

            return strings;
        }
    }
}
