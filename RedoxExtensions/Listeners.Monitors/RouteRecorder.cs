using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Listeners.Monitors;

/// <summary>
/// Records a VTank nav route by adding a waypoint each interval while the character is moving
/// </summary>
public class RouteRecorder : IDisposable
{
    private static readonly TimeSpan RecordInterval = TimeSpan.FromSeconds(1);

    private VTNavRoute _route;
    private string _routeName;
    private string _routePath;
    private bool _inPortalSpace;
    private DateTime _lastCheckAt;
    private Location.Location _anchor;

    public RouteRecorder()
    {
        REPlugin.Instance.Events.Decal.RenderFrame += Decal_RenderFrame;
        REPlugin.Instance.Events.Decal.ChangePortalMode += Decal_ChangePortalMode;
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
    }

    private void AddPointAtCurrentLocation()
    {
        var current = Location.Location.CaptureCurrent();
        this._route.AddPoint(VTNavCoord.FromLandcell(current.LandBlock, current.X, current.Y, current.Z));
        this._route.Save(this._routePath);
        this._anchor = current;
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

    void Decal_ChangePortalMode(object sender, ChangePortalModeEventArgs e)
    {
        this._inPortalSpace = e.Type == PortalEventType.EnterPortal;
    }
}
