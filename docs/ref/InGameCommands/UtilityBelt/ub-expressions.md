# UtilityBelt Expressions

> Source: <https://utilitybelt.gitlab.io/docs/expressions/> (docs build 0.2.7, commit 6edb6c0, 2022-07-28; retrieved 2026-10-20).

UtilityBelt can replace VTank's built-in meta expression engine with its own. The UB engine is backwards compatible with [VTank meta expressions](http://www.virindi.net/wiki/index.php/Meta_Expressions) and adds data types (lists, dictionaries, coordinates, world objects, stopwatches) and the 238 functions listed below.

## Enabling and Running Expressions

Enable the engine with:

```
/ub opt set VTank.PatchExpressionEngine true
```

Once enabled, UB handles:

- Meta conditions of type **Chat Message Capture** and **Expression**.
- Meta actions of type **Expression Action** and **Chat Expression**.
- Meta view button expressions.

To evaluate an expression from the chat box, use `/ub mexec <expression>` (see [ub-commands.md](ub-commands.md)). Examples:

```
/ub mexec echo[wobjectgetname[wobjectgetselection[]],15]
/ub mexec getcharvital_current[1]
/ub mexec coordinatetostring[getplayercoordinates[]]
/ub mexec listcount[wobjectfindallbyobjectclass[24]]
```

## Syntax

### Function calls

Every function is called as `name[args]`. Square brackets are required even with no arguments (`wobjectgetplayer[]`). Arguments are separated by `,`. Calls nest, so the result of one function can be passed to another:

```
wobjectgetname[wobjectgetplayer[]]
```

Multiple expressions can be chained with `;`. The value of the last one is the result:

```
setvar[myList,listcreate[1,2,3]]; listadd[$myList,4]
```

### Data types

| Type | Notes |
| --- | --- |
| Number | Stored as a double (15–16 digits of precision, range ±5.0×10⁻³²⁴ to ±1.7×10³⁰⁸). Hex literals allowed: `0xff`. `true`/`false` are keywords for `1`/`0`. |
| String | Any length. Supports slices like lists: `test[1:]`. See escaping below. |
| List | Ordered items of any type. Index with `$list{index}`; slice with `{start:stop}`, e.g. `$list{:-1}`, `$list[1:3]`. |
| Dictionary | Key/value pairs. Keys must be strings; values can be any type. Access with `$dict{key}`. |
| Coordinates | A set of NS/EW/Z game coordinates. |
| WorldObject | A game object (player, item, monster, NPC, etc.). |
| Stopwatch | A time-based counter that can be started and stopped. |
| UIControl | A reference to a control in a meta view (from `uigetcontrol`). |

### String escaping

Strings that contain only letters and spaces need no escaping. Anything else must either have each special character escaped with `\`, or the whole string wrapped in backticks:

| Raw text | Escaped |
| --- | --- |
| `some example string` | `some example string` (no escaping needed) |
| `123 test` | `\1\2\3 test` or `` `123 test` `` |
| `p@cMan$)` | `p\@cMan\$\)` or `` `p@cMan$)` `` |

Backticks nest by escaping the inner ones with `\`, which is how expression strings are passed to `ifthen`, `exec`, `listfilter`, etc.:

```
ifthen[1, `chatbox[\`true\`]`, `chatbox[\`false\`]`]
```

### Operators

| Operator | Meaning | Operand types |
| --- | --- | --- |
| `==` | Equal (both sides must be the same type) | any |
| `>` `<` `>=` `<=` | Comparison | numbers |
| `+` | Add / concatenate | numbers, strings |
| `-` `*` `/` `%` | Subtract, multiply, divide, remainder | numbers |
| `#` | Regex match, case-insensitive: `test#s` is true if `test` matches `s` | strings |
| `&&` | Logical AND | any |
| `\|\|` | Logical OR | any |
| `~` | Bitwise complement | numbers |
| `<<` `>>` | Bit shift left / right | numbers |
| `&` `^` `\|` | Bitwise AND / XOR / OR | numbers |

### Variables

| Scope | Lifetime | Set | Get | Shorthand |
| --- | --- | --- | --- | --- |
| Memory | Until the character logs out. Can hold any type. | `setvar[name,value]` | `getvar[name]` | `$name` |
| Persistent | Saved to a database per character; survives client restarts. | `setpvar[name,value]` | `getpvar[name]` | `@name` |
| Global | Shared by all clients/characters on the server; saved on every change. | `setgvar[name,value]` | `getgvar[name]` | `&name` |

The shorthand forms accept an expression as the identifier: `$wobjectgetname[wobjectgetplayer[]]` is equivalent to `getvar[wobjectgetname[wobjectgetplayer[]]]`.

Use `/ub listvars`, `/ub listpvars`, and `/ub listgvars` to print the currently defined variables.

### Callback expressions

`listfilter`, `listmap`, `listreduce`, and `listsort` take an expression **string** that is run once per item, with these memory variables set:

| Variable | `listfilter` / `listmap` | `listreduce` | `listsort` |
| --- | --- | --- | --- |
| `$0` | iteration index | iteration index | — |
| `$1` | current item | current item | item a |
| `$2` | — | accumulated value | item b |
| Return | `1` keep / `0` drop (filter); mapped value (map) | new accumulated value | `-1`, `0`, or `1` |

### Gotchas

- Functions that return a `WorldObject` return `0` when nothing is found. Passing that `0` to a function expecting a `WorldObject` throws an error, so check first with `getobjectinternaltype` (values: `0`=none, `1`=number, `3`=string, `7`=object) or `wobjectisvalid`.
- `iif` always evaluates both branches. Use `ifthen` (which takes expression strings) when only one branch should run.
- Assess data (`wobjectgetintprop` etc. on items/creatures) may not be present until requested: check `wobjecthasdata` and call `wobjectrequestdata` if needed.
- Quest functions rely on `/myquests` data. UB runs it once at login; run `/myquests` (or `/ub myquests`) again after completing a quest.
- `wobjectgethealth` only updates for the currently selected object.

## Common Recipes

| Goal | Expression |
| --- | --- |
| Name of current selection | `wobjectgetname[wobjectgetselection[]]` |
| Distance to selection (3D) | `coordinatedistancewithz[getplayercoordinates[], wobjectgetphysicscoordinates[wobjectgetselection[]]]` |
| Heading to selection | `getheadingto[wobjectgetselection[]]` |
| Current health / stamina / mana | `getcharvital_current[1]` / `[2]` / `[3]` |
| Health percent | `getcharvital_current[1]/getcharvital_buffedmax[1]*100` |
| Character level | `getcharintprop[25]` |
| Count items by name | `getitemcountininventorybyname[Prismatic Taper]` |
| Nearby players at level 275 | ``listfilter[wobjectfindallbyobjectclass[24],`wobjectgetintprop[$1,25]==275`]`` |
| Names of all nearby players | ``listmap[wobjectfindallbyobjectclass[24],`wobjectgetname[$1]`]`` |
| Is a buff about to expire (< 5 min)? | ``getspellexpirationbyname[`acid prot`]<300`` |
| Guard against a missing object | ``ifthen[getobjectinternaltype[wobjectfindnearestmonster[]]==7, `echo[wobjectgetname[wobjectfindnearestmonster[]],15]`]`` |
| Time an action | `setvar[sw,stopwatchstart[stopwatchcreate[]]]` … `stopwatchelapsedseconds[$sw]` |
| Switch meta state | `vtsetmetastate[myState]` |
| Read a UB setting | ``uboptget[`Plugin.Debug`]`` |


## Function Index

Every function, grouped by category. Click a name for full details. `?` marks an optional parameter; `...` marks a variable-length argument list.


### Variables

Memory (`$`), persistent (`@`), and global (`&`) variables.

| Function | Returns | Summary |
| --- | --- | --- |
| [`testvar[varname]`](#testvar) | number | Checks if a variable is defined |
| [`getvar[varname]`](#getvar) | Object | Returns the value stored in a variable |
| [`setvar[varname, value]`](#setvar) | Object | Stores a value in a variable |
| [`touchvar[varname]`](#touchvar) | number | Sets the value of a variable to 0 if the variable was previously undefined |
| [`clearvar[varname]`](#clearvar) | number | Clears the value of a variable |
| [`clearallvars[]`](#clearallvars) | number | Unsets all variables |
| [`testpvar[varname]`](#testpvar) | number | Checks if a persistent variable is defined |
| [`getpvar[varname]`](#getpvar) | Object | Returns the value stored in a persistent variable |
| [`setpvar[varname, value]`](#setpvar) | Object | Stores a value in a persistent variable that is available ever after relogging |
| [`touchpvar[varname]`](#touchpvar) | number | Sets the value of a persistent variable to 0 if the variable was previously undefined |
| [`clearpvar[varname]`](#clearpvar) | number | Clears the value of a persistent variable |
| [`clearallpvars[]`](#clearallpvars) | number | Unsets all persistent variables |
| [`testgvar[varname]`](#testgvar) | number | Checks if a global variable is defined |
| [`getgvar[varname]`](#getgvar) | Object | Returns the value stored in a global variable |
| [`setgvar[varname, value]`](#setgvar) | Object | Stores a value in a global variable |
| [`touchgvar[varname]`](#touchgvar) | number | Sets the value of a global variable to 0 if the variable was previously undefined |
| [`cleargvar[varname]`](#cleargvar) | number | Clears the value of a global variable |
| [`clearallgvars[]`](#clearallgvars) | number | Unsets all global variables |

### Logic and Evaluation

Conditionals, truthiness, and evaluating expression strings.

| Function | Returns | Summary |
| --- | --- | --- |
| [`iif[value, truevalue, falsevalue]`](#iif) | Object | Checks if the first parameter is true, if so returns the second argument |
| [`ifthen[value, trueexpr, falseexpr?]`](#ifthen) | Object | If value is truthy, will execute the trueexpr string and return the results |
| [`istrue[value]`](#istrue) | number | Checks if a value is equal to true (1) |
| [`isfalse[value]`](#isfalse) | number | Checks if a value is equal to false (0) |
| [`exec[text]`](#exec) | Object | Evaluates a string as an expression |
| [`delayexec[delay, text]`](#delayexec) | Object | Evaluates a string as an expression, after the specified delay |

### Strings and Conversion

Type conversion, formatting, and string helpers.

| Function | Returns | Summary |
| --- | --- | --- |
| [`tostring[obj]`](#tostring) | Object | Converts an object to a string |
| [`cstr[number]`](#cstr) | string | Converts a number to a string |
| [`cstrf[number, format]`](#cstrf) | string | Converts a number to a string using a specified format |
| [`cnumber[number]`](#cnumber) | number | Converts a string to a floating point number |
| [`hexstr[d]`](#hexstr) | string | Converts a number to a hexadecimal string |
| [`strlen[tocheck]`](#strlen) | number | Gets the length of a string |
| [`ord[character]`](#ord) | number | Returns an integer representing the Unicode character |
| [`chr[character]`](#chr) | string | Returns a string representing a character whose Unicode code point is an integer |
| [`getregexmatch[text, regex]`](#getregexmatch) | string | Returns the part of the text that matches the specified regex |
| [`getobjectinternaltype[tocheck]`](#getobjectinternaltype) | number | Gets internal type of an object, as a number |

### Math

Arithmetic helpers, rounding, random numbers, and trigonometry (radians).

| Function | Returns | Summary |
| --- | --- | --- |
| [`abs[number]`](#abs) | number | Returns the absolute value of a number |
| [`floor[number]`](#floor) | number | Returns the largest integer less than or equal to a given number |
| [`ceiling[number]`](#ceiling) | number | Rounds a number up to the next largest whole number or integer |
| [`round[number]`](#round) | number | Returns the value of a number rounded to the nearest integer |
| [`randint[min, max]`](#randint) | number | Generates a random number between min and (max-1) |
| [`sqrt[d]`](#sqrt) | number | Returns the square root of a specified number |
| [`sin[d]`](#sin) | number | Returns the sine of the specified angle |
| [`cos[d]`](#cos) | number | Returns the cosine of the specified angle |
| [`tan[d]`](#tan) | number | Returns the tangent of the specified angle |
| [`asin[d]`](#asin) | number | Returns the angle whose sine is the specified number |
| [`acos[d]`](#acos) | number | Returns the angle whos cosine is the specified number |
| [`atan[d]`](#atan) | number | Returns the angle whose tangent is the specified number |
| [`atan2[y, x]`](#atan2) | number | Returns the angle whose tangent is the quotient of two specified numbers |
| [`sinh[d]`](#sinh) | number | Returns the hyperbolic sine of the specified angle |
| [`cosh[d]`](#cosh) | number | Returns the hyperbolic cosine of the specified angle |
| [`tanh[d]`](#tanh) | number | Returns the hyperbolic tangent of the specified angle |

### Lists

Creating and manipulating lists. Indexes are zero based.

| Function | Returns | Summary |
| --- | --- | --- |
| [`listcreate[...items]`](#listcreate) | List | Creates a new list object |
| [`listfromrange[start, end]`](#listfromrange) | List | Create a list of numbers between start and end inclusively |
| [`listadd[list, item]`](#listadd) | List | Adds an item to the end of a list |
| [`listinsert[list, item, index]`](#listinsert) | List | Inserts an item at the specified index of the list |
| [`listremove[list, item]`](#listremove) | List | Removes the first occurrence of an item from the list |
| [`listremoveat[list, index]`](#listremoveat) | List | Removes the specified index from the list |
| [`listpop[list, index?]`](#listpop) | Object | Removes the value at the specified index of the array and returns the item |
| [`listclear[list]`](#listclear) | List | Clears all items from the specified list |
| [`listgetitem[list, index]`](#listgetitem) | Object | Retrieves the item at the specified list index |
| [`listcontains[list, item]`](#listcontains) | number | Checks if the specified item is contained in the list |
| [`listindexof[list, item]`](#listindexof) | number | Finds the index of the first occurrence of an item in a list |
| [`listlastindexof[list, item]`](#listlastindexof) | number | Finds the index of the last occurrence of an item in a list |
| [`listcount[list]`](#listcount) | number | Counts the number of items contained in a list |
| [`listcopy[list]`](#listcopy) | List | Creates a copy of a list |
| [`listreverse[list]`](#listreverse) | List | Creates a copy of a list, and reverses the order |
| [`listfilter[list, expression]`](#listfilter) | List | Creates a new list filtered by the specified expression |
| [`listmap[list, expression]`](#listmap) | List | Creates a new list mapped by the specified expression |
| [`listreduce[list, expression]`](#listreduce) | Object | Reduces a list down into a single value |
| [`listsort[list, expression]`](#listsort) | List | Creates a list from another, sorted by expression |

### Dictionaries

Key/value maps with string keys.

| Function | Returns | Summary |
| --- | --- | --- |
| [`dictcreate[...items]`](#dictcreate) | Dictionary | Creates a new dictionary object |
| [`dictgetitem[dict, key]`](#dictgetitem) | Object | Retrieves the item at the specified dictionary key |
| [`dictadditem[dict, key, value]`](#dictadditem) | number | Adds the Key Value pair to the dictionary |
| [`dicthaskey[dict, key]`](#dicthaskey) | number | Checks if the dictionary contains the key |
| [`dictremovekey[dict, key]`](#dictremovekey) | number | Deletes the specified key from the dictionary |
| [`dictkeys[dict]`](#dictkeys) | List | Returns a list of keys |
| [`dictvalues[dict]`](#dictvalues) | List | Returns a list of values |
| [`dictsize[dict]`](#dictsize) | number | Returns the size of the dictionary |
| [`dictclear[dict]`](#dictclear) | Dictionary | Clears the dictionary contents and returns the dictionary |
| [`dictcopy[dict]`](#dictcopy) | Dictionary | Creates a new dictionary and copies over all the key value pairs |

### Chat and HUD Output

Writing to the chat box and the Virindi HUDs Status HUD.

| Function | Returns | Summary |
| --- | --- | --- |
| [`echo[message, color]`](#echo) | number | Echos chat to the chatbox with a color |
| [`chatbox[message]`](#chatbox) | string | Sends a message to the chatbox as if you had typed it in |
| [`chatboxpaste[message]`](#chatboxpaste) | number | Pastes a message to the chatbox, leaving focus, so that the user can complete typing it |
| [`statushud[key, value]`](#statushud) | number | Updates an entry in the Virindi HUDs Status HUD |
| [`statushudcolored[key, value, color]`](#statushudcolored) | number | Updates an entry in the Virindi HUDs Status HUD with color |

### Character

Your own character: properties, vitals, skills, attributes, state.

| Function | Returns | Summary |
| --- | --- | --- |
| [`getcharintprop[property]`](#getcharintprop) | number | Returns an int property from your character, or 0 if it’s undefined |
| [`getchardoubleprop[property]`](#getchardoubleprop) | number | Returns a double property from your character, or 0 if it’s undefined |
| [`getcharquadprop[property]`](#getcharquadprop) | number | Returns a quad property from your character, or 0 if it’s undefined |
| [`getcharboolprop[property]`](#getcharboolprop) | number | Returns a bool property from your character, or 0 if it’s undefined |
| [`getcharstringprop[property]`](#getcharstringprop) | string | Returns a string property from your character, or false if it’s undefined |
| [`getcharvital_base[vitalId]`](#getcharvital_base) | number | Gets your characters base (unbuffed) vital value |
| [`getcharvital_current[vitalId]`](#getcharvital_current) | number | Gets your characters current vital value |
| [`getcharvital_buffedmax[vitalId]`](#getcharvital_buffedmax) | number | Gets your characters buffed maximum vital value |
| [`getcharskill_traininglevel[skillId]`](#getcharskill_traininglevel) | number | Gets your characters training level for a specified skill |
| [`getcharskill_base[skillId]`](#getcharskill_base) | number | Gets your characters base skill level for a specified skill |
| [`getcharskill_buffed[skillId]`](#getcharskill_buffed) | number | Gets your characters buffed skill level for a specified skill |
| [`getcharattribute_base[attributeId]`](#getcharattribute_base) | number | Gets your characters base attribute level for a specified attribute |
| [`getcharattribute_buffed[attributeId]`](#getcharattribute_buffed) | number | Gets your characters buffed attribute level for a specified attribute |
| [`getcharburden[]`](#getcharburden) | number | Gets your current burden shown on the character panel |
| [`vitae[]`](#vitae) | number | Gets your character’s current vitae percentage as a number |
| [`getcombatstate[]`](#getcombatstate) | string | Get the combat state of your character |
| [`setcombatstate[str]`](#setcombatstate) | number | Sets the combat state of your character |
| [`getbusystate[]`](#getbusystate) | number | Get the busy state of your character and returns a number for the state (0=idle, 1=combining a stack, 2=splitting a stack, 3=???, 4=picking up an item from the ground, 5=moving or unequipping an item, 6=dropping an item to the ground, 7=equipping an item) |
| [`getequippedweapontype[]`](#getequippedweapontype) | string | Returns the equipped weapons object class |
| [`isportaling[]`](#isportaling) | number | Checks if your character is currently in portalspace |

### Spells and Components

Spell lookup, known spells, castability, and active spell timers.

| Function | Returns | Summary |
| --- | --- | --- |
| [`spellname[spellid]`](#spellname) | string | Gets the name of a spell by id |
| [`spelldata[spellid]`](#spelldata) | Dictionary | Gets a dictionary of information about the passed spell id |
| [`getknownspells[]`](#getknownspells) | List | Returns a list of spell ids known by this character |
| [`getisspellknown[spellId]`](#getisspellknown) | number | Checks if your character knows a spell by id |
| [`getcancastspell_hunt[spellId]`](#getcancastspell_hunt) | number | Checks if your character is capable of casting spellId while hunting |
| [`getcancastspell_buff[spellId]`](#getcancastspell_buff) | number | Checks if your character is capable of casting spellId while buffing |
| [`getspellexpiration[spellId]`](#getspellexpiration) | number | Gets the number of seconds until a spell expires by id |
| [`getspellexpirationbyname[spellName]`](#getspellexpirationbyname) | number | Gets the number of seconds until a spell expires by name |
| [`componentname[componentid]`](#componentname) | string | Gets the name of a spell component by id |
| [`componentdata[componentid]`](#componentdata) | Dictionary | Returns a dictionary containing component data for the passed component id |

### Position and Coordinates

Player location, coordinate objects, distances, and headings.

| Function | Returns | Summary |
| --- | --- | --- |
| [`getplayerlandcell[]`](#getplayerlandcell) | number | Gets the landcell your character is currently standing in |
| [`getplayerlandblock[]`](#getplayerlandblock) | number | Gets the landblock your character is currently standing in |
| [`getplayercoordinates[]`](#getplayercoordinates) | Coordinates | Gets the a coordinates object representing your characters current global position |
| [`wobjectgetphysicscoordinates[wo]`](#wobjectgetphysicscoordinates) | Coordinates | Gets a coordinates object representing a world objects current position |
| [`coordinateparse[coordstring]`](#coordinateparse) | Coordinates | Converts a coordinate string like `1.2N, 3.4E` to a coordinates object |
| [`coordinatetostring[obj]`](#coordinatetostring) | string | Converts a coordinates object to a string representation |
| [`coordinategetns[obj]`](#coordinategetns) | number | Gets the NS position of a coordinates object as a number |
| [`coordinategetwe[obj]`](#coordinategetwe) | number | Gets the EW position of a coordinates object as a number |
| [`coordinategetz[obj]`](#coordinategetz) | number | Gets the Z position of a coordinates object as a number |
| [`coordinatedistancewithz[obj1, obj2]`](#coordinatedistancewithz) | number | Gets the 3d distance in meters between two coordinates objects |
| [`coordinatedistanceflat[obj1, obj2]`](#coordinatedistanceflat) | number | Gets the 2d distance in meters between two coordinates objects (ignoring Z) |
| [`getheading[obj]`](#getheading) | number | Returns the heading in degrees (0-360 clockwise, 0 is north) the target object is facing |
| [`getheadingto[obj]`](#getheadingto) | number | Calculates the heading in degrees (0-360 clockwise, 0 is north) from your player to the target object |

### World Objects: Finding

Locating a single world object, or lists of them. Single-object finders return `0` when nothing matches.

| Function | Returns | Summary |
| --- | --- | --- |
| [`wobjectgetplayer[]`](#wobjectgetplayer) | WorldObject | Gets a worldobject representing the current player |
| [`wobjectgetselection[]`](#wobjectgetselection) | WorldObject | Gets a worldobject representing the currently selected object |
| [`wobjectgetopencontainer[]`](#wobjectgetopencontainer) | WorldObject | Gets a worldobject representing the currently opened container, or 0 if none |
| [`wobjectfindbyid[id]`](#wobjectfindbyid) | WorldObject | Gets a worldobject representing the object with the passed id |
| [`wobjectfindnearestmonster[]`](#wobjectfindnearestmonster) | WorldObject | Gets a worldobject representing the nearest monster, or 0 if none was found |
| [`wobjectfindnearestdoor[]`](#wobjectfindnearestdoor) | WorldObject | Gets a worldobject representing the nearest door, or 0 if none was found |
| [`wobjectfindnearestbyobjectclass[objectclass]`](#wobjectfindnearestbyobjectclass) | WorldObject | Gets a worldobject representing the nearest object matching objectclass, or 0 if none was found |
| [`wobjectfindnearestbynameandobjectclass[objectclass, namerx]`](#wobjectfindnearestbynameandobjectclass) | WorldObject | Gets a worldobject representing the first object matching objectclass and name regex, or 0 if none was found |
| [`wobjectfindnearestbytemplatetype[templatetype]`](#wobjectfindnearestbytemplatetype) | WorldObject | Attempts to find the nearest landscape world object with the specified template type |
| [`wobjectfindininventorybyname[name]`](#wobjectfindininventorybyname) | WorldObject | Gets a worldobject representing the first inventory item matching an exact name, or 0 if none was found |
| [`wobjectfindininventorybynamerx[namerx]`](#wobjectfindininventorybynamerx) | WorldObject | Gets a worldobject representing the first inventory item matching name regex, or 0 if none was found |
| [`wobjectfindininventorybytemplatetype[templatetype]`](#wobjectfindininventorybytemplatetype) | WorldObject | Gets a worldobject representing the first inventory item matching template type, or 0 if none was found |
| [`wobjectfindall[]`](#wobjectfindall) | List | Gets a list of all worldobjects known by the client |
| [`wobjectfindallbyobjectclass[objectclass]`](#wobjectfindallbyobjectclass) | List | Gets a list of all worldobjects of the passed objectclass |
| [`wobjectfindallbytemplatetype[templatetype]`](#wobjectfindallbytemplatetype) | List | Gets a list of all wobjects matching templatetype |
| [`wobjectfindallbynamerx[namerx]`](#wobjectfindallbynamerx) | List | Gets a list of all wobjects matching the passed regular expression |
| [`wobjectfindallbycontainer[container]`](#wobjectfindallbycontainer) | List | Gets a list of all worldobjects inside the specified container |
| [`wobjectfindallinventory[]`](#wobjectfindallinventory) | List | Gets a list of all worldobjects in the players inventory |
| [`wobjectfindallinventorybyobjectclass[objectclass]`](#wobjectfindallinventorybyobjectclass) | List | Gets a list of all inventory worldobjects of the passed objectclass |
| [`wobjectfindallinventorybytemplatetype[templatetype]`](#wobjectfindallinventorybytemplatetype) | List | Gets a list of all inventory items matching templatetype |
| [`wobjectfindallinventorybynamerx[namerx]`](#wobjectfindallinventorybynamerx) | List | Gets a list of all inventory wobjects with name matching the passed regular expression |
| [`wobjectfindalllandscape[]`](#wobjectfindalllandscape) | List | Gets a list of all worldobjects in the landscape |
| [`wobjectfindalllandscapebyobjectclass[objectclass]`](#wobjectfindalllandscapebyobjectclass) | List | Gets a list of all landscape worldobjects of the passed objectclass |
| [`wobjectfindalllandscapebytemplatetype[templatetype]`](#wobjectfindalllandscapebytemplatetype) | List | Gets a list of all landscape items matching templatetype |
| [`wobjectfindalllandscapebynamerx[namerx]`](#wobjectfindalllandscapebynamerx) | List | Gets a list of all landscape wobjects with name matching the passed regular expression |

### World Objects: Properties

Reading data from a world object.

| Function | Returns | Summary |
| --- | --- | --- |
| [`wobjectgetname[wo]`](#wobjectgetname) | string | Gets the name string of a wobject |
| [`wobjectgetid[wo]`](#wobjectgetid) | number | Gets the id of a wobject |
| [`wobjectgetobjectclass[wo]`](#wobjectgetobjectclass) | number | Gets the objectclass as a number from a wobject |
| [`wobjectgettemplatetype[wo]`](#wobjectgettemplatetype) | number | Gets the template type as a number from a wobject |
| [`wobjectgetintprop[obj, property]`](#wobjectgetintprop) | number | Returns an int property from a specific world object, or 0 if it’s undefined |
| [`wobjectgetdoubleprop[obj, property]`](#wobjectgetdoubleprop) | number | Returns a double property from a specific world object, or 0 if it’s undefined |
| [`wobjectgetboolprop[obj, property]`](#wobjectgetboolprop) | number | Returns a bool property from a specific world object, or 0 if it’s undefined |
| [`wobjectgetstringprop[obj, property]`](#wobjectgetstringprop) | string | Returns a string property from a specific world object, or and empty string if it’s undefined |
| [`wobjectgethealth[obj]`](#wobjectgethealth) | number | Gets the specified mob/player wobject current health percentage |
| [`wobjectgetspellids[obj]`](#wobjectgetspellids) | List | Returns a list of spells on a World Object |
| [`wobjectgetactivespellids[obj]`](#wobjectgetactivespellids) | List | Returns a list of active spells on a World Object |
| [`wobjectgetisdooropen[wo]`](#wobjectgetisdooropen) | number | Checks if a door wobject is open |
| [`wobjectisvalid[obj]`](#wobjectisvalid) | number | Checks whether the passed world object is still valid (exists in the client) |
| [`wobjecthasdata[obj]`](#wobjecthasdata) | number | Checks if a wobject has assess data from the server |
| [`wobjectrequestdata[obj]`](#wobjectrequestdata) | number | Requests assess data for a wobject from the server |
| [`wobjectlastidtime[obj]`](#wobjectlastidtime) | number | Gets the last time a wobjects data was recieved from the server |

### Inventory and Containers

Item counts and container slot information.

| Function | Returns | Summary |
| --- | --- | --- |
| [`getitemcountininventorybyname[name]`](#getitemcountininventorybyname) | number | Counts how many items you have in your inventory exactly matching `name` |
| [`getitemcountininventorybynamerx[namerx]`](#getitemcountininventorybynamerx) | number | Counts how many items you have in your inventory matching regex `namerx` |
| [`getinventorycountbytemplatetype[templatetype]`](#getinventorycountbytemplatetype) | number | Counts how many items you have in your inventory of a certain template type |
| [`getfreeitemslots[container?]`](#getfreeitemslots) | number | Gets the number of free item slots in a container |
| [`getfreecontainerslots[container?]`](#getfreecontainerslots) | number | Gets the number of free container slots |
| [`getcontaineritemcount[container?]`](#getcontaineritemcount) | number | Gets the number of items in a container |

### Actions

Selecting, using, giving, casting, salvaging, and movement.

| Function | Returns | Summary |
| --- | --- | --- |
| [`actiontryselect[obj]`](#actiontryselect) | number | Attempts to select a worldobject |
| [`actiontryuseitem[obj]`](#actiontryuseitem) | number | Attempts to use a worldobject |
| [`actiontryapplyitem[useObj, onObj]`](#actiontryapplyitem) | number | Attempts to use a worldobject on another worldobject |
| [`actiontrygiveitem[give, destination]`](#actiontrygiveitem) | number | Attempts to give a worldobject to another worlobject, like an npc |
| [`actiontrygiveprofile[lootprofile, target]`](#actiontrygiveprofile) | number | Gives all items matching the specified loot profile to a character or npc |
| [`actiontryequipanywand[]`](#actiontryequipanywand) | number | Attempts to take one step towards equipping any wand from the current profile’s items list |
| [`actiontrycastbyid[spellId]`](#actiontrycastbyid) | number | Attempts to cast a spell by id |
| [`actiontrycastbyidontarget[spellId, target]`](#actiontrycastbyidontarget) | number | Attempts to cast a spell by id on a worldobject |
| [`ustopen[]`](#ustopen) | number | Opens the ust panel |
| [`ustadd[obj]`](#ustadd) | number | Adds an item to the ust panel |
| [`ustsalvage[]`](#ustsalvage) | number | Salvages the items in the ust panel |
| [`setmotion[motion, state]`](#setmotion) | number | Sets a characters wanted motion state |
| [`getmotion[motion]`](#getmotion) | number | Gets a character motion state |
| [`clearmotion[]`](#clearmotion) | number | Clears all motion states |

### Fellowship

Fellowship status and membership.

| Function | Returns | Summary |
| --- | --- | --- |
| [`getfellowshipstatus[]`](#getfellowshipstatus) | number | Checks if you are currently in a fellowship |
| [`getfellowshipname[]`](#getfellowshipname) | string | Gets the name of your current fellowship |
| [`getfellowshipcount[]`](#getfellowshipcount) | number | Gets the number of members in your current fellowship |
| [`getfellowshipleaderid[]`](#getfellowshipleaderid) | number | Returns the character id of the fellowship leader |
| [`getfellowshipisleader[]`](#getfellowshipisleader) | number | Returns 1 if you are the leader of your fellowship |
| [`getfellowshiplocked[]`](#getfellowshiplocked) | number | Returns 1 if your fellowship is Locked |
| [`getfellowshipisopen[]`](#getfellowshipisopen) | number | Returns 1 if your fellowship is open |
| [`getfellowshipisfull[]`](#getfellowshipisfull) | number | Returns 1 if you’re in a fellowship, and it is full |
| [`getfellowshipcanrecruit[]`](#getfellowshipcanrecruit) | number | Returns 1 if your fellowship is open, or you are the leader, and the fellowship is not full |
| [`getfellowid[iterator]`](#getfellowid) | number | Returns a fellowship member’s character id |
| [`getfellowname[iterator]`](#getfellowname) | string | Returns a fellowship member’s character name |
| [`getfellowids[]`](#getfellowids) | List | Returns a list of character ids in your current fellowship, sorted numerically |
| [`getfellownames[]`](#getfellownames) | List | Returns a list of character names in your current fellowship, sorted alphabetically |

### Quests

Quest flags and kill tasks (data from `/myquests`).

| Function | Returns | Summary |
| --- | --- | --- |
| [`testquestflag[questflag]`](#testquestflag) | number | Checks if a quest flag has ever been completed |
| [`getqueststatus[questflag]`](#getqueststatus) | number | Checks if a quest flag has a timer on it |
| [`getquestktprogress[questflag]`](#getquestktprogress) | number | Checks progress of a killtask quest |
| [`getquestktrequired[questflag]`](#getquestktrequired) | number | Checks total required solves of a killtask quest |
| [`isrefreshingquests[]`](#isrefreshingquests) | number | Checks if quests are currently being fetched or processed |

### Time and Stopwatches

Real-world time and elapsed-time counters.

| Function | Returns | Summary |
| --- | --- | --- |
| [`getdatetimelocal[text]`](#getdatetimelocal) | string | Gets the local date and time using the format provided |
| [`getdatetimeutc[text]`](#getdatetimeutc) | string | Gets the universal date and time using the format provided |
| [`getunixtime[]`](#getunixtime) | string | Gets the total number of seconds since the unix epoch (jan 1, 1970) |
| [`stopwatchcreate[]`](#stopwatchcreate) | Stopwatch | Creates a new stopwatch object |
| [`stopwatchstart[watch]`](#stopwatchstart) | Stopwatch | Starts a stopwatch if not already started |
| [`stopwatchstop[watch]`](#stopwatchstop) | Stopwatch | Stops a stopwatch if not already stopped |
| [`stopwatchelapsedseconds[watch]`](#stopwatchelapsedseconds) | number | Gets the amount of seconds a stopwatch has been running for |

### Game Time

Dereth calendar and day/night cycle.

| Function | Returns | Summary |
| --- | --- | --- |
| [`getgameyear[]`](#getgameyear) | number | Gets the current ingame year |
| [`getgamemonth[]`](#getgamemonth) | number | Gets the current ingame month as a number |
| [`getgamemonthname[monthIndex]`](#getgamemonthname) | string | Returns the name of a month index |
| [`getgameday[]`](#getgameday) | number | Returns the current ingame day of the month |
| [`getgamehour[]`](#getgamehour) | number | Returns the current ingame hour of the day |
| [`getgamehourname[hourIndex]`](#getgamehourname) | string | Returns the name of a hour index |
| [`getgameticks[]`](#getgameticks) | number | Returns the current ingame ticks |
| [`getisday[]`](#getisday) | number | Returns 1 if it is currently day time, 0 otherwise |
| [`getisnight[]`](#getisnight) | number | Returns 1 if it is currently night time, 0 otherwise |
| [`getminutesuntilday[]`](#getminutesuntilday) | number | Returns real world minutes left until the next day cycle |
| [`getminutesuntilnight[]`](#getminutesuntilnight) | number | Returns real world minutes left until the next night cycle |

### VTank Integration

Meta state and VTank settings.

| Function | Returns | Summary |
| --- | --- | --- |
| [`vtgetmetastate[]`](#vtgetmetastate) | string | Gets the current vtank meta state as a string |
| [`vtsetmetastate[state]`](#vtsetmetastate) | number | Changes the current vtank meta state |
| [`vtgetsetting[setting]`](#vtgetsetting) | string | Gets the value of a vtank setting |
| [`vtsetsetting[setting, value]`](#vtsetsetting) | number | Sets the specified vtank setting |

### Meta Views (UI)

Controls in meta views.

| Function | Returns | Summary |
| --- | --- | --- |
| [`uigetcontrol[windowName, controlName]`](#uigetcontrol) | UIControl | Gets a reference to the named control in the named window |
| [`uisetlabel[control, label]`](#uisetlabel) | number | Changes the label text for the given UIControl |
| [`uisetvisible[control, visible]`](#uisetvisible) | number | Changes the visibility for the given UIControl |
| [`uiviewexists[windowName]`](#uiviewexists) | number | Checks if a meta view exists |
| [`uiviewvisible[windowName]`](#uiviewvisible) | number | Checks if a meta view is visible |

### UtilityBelt, Network, and Client

UB settings, networked clients, and client/account info.

| Function | Returns | Summary |
| --- | --- | --- |
| [`uboptget[name]`](#uboptget) | Object | Returns the value of a ub setting |
| [`uboptset[name, newValue]`](#uboptset) | Object | Changes the value of a ub setting |
| [`netclients[tag?]`](#netclients) | List | Gets a list of network clients, optionally filtered by tag |
| [`getworldname[]`](#getworldname) | string | Gets the name of the currently connected world/server |
| [`getaccounthash[]`](#getaccounthash) | string | Returns a hash of the current account name |

## Function Reference


### Variables reference

#### testvar

`testvar[varname]` → number

Checks if a variable is defined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to check |

**Returns:** number — Returns 1 if the variable is defined, 0 if it isn’t

**Examples:**

- `testvar[myvar]` — Returns 1 if `myvar` variable is defined

#### getvar

`getvar[varname]` → Object

Returns the value stored in a variable

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to get |

**Returns:** Object — Returns the value of a variable, or 0 if undefined

**Examples:**

- `getvar[myvar]` — Returns the value stored in `myvar` variable

#### setvar

`setvar[varname, value]` → Object

Stores a value in a variable

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to set |
| 1 | `value` | Object | Value to store |

**Returns:** Object — Returns the newly set value

**Examples:**

- `setvar[myvar,1]` — Stores the number value `1` inside of `myvar` variable

#### touchvar

`touchvar[varname]` → number

Sets the value of a variable to 0 if the variable was previously undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to touch |

**Returns:** number — Returns 1 if the variable was previously defined, 0 otherwise

**Examples:**

- `touchvar[myvar]` — Ensures that `myvar` has a value set

#### clearvar

`clearvar[varname]` → number

Clears the value of a variable

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to clear |

**Returns:** number — Returns 1 if the variable was defined, 0 otherwise

**Examples:**

- `clearvar[myvar]` — Clears the value stored in `myvar` variable

#### clearallvars

`clearallvars[]` → number

Unsets all variables

**Returns:** number — Returns 1

**Examples:**

- `clearallvars[]` — Unset all variables

#### testpvar

`testpvar[varname]` → number

Checks if a persistent variable is defined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to check |

**Returns:** number — Returns 1 if the variable is defined, 0 if it isn’t

**Examples:**

- `testpvar[myvar]` — Returns 1 if `myvar` persistent variable is defined

#### getpvar

`getpvar[varname]` → Object

Returns the value stored in a persistent variable

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to get |

**Returns:** Object — Returns the value of a variable, or 0 if undefined

**Examples:**

- `getpvar[myvar]` — Returns the value stored in the persistent `myvar` variable

#### setpvar

`setpvar[varname, value]` → Object

Stores a value in a persistent variable that is available ever after relogging. Persistent variables are not shared between characters.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to set |
| 1 | `value` | Object | Value to store |

**Returns:** Object — Returns the newly set value

**Examples:**

- `setpvar[myvar,1]` — Stores the number value `1` inside of `myvar` variable

#### touchpvar

`touchpvar[varname]` → number

Sets the value of a persistent variable to 0 if the variable was previously undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to touch |

**Returns:** number — Returns 1 if the variable was previously defined, 0 otherwise

**Examples:**

- `touchpvar[myvar]` — Ensures that `myvar` has a value set

#### clearpvar

`clearpvar[varname]` → number

Clears the value of a persistent variable

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to clear |

**Returns:** number — Returns 1 if the variable was defined, 0 otherwise

**Examples:**

- `clearpvar[myvar]` — Clears the value stored in `myvar` persistent variable

#### clearallpvars

`clearallpvars[]` → number

Unsets all persistent variables

**Returns:** number — Returns 1

**Examples:**

- `clearallpvars[]` — Unset all persistent variables

#### testgvar

`testgvar[varname]` → number

Checks if a global variable is defined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to check |

**Returns:** number — Returns 1 if the variable is defined, 0 if it isn’t

**Examples:**

- `testgvar[myvar]` — Returns 1 if `myvar` global variable is defined

#### getgvar

`getgvar[varname]` → Object

Returns the value stored in a global variable

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to get |

**Returns:** Object — Returns the value of a variable, or 0 if undefined

**Examples:**

- `getgvar[myvar]` — Returns the value stored in `myvar` global variable

#### setgvar

`setgvar[varname, value]` → Object

Stores a value in a global variable. This variable is shared between all characters on the same server.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to set |
| 1 | `value` | Object | Value to store |

**Returns:** Object — Returns the newly set value

**Examples:**

- `setgvar[myvar,1]` — Stores the number value `1` inside of global `myvar` variable

#### touchgvar

`touchgvar[varname]` → number

Sets the value of a global variable to 0 if the variable was previously undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to touch |

**Returns:** number — Returns 1 if the variable was previously defined, 0 otherwise

**Examples:**

- `touchgvar[myvar]` — Ensures that `myvar` global variable has a value set

#### cleargvar

`cleargvar[varname]` → number

Clears the value of a global variable

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `varname` | string | Variable name to clear |

**Returns:** number — Returns 1 if the variable was defined, 0 otherwise

**Examples:**

- `cleargvar[myvar]` — Clears the value stored in `myvar` global variable

#### clearallgvars

`clearallgvars[]` → number

Unsets all global variables

**Returns:** number — Returns 1

**Examples:**

- `clearallgvars[]` — Unset all global variables


### Logic and Evaluation reference

#### iif

`iif[value, truevalue, falsevalue]` → Object

Checks if the first parameter is true, if so returns the second argument. If the first parameter is false or not a number, returns the third argument. Both arguments will always be evaluated. If you want conditional evaluation use `ifthen[]`

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `value` | Object | value to check |
| 1 | `truevalue` | Object | value to return if value is true |
| 2 | `falsevalue` | Object | value to return if value is false |

**Returns:** Object — Returns `truevalue` if value is true, otherwise `falsevalue`

**Examples:**

- `iif[1,2,3]` — Returns 2 (second param) because 1 (first param) is true

#### ifthen

`ifthen[value, trueexpr, falseexpr?]` → Object

If value is truthy, will execute the trueexpr string and return the results. If value is not truthy it will evaluate falseexpr string and return the results

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `value` | Object | value to check for truthiness |
| 1 | `trueexpr` | string | expression string to evaluate if value is truthy |
| 2 | `falseexpr` *(optional)* | string | expression string to evaluate if value is not truthy, optional |

**Returns:** Object — Returns the result of evaluating the conditional expression string

**Examples:**

- `` ifthen[1, `chatbox[\`true\`]`,`chatbox[\`false\`]`] `` — writes `true` to the chatbox
- `` ifthen[0, `chatbox[\`true\`]`,`chatbox[\`false\`]`] `` — writes `false` to the chatbox

#### istrue

`istrue[value]` → number

Checks if a value is equal to true (1)

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `value` | Object | value to check |

**Returns:** number — Returns 1 if value is true, 0 otherwise

**Examples:**

- `istrue[1]` — Checks that 1 is true, and returns true because it is

#### isfalse

`isfalse[value]` → number

Checks if a value is equal to false (0)

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `value` | Object | value to check |

**Returns:** number — Returns 1 if value is false, 0 otherwise

**Examples:**

- `isfalse[0]` — Checks that 0 is false, and returns true because it is

#### exec

`exec[text]` → Object

Evaluates a string as an expression

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `text` | string | The expression string to evaluate |

**Returns:** Object — Returns the result of evaluating the expression string

**Examples:**

- `` exec[`1+1`] `` — returns 2

#### delayexec

`delayexec[delay, text]` → Object

Evaluates a string as an expression, after the specified delay

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `delay` | number | The delay in milliseconds. 1 second = 1000 milliseconds. |
| 1 | `text` | string | The expression string to evaluate |

**Returns:** Object — Returns 1

**Examples:**

- `` delayexec[1000, `1+1`] `` — evaluates the expression `1+1` after a 1000ms (1s) delay


### Strings and Conversion reference

#### tostring

`tostring[obj]` → Object

Converts an object to a string

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | Object | The object to convert to a string |

**Returns:** Object — Returns the object, converted to a string

**Examples:**

- `tostring[2]` — returns the string `2`

#### cstr

`cstr[number]` → string

Converts a number to a string

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `number` | number | number to convert |

**Returns:** string — Returns a string representation of number

**Examples:**

- `cstr[2]` — Returns a string of `2`

#### cstrf

`cstrf[number, format]` → string

Converts a number to a string using a specified format

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `number` | number | number to convert |
| 1 | `format` | string | string format. See: [http://msdn.microsoft.com/en-us/library/kfsatb94.aspx](http://msdn.microsoft.com/en-us/library/kfsatb94.aspx) |

**Returns:** string — formatted string, from a number

**Examples:**

- `` cstrf[3.14159,`N3`] `` — Formats 3.14159 to a string with 3 decimal places

#### cnumber

`cnumber[number]` → number

Converts a string to a floating point number

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `number` | string | string number to convert |

**Returns:** number — floating point number from a string

**Examples:**

- `` cnumber[`3.14159`] `` — Converts `3.14159` to a number

#### hexstr

`hexstr[d]` → string

converts a number to a hexadecimal string

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | the number to convert |

**Returns:** string — Returns a string

#### strlen

`strlen[tocheck]` → number

Gets the length of a string

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `tocheck` | string | string to check |

**Returns:** number — Returns the length of tocheck string

**Examples:**

- `strlen[test]` — Returns a length of 4

#### ord

`ord[character]` → number

returns an integer representing the Unicode character

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `character` | string | string to convert |

**Returns:** number — number representing unicode character

**Examples:**

- `ord[c]` — Converts ‘c’ to a number representing its unicode character (99)

#### chr

`chr[character]` → string

returns a string representing a character whose Unicode code point is an integer.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `character` | number | number to convert |

**Returns:** string — string representing unicode character

**Examples:**

- `chr[99]` — Converts `99` to a string representing its unicode character ‘c’

#### getregexmatch

`getregexmatch[text, regex]` → string

Returns the part of the text that matches the specified regex

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `text` | string | The text to check the regex against |
| 1 | `regex` | string | The regex |

**Returns:** string — Returns the part of the text that matches the specified regex

**Examples:**

- `` getregexmatch[`test 123`,`\d+`] `` — Returns the string `123`

#### getobjectinternaltype

`getobjectinternaltype[tocheck]` → number

Gets internal type of an object, as a number

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `tocheck` | Object | object to check |

**Returns:** number — Values are: 0=none, 1=number, 3=string, 7=object

**Examples:**

- `getobjectinternaltype[test]` — Returns 3 (string)


### Math reference

#### abs

`abs[number]` → number

Returns the absolute value of a number

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `number` | number | number to get absolute value of |

**Returns:** number — Returns a positive number

**Examples:**

- `abs[-3.14159]` — returns 3.14159

#### floor

`floor[number]` → number

returns the largest integer less than or equal to a given number.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `number` | number | number to floor |

**Returns:** number — Returns a whole number

**Examples:**

- `floor[3.14159]` — returns 3

#### ceiling

`ceiling[number]` → number

rounds a number up to the next largest whole number or integer

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `number` | number | number to ceil |

**Returns:** number — Returns a whole number

**Examples:**

- `ceiling[3.14159]` — returns 4

#### round

`round[number]` → number

returns the value of a number rounded to the nearest integer

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `number` | number | number to round |

**Returns:** number — Returns a whole number

**Examples:**

- `round[3.14159]` — returns 3

#### randint

`randint[min, max]` → number

Generates a random number between min and (max-1)

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `min` | number | minimum value to return |
| 1 | `max` | number | maximum value to return (-1) |

**Returns:** number — Returns a number between min and (max-1)

**Examples:**

- `randint[0,2]` — Returns a random number, 0 or 1, but not 2

#### sqrt

`sqrt[d]` → number

Returns the square root of a specified number.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | The number whose square root is to be found |

**Returns:** number — Returns the square root of a specified number.

#### sin

`sin[d]` → number

Returns the sine of the specified angle.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | An angle, measured in radians. |

**Returns:** number — Returns the sine of the specified angle.

#### cos

`cos[d]` → number

Returns the cosine of the specified angle.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | An angle, measured in radians. |

**Returns:** number — Returns the cosine of the specified angle.

#### tan

`tan[d]` → number

Returns the tangent of the specified angle.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | An angle, measured in radians. |

**Returns:** number — Returns the tangent of the specified angle.

#### asin

`asin[d]` → number

Returns the angle whose sine is the specified number

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | A number representing a sine, where d must be greater than or equal to -1, but less than or equal to 1. |

**Returns:** number — Returns the angle whose sine is the specified number

#### acos

`acos[d]` → number

Returns the angle whos cosine is the specified number

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | A number representing a cosine, where d must be greater than or equal to -1, but less than or equal to 1. |

**Returns:** number — Returns the angle whos cosine is the specified number

#### atan

`atan[d]` → number

Returns the angle whose tangent is the specified number.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | A number representing a tangent. |

**Returns:** number — Returns the angle whose tangent is the specified number.

#### atan2

`atan2[y, x]` → number

Returns the angle whose tangent is the quotient of two specified numbers.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `y` | number | The y coordinate of a point. |
| 1 | `x` | number | The x coordinate of a point. |

**Returns:** number — Returns the angle whose tangent is the quotient of two specified numbers.

#### sinh

`sinh[d]` → number

Returns the hyperbolic sine of the specified angle.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | An angle, measured in radians. |

**Returns:** number — Returns the hyperbolic sine of the specified angle.

#### cosh

`cosh[d]` → number

Returns the hyperbolic cosine of the specified angle.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | An angle, measured in radians. |

**Returns:** number — Returns the hyperbolic cosine of the specified angle.

#### tanh

`tanh[d]` → number

Returns the hyperbolic tangent of the specified angle.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `d` | number | An angle, measured in radians. |

**Returns:** number — Returns the hyperbolic tangent of the specified angle.


### Lists reference

#### listcreate

`listcreate[...items]` → List

Creates a new list object. The list is empty by default.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `items` | …items | items to instantiate the list with |

**Returns:** List — Creates and returns a new list

**Examples:**

- `listcreate[]` — Returns a new empty list
- `listcreate[1,2,3]` — Returns a new list with 3 items: 1,2,3
- `setvar[myList,listcreate[]]` — Creates a new list and stores it in `myList` variable

#### listfromrange

`listfromrange[start, end]` → List

Create a list of numbers between start and end inclusively

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `start` | number | inclusive start |
| 1 | `end` | number | inclusive end |

**Returns:** List — Returns a list of numbers between start to end, inclusive

**Examples:**

- `listfromrange[1,10]` — returns a list with numbers 1-10

#### listadd

`listadd[list, item]` → List

Adds an item to the end of a list

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to add the item to |
| 1 | `item` | Object | Item to add to list, can be any type. Lists cannot contain references to themselves. |

**Returns:** List — Returns the list object

**Examples:**

- `` listadd[getvar[myList],`some value`] `` — Adds `some value` string to the list stored in `myList` variable

#### listinsert

`listinsert[list, item, index]` → List

Inserts an item at the specified index of the list

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to add the item to |
| 1 | `item` | Object | Item to add to list, can be any type. Lists cannot contain references to themselves. |
| 2 | `index` | number | The index in the list where the item should be inserted |

**Returns:** List — Returns the list object

**Examples:**

- `` listinsert[getvar[myList],`some value`,1] `` — Inserts `some value` at index 1 (second item) in the list

#### listremove

`listremove[list, item]` → List

Removes the first occurrence of an item from the list

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to remove the item from |
| 1 | `item` | Object | Item to remove from the list. |

**Returns:** List — Returns the list object

**Examples:**

- `` listremove[getvar[myList],`some value`] `` — Removes the first occurrence of `some value` string from the list stored in `myList` variable

#### listremoveat

`listremoveat[list, index]` → List

Removes the specified index from the list

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to remove the item from |
| 1 | `index` | number | Index to remove from the list. |

**Returns:** List — Returns the list object

**Examples:**

- `listremoveat[getvar[myList],1]` — Removes the second item from the list stored in `myList` variable

#### listpop

`listpop[list, index?]` → Object

Removes the value at the specified index of the array and returns the item. If no index is passed, it uses the index of the last item in the array. Indexes are zero based.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to pop the item from |
| 1 | `index` *(optional)* | number | Optional index to remove, if not provided, uses the index of the last item in the array (default: `-1`) |

**Returns:** Object — Returns the value removed from the array

**Examples:**

- `listpop[getvar[myList]]` — Removes and returns the last item in the list stored in myList variable
- `listpop[getvar[myList],0]` — Removes and returns the first item in the list stored in myList variable

#### listclear

`listclear[list]` → List

Clears all items from the specified list

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to clear |

**Returns:** List — Returns the list

**Examples:**

- `listclear[getvar[myList]]` — Removes all items from the list stored in the variable `myList`

#### listgetitem

`listgetitem[list, index]` → Object

Retrieves the item at the specified list index. Lists are zero based, so the first item in the list is at index 0. If the index does not exist, it throws an error

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to retrieve the item from |
| 1 | `index` | number | Index to retrieve from the list. Lists are zero based, so the first item is at index 0 |

**Returns:** Object — Returns the item at the specified list index

**Examples:**

- `listgetitem[getvar[myList],0]` — Retrieves the first item from the list stored in `myList` variable

#### listcontains

`listcontains[list, item]` → number

Checks if the specified item is contained in the list

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to check |
| 1 | `item` | Object | The item to check if the list contains |

**Returns:** number — Returns 1 if found, 0 otherwise

**Examples:**

- `` listcontains[getvar[myList],`some value`] `` — Checks if the list stored in `myList` variable contains the string `some value`

#### listindexof

`listindexof[list, item]` → number

Finds the index of the first occurrence of an item in a list. Indexes are zero based.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to check |
| 1 | `item` | Object | The item to check if the list contains |

**Returns:** number — Returns the index of the first occurrence of the specified item, or -1 if not found

**Examples:**

- `` listindexof[getvar[myList],`some value`] `` — Returns the index of the first occurrence of the string `some value`

#### listlastindexof

`listlastindexof[list, item]` → number

Finds the index of the last occurrence of an item in a list. Indexes are zero based.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to check |
| 1 | `item` | Object | The item to check if the list contains |

**Returns:** number — Returns the index of the last occurrence of the specified item, or -1 if not found

**Examples:**

- `` listlastindexof[getvar[myList],`some value`] `` — Returns the index of the last occurrence of the string `some value`

#### listcount

`listcount[list]` → number

Counts the number of items contained in a list

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to count the items in |

**Returns:** number — Returns a count of how many items are contained in the list

**Examples:**

- `listcount[getvar[myList]]` — Returns the number of items stored in the list stored in `myList` variable

#### listcopy

`listcopy[list]` → List

Creates a copy of a list

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to copy |

**Returns:** List — Returns a copy of the specified list

**Examples:**

- `setvar[myListTwo,listcopy[getvar[myList]]]` — Copies the list stored in myList variable to a variable called myListTwo

#### listreverse

`listreverse[list]` → List

Creates a copy of a list, and reverses the order

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to reverse |

**Returns:** List — Returns a copy of the specified list, but the order is reversed

**Examples:**

- `setvar[myListReversed,listreverse[getvar[myList]]]` — Copies the list stored in myList variable, reverses it, and stores it to a variable called myListReversed

#### listfilter

`listfilter[list, expression]` → List

Creates a new list filtered by the specified expression

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to filter |
| 1 | `expression` | string | The expression to use for filtering. $0 (getvar[`0`]) will be set to the current iteration count, $1 will be set to the current item. return 1 to include, 0 to exclude |

**Returns:** List — Returns a new filtered list

**Examples:**

- `` listfilter[wobjectfindallbyobjectclass[24],`wobjectgetintprop[$1,25]==275`] `` — creates a new list with all nearby level 275 characters

#### listmap

`listmap[list, expression]` → List

Creates a new list mapped by the specified expression

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to map |
| 1 | `expression` | string | The expression to use for mapping. $0 (getvar[`0`]) will be set to the current iteration count, $1 will be set to the current item. return the mapped result |

**Returns:** List — Returns a new mapped list

**Examples:**

- `` listmap[wobjectfindallbyobjectclass[24],`wobjectgetstringprop[$1,5]`] `` — creates a new list with all nearby players' titles

#### listreduce

`listreduce[list, expression]` → Object

Reduces a list down into a single value

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to reduce to a single value |
| 1 | `expression` | string | The expression to use for reducing. $0 (getvar[`0`]) will be set to the current iteration count, $1 will be set to the current item, $2 will be set to the current reduced value, return the modified result |

**Returns:** Object — Returns the final reduced value

**Examples:**

- `` listreduce[wobjectfindallbyobjectclass[24],`$2+wobjectgetintprop[$1,25]`] `` — find the sum of all nearby character levels

#### listsort

`listsort[list, expression]` → List

Creates a list from another, sorted by expression

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `list` | List | The list to sort |
| 1 | `expression` | string | The expression to use for sorting. $1 (getvar[`1`]) will be set to item a, $2 will be set item b, return 0 if they are equal, -1 if $1 is less than $2, and 1 if $1 is greater than $2 |

**Returns:** List — Returns a new list sorted by expression

**Examples:**

- `` listsort[wobjectfindallbyobjectclass[24],`setvar[la,wobjectgetintprop[$1,25]];setvar[lb,wobjectgetintprop[$2,25]];iif[$la==$lb,0,iif[$la<$lb,-1,1]]`] `` — get a list of all characters sorted by level


### Dictionaries reference

#### dictcreate

`dictcreate[...items]` → Dictionary

Creates a new dictionary object. The dictionary is empty by default. The list of arguments must be even and broken in key-value pairs. Keys must be strings. Values may be any type.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `items` | …items | items to instantiate the dictionary with. alternates keys and values |

**Returns:** Dictionary — Creates and returns a new dictionary

**Examples:**

- `dictcreate[]` — Returns a new empty dictionary
- `dictcreate[a,2,b,listcreate[4,5]]` — Returns a new dictionary with 2 items: a=>2,b=>[4,5]
- `setvar[myDict,dictcreate[]]` — Creates a new empty dictionary and stores it in `myDict` variable

#### dictgetitem

`dictgetitem[dict, key]` → Object

Retrieves the item at the specified dictionary key. If the key does not exist, it throws an error

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dict to retrieve the item from |
| 1 | `key` | string | Key to retrieve from the dict. |

**Returns:** Object — Returns the item for the specified key

**Examples:**

- `dictgetitem[getvar[myDict],foo]` — Retrieves the item stored in `myDict` for the key `foo`

#### dictadditem

`dictadditem[dict, key, value]` → number

Adds the Key Value pair to the dictionary. Returns true if it overwrote a key, false otherwise. Will throw an exception if the key is not a string

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dict to add key item pair to |
| 1 | `key` | string | Key to add to the dict |
| 2 | `value` | Object | Value to add to the dict |

**Returns:** number — Returns True if it overwrote a key, False otherwise

**Examples:**

- `dictadditem[getvar[myDict],foo,1]` — adds a key foo to `myDict` and sets the value to 1

#### dicthaskey

`dicthaskey[dict, key]` → number

Checks if the dictionary contains the key. Returns true if it does, false otherwise

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dict to check for a key |
| 1 | `key` | string | Key to look for |

**Returns:** number — Returns True if dictionary has the key, False otherwise

**Examples:**

- `dicthaskey[getvar[myDict],foo]` — Returns true if `myDict` has the key `foo`, false otherwise

#### dictremovekey

`dictremovekey[dict, key]` → number

Deletes the specified key from the dictionary.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dict to delete the key from |
| 1 | `key` | string | Key to remove |

**Returns:** number — Returns True if dictionary had the key, False otherwise

**Examples:**

- `dictremovekey[getvar[myDict],foo]` — Removes the key `foo` from the dictionary `myDict`

#### dictkeys

`dictkeys[dict]` → List

Returns a list of keys

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dictionary to return keys from |

**Returns:** List — Returns a list of keys

**Examples:**

- `dictkeys[getvar[myDict]]` — Returns a list of all the keys in `myDict`

#### dictvalues

`dictvalues[dict]` → List

Returns a list of values

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dictionary to return values from |

**Returns:** List — Returns a list of values

**Examples:**

- `dictvalues[getvar[myDict]]` — Returns a list of all the values in `myDict`

#### dictsize

`dictsize[dict]` → number

Returns the size of the dictionary

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dictionary to return size of |

**Returns:** number — Returns a number size

**Examples:**

- `dictsize[dictcreate[a,b,c,d]]` — Returns 2 as the example dict is `a=>b, c=>d`

#### dictclear

`dictclear[dict]` → Dictionary

Clears the dictionary contents and returns the dictionary

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dictionary to clear |

**Returns:** Dictionary — Returns the dictionary

**Examples:**

- `dictclear[getvar[myDict]]` — Returns the empty dictionary `myDict`

#### dictcopy

`dictcopy[dict]` → Dictionary

Creates a new dictionary and copies over all the key value pairs. This is a shallow copy.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `dict` | Dictionary | The dictionary copy |

**Returns:** Dictionary — Returns the new copy

**Examples:**

- `setvar[myDict2,dictcopy[getvar[myDict]]]` — Creates a shallow copy of `myDict` and stores it in `myDict2`


### Chat and HUD Output reference

#### echo

`echo[message, color]` → number

Echos chat to the chatbox with a color. Use `/ub printcolors` to view all colors

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `message` | string | Message to echo |
| 1 | `color` | number | Message color |

**Returns:** number — Returns 1 on success, 0 on failure

**Examples:**

- `echo[test,15]` — echos ‘test’ to the chatbox in red (Help)

#### chatbox

`chatbox[message]` → string

Sends a message to the chatbox as if you had typed it in

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `message` | string | Message to send |

**Returns:** string — Returns the string sent to the chatbox

**Examples:**

- `chatbox[test]` — sends ‘test’ to the chatbox

#### chatboxpaste

`chatboxpaste[message]` → number

Pastes a message to the chatbox, leaving focus, so that the user can complete typing it

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `message` | string | Message to paste |

**Returns:** number — Returns 1 if successful

**Examples:**

- `chatboxpaste[test]` — pastes `test` to the chatbox, without sending

#### statushud

`statushud[key, value]` → number

Updates an entry in the Virindi HUDs Status HUD.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `key` | string | key to update |
| 1 | `value` | string | value to update with |

**Returns:** number — Returns 1

**Examples:**

- `statushud[test,my value]` — Updates the V Status Hud key `test` to `my value`

#### statushudcolored

`statushudcolored[key, value, color]` → number

Updates an entry in the Virindi HUDs Status HUD with color

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `key` | string | key to update |
| 1 | `value` | string | value to update with |
| 2 | `color` | number | The color, in RGB number format. For example, pure red is 16711680 (0xFF0000) |

**Returns:** number — Returns 1

**Examples:**

- `statushudcolored[test,my value,16711680]` — Updates the V Status Hud key `test` to `my value` in red


### Character reference

#### getcharintprop

`getcharintprop[property]` → number

Returns an int property from your character, or 0 if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `property` | number | IntProperty to return |

**Returns:** number — Returns an int property from your character

**Examples:**

- `getcharintprop[25]` — Returns your character’s current level

#### getchardoubleprop

`getchardoubleprop[property]` → number

Returns a double property from your character, or 0 if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `property` | number | DoubleProperty to return |

**Returns:** number — Returns a double property from your character

**Examples:**

- `getchardoubleprop[25]` — Returns the value of DoubleProperty 25 for your character (upstream example text is copied from `getcharintprop`)

#### getcharquadprop

`getcharquadprop[property]` → number

Returns a quad property from your character, or 0 if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `property` | number | QuadProperty to return |

**Returns:** number — Returns a quad property from your character

**Examples:**

- `getcharquadprop[1]` — Returns your character’s TotalExperience

#### getcharboolprop

`getcharboolprop[property]` → number

Returns a bool property from your character, or 0 if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `property` | number | BoolProperty to return |

**Returns:** number — Returns a bool property from your character

**Examples:**

- `getcharboolprop[110]` — Returns your character’s AwayFromKeyboard status

#### getcharstringprop

`getcharstringprop[property]` → string

Returns a string property from your character, or false if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `property` | number | StringProperty to return |

**Returns:** string — Returns a string property from your character

**Examples:**

- `getcharstringprop[1]` — Returns your character’s name

#### getcharvital_base

`getcharvital_base[vitalId]` → number

Gets your characters base (unbuffed) vital value

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `vitalId` | number | Which vital to get. 1 = Health, 2 = Stamina, 3 = Mana. |

**Returns:** number — Returns the base (unbuffed) vital value

**Examples:**

- `getcharvital_base[1]` — Returns your character’s base health

#### getcharvital_current

`getcharvital_current[vitalId]` → number

Gets your characters current vital value

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `vitalId` | number | Which vital to get. 1 = Health, 2 = Stamina, 3 = Mana. |

**Returns:** number — Returns the current vital value

**Examples:**

- `getcharvital_current[2]` — Returns your character’s current stamina

#### getcharvital_buffedmax

`getcharvital_buffedmax[vitalId]` → number

Gets your characters buffed maximum vital value

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `vitalId` | number | Which vital to get. 1 = Health, 2 = Stamina, 3 = Mana. |

**Returns:** number — Returns the buffed maximum vital value

**Examples:**

- `getcharvital_buffedmax[3]` — Returns your character’s buffed maximum mana

#### getcharskill_traininglevel

`getcharskill_traininglevel[skillId]` → number

Gets your characters training level for a specified skill

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `skillId` | number | Which skill to check. |

**Returns:** number — Returns current training level of a skill. 0 = Unusable, 1 = Untrained, 2 = Trained, 3 = Specialized

**Examples:**

- `getcharskill_traininglevel[23]` — Returns your character’s LockPick skill training level

#### getcharskill_base

`getcharskill_base[skillId]` → number

Gets your characters base skill level for a specified skill

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `skillId` | number | Which skill to check. |

**Returns:** number — Returns base skill level of the specified skill

**Examples:**

- `getcharskill_base[43]` — Returns your character’s base Void skill level

#### getcharskill_buffed

`getcharskill_buffed[skillId]` → number

Gets your characters buffed skill level for a specified skill

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `skillId` | number | Which skill to check. |

**Returns:** number — Returns buffed skill level of the specified skill

**Examples:**

- `getcharskill_buffed[33]` — Returns your character’s buffed Life Magic skill level

#### getcharattribute_base

`getcharattribute_base[attributeId]` → number

Gets your characters base attribute level for a specified attribute

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `attributeId` | number | Which attribute to check. 1 = Strength, 2 = Endurance, 3 = Quickness, 4 = Coordination, 5 = Focus, 6 = Self |

**Returns:** number — Returns base attribute level of the specified attribute

**Examples:**

- `getcharattribute_base[1]` — Returns your character’s base Strength attribute level

#### getcharattribute_buffed

`getcharattribute_buffed[attributeId]` → number

Gets your characters buffed attribute level for a specified attribute

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `attributeId` | number | Which attribute to check. 1 = Strength, 2 = Endurance, 3 = Quickness, 4 = Coordination, 5 = Focus, 6 = Self |

**Returns:** number — Returns buffed attribute level of the specified attribute

**Examples:**

- `getcharattribute_buffed[1]` — Returns your character’s buffed Strength attribute level

#### getcharburden

`getcharburden[]` → number

Gets your current burden shown on the character panel

**Returns:** number — Return current burden shown in character panel

**Examples:**

- `getcharburden[]` — Returns your character’s current burden level

#### vitae

`vitae[]` → number

Gets your character’s current vitae percentage as a number

**Returns:** number — Returns a number

**Examples:**

- `vitae[]` — returns your current vitae % as a number

#### getcombatstate

`getcombatstate[]` → string

Get the combat state of your character

**Returns:** string — Returns your current combat state

**Examples:**

- `getcombatstate[]` — Returns the string peace if you are in peace mode

#### setcombatstate

`setcombatstate[str]` → number

Sets the combat state of your character

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `str` | string | combatstate to set |

**Returns:** number — Returns 1 if executed successfully.

**Examples:**

- `setcombatstate[peace]` — Sets your combat state to peace mode

#### getbusystate

`getbusystate[]` → number

Get the busy state of your character and returns a number for the state (0=idle, 1=combining a stack, 2=splitting a stack, 3=???, 4=picking up an item from the ground, 5=moving or unequipping an item, 6=dropping an item to the ground, 7=equipping an item)

**Returns:** number — Returns the number corresponding to your busy state (moving an item)

**Examples:**

- `getbusystate[]` — Returns 0 if your character is idle, otherwise returns the appropriate value

#### getequippedweapontype

`getequippedweapontype[]` → string

Returns the equipped weapons object class

**Returns:** string — Returns the equipped weapons object class

**Examples:**

- `getequippedweapontype[]` — Returns the equipped weapons object class

#### isportaling

`isportaling[]` → number

Checks if your character is currently in portalspace

**Returns:** number — Returns 1 if in portalspace, 0 otherwise

**Examples:**

- `isportaling[]` — Returns 1 if in portalspace, 0 otherwise


### Spells and Components reference

#### spellname

`spellname[spellid]` → string

Gets the name of a spell by id

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellid` | number | Spell ID |

**Returns:** string — Returns a string name of the passed spell id.

**Examples:**

- `spellname[1]` — Returns `Strength Other I` for spell id 1

#### spelldata

`spelldata[spellid]` → Dictionary

Gets a dictionary of information about the passed spell id

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellid` | number | Spell ID |

**Returns:** Dictionary — Returns a dictionary of spell data

**Examples:**

- `spelldata[1]` — returns spell data for spellid 1

#### getknownspells

`getknownspells[]` → List

Returns a list of spell ids known by this character

**Returns:** List — Returns a list of spell ids known by this character

**Examples:**

- `getknownspells[]` — Returns a list of spell ids known by this character

#### getisspellknown

`getisspellknown[spellId]` → number

Checks if your character knows a spell by id

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellId` | number | Spell ID to check |

**Returns:** number — Returns 1 if your character knows this spell, 0 otherwise

**Examples:**

- `getisspellknown[2931]` — Checks if your character knows the Recall Aphus Lassel spell

#### getcancastspell_hunt

`getcancastspell_hunt[spellId]` → number

Checks if your character is capable of casting spellId while hunting

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellId` | number | Spell ID to check |

**Returns:** number — Returns 1 if your character can cast this spell while hunting, 0 otherwise

**Examples:**

- `getcancastspell_hunt[2931]` — Checks if your character is capable of casting the Recall Aphus Lassel spell while hunting

#### getcancastspell_buff

`getcancastspell_buff[spellId]` → number

Checks if your character is capable of casting spellId while buffing

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellId` | number | Spell ID to check |

**Returns:** number — Returns 1 if your character can cast this spell while buffing, 0 otherwise

**Examples:**

- `getcancastspell_buff[2931]` — Checks if your character is capable of casting the Recall Aphus Lassel spell while buffing

#### getspellexpiration

`getspellexpiration[spellId]` → number

Gets the number of seconds until a spell expires by id

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellId` | number | Spell ID to check |

**Returns:** number — Returns the number of seconds until a spell expires. 0 if spell not active, MaxInt if it doesn’t expire.

**Examples:**

- `getspellexpiration[515]` — Get the number of seconds until Acid Protection Self I expires

#### getspellexpirationbyname

`getspellexpirationbyname[spellName]` → number

Gets the number of seconds until a spell expires by name

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellName` | string | Spell name to check |

**Returns:** number — Returns the number of seconds until a spell expires. 0 if spell not active, MaxInt if it doesn’t expire, -1 if an error occurred.

**Examples:**

- `` getspellexpirationbyname[`acid prot`] `` — Get the number of seconds until an Acid Protection spell expires

#### componentname

`componentname[componentid]` → string

Gets the name of a spell component by id

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `componentid` | number | Component ID |

**Returns:** string — Returns the string name of the passed component id

**Examples:**

- `componentname[1]` — Returns the name of component id 1

#### componentdata

`componentdata[componentid]` → Dictionary

Returns a dictionary containing component data for the passed component id

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `componentid` | number | component ID |

**Returns:** Dictionary — Returns a dictionary containing component data

**Examples:**

- `componentdata[1]` — returns a dictionary of information about component id 1


### Position and Coordinates reference

#### getplayerlandcell

`getplayerlandcell[]` → number

Gets the landcell your character is currently standing in

**Returns:** number — Returns the landcell your character is currently standing in

**Examples:**

- `getplayerlandcell[]` — Returns your character’s current landcell as a uint

#### getplayerlandblock

`getplayerlandblock[]` → number

Gets the landblock your character is currently standing in

**Returns:** number — Returns the landblock your character is currently standing in

**Examples:**

- `getplayerlandblock[]` — Returns your character’s current landblock as a uint

#### getplayercoordinates

`getplayercoordinates[]` → Coordinates

Gets the a coordinates object representing your characters current global position

**Returns:** Coordinates — Returns your character’s global coordinates object

**Examples:**

- `getplayercoordinates[]` — Returns your character’s current position as coordinates object

#### wobjectgetphysicscoordinates

`wobjectgetphysicscoordinates[wo]` → Coordinates

Gets a coordinates object representing a world objects current position

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `wo` | WorldObject | world object to get coordinates of |

**Returns:** Coordinates — Returns a coordinates object representing the passed wobject

**Examples:**

- `wobjectgetphysicscoordinates[wobjectgetplayer[]]` — Returns a coordinates object representing the current player’s position

#### coordinateparse

`coordinateparse[coordstring]` → Coordinates

Converts a coordinate string like `1.2N, 3.4E` to a coordinates object

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `coordstring` | string | coordinates string to parse |

**Returns:** Coordinates — Returns a coordinates object

**Examples:**

- `` coordinateparse[`1.2N, 3.4E`] `` — Returns a coordinates object representing `1.2N, 3.4E`

#### coordinatetostring

`coordinatetostring[obj]` → string

Converts a coordinates object to a string representation

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | Coordinates | coordinates object to convert to a string |

**Returns:** string — Returns a string representation of a coordinates object, like `1.2N, 34.5E`

**Examples:**

- `coordinatetostring[getplayercoordinates[]]` — Returns your character’s current coordinates as a string, eg `1.2N, 34.5E`

#### coordinategetns

`coordinategetns[obj]` → number

Gets the NS position of a coordinates object as a number

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | Coordinates | coordinates object to get the NS position of |

**Returns:** number — Returns the NS position of a coordinates object as a number

**Examples:**

- `coordinategetns[getplayercoordinates[]]` — Returns your character’s current NS position

#### coordinategetwe

`coordinategetwe[obj]` → number

Gets the EW position of a coordinates object as a number

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | Coordinates | coordinates object to get the EW position of |

**Returns:** number — Returns the EW position of a coordinates object as a number

**Examples:**

- `coordinategetwe[getplayercoordinates[]]` — Returns your character’s current EW position

#### coordinategetz

`coordinategetz[obj]` → number

Gets the Z position of a coordinates object as a number

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | Coordinates | coordinates object to get the Z position of |

**Returns:** number — Returns the Z position of a coordinates object as a number

**Examples:**

- `coordinategetz[getplayercoordinates[]]` — Returns your character’s current Z position

#### coordinatedistancewithz

`coordinatedistancewithz[obj1, obj2]` → number

Gets the 3d distance in meters between two coordinates objects

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj1` | Coordinates | first coordinates object |
| 1 | `obj2` | Coordinates | second coordinates object |

**Returns:** number — Returns the 3d distance in meters between obj1 and obj2

**Examples:**

- `` coordinatedistancewithz[coordinateparse[`1.2N, 3.4E`], coordinateparse[`5.6N, 7.8E`]] `` — Returns the 3d distance between `1.2N, 3.4E` and `5.6N, 7.8E`

#### coordinatedistanceflat

`coordinatedistanceflat[obj1, obj2]` → number

Gets the 2d distance in meters between two coordinates objects (ignoring Z)

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj1` | Coordinates | first coordinates object |
| 1 | `obj2` | Coordinates | second coordinates object |

**Returns:** number — Returns the 2d distance in meters between obj1 and obj2 (ignoring Z)

**Examples:**

- `` coordinatedistanceflat[coordinateparse[`1.2N, 3.4E`], coordinateparse[`5.6N, 7.8E`]] `` — Returns the 2d distance between `1.2N, 3.4E` and `5.6N, 7.8E` (ignoring Z)

#### getheading

`getheading[obj]` → number

Returns the heading in degrees (0-360 clockwise, 0 is north) the target object is facing

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to get the heading of |

**Returns:** number — Returns the heading in degrees that the target object is facing

**Examples:**

- `getheading[wobjectgetselection[]]` — Returns the heading in degrees that your current selection is facing

#### getheadingto

`getheadingto[obj]` → number

Calculates the heading in degrees (0-360 clockwise, 0 is north) from your player to the target object

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to calculate heading towards |

**Returns:** number — Returns the heading in degrees from your player to the target object

**Examples:**

- `getheadingto[wobjectgetselection[]]` — Returns the heading in degrees from your player to the target object


### World Objects: Finding reference

#### wobjectgetplayer

`wobjectgetplayer[]` → WorldObject

Gets a worldobject representing the current player

**Returns:** WorldObject — Returns a worldobject representing the current player

**Examples:**

- `wobjectgetplayer[]` — Returns a worldobject representing the current player

#### wobjectgetselection

`wobjectgetselection[]` → WorldObject

Gets a worldobject representing the currently selected object

**Returns:** WorldObject — Returns a worldobject representing the currently selected object, or 0 if none

**Examples:**

- `wobjectgetselection[]` — Returns a worldobject representing the currently selected object

#### wobjectgetopencontainer

`wobjectgetopencontainer[]` → WorldObject

Gets a worldobject representing the currently opened container, or 0 if none

**Returns:** WorldObject — Returns a worldobject representing the currently opened container

**Examples:**

- `wobjectgetopencontainer[]` — Returns a worldobject representing the currently opened container

#### wobjectfindbyid

`wobjectfindbyid[id]` → WorldObject

Gets a worldobject representing the object with the passed id

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `id` | number | object id to find |

**Returns:** WorldObject — Returns a worldobject

**Examples:**

- `wobjectfindbyid[123412]` — Returns a worldobject of the object with id 123412

#### wobjectfindnearestmonster

`wobjectfindnearestmonster[]` → WorldObject

Gets a worldobject representing the nearest monster, or 0 if none was found

**Returns:** WorldObject — Returns a worldobject representing the nearest monster

**Examples:**

- `wobjectfindnearestmonster[]` — Returns a worldobject representing the nearest monster

#### wobjectfindnearestdoor

`wobjectfindnearestdoor[]` → WorldObject

Gets a worldobject representing the nearest door, or 0 if none was found

**Returns:** WorldObject — Returns a worldobject representing the nearest door

**Examples:**

- `wobjectfindnearestdoor[]` — Returns a worldobject representing the nearest door

#### wobjectfindnearestbyobjectclass

`wobjectfindnearestbyobjectclass[objectclass]` → WorldObject

Gets a worldobject representing the nearest object matching objectclass, or 0 if none was found

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `objectclass` | number | objectclass to filter by |

**Returns:** WorldObject — Returns a worldobject representing the nearest matching objectclass

**Examples:**

- `wobjectfindnearestbyobjectclass[24]` — Returns a worldobject of the nearest matching objectclass

#### wobjectfindnearestbynameandobjectclass

`wobjectfindnearestbynameandobjectclass[objectclass, namerx]` → WorldObject

Gets a worldobject representing the first object matching objectclass and name regex, or 0 if none was found

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `objectclass` | number | objectclass to filter by |
| 1 | `namerx` | string | name regex to filter by |

**Returns:** WorldObject — Returns a worldobject

**Examples:**

- `` wobjectfindnearestbynameandobjectclass[24,`Crash.*`] `` — Returns a worldobject of the first object found matching objectclass 24 (player) and name regex `Crash.*`

#### wobjectfindnearestbytemplatetype

`wobjectfindnearestbytemplatetype[templatetype]` → WorldObject

Attempts to find the nearest landscape world object with the specified template type

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `templatetype` | number | templatetype to filter by |

**Returns:** WorldObject — Returns a worldobject

**Examples:**

- `wobjectfindnearestbytemplatetype[42137]` — Returns a worldobject with template type 42137 (level 10 ice tachi warden)

#### wobjectfindininventorybyname

`wobjectfindininventorybyname[name]` → WorldObject

Gets a worldobject representing the first inventory item matching an exact name, or 0 if none was found

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `name` | string | exact name to filter by |

**Returns:** WorldObject — Returns a worldobject

**Examples:**

- `wobjectfindininventorybyname[Massive Mana Charge]` — Returns a worldobject of the first inventory item that is named `Massive Mana Charge`

#### wobjectfindininventorybynamerx

`wobjectfindininventorybynamerx[namerx]` → WorldObject

Gets a worldobject representing the first inventory item matching name regex, or 0 if none was found

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `namerx` | string | name regex to filter by |

**Returns:** WorldObject — Returns a worldobject

**Examples:**

- `` wobjectfindininventorybynamerx[`Massive.*`] `` — Returns a worldobject of the first inventory item that matches regex `Massive.*`

#### wobjectfindininventorybytemplatetype

`wobjectfindininventorybytemplatetype[templatetype]` → WorldObject

Gets a worldobject representing the first inventory item matching template type, or 0 if none was found

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `templatetype` | number | templatetype to filter by |

**Returns:** WorldObject — Returns a worldobject

**Examples:**

- `wobjectfindininventorybytemplatetype[9060]` — Returns a worldobject of the first inventory item that is a Titan Mana Charge (template type 9060)

#### wobjectfindall

`wobjectfindall[]` → List

Gets a list of all worldobjects known by the client

**Returns:** List — Returns a list of all worldobjects

**Examples:**

- `wobjectfindall[]` — Returns a list of all wobjects

#### wobjectfindallbyobjectclass

`wobjectfindallbyobjectclass[objectclass]` → List

Gets a list of all worldobjects of the passed objectclass

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `objectclass` | number | objectclass to filter by |

**Returns:** List — Returns a list of all matching worldobjects

**Examples:**

- `wobjectfindallbyobjectclass[24]` — Returns a list of all players the client is aware of

#### wobjectfindallbytemplatetype

`wobjectfindallbytemplatetype[templatetype]` → List

Gets a list of all wobjects matching templatetype

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `templatetype` | number | templatetype to filter by |

**Returns:** List — Returns a list of all matching worldobjects

**Examples:**

- `wobjectfindallbytemplatetype[9060]` — Returns a list of worldobjects that are a Titan Mana Charge (template type 9060)

#### wobjectfindallbynamerx

`wobjectfindallbynamerx[namerx]` → List

Gets a list of all wobjects matching the passed regular expression

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `namerx` | string | regular expression to filter name by |

**Returns:** List — Returns a list of all matching worldobjects

**Examples:**

- `` wobjectfindallbynamerx[`Crash.*`] `` — Returns a list of all worldobjects that match the regex `Crash.*`

#### wobjectfindallbycontainer

`wobjectfindallbycontainer[container]` → List

Gets a list of all worldobjects inside the specified container

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `container` | Object | container to find objects in. can be an id or a wobject |

**Returns:** List — Returns a list of worldobjects

**Examples:**

- `wobjectfindallbycontainer[wobjectgetselection[]]` — Returns a list of all wobjects in the currently selected container
- `wobjectfindallbycontainer[0x1234ABCD]` — Returns a list of all wobjects in the container with id 0x1234ABCD

#### wobjectfindallinventory

`wobjectfindallinventory[]` → List

Gets a list of all worldobjects in the players inventory

**Returns:** List — Returns a list of all worldobjects in the player inventory

**Examples:**

- `wobjectfindallinventory[]` — Returns a list of all your inventory wobjects

#### wobjectfindallinventorybyobjectclass

`wobjectfindallinventorybyobjectclass[objectclass]` → List

Gets a list of all inventory worldobjects of the passed objectclass

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `objectclass` | number | objectclass to filter by |

**Returns:** List — Returns a list of all matching worldobjects

**Examples:**

- `wobjectfindallinventorybyobjectclass[16]` — Returns a list of all mana stones in the players inventory

#### wobjectfindallinventorybytemplatetype

`wobjectfindallinventorybytemplatetype[templatetype]` → List

Gets a list of all inventory items matching templatetype

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `templatetype` | number | templatetype to filter by |

**Returns:** List — Returns a list of all matching inventory worldobjects

**Examples:**

- `wobjectfindallinventorybytemplatetype[9060]` — Returns a list of inventory worldobjects that are a Titan Mana Charge (template type 9060)

#### wobjectfindallinventorybynamerx

`wobjectfindallinventorybynamerx[namerx]` → List

Gets a list of all inventory wobjects with name matching the passed regular expression

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `namerx` | string | regular expression to filter name by |

**Returns:** List — Returns a list of all matching worldobjects

**Examples:**

- `` wobjectfindallinventorybynamerx[`Crash.*`] `` — Returns a list of all inventory worldobjects that match the regex `Crash.*`

#### wobjectfindalllandscape

`wobjectfindalllandscape[]` → List

Gets a list of all worldobjects in the landscape

**Returns:** List — Returns a list of all worldobjects in the landscape

**Examples:**

- `wobjectfindalllandscape[]` — Returns a list of all landscape wobjects

#### wobjectfindalllandscapebyobjectclass

`wobjectfindalllandscapebyobjectclass[objectclass]` → List

Gets a list of all landscape worldobjects of the passed objectclass

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `objectclass` | number | objectclass to filter by |

**Returns:** List — Returns a list of all matching worldobjects

**Examples:**

- `wobjectfindalllandscapebyobjectclass[16]` — Returns a list of all mana stones in the landscape

#### wobjectfindalllandscapebytemplatetype

`wobjectfindalllandscapebytemplatetype[templatetype]` → List

Gets a list of all landscape items matching templatetype

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `templatetype` | number | templatetype to filter by |

**Returns:** List — Returns a list of all matching worldobjects

**Examples:**

- `wobjectfindalllandscapebytemplatetype[9060]` — Returns a list of landscape worldobjects that are a Titan Mana Charge (template type 9060)

#### wobjectfindalllandscapebynamerx

`wobjectfindalllandscapebynamerx[namerx]` → List

Gets a list of all landscape wobjects with name matching the passed regular expression

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `namerx` | string | regular expression to filter name by |

**Returns:** List — Returns a list of all matching worldobjects

**Examples:**

- `` wobjectfindalllandscapebynamerx[`Crash.*`] `` — Returns a list of all landscape worldobjects that match the regex `Crash.*`


### World Objects: Properties reference

#### wobjectgetname

`wobjectgetname[wo]` → string

Gets the name string of a wobject

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `wo` | WorldObject | world object to get the name of |

**Returns:** string — Returns a string of wobject’s name

**Examples:**

- `wobjectgetname[wobjectgetplayer[]]` — Returns a string representing the name of the world object

#### wobjectgetid

`wobjectgetid[wo]` → number

Gets the id of a wobject

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `wo` | WorldObject | world object to get the id of |

**Returns:** number — Returns the id of the passed wobject

**Examples:**

- `wobjectgetid[wobjectgetplayer[]]` — Returns the id of the current player

#### wobjectgetobjectclass

`wobjectgetobjectclass[wo]` → number

Gets the objectclass as a number from a wobject

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `wo` | WorldObject | world object to get the objectclass of |

**Returns:** number — Returns a number representing the passed wobjects objectclass

**Examples:**

- `wobjectgetobjectclass[wobjectgetplayer[]]` — Returns 24 (Player ObjectClass)

#### wobjectgettemplatetype

`wobjectgettemplatetype[wo]` → number

Gets the template type as a number from a wobject

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `wo` | WorldObject | world object to get the template type of |

**Returns:** number — Returns a number representing the passed wobjects template type

**Examples:**

- `wobjectgettemplatetype[wobjectgetplayer[]]` — Returns 1 (Player template type)

#### wobjectgetintprop

`wobjectgetintprop[obj, property]` → number

Returns an int property from a specific world object, or 0 if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to get int property of |
| 1 | `property` | number | IntProperty to return |

**Returns:** number — Returns an int property value

**Examples:**

- `wobjectgetintprop[wobjectgetselection[],218103808]` — Returns the template type of the currently selected object

#### wobjectgetdoubleprop

`wobjectgetdoubleprop[obj, property]` → number

Returns a double property from a specific world object, or 0 if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to get double property of |
| 1 | `property` | number | DoubleProperty to return |

**Returns:** number — Returns a double property value

**Examples:**

- `wobjectgetdoubleprop[wobjectgetselection[],167772169]` — Returns the salvage workmanship of the currently selected object

#### wobjectgetboolprop

`wobjectgetboolprop[obj, property]` → number

Returns a bool property from a specific world object, or 0 if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to get bool property of |
| 1 | `property` | number | BoolProperty to return |

**Returns:** number — Returns a bool property value

**Examples:**

- `wobjectgetboolprop[wobjectgetselection[],99]` — Returns 1 if the currently selected object is ivoryable

#### wobjectgetstringprop

`wobjectgetstringprop[obj, property]` → string

Returns a string property from a specific world object, or and empty string if it’s undefined

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to get string property of |
| 1 | `property` | number | StringProperty to return |

**Returns:** string — Returns a string property value

**Examples:**

- `wobjectgetstringprop[wobjectgetselection[],1]` — Returns the name of the currently selected object

#### wobjectgethealth

`wobjectgethealth[obj]` → number

Gets the specified mob/player wobject current health percentage. Note: You must have the wobject selected in order to receive health updates

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to get health of |

**Returns:** number — Returns a value from 0-1 representing the objects current health percentage. If the objects health is not currently being tracked this will return -1

**Examples:**

- `wobjectgethealth[wobjectgetselection[]]` — Returns the health of the currently selected mob/player

#### wobjectgetspellids

`wobjectgetspellids[obj]` → List

Returns a list of spells on a World Object.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to get spells of |

**Returns:** List — List of spell IDs

**Examples:**

- `wobjectgetspellids[wobjectgetselection[]]` — Returns a list of spell IDs of the selected item

#### wobjectgetactivespellids

`wobjectgetactivespellids[obj]` → List

Returns a list of active spells on a World Object.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to get spells of |

**Returns:** List — List of spell IDs

**Examples:**

- `wobjectgetactivespellids[wobjectgetselection[]]` — Returns a list of active spell IDs of the selected item

#### wobjectgetisdooropen

`wobjectgetisdooropen[wo]` → number

Checks if a door wobject is open

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `wo` | WorldObject | door world object to check |

**Returns:** number — Returns 1 if the wo door is open, 0 otherwise

**Examples:**

- `wobjectgetisdooropen[wobjectfindnearestdoor[]]` — Returns 1 if the door is open, 0 otherwise

#### wobjectisvalid

`wobjectisvalid[obj]` → number

Checks whether the passed world object is still valid (exists in the client). *Upstream text for this entry is a copy of `getheadingto`; this description is inferred from the name.*

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to check |

**Returns:** number — Presumably 1 if valid, 0 otherwise

**Examples:**

- `wobjectisvalid[wobjectgetselection[]]` — Checks if the current selection is a valid world object

#### wobjecthasdata

`wobjecthasdata[obj]` → number

Checks if a wobject has assess data from the server

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to check if we have assess data for |

**Returns:** number — Returns 1 if we have assess data for the object

**Examples:**

- `wobjecthasdata[wobjectgetselection[]]` — Returns 1 if the currently selected object has assess data from the server

#### wobjectrequestdata

`wobjectrequestdata[obj]` → number

Requests assess data for a wobject from the server

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to request assess data for |

**Returns:** number — Returns 1

**Examples:**

- `wobjectrequestdata[wobjectgetselection[]]` — Requests assess data from the server for the currently selected object

#### wobjectlastidtime

`wobjectlastidtime[obj]` → number

Gets the last time a wobjects data was recieved from the server

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to check |

**Returns:** number — Returns 1

**Examples:**

- `wobjectlastidtime[wobjectgetselection[]]` — Checks the last time the server sent id data for the currently selected object


### Inventory and Containers reference

#### getitemcountininventorybyname

`getitemcountininventorybyname[name]` → number

Counts how many items you have in your inventory exactly matching `name`. Stack sizes are counted

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `name` | string | Exact item name to match |

**Returns:** number — Returns a count of the number of items found. stack size is counted

**Examples:**

- `getitemcountininventorybyname[Prismatic Taper]` — Returns total count of prismatic tapers in your inventory

#### getitemcountininventorybynamerx

`getitemcountininventorybynamerx[namerx]` → number

Counts how many items you have in your inventory matching regex `namerx`. Stack sizes are counted

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `namerx` | string | Regex item name to match |

**Returns:** number — Returns a count of the number of items found. stack size is counted

**Examples:**

- `getitemcountininventorybynamerx[Scarab]` — Returns total count of scarabs in your inventory

#### getinventorycountbytemplatetype

`getinventorycountbytemplatetype[templatetype]` → number

Counts how many items you have in your inventory of a certain template type. Stack sizes are counted

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `templatetype` | number | templatetype to filter by |

**Returns:** number — Returns a count of the number of items found. stack size is counted

**Examples:**

- `getinventorycountbytemplatetype[42137]` — Returns total count of inventory items with template type 42137

#### getfreeitemslots

`getfreeitemslots[container?]` → number

Gets the number of free item slots in a container

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `container` *(optional)* | WorldObject | Optional container to get free item slots of, defaulting to player. |

**Returns:** number — Returns the number of free slots of container, or -1 if the WorldObject was invalid

**Examples:**

- `getfreeitemslots[]` — Returns the free item slots of the player
- `getfreeitemslots[wobjectgetselection[]]` — Returns the free item slots of the selected object

#### getfreecontainerslots

`getfreecontainerslots[container?]` → number

Gets the number of free container slots

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `container` *(optional)* | WorldObject | Optional container to get free item slots of, defaulting to player. |

**Returns:** number — Returns the number of free container slots, or -1 if the WorldObject was invalid

**Examples:**

- `getfreecontainerslots[]` — Returns the free container slots of the player
- `getfreecontainerslots[wobjectgetselection[]]` — Returns the free container slots of the selected object

#### getcontaineritemcount

`getcontaineritemcount[container?]` → number

Gets the number of items in a container

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `container` *(optional)* | WorldObject | Optional container to get number of items in, defaulting to player. |

**Returns:** number — Returns the number of items in a container, or -1 if the WorldObject was invalid

**Examples:**

- `getcontaineritemcount[]` — Returns the used container slots of the player
- `getcontaineritemcount[wobjectgetselection[]]` — Returns the number of items in the selected item


### Actions reference

#### actiontryselect

`actiontryselect[obj]` → number

Attempts to select a worldobject

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | wobject to select |

**Returns:** number — Returns 0

**Examples:**

- `actiontryselect[wobjectgetplayer[]]` — Attempts to select the current player

#### actiontryuseitem

`actiontryuseitem[obj]` → number

Attempts to use a worldobject

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | wobject to try to use |

**Returns:** number — Returns 0

**Examples:**

- `actiontryuseitem[wobjectgetplayer[]]` — Attempts to use the current player (opens backpack)

#### actiontryapplyitem

`actiontryapplyitem[useObj, onObj]` → number

Attempts to use a worldobject on another worldobject

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `useObj` | WorldObject | wobject to use first |
| 1 | `onObj` | WorldObject | wobject to be used on |

**Returns:** number — Returns 0 if failed, and 1 if it could succeed

**Examples:**

- `` actiontryapplyitem[wobjectfindininventorybynamerx[`.* Healing Kit`],wobjectgetplayer[]] `` — Attempts to use any healing kit in your inventory, on yourself

#### actiontrygiveitem

`actiontrygiveitem[give, destination]` → number

Attempts to give a worldobject to another worlobject, like an npc

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `give` | WorldObject | wobject to give |
| 1 | `destination` | WorldObject | wobject to be given to |

**Returns:** number — Returns 0 if failed, and 1 if it could succeed

**Examples:**

- `` actiontrygiveitem[wobjectfindininventorybynamerx[`.* Healing Kit`],wobjectgetselection[]] `` — Attempts to give any healing kit in your inventory to the currently selected object

#### actiontrygiveprofile

`actiontrygiveprofile[lootprofile, target]` → number

Gives all items matching the specified loot profile to a character or npc

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `lootprofile` | string | loot profile to give |
| 1 | `target` | string | target npc/player name to give to |

**Returns:** number — Returns 1 if an attempt was made, 0 if it was not (busy)

**Examples:**

- `actiontrygiveprofile[salvage\.utl,My Salvage Mule]` — Gives all items matching loot profile salvage.utl to a character named My Salvage Mule

#### actiontryequipanywand

`actiontryequipanywand[]` → number

Attempts to take one step towards equipping any wand from the current profile’s items list

**Returns:** number — Returns 1 if a wand is already equipped, 0 otherwise

**Examples:**

- `actiontryequipanywand[]` — Attempts to equip any wand

#### actiontrycastbyid

`actiontrycastbyid[spellId]` → number

Attempts to cast a spell by id. Checks spell requirements as if it were a vtank ‘hunting’ spell. If the character is not in magic mode, one step is taken towards equipping any wand

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellId` | number | spellId to cast |

**Returns:** number — Returns 1 if the attempt has begun, 0 if the attempt has not yet been made, or 2 if the attempt is impossible

**Examples:**

- `actiontrycastbyid[2931]` — Attempts to cast Recall Aphus Lassel

#### actiontrycastbyidontarget

`actiontrycastbyidontarget[spellId, target]` → number

Attempts to cast a spell by id on a worldobject. Checks spell requirements as if it were a vtank ‘hunting’ spell. If the character is not in magic mode, one step is taken towards equipping any wand

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `spellId` | number | spellId to cast |
| 1 | `target` | WorldObject | target to cast on |

**Returns:** number — Returns 1 if the attempt has begun, 0 if the attempt has not yet been made, or 2 if the attempt is impossible

**Examples:**

- `actiontrycastbyidontarget[1,wobjectgetselection[]]` — Attempts to cast Strength Other, on your currently selected target

#### ustopen

`ustopen[]` → number

Opens the ust panel

**Returns:** number — Returns 1 on success, 0 on failure

**Examples:**

- `ustopen[]` — Opens the ust panel

#### ustadd

`ustadd[obj]` → number

Adds an item to the ust panel

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `obj` | WorldObject | World object to add to the ust panel |

**Returns:** number — Returns 1

**Examples:**

- `ustadd[wobjectgetselection[]]` — Adds the currently selected item to your ust panel

#### ustsalvage

`ustsalvage[]` → number

Salvages the items in the ust panel

**Returns:** number — Returns 1

**Examples:**

- `ustsalvage[]` — Salvages all the items in the ust panel

#### setmotion

`setmotion[motion, state]` → number

Sets a characters wanted motion state.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `motion` | string | The motion to set. Valid motions are Forward\|Backward\|TurnRight\|TurnLeft\|StrafeRight\|StrafeLeft\|Walk |
| 1 | `state` | number | 0 = off, 1 = on |

**Returns:** number — Returns 1 if successful, 0 otherwise

**Examples:**

- `setmotion[Forward, 1]` — Makes your character move forward until turned off
- `setmotion[Forward, 0]` — Makes your character stop moving forward

#### getmotion

`getmotion[motion]` → number

Gets a character motion state.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `motion` | string | The motion to get. Valid motions are Forward\|Backward\|TurnRight\|TurnLeft\|StrafeRight\|StrafeLeft |

**Returns:** number — Returns 0 if the motion is inactive and unwanted, -1 if the motion is unwanted but active, 1 if the motion is wanted but inactive, 2 if the motion is wanted and active.

**Examples:**

- `getmotion[Forward]` — Returns 1 if your character is moving forward, 0 if not

#### clearmotion

`clearmotion[]` → number

Clears all motion states

**Returns:** number — Returns 1

**Examples:**

- `clearmotion[]` — Clears all motion states


### Fellowship reference

#### getfellowshipstatus

`getfellowshipstatus[]` → number

Checks if you are currently in a fellowship

**Returns:** number — Returns 1 if you are in a fellowship, 0 otherwise

**Examples:**

- `getfellowshipstatus[]` — returns 1 if you are in a fellowship, 0 otherwise

#### getfellowshipname

`getfellowshipname[]` → string

Gets the name of your current fellowship

**Returns:** string — Returns the name of a fellowship, or an empty string if none

**Examples:**

- `getfellowshipname[]` — returns the name of your current fellowship

#### getfellowshipcount

`getfellowshipcount[]` → number

Gets the number of members in your current fellowship

**Returns:** number — Returns the number of members in your fellowship, or 0 if you are not in a fellowship

**Examples:**

- `getfellowshipcount[]` — Returns the number of members in your fellowship, or 0 if you are not in a fellowship

#### getfellowshipleaderid

`getfellowshipleaderid[]` → number

Returns the character id of the fellowship leader

**Returns:** number — Returns the character id of the fellowship leader

**Examples:**

- `getfellowshipleaderid[]` — Returns the character id of the fellowship leader

#### getfellowshipisleader

`getfellowshipisleader[]` → number

Returns 1 if you are the leader of your fellowship

**Returns:** number — Returns 1 if you are the leader of your fellowship, 0 otherwise

**Examples:**

- `getfellowshipisleader[]` — Returns 1 if you are the leader of your fellowship

#### getfellowshiplocked

`getfellowshiplocked[]` → number

Returns 1 if your fellowship is Locked

**Returns:** number — Returns 1 if your fellowship is Locked, 0 otherwise

**Examples:**

- `getfellowshiplocked[]` — Returns 1 if your fellowship is Locked

#### getfellowshipisopen

`getfellowshipisopen[]` → number

Returns 1 if your fellowship is open

**Returns:** number — Returns 1 if your fellowship is open, 0 otherwise

**Examples:**

- `getfellowshipisopen[]` — Returns 1 if your fellowship is open

#### getfellowshipisfull

`getfellowshipisfull[]` → number

Returns 1 if you’re in a fellowship, and it is full

**Returns:** number — Returns 1 if you’re in a fellowship and it is full. 0 otherwise

**Examples:**

- `getfellowshipisfull[]` — Returns 1 if you’re in a fellowship, and it is full

#### getfellowshipcanrecruit

`getfellowshipcanrecruit[]` → number

Returns 1 if your fellowship is open, or you are the leader, and the fellowship is not full

**Returns:** number — Returns 1 if you’re in a fellowship, and are the leader, or the fellowship is open, and the fellowship is not full

**Examples:**

- `getfellowshipcanrecruit[]` — Returns 1 if you’re in a fellowship, and are the leader, or the fellowship is open, and the fellowship is not full

#### getfellowid

`getfellowid[iterator]` → number

Returns a fellowship member’s character id. It is recommended to save the output to a variable.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `iterator` | number | fellowship member to return (0-8) |

**Returns:** number — Character ID of requested Fellowship Member, or 0

**Examples:**

- `getfellowid[0]` — Returns the id of the first Fellowship Member

#### getfellowname

`getfellowname[iterator]` → string

Returns a fellowship member’s character name. It is recommended to save the output to a variable.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `iterator` | number | fellowship member to return (0-8) |

**Returns:** string — Character name of requested Fellowship Member, or ""

**Examples:**

- `getfellowname[0]` — Returns the name of the first Fellowship Member

#### getfellowids

`getfellowids[]` → List

Returns a list of character ids in your current fellowship, sorted numerically.

**Returns:** List — List of character ids in the fellowship, sorted numerically

**Examples:**

- `getfellowids[]` — Returns a list of character ids in your current fellowship, sorted numerically

#### getfellownames

`getfellownames[]` → List

Returns a list of character names in your current fellowship, sorted alphabetically.

**Returns:** List — List of character names in the fellowship, sorted alphabetically

**Examples:**

- `getfellownames[]` — Returns a list of character names in your current fellowship, sorted alphabetically


### Quests reference

#### testquestflag

`testquestflag[questflag]` → number

Checks if a quest flag has ever been completed. You must manually refresh `/myquests` after completing a quest, for these values to be up to date. UB will call `/myquests` once on login automatically.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `questflag` | string | quest flag to check |

**Returns:** number — Returns 1 if you have ever completed this quest flag, 0 otherwise

**Examples:**

- `testquestflag[spokenbobo]` — Checks if your character is flagged for bobo

#### getqueststatus

`getqueststatus[questflag]` → number

Checks if a quest flag has a timer on it. If there is a timer it returns 0, if it’s ready it returns 1. You must manually refresh `/myquests` after completing a quest, for these values to be up to date. UB will call `/myquests` once on login automatically.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `questflag` | string | quest flag to check |

**Returns:** number — Returns status of a quest flag. 0 = Not Ready, 1 = Ready

**Examples:**

- `getqueststatus[blankaug]` — Checks if your character is ready to run the blankaug quest again.

#### getquestktprogress

`getquestktprogress[questflag]` → number

Checks progress of a killtask quest. You must manually refresh `/myquests` after completing a quest, for these values to be up to date. UB will call `/myquests` once on login automatically.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `questflag` | string | quest flag to check |

**Returns:** number — Returns a progress count

**Examples:**

- `getquestktprogress[totalgolemmagmaexarchdead]` — Checks how many exarch kills you currently have

#### getquestktrequired

`getquestktrequired[questflag]` → number

Checks total required solves of a killtask quest. If you have not started the killtask, this will return 0. You must manually refresh `/myquests` after completing a quest, for these values to be up to date. UB will call `/myquests` once on login automatically.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `questflag` | string | quest flag to check |

**Returns:** number — Returns a required count

**Examples:**

- `getquestktrequired[totalgolemmagmaexarchdead]` — Checks required total exarch kills for this kill task

#### isrefreshingquests

`isrefreshingquests[]` → number

Checks if quests are currently being fetched or processed

**Returns:** number — Returns 1 if quests are currently being fetched and processed, 0 otherwise

**Examples:**

- `isrefreshingquests[]` — Returns 1 if quests are currently being fetched and processed


### Time and Stopwatches reference

#### getdatetimelocal

`getdatetimelocal[text]` → string

Gets the local date and time using the format provided

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `text` | string | The format to return the date/time |

**Returns:** string — Returns the local date and time

**Examples:**

- `` getdatetimelocal[`hh:mm:ss tt`] `` — Returns current local time ‘06:09:01 PM’

#### getdatetimeutc

`getdatetimeutc[text]` → string

Gets the universal date and time using the format provided

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `text` | string | The format to return the date/time |

**Returns:** string — Returns the universal date and time

**Examples:**

- `` getdatetimeutc[`hh:mm:ss tt`] `` — Returns current universal time ‘06:09:01 PM’

#### getunixtime

`getunixtime[]` → string

Gets the total number of seconds since the unix epoch (jan 1, 1970)

**Returns:** string — Returns the number of seconds since the unix epoch

**Examples:**

- `getunixtime[]` — Returns the number of seconds since the unix epoch

#### stopwatchcreate

`stopwatchcreate[]` → Stopwatch

Creates a new stopwatch object. The stopwatch object is stopped by default.

**Returns:** Stopwatch — Returns a stopwatch object

**Examples:**

- `stopwatchcreate[]` — returns a new stopwatch

#### stopwatchstart

`stopwatchstart[watch]` → Stopwatch

Starts a stopwatch if not already started

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `watch` | Stopwatch | stopwatch to start |

**Returns:** Stopwatch — Returns a stopwatch object

**Examples:**

- `stopwatchstart[stopwatchcreate[]]` — starts a new stopwatch

#### stopwatchstop

`stopwatchstop[watch]` → Stopwatch

Stops a stopwatch if not already stopped

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `watch` | Stopwatch | stopwatch to stop |

**Returns:** Stopwatch — Returns a stopwatch object

**Examples:**

- `stopwatchstop[stopwatchcreate[]]` — stops a new stopwatch

#### stopwatchelapsedseconds

`stopwatchelapsedseconds[watch]` → number

Gets the amount of seconds a stopwatch has been running for

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `watch` | Stopwatch | stopwatch to check |

**Returns:** number — Returns the elapsed seconds

**Examples:**

- `stopwatchelapsedseconds[stopwatchcreate[]]` — Returns the amount of a seconds a new stopwatch has been running for (0 obv)


### Game Time reference

#### getgameyear

`getgameyear[]` → number

Gets the current ingame year

**Returns:** number — Returns the current ingame year. Years have 360 days.

**Examples:**

- `getgameyear[]` — Returns the current ingame year, ie 249

#### getgamemonth

`getgamemonth[]` → number

Gets the current ingame month as a number. Months are: 0=Morningthaw, 1=Solclaim, 2=Seedsow, 3=Leafdawning, 4=Verdantine, 5=Thistledown, 6=Harvestgain, 7=Leafcull, 8=Frostfell, 9=Snowreap, 10=Coldeve, 11=Wintersebb

**Returns:** number — Returns the current ingame month as a number

**Examples:**

- `getgamemonth[]` — Returns the current ingame month as a number, ie 8

#### getgamemonthname

`getgamemonthname[monthIndex]` → string

Returns the name of a month index. Months are: 0=Morningthaw, 1=Solclaim, 2=Seedsow, 3=Leafdawning, 4=Verdantine, 5=Thistledown, 6=Harvestgain, 7=Leafcull, 8=Frostfell, 9=Snowreap, 10=Coldeve, 11=Wintersebb

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `monthIndex` | number | month index to get the name of |

**Returns:** string — Returns the name of a month index

**Examples:**

- `getgamemonthname[1]` — Returns Solclaim

#### getgameday

`getgameday[]` → number

Returns the current ingame day of the month. 1-30.

**Returns:** number — Returns the current ingame day of the month. 1-30

**Examples:**

- `getgameday[]` — returns the current ingame day of the month, ie 8

#### getgamehour

`getgamehour[]` → number

Returns the current ingame hour of the day. Days have 16 hours

**Returns:** number — Returns the current ingame hour. 0-16

**Examples:**

- `getgamehour[]` — returns the current ingame hour of the day, ie 3

#### getgamehourname

`getgamehourname[hourIndex]` → string

Returns the name of a hour index.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `hourIndex` | number | hour index to get the name of |

**Returns:** string — Returns the name of a hour index

**Examples:**

- `getgamehourname[1]` — Returns Darktide-and-Half

#### getgameticks

`getgameticks[]` → number

Returns the current ingame ticks.

**Returns:** number — Returns the current ingame ticks

**Examples:**

- `getgameticks[]` — returns the current ingame ticks

#### getisday

`getisday[]` → number

Returns 1 if it is currently day time, 0 otherwise

**Returns:** number — Returns 1 if it is currently day time, 0 otherwise

**Examples:**

- `getisday[]` — returns 1 if it is currently day time

#### getisnight

`getisnight[]` → number

Returns 1 if it is currently night time, 0 otherwise

**Returns:** number — Returns 1 if it is currently night time, 0 otherwise

**Examples:**

- `getisnight[]` — returns 1 if it is currently night time

#### getminutesuntilday

`getminutesuntilday[]` → number

Returns real world minutes left until the next day cycle

**Returns:** number — Returns real world minutes left until the next day cycle

**Examples:**

- `getminutesuntilday[]` — gets the number of real world minutes left until the next day cycle

#### getminutesuntilnight

`getminutesuntilnight[]` → number

Returns real world minutes left until the next night cycle

**Returns:** number — Returns real world minutes left until the next night cycle

**Examples:**

- `getminutesuntilnight[]` — gets the number of real world minutes left until the next night cycle


### VTank Integration reference

#### vtgetmetastate

`vtgetmetastate[]` → string

Gets the current vtank meta state as a string

**Returns:** string — Returns the current vt meta state as a string

**Examples:**

- `vtgetmetastate[]` — Returns the current vt meta state as a string

#### vtsetmetastate

`vtsetmetastate[state]` → number

Changes the current vtank meta state

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `state` | string | new state to switch to |

**Returns:** number — Returns 1

**Examples:**

- `vtsetmetastate[myState]` — sets vtank meta state to `myState`

#### vtgetsetting

`vtgetsetting[setting]` → string

Gets the value of a vtank setting

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `setting` | string | setting to get the state of |

**Returns:** string — Returns the string value of the specific setting or an empty string if undefined

**Examples:**

- `vtgetsetting[EnableCombat]` — Gets the value of the vtank EnableCombat setting
- `cnumber[vtgetsetting[RingDistance]]` — Gets the number value of the vtank RingDistance setting

#### vtsetsetting

`vtsetsetting[setting, value]` → number

Sets the specified vtank setting

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `setting` | string | setting to set the value of |
| 1 | `value` | string | value to set the setting to |

**Returns:** number — Returns 0 if known to fail and 1 if it may have succeeded

**Examples:**

- `` vtsetsetting[EnableCombat,`1`] `` — Sets the vtank EnableCombat setting to true. Any other number sets to false
- `` vtsetsetting[RingDistance,cstrf[cnumber[vtgetsetting[DoorOpenRange]],`N5`]] `` — Sets the vtank RingDistance setting to match the DoorOpenRange


### Meta Views (UI) reference

#### uigetcontrol

`uigetcontrol[windowName, controlName]` → UIControl

Gets a reference to the named control in the named window

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `windowName` | string | Name of the window the control belongs to |
| 1 | `controlName` | string | Name of the control to get |

**Returns:** UIControl — Returns a UIControl, or 0 if not found

**Examples:**

- `uigetcontrol[myWindow, myControl]` — Gets a reference to the control `myControl` in the view `myWindow`

#### uisetlabel

`uisetlabel[control, label]` → number

Changes the label text for the given UIControl. Currently only supports buttons.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `control` | UIControl | UIControl of the control to change |
| 1 | `label` | string | The new label text for the control |

**Returns:** number — Returns 1 on success, 0 on failure

**Examples:**

- `uisetlabel[uigetcontrol[myWindow, myControl], new label]` — Changes the control `myControl` label in the view `myWindow` to `new label`

#### uisetvisible

`uisetvisible[control, visible]` → number

Changes the visibility for the given UIControl. Currently only supports buttons.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `control` | UIControl | UIControl of the control to change |
| 1 | `visible` | number | pass 1 to make the control visible, 0 to make it hidden |

**Returns:** number — Returns 1 on success, 0 on failure

**Examples:**

- `uisetvisible[uigetcontrol[myWindow, myControl], 0]` — Changes the control `myControl` visibility in the view `myWindow` to hidden

#### uiviewexists

`uiviewexists[windowName]` → number

Checks if a meta view exists

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `windowName` | string | Name of the window to check the existence of |

**Returns:** number — Returns 1 if it exists, 0 if not

**Examples:**

- `uiviewexists[myWindow]` — returns true if a view with the title `myWindow` exists

#### uiviewvisible

`uiviewvisible[windowName]` → number

Checks if a meta view is visible

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `windowName` | string | Name of the window to check the visibilty of |

**Returns:** number — Returns 1 if it is visible, 0 if not

**Examples:**

- `uiviewvisible[myWindow]` — returns true if a view with the title `myWindow` is visible


### UtilityBelt, Network, and Client reference

#### uboptget

`uboptget[name]` → Object

Returns the value of a ub setting.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `name` | string | The name of the variable to get. Prepend global options with `global.`. |

**Returns:** Object — Returns the value of the setting, if valid. 0 if invalid setting.

**Examples:**

- `` uboptget[`Plugin.Debug`] `` — Gets the current value for the `Plugin.Debug` setting

#### uboptset

`uboptset[name, newValue]` → Object

Changes the value of a ub setting.

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `name` | string | The name of the variable to set. Prepend global options with `global.`. |
| 1 | `newValue` | Object | The new value to set. |

**Returns:** Object — Returns 1 if successful, 0 if it failed.

**Examples:**

- `` uboptset[`Plugin.Debug`, 1] `` — Sets the current value for the `Plugin.Debug` setting to true

#### netclients

`netclients[tag?]` → List

Gets a list of network clients, optionally filtered by tag

| # | Parameter | Type | Description |
| --- | --- | --- | --- |
| 0 | `tag` *(optional)* | string | Optional network tag to filter by |

**Returns:** List — Returns a list of network client data, optionally filtered by tag

**Examples:**

- `netclients[]` — returns a list of all network clients
- `netclients[test]` — returns a list of all network clients with the tag `test`

#### getworldname

`getworldname[]` → string

Gets the name of the currently connected world/server

**Returns:** string — Returns the name of the current world/server

**Examples:**

- `getworldname[]` — Returns the name of the current world/server

#### getaccounthash

`getaccounthash[]` → string

Returns a hash of the current account name

**Returns:** string — Returns a hash of the current account name

**Examples:**

- `getaccounthash[]` — Returns a hash of the current account name

## ID Quick Reference

IDs that appear in the upstream examples. The property functions (`wobjectget*prop`, `getchar*prop`) take Decal value-key numbers; see [`RedoxLib/GameValues/IntValueKey.cs`](../../../../RedoxLib/GameValues/IntValueKey.cs) for int keys (e.g. `CreatureLevel = 25`, `Type = 218103808`). Skill and attribute IDs match `SkillID` and `AttrID` in [decal/enum-types.md](../../decal/enum-types.md). Vital IDs for the `getcharvital_*` functions do **not** match the protocol `VitalID` enum.

| Kind | ID | Meaning |
| --- | --- | --- |
| Int property | `25` | Level |
| Int property | `218103808` | Template type |
| Double property | `167772169` | Salvage workmanship |
| Bool property | `99` | Ivoryable |
| Bool property | `110` | Away from keyboard |
| Quad property | `1` | Total experience |
| String property | `1` | Name |
| String property | `5` | Title |
| Object class | `16` | Mana stone |
| Object class | `24` | Player |
| Vital (`getcharvital_*`) | `1` / `2` / `3` | Health / Stamina / Mana |
| Attribute | `1`–`6` | Strength, Endurance, Quickness, Coordination, Focus, Self |
| Skill | `23` | Lockpick |
| Skill | `33` | Life Magic |
| Skill | `43` | Void Magic |
| Skill training level | `0`–`3` | Unusable, Untrained, Trained, Specialized |
| Busy state (`getbusystate`) | `0`–`7` | Idle, combining stack, splitting stack, ?, picking up from ground, moving/unequipping, dropping to ground, equipping |
| Motion state (`getmotion`) | `0` / `-1` / `1` / `2` | Inactive & unwanted / active & unwanted / wanted & inactive / wanted & active |
| Game month | `0`–`11` | Morningthaw, Solclaim, Seedsow, Leafdawning, Verdantine, Thistledown, Harvestgain, Leafcull, Frostfell, Snowreap, Coldeve, Wintersebb |
| Spell | `1` | Strength Other I |
| Spell | `515` | Acid Protection Self I |
| Spell | `2931` | Recall Aphus Lassel |
| Template type | `9060` | Titan Mana Charge |
| Template type | `42137` | Level 10 Ice Tachi Warden |

## Source Discrepancies

The upstream page has copy/paste errors and typos. This document uses the evident intent; the notable changes are:

- `wobjectisvalid`: upstream description, parameter, return, and example are all copied from `getheadingto`. The description here is inferred from the name.
- `dictvalues`: upstream says "Returns a list of keys" and its example calls `dictkeys`.
- `getqueststatus`: upstream says it returns `0` both when on a timer and when ready; the return line says `1` = ready.
- `getfellowshipcount`: upstream description says "Gets the name of your current fellowship".
- `componentname`: upstream parameter/return refer to a spell id, and the example text is copied from an item-count function.
- `getchardoubleprop`: upstream example (`[25]`, "current level") is copied from `getcharintprop`.
- `iif`: upstream return line is copied from `istrue`, and it refers to `if[]`, which is not a listed function (`ifthen[]` is the conditional-evaluation form).
- `listreduce` return type listed as List; `stopwatchelapsedseconds` as Stopwatch; `uiviewexists`/`uiviewvisible` as UIControl; `coordinatetostring` and `hexstr` as number; `getvar`/`getpvar`/`getgvar` as number. Corrected to the evident types.
- Examples fixed to call the function they document: `getgamemonthname` (was `getmonthname`), `getcancastspell_buff` (was `_hunt`), `getcharvital_buffedmax` (was `_current`), `coordinatedistanceflat` (was `withz`), `getcharburden[]` (was `[0]`), `actiontryapplyitem` (was `wobjectwobjectgetplayer`), `dictvalues` (was `dictkeys`).
- Unbalanced brackets fixed in the `listcopy`, `listreverse`, `listmap`, and `vtsetsetting` examples. `vtsetsetting`'s second parameter is named `setting` upstream; renamed `value`.
- Wrong example results: `ceiling[3.14159]` (upstream says 3), `getobjectinternaltype[test]` (upstream says "a length of 4"), `istrue[1]` (upstream says "Checks that 0 is true").
- Upstream escaping example for `p@cMan$)` drops the `)` from the backtick form.
- Unverified upstream claims kept as-is: `getunixtime` returns string, `wobjectlastidtime` "Returns 1", `getgamehour` range "0-16" (days have 16 hours, so likely 0–15).
- Spelling fixed throughout (e.g. "occurance", "templaye", "speficied").

## Related Docs

- [ub-commands.md](ub-commands.md) — `/ub` chat commands, including `/ub mexec`.
- [../Virindi/vtank-commands.md](../Virindi/vtank-commands.md) — `/vt mexec` and VTank meta commands.
- [VTank Meta Expressions](http://www.virindi.net/wiki/index.php/Meta_Expressions) — original VTank expression reference.
- [Plugin Settings](https://utilitybelt.gitlab.io/docs/plugin-settings/) — settings readable via `uboptget`.
