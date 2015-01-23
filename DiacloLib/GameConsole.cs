using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace DiacloLib
{
    public enum ConsoleMessageTypes
    {
        Unknown = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Debug = 4
    }
    public enum PerformanceCategory
    {
        PathFinding,
        GraphicImporting,
        ClientNetworkUpdate,
        ClientGameUpdate,
        ClientDraw
    }
    public static class GameConsole
    {
        public static double[] PerformanceCounter = new double[5];
        public static double ClientDrawTimer;
        public static double ServerUpdateTimer;

        public static List<ConsoleMessage> Messages = new List<ConsoleMessage>();

        public static void Write(String msg)
        {
            Messages.Add(new ConsoleMessage(msg));
        }
        public static void Write(String msg, ConsoleMessageTypes type)
        {
            Messages.Add(new ConsoleMessage(msg, type));
        }
        public static void ReportPerformance(PerformanceCategory cat, long ticks)
        {
            double time = ((double)ticks / (double)Stopwatch.Frequency);
            GameConsole.PerformanceCounter[(int)cat] += time;
        }
    }
}
