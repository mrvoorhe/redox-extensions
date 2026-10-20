Decal Plugin for the game Asheron's Call

## Coding

* Decal events happen on the game thread.  Do not perform long running operations on the game thread.  Use the systems for moving the work to a background thead.

* DO NOT make up commands.  All commands you want to use must exist.  See documented commands under docs/ref/InGameCommands.  Prompt me if you think a command exists but is not documented.


## Testing

* Tests use NUnit 5, target net10.0, and run on macOS/Linux/Windows with `dotnet test RedoxExtensions.slnx` (or `test.pl`).
* Plugin assemblies stay on net35 and must not be retargeted.  They build AnyCPU by default so the 64-bit test host can load them.
* Deploy builds (`copy_dlls_debug.bat` / `copy_dlls_release.bat` -> `deploy.bat`) build the solution with `-p:PlatformTarget=x86`, which `Directory.Build.props` redirects to `bin\x86\<Config>` and `obj\x86\<Config>` so x86 and AnyCPU builds never share output.
* `External/Decal/bin/Decal.Adapter.dll` and `External/Virindi/utank2-i.dll` are compile-time copies patched to clear the 32BITREQUIRED flag.  Re-run `perl External/clear_32bit_required.pl <dll>` after updating them.
* Known failures: `PipelineDispatcherTests` (WinForms `Timer` isn't available on .NET 10) and a number of stale tests.
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