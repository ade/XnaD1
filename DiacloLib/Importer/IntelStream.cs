using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DiacloLib.Importer
{
    /// <summary>
    /// Rumor has it Intel is responsible for this way of writing data, so I'm going to call this class IntelStream.
    /// </summary>
    class IntelStream : MemoryStream
    {
        public IntelStream(byte[] data) : base(data) { }
        public uint ReadDWord()
        {
            //Read reversed dword
            byte b1 = (byte)this.ReadByte(); //Low 
            byte b2 = (byte)this.ReadByte(); //Mid 
            byte b3 = (byte)this.ReadByte(); //High
            byte b4 = (byte)this.ReadByte(); //High
            return (uint)(b1 + b2 * 256 + b3 * 65536 + b4 * 16777216);
        }
        public ushort ReadWord()
        {
            //Read reversed word
            byte b1 = (byte)this.ReadByte(); //Low 
            byte b2 = (byte)this.ReadByte(); //High 
            return (ushort)(b1 + b2 * 256);
        }
    }
}
