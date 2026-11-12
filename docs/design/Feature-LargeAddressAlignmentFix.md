# Large-address alignment fix

`RedoxExtensions.Wrapper` patches acclient in memory at startup so that a large-address-aware client doesn't corrupt
dat and network unpacks once it uses memory above 2 GB.  Ported from RynthCore
(`RynthCore.Engine/Compatibility/LargeAddressAlignmentPatch.cs`).

Code: `RedoxExtensions.Wrapper/Compatibility/`.  Tests: `RedoxExtensions.Tests/LargeAddressAlignmentScanTests.cs`.

## The bug

AC pads 4-byte aligned pack/unpack fields with code MSVC compiled from a signed `(int)ptr % 4`:

```
and  r, 0x80000003
jns  +5
dec  r / or r, -4 / inc r
je   skip
... pad 4 - r bytes
```

A buffer above `0x80000000` is a negative int, so the remainder is -3..-1 and AC pads 5..7 bytes instead of 1..3.  The
cursor runs 4 bytes past the field and the unpack reads garbage (motion-table and setup unpack crashes).

The fix rewrites the immediate's top byte from `0x80` to `0x00` (`and r, 3`) at each of the 190 sites in the retail
client.  Below 2 GB that gives exactly the same padding as the stock code.  acclient.exe on disk is not changed.

## Do I need to set IMAGE_FILE_LARGE_ADDRESS_AWARE?

The patch runs whether or not the bit is set.  It only matters when it is.

* **Bit not set (stock client).**  A 32-bit process gets 2 GB of address space, every buffer is below `0x80000000`
  and the bug can't happen.  The patch is applied but changes nothing.
* **Bit set.**  On 64-bit Windows the client can use up to 4 GB.  Once the heap hands out buffers above 2 GB, the
  stock code corrupts unpacks.  This is the case the patch is for.

The bit gives the client the extra memory; the patch makes using it safe.  The patch never sets the bit.

### Setting the bit

The bit is `0x0020` in the PE file header's Characteristics field.  Back up acclient.exe first, then either:

* `editbin /LARGEADDRESSAWARE acclient.exe` (Visual Studio tools), or
* a "4GB patch" tool.

### Checking the bit

`dumpbin /headers acclient.exe` lists "Application can handle large (>2GB) addresses" when it is set.

The in-game status line does not tell you this: it reports `190 site(s) patched` with or without the bit.

## Caveats when the bit is set

* The patch only covers acclient's own padding code.  Native code in Decal, Virindi Tank or other plugins that treats
  pointers as signed ints could still break above 2 GB.  This hasn't been checked; RynthCore's experience running
  large-address-aware clients is the evidence so far.
* The patch is applied when the Wrapper plugin starts, not when the process starts.  Unpacks before that use the stock
  code.  Memory use is low that early, so buffers above 2 GB are unlikely but not impossible.

## Status and logging

* At startup the result is written to `Logs/RTDebugStatic.txt` (`[REWrapper] ... Large-address alignment fix: ...`).
* At login it is written to the debug chat window and the agent bridge events.

Expected messages:

| Message | Meaning |
| --- | --- |
| `190 site(s) patched, 0 already patched, 0 failed (190 found).` | Normal. |
| `0 site(s) patched, 190 already patched, 0 failed (190 found).` | Another component (e.g. RynthCore) already patched it. |
| `Disabled via settings.json (EnableLargeAddressAlignmentFix=false).` | Turned off, see below. |
| `Skipped: ...` | Not a 32-bit Windows process, or acclient's `.text` section wasn't found. |
| `Failed: ...` | An exception, e.g. `settings.json` isn't valid JSON.  The fix was not applied. |

A found count other than 190 means a different acclient build; check the sites before trusting the patch.

## Turning it off

Add to `settings.json` in the install directory (next to `RedoxExtensions.Wrapper.dll`):

```json
{
  "UserSettingsFilePath": "mysettings.json",
  "EnableLargeAddressAlignmentFix": false
}
```

Only a JSON `false` turns it off; a missing key or `true` leaves it on.  It's read once at plugin startup, so a change
needs a client restart.  `deploy.bat` never overwrites `settings.json`, so the value survives deploys.

## Tests

`dotnet test RedoxExtensions.slnx --filter "LargeAddressAlignment|CompatibilitySettings"`

The real-client test scans `C:\Turbine\Asheron's Call\acclient.exe`, or the path in the `ACCLIENT_EXE` environment
variable, and is skipped when the file isn't there.
