using System;
using DiacloServer;

namespace Diaclo
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Game game = new Game())
            {
                game.Run();
            }

            //Shut down
            Server.Run = false;
        }
    }
}

