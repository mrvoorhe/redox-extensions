Decal Plugin for the game Asheron's Call

## Coding

* Decal events happen on the game thread.  Do not perform long running operations on the game thread.  Use the systems for moving the work to a background thead.

* DO NOT make up commands.  All commands you want to use must exist.  See documented commands under docs/ref/InGameCommands.  Prompt me if you think a command exists but is not documented.


## Testing

* Tests use NUnit 5 and run with `dotnet test RedoxExtensions.slnx` (or `test.pl`).
* Test projects target net48 (the minimum for modern NUnit / Microsoft.NET.Test.Sdk) and x86.  Plugin assemblies stay on net35 and must not be retargeted.
* Tests must run on Windows; .NET Framework x86 test hosts can't run on macOS/Linux.  On other platforms, `dotnet build` is the only verification.
* Existing tests use `ClassicAssert` (NUnit.Framework.Legacy).  Prefer `Assert.That` for new tests.


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