using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace DiacloLib
{
    public class Character
    {
        //Character attributes
        public string Name { get; set; }
        public CharacterClass Class { get; set; }
        public int Level { get; set; }
        [XmlAttribute("Strength")]
        public UInt16 AttStr { get; set; }
        [XmlAttribute("Dexterity")]
        public UInt16 AttDex { get; set; }
        [XmlAttribute("Vitality")]
        public UInt16 AttVit { get; set; }
        [XmlAttribute("Magic")]
        public UInt16 AttMag { get; set; }
        [XmlAttribute("PointsToSpend")]
        public int LevelUpPoints { get; set; }
        [XmlAttribute("Experience")]
        public uint Experience { get; set; }

        public Character()
        {
        }
        public Character(CharacterClass c)
        {
            switch (c)
            {
                case CharacterClass.Warrior:
                default:
                    this.AttStr = 30;
                    this.AttDex = 20;
                    this.AttMag = 10;
                    this.AttVit = 25;
                    break;
            }
            this.LevelUpPoints = 0;
            this.Level = 1;
            this.Name = "Unnamed";
        }
    }
}
