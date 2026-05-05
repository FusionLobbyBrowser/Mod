using System;
using System.Threading.Tasks;
using System.Web;

namespace Bridge
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Bridge launched, checking for arguments...");
            string arg = "";
            if (args.Length > 0)
                arg = args[0];
            var uri = new Uri(arg);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Console.WriteLine($"Received argument: {args[0]}");
            await Task.Delay(-1);
        }
    }
}