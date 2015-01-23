using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace DiacloLib
{
    public class CharacterLibrary
    {
        public static string SAVE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Save\";
        public static string FILE_EXT = ".xml";
        public static Character LoadCharacter(string name)
        {
            Character c = null;
            XmlSerializer s = new XmlSerializer(typeof(Character));
            if (File.Exists(SAVE_PATH + name + FILE_EXT))
            {
                TextReader r = new StreamReader(SAVE_PATH + name + FILE_EXT);
                c = (Character)s.Deserialize(r);
                r.Close();
            }
            return c;
        }
        public static void SaveCharacter(Character c)
        {
            if (!Directory.Exists(SAVE_PATH))
            {
                Directory.CreateDirectory(SAVE_PATH);
            }

            XmlSerializer s = new XmlSerializer(typeof(Character));
            TextWriter w = new StreamWriter(SAVE_PATH + c.Name + FILE_EXT);
            s.Serialize(w, c);
            w.Close();
        }
        public static string[] GetCharacters()
        {
            DirectoryInfo di = new DirectoryInfo(SAVE_PATH);
            string[] files;

            FileInfo[] f = di.GetFiles();
            files = new string[f.Length];

            for (int i = 0; i < f.Length; i++)
            {
                files[i] = f[i].Name.Substring(0, f[i].Name.ToLower().IndexOf(FILE_EXT));
            }

            return files;
        }

    }
}
