Decal Plugin for the game Asheron's Call

## Coding

* Decal events happen on the game thread.  Do not perform long running operations on the game thread.  Use the systems for moving the work to a background thead.

* DO NOT make up commands.  All commands you want to use must exist.  See documented commands under docs/ref/InGameCommands.  Prompt me if you think a command exists but is not documented.


## Testing

* TODO : Need to update, fix, and document


## Other Plugins we can assume are available at runtime.

* Virindi Tank Suite
    * Source : Not available.  Obfuscated binaries in External/Virindi
    * Docs Website : http://www.virindi.net/wiki/index.php/Main_Page

* Mag Plugins (MagTools, MagFilter)
    * Source : https://github.com/Mag-nus/Mag-Plugins
    * Docs Website : https://github.com/Mag-nus/Mag-Plugins/wiki

* Utility Belt
    * Source : https://gitlab.com/utilitybelt/utilitybelt.gitlab.io specifically the "ILT_Customizations" branch.
    * Docs Website : https://utilitybelt.gitlab.io/docs/overview/