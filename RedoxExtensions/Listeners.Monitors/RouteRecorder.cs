using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Listeners.Monitors;

/// <summary>
/// Records a VTank nav route by adding a waypoint each interval while the character is moving
/// </summary>
public class RouteRecorder : IDisposable
{
    private const int GameActionMessageType = 0xF7B1;
    private const int UseItemAction = 0x0036;
    private const int NpcContainerIcon = 8384;

    private static readonly TimeSpan RecordInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RepeatUseWindow = TimeSpan.FromSeconds(3);

    private VTNavRoute _route;
    private string _routeName;
    private string _routePath;
    private bool _inPortalSpace;
    private DateTime _lastCheckAt;
    private Location.Location _anchor;
    private int _lastUsedObjectId;
    private DateTime _lastUsedAt;

    public RouteRecorder()
    {
        REPlugin.Instance.Events.Decal.RenderFrame += Decal_RenderFrame;
        REPlugin.Instance.Events.Decal.ChangePortalMode += Decal_ChangePortalMode;
        REPlugin.Instance.Events.Decal.ClientDispatch += Decal_ClientDispatch;
    }

    public bool Recording => this._route != null;

    public void Start(string routeName, VTNavRouteType routeType)
    {
        if (this.Recording)
        {
            REPlugin.Instance.Chat.WriteLine("[RE] Already recording route : {0}", this._routeName);
            return;
        }

        var routePath = Path.Combine(VTUtilities.VTankDirectory, routeName + VTNavRoute.FileExtension);
        if (File.Exists(routePath))
        {
            REPlugin.Instance.Chat.WriteLine("[RE] Nav route already exists : {0}", routePath);
            return;
        }

        this._route = new VTNavRoute(routeType);
        this._routeName = routeName;
        this._routePath = routePath;
        this._lastCheckAt = DateTime.Now;

        this.AddPointAtCurrentLocation();

        REPlugin.Instance.Chat.WriteLine("[RE] Recording {0} route : {1}", routeType, routeName);
    }

    public void Stop()
    {
        if (!this.Recording)
        {
            REPlugin.Instance.Chat.WriteLine("[RE] Not recording");
            return;
        }

        this._route.Save(this._routePath);
        VTActions.LoadNav(this._routeName);

        REPlugin.Instance.Chat.WriteLine("[RE] Recorded route {0} with {1} waypoints", this._routeName, this._route.Count);

        this._route = null;
        this._routeName = null;
        this._routePath = null;
        this._anchor = null;
    }

    public void Dispose()
    {
        REPlugin.Instance.Events.Decal.RenderFrame -= Decal_RenderFrame;
        REPlugin.Instance.Events.Decal.ChangePortalMode -= Decal_ChangePortalMode;
        REPlugin.Instance.Events.Decal.ClientDispatch -= Decal_ClientDispatch;
    }

    private void AddPointAtCurrentLocation()
    {
        var current = Location.Location.CaptureCurrent();
        this._route.AddPoint(VTNavCoord.FromLandcell(current.LandBlock, current.X, current.Y, current.Z));
        this._route.Save(this._routePath);
        this._anchor = current;
    }

    private void AddPortalNpc(WorldObject worldObject)
    {
        var objectCoords = worldObject.Coordinates();
        var objectRawCoords = worldObject.RawCoordinates();
        if (objectCoords == null || objectRawCoords == null)
        {
            return;
        }

        var current = Location.Location.CaptureCurrent();
        this._route.AddPortalNpc(
            VTNavCoord.FromLandcell(current.LandBlock, current.X, current.Y, current.Z),
            worldObject.Name,
            (int)worldObject.ObjectClass,
            new VTNavCoord(objectCoords.EastWest, objectCoords.NorthSouth, objectRawCoords.Z / 240.0));
        this._route.Save(this._routePath);
        this._anchor = current;

        REPlugin.Instance.Chat.WriteLine("[RE] Recorded use of {0}", worldObject.Name);
    }

    private void AddOpenVendor(WorldObject worldObject)
    {
        if (Location.Location.HasMoved(this._anchor, Location.Location.CaptureCurrent()))
        {
            this.AddPointAtCurrentLocation();
        }

        this._route.AddOpenVendor(worldObject.Id, worldObject.Name);
        this._route.Save(this._routePath);

        REPlugin.Instance.Chat.WriteLine("[RE] Recorded open vendor {0}", worldObject.Name);
    }

    private bool IsRepeatUse(int objectId)
    {
        var now = DateTime.Now;
        var isRepeat = objectId == this._lastUsedObjectId && now - this._lastUsedAt < RepeatUseWindow;

        this._lastUsedObjectId = objectId;
        this._lastUsedAt = now;

        return isRepeat;
    }

    void Decal_RenderFrame(object sender, EventArgs e)
    {
        if (!this.Recording || this._inPortalSpace)
        {
            return;
        }

        var now = DateTime.Now;
        if (now - this._lastCheckAt < RecordInterval)
        {
            return;
        }

        this._lastCheckAt = now;

        if (Location.Location.HasMoved(this._anchor, Location.Location.CaptureCurrent()))
        {
            this.AddPointAtCurrentLocation();
        }
    }

    void Decal_ClientDispatch(object sender, NetworkMessageEventArgs e)
    {
        if (!this.Recording || e.Message.Type != GameActionMessageType || Convert.ToInt32(e.Message["action"]) != UseItemAction)
        {
            return;
        }

        var objectId = Convert.ToInt32(e.Message["object"]);
        var worldObject = REPlugin.Instance.WorldFilter[objectId];
        if (worldObject == null || this.IsRepeatUse(objectId))
        {
            return;
        }

        switch (worldObject.ObjectClass)
        {
            case ObjectClass.Npc:
            case ObjectClass.Container when worldObject.Icon == NpcContainerIcon:
                this.AddPortalNpc(worldObject);
                break;
            case ObjectClass.Vendor:
                this.AddOpenVendor(worldObject);
                break;
        }
    }

    void Decal_ChangePortalMode(object sender, ChangePortalModeEventArgs e)
    {
        this._inPortalSpace = e.Type == PortalEventType.EnterPortal;
    }
}
