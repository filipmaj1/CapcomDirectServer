using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FMaj.CapcomDirectServer
{
    class Program
    {
        public const int PORT_START = 8888;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Log.Info("=============================");
            Log.Info("Capcom Direct Server");
            Log.Info("=============================");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Server server = new Server();
            server.StartServer(PORT_START);

            while (true) {
                string input = Console.ReadLine();
                if (input.StartsWith('!'))
                {
                    server.SendAll(Encoding.ASCII.GetBytes(input.Substring(1)));
                } else if (input.StartsWith('@'))
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(".\\" + input.Substring(1));
                        server.SendAll(data);
                    }catch (IOException) { Console.WriteLine("> File not found"); }
                }
                Thread.Sleep(200);
                };
        }
    }
}
