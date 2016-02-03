using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Filters;

namespace RedoxLib
{
    public static class SpellUtilities
    {
        public static int LookUpSpellIdByName(string spellName)
        {
            // NOTE : Code copied from MagTools
            FileService service = PluginProvider.Instance.CoreManager.Filter<FileService>();

            for (int i = 0; i < service.SpellTable.Length; i++)
            {
                Spell spell = service.SpellTable[i];

                if (String.Equals(spellName, spell.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return spell.Id;
                }
            }

            return 0;
        }
    }
}
