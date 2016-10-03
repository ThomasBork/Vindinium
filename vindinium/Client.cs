using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vindinium
{
    class Client
    {
        /**
         * Launch client.
         * @param args args[0] Private key
         * @param args args[1] [training|arena]
         * @param args args[2] number of turns
         * @param args args[3] HTTP URL of Vindinium server (optional)
         */
        static void Main(string[] args)
        {
            string serverURL = args.Length == 4 ? args[3] : "http://vindinium.org";

            string key = args.Length > 0 ? args[0] : "xoplygjc";
            bool isTraining = args.Length > 0 ? args[1] != "arena" : false;
            uint numberOfTurns = args.Length > 0 ? uint.Parse(args[2]) : 100;

            //create the server stuff, when not in training mode, it doesnt matter
            //what you use as the number of turns
            ServerStuff serverStuff = new ServerStuff(key, isTraining, numberOfTurns, serverURL, null);

            //create the random bot, replace this with your own bot
            FarmBot bot = new FarmBot(serverStuff);

            //now kick it all off by running the bot.
            bot.run();

            Console.Out.WriteLine("done");

            Console.Read();
        }
    }
}
