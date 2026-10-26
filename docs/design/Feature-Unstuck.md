I'm trying to make my Virindi Meta files more robust.  I've noticed a few different issues that can happen while a meta is running that leads to the character getting stuck.

* There's one issue I can't explain where the meta just sits there doing nothing.  Moving my character a little bit gets the meta going again.
* The character gets blocked running a route with combat off due to monsters blocking the path.  This is most common in hallways.
* Using a key to unlock a door fails.  I'm not sure why this happens, but I think maybe it has to do with monsters around the character messing up the approach to the door.

This feature involves two parts.

# Part 1

1) Create a `/re unstuck` command.  Or `/re us` for short.   This command should to the following

If VT combat is disabled, then the first thing we can try and enabling combat to see if clearing monsters helps.  Do the following 

* Cache the current VT combat range
* Set the current combat range to 4
* Enable VT Combat

Then schedule a delayed function.  The delay should be 30 seconds.  After 30 seconds, do the following.

* Restore the VT combat range
* Disable combat again
* Issue the `/vt clearbusy` command
* Have the character move a bit to the left and then the right.

If VT combat was not enabled, then we can assume monsters are not around and therefore don't need to try and kill them.  Instead, do the following

* Issue the `/vt clearbusy` command
* Have the character move a bit to the left and then the right.


# Part 2

Next we need to create a system to detect that the character is stuck.  If we detect we are stuck, we should should use the new command you wrote and then reset the stuck timer.

To determine if we are stuck let's use somse heuristics

1) The characters position in the world has not changed for 1min.
2) The character has not attacked anything in 1min.

We don't want this stuck detection enabled by default.  That would be annoying doing normal play.  Instead, let's introduce a command to control it.  The default state should be off.

`/re unstuckdetect` - No args toggles state.  `on` explicitly turns on.  `off` explicitly turns off.  Accept 1 optional argument which is the timestamp needed to trigger.  Use 1min as the default.  The value should be in minutes.  So .5 would be 30s.  2 would be 2 minutes.

