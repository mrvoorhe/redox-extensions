Let's implement a feature to help with recording vtank nav routes.  Manually clicking the add waypoint button as your run around is tedious and often unnecessary for crude basic route creation.

The new command should be called

`/re recordroute` or `/re rr` for short.  It should accept one argument which is the name of the nav route to create.

Once the command is issued we should start creating way points.  The behavior should be

* Create a new route with the specified name
* Add a waypoint for the current location.

Then we should start an interval based waypoint adding.  Let's start with 1 second.  No waypoint should be added if the characters location in the world is the same as it was at the last internal.  We don't want to record a bunch of waypoints while I'm standing still.

To stop recording, the command should be issued again with no arguments.  Or optionally, with a "stop" argument passed.  Treat "stop" as a special argument that always means stop.  Never create a new nav route called "stop".

# Part 2

After the basic waypoint recording is implement, let's try to make this more advantaged.  This should be done as a seperate commit in case it doesn't work out in practice.

If I use an NPC while recording is active, then a use npc action should be recorded in the nav.

# Part 3

Again, more advanced.  Separate commit.  Record portal usage.  If I use a portal add an action to use the portal.


# Part 4

Again, more advanced.  If I use a portal gem in my inventory add an action to use that portal gem to the nav route.  I'm not exactly sure how to implement this.  See if you can figure it out.  It looks like most of the gems I care about are named ending in the word "Gem".  Some examples "Town Network Sending Gem" and "Marketplace Recall Gem"