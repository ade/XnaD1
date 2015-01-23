using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using DiacloLib;
using MpqReader;
using DiacloLib.Importer;

namespace DiacloLib
{
    public class LegacyContent
    {
        private static MpqArchive MPQ;


        private static void LoadMPQ()
        {
            //Mount original content MPQ
            MPQ = new MpqArchive("spawn.mpq");
            MPQ.ExternalListFile = "spawn.txt";
        }

        /// <summary>
        /// Extract a file from the default MPQ archive and return as byte array
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static byte[] GetMPQFile(string filename) {
            if (MPQ == null)
            {
                LegacyContent.LoadMPQ();
            }
            MpqStream mpqdata = MPQ.OpenFile(filename);
            byte[] filedata = new byte[mpqdata.Length];
            mpqdata.Read(filedata, 0, (int)filedata.Length);
            return filedata;
        }

    }
}
