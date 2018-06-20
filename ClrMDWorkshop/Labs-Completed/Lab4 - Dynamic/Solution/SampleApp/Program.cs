using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // the goal of this application is to allocate concurrent dictionaries and queues
            // that will be parsed by the lab 4 code
            List<ConcurrentDictionary<int, string>> dictionaries = new List<ConcurrentDictionary<int, string>>()
            {
                CreateConcurrentDictionary(5),
                CreateConcurrentDictionary(10),
                CreateConcurrentDictionary(20),
                CreateConcurrentDictionary(30),
            };
            List<ConcurrentQueue<int>> queues = new List<ConcurrentQueue<int>>()
            {
                CreateConcurrentQueue(5),
                CreateConcurrentQueue(50),
            };

            Console.WriteLine($"Time to use procdump -ma {Process.GetCurrentProcess().Id}");
            Console.WriteLine("then press ENTER to exit...");
            Console.ReadLine();
        }

        private static ConcurrentDictionary<int, string> CreateConcurrentDictionary(int count)
        {
            ConcurrentDictionary<int, string> dico = new ConcurrentDictionary<int, string>();
            for (int i = 0; i < count; i++)
            {
                var guid = Guid.NewGuid();
                dico.TryAdd(i, guid.ToString());
            }

            return dico;
        }

        private static ConcurrentQueue<int> CreateConcurrentQueue(int count)
        {
            ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
            for (int i = 1; i <= count; i++)
            {
                queue.Enqueue(i);
            }

            return queue;
        }
    }
}
