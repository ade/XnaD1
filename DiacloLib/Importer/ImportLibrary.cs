using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer
{
    public class ImportLibrary
    {
        public MonsterTemplate[] MonsterTemplates { get; set; }
        public void LoadMonsterTemplates()
        {
            this.MonsterTemplates = MonsterImporter.MonsterImporter.GetMonsters();
        }
    }
}
