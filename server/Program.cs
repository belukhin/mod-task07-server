using System;
using System.Threading;
using System.IO;

namespace SERVER
{
    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
    }

    /// <summary>
    /// PoolRecord struct
    /// </summary>
    struct PoolRecord
    {
        public Thread thread;
        public bool in_use;
        public int wait;
        public int work;
    }

    /// <summary>
    /// Client class
    /// </summary>
    class Client
    {
        public event EventHandler<procEventArgs> request;
        Server server;

        int index = 0;

        public Client(Server server)
        {
            this.server = server;
            this.request += server.proc;
            index = 0;
        }
        protected virtual void OnProc(procEventArgs e)
        {
            EventHandler<procEventArgs> handler = request;
            if (handler != null)
                handler(this, e);
        }
        public void Work()
        {
            procEventArgs e = new procEventArgs();
            index++;
            e.id = index;
            this.OnProc(e);
        }
    }

    /// <summary>
    /// Server class
    /// </summary>
    class Server
    {
        public int CountRequests;
        public int CountProcessed;
        public int CountRejected;
        public int CountPool;

        public PoolRecord[] pool;
        object threadLock;

        public Server(int count, PoolRecord[] pool)
        {
            CountRequests = 0;
            CountProcessed = 0;
            CountRequests = 0;
            this.pool = pool;
            this.CountPool = count;
            threadLock = new object();
            for (int i = 0; i < CountPool; i++)
                pool[i].in_use = false;
        }
        void Answer(object e)
        {
            Console.WriteLine("Request with number {0} is in progress", e);
            Thread.Sleep(10);
            Console.WriteLine("Request with number {0} completed", e);
            for (int i = 0; i < CountPool; i++)
            {
                if (pool[i].thread == Thread.CurrentThread)
                {
                    pool[i].in_use = false;
                    pool[i].thread = null;
                    break;
                }
            }
        }
        public void proc(object sender, procEventArgs e)
        {
            lock (threadLock)
            {
                Console.WriteLine("Request wtith number {0}", e.id);
                CountRequests++;
                for (int i = 0; i < CountPool; i++)
                {
                    if (!pool[i].in_use)
                        pool[i].wait++;
                }
                for (int i = 0; i < CountPool; i++)
                {
                    if (!pool[i].in_use)
                    {
                        pool[i].work++;
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        pool[i].thread.Start(e.id);
                        CountProcessed++;
                        return;
                    }
                }
                CountRejected++;
            }
        }
    }

    /// <summary>
    /// Main program
    /// </summary>
    class Program
    {
        static long Fact(long n)
        {
            if (n == 0)
                return 1;
            else
                return n * Fact(n - 1);
        }
        static string Conclusion(Server server, int requests)
        {
            string output = "";

            double p = requests / server.CountPool;
            double temp = 0;

            for (int i = 0; i < server.CountPool; i++)
                temp += Math.Pow(p, i) / Fact(i);

            double p0 = 1 / temp;
            double pn = Math.Pow(p, server.CountPool) * p0 / Fact(server.CountPool);

            output += "Number of threads: " + server.CountPool + '\n' + "Total requests: " + server.CountRequests + '\n' 
                    + "Requests completed: " + server.CountProcessed + '\n' + "Rejected requests: " + server.CountRejected + '\n';

            for (int i = 0; i < server.CountPool; i++)
                output += "Tread #" + (i + 1) + " completed: " + server.pool[i].work + " requests; waiting ticks: " + server.pool[i].wait + '\n';

            output += "Intensity of the flow of requests: " + p + '\n';
            output += "System downtime probability: " + p0 + '\n';
            output += "System failure probability: " + pn + '\n';
            output += "Relative bandwidth: " + (1 - pn) + '\n';
            output += "Absolute bandwidth: " + (requests * (1 - pn)) + '\n';
            output += "Average busy channels: " + ((requests * (1 - pn)) / server.CountPool) + '\n';

            return output;
        }

        static int ThreadCount = 8;
        static int CountRequests = 50;
        static PoolRecord[] pool = new PoolRecord[ThreadCount];

        static void Main(string[] args)
        {
            Server server = new Server(ThreadCount, pool);
            Client client = new Client(server);
            for (int i = 0; i < CountRequests; i++)
            {
                client.Work();
            }
            Thread.Sleep(1000);
            Console.WriteLine("\n--------\n");
            string output = Conclusion(server, CountRequests);
            Console.WriteLine(output);
            File.WriteAllText("OUT.txt", output);
        }
    }
}