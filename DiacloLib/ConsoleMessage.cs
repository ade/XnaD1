using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace DiacloLib
{
    public class ConsoleMessage
    {
        public String Message { get; set; }
        public Color Color { get; set; }
        public ConsoleMessageTypes Type { get; set; }
        public ConsoleMessage(String msg)
        {
            this.Message = msg;
            this.Type = ConsoleMessageTypes.Unknown;
            this.Color = DefaultColor(this.Type);
        }
        public ConsoleMessage(String msg, ConsoleMessageTypes type)
        {
            this.Message = msg;
            this.Type = type;
            this.Color = DefaultColor(type);
        }

        private Color DefaultColor(ConsoleMessageTypes type)
        {
            Color ret = Color.Black;
            switch (type)
            {
                case ConsoleMessageTypes.Error:
                    ret = Color.Red;
                    break;
                case ConsoleMessageTypes.Info:
                    ret = Color.Black;
                    break;
                case ConsoleMessageTypes.Warning:
                    ret = Color.Brown;
                    break;
                case ConsoleMessageTypes.Unknown:
                    ret = Color.Black;
                    break;
                case ConsoleMessageTypes.Debug:
                    ret = Color.CornflowerBlue;
                    break;

            }
            return ret;
        }
        public ConsoleMessage(String msg, Color color)
        {
            this.Message = msg;
            this.Type = ConsoleMessageTypes.Unknown;
            this.Color = color;
        }
        public ConsoleMessage(String msg, ConsoleMessageTypes type, Color color)
        {
            this.Message = msg;
            this.Type = type;
            this.Color = color;
        }
    }
}
