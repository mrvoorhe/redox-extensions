I want to implement a feature where a configurable set of items are automatically given to a configurable character once that character is detected within a close range.

I don't know the scaling of the game off hand, but the range should be close.  Like .001 or something to start with.  I can always tweak it later.  Better yet, add a command to let me change the range so I can tinker with the value.  The command should be
`/re automulerange <value>`

Introduce a `/re` option and `/rf` option to control automule on/off.  Default to off.

The first part is how to configure the information.  Look for a directory next to the executable called "AutoMule".  Each file in the directory will correspond to a mule characters name.  Let's keep the file format simple at first.  Let's do "*.txt" files.

Each line the .txt file will be a Contains checked string.  For example, a file called "Foo.txt" with the contents
```
Salvage
```

would result in us looking for the characer "Foo" to be near by and once they were we would give all items in our inventory that have the name "Salvage" in their Name to that character.

Use a 100ms delay between each give.

Some additional restrictions.
* The character should be in peace mode in order for this to activate.
* Walking out of range of the auto give range should interrupt the giving and cause it to stop.
