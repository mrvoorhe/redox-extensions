using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace RedoxExtensions.VirindiInterop;

public enum VTNavRouteType
{
    Circular = 1,
    Linear = 2,
    Once = 4
}

public struct VTNavCoord
{
    public VTNavCoord(double eastWest, double northSouth, double z)
    {
        this.EastWest = eastWest;
        this.NorthSouth = northSouth;
        this.Z = z;
    }

    public double EastWest { get; }
    public double NorthSouth { get; }
    public double Z { get; }

    /// <summary>
    /// Mirrors how VTank converts a landcell and local position into nav file coordinates
    /// </summary>
    public static VTNavCoord FromLandcell(int landcell, double localX, double localY, double localZ)
    {
        int landblockX = (landcell >> 24) & 0xFF;
        int landblockY = (landcell >> 16) & 0xFF;

        return new VTNavCoord(
            ((landblockX - 127) * 192.0 + localX - 84.0) / 240.0,
            ((landblockY - 127) * 192.0 + localY - 84.0) / 240.0,
            localZ / 240.0);
    }
}

/// <summary>
/// Builds a VTank nav route and writes it in the "uTank2 NAV 1.2" file format
/// </summary>
public class VTNavRoute
{
    public const string FileHeader = "uTank2 NAV 1.2";
    public const string FileExtension = ".nav";

    private const int PointNodeType = 0;
    private const int PauseNodeType = 3;
    private const int ChatCommandNodeType = 4;
    private const int OpenVendorNodeType = 5;
    private const int PortalNpcNodeType = 6;

    private readonly List<Node> _nodes = new();

    public VTNavRoute(VTNavRouteType routeType)
    {
        this.RouteType = routeType;
    }

    public VTNavRouteType RouteType { get; }

    public int Count => this._nodes.Count;

    public static string NormalizeName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase)
            ? trimmed.Substring(0, trimmed.Length - FileExtension.Length)
            : trimmed;
    }

    public void AddPoint(VTNavCoord position)
    {
        this._nodes.Add(new Node(PointNodeType, position));
    }

    public void AddPause(int milliseconds)
    {
        this._nodes.Add(new Node(PauseNodeType, default, Format(milliseconds)));
    }

    public void AddChatCommand(string text)
    {
        this._nodes.Add(new Node(ChatCommandNodeType, default, text));
    }

    public void AddOpenVendor(int vendorId, string vendorName)
    {
        this._nodes.Add(new Node(OpenVendorNodeType, default, Format(vendorId), vendorName));
    }

    public void AddPortalNpc(VTNavCoord position, string objectName, int objectClass, VTNavCoord objectPosition)
    {
        this._nodes.Add(new Node(
            PortalNpcNodeType,
            position,
            objectName,
            Format(objectClass),
            bool.TrueString,
            Format(objectPosition.EastWest),
            Format(objectPosition.NorthSouth),
            Format(objectPosition.Z)));
    }

    public string Serialize()
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder, CultureInfo.InvariantCulture))
        {
            writer.NewLine = "\r\n";
            writer.WriteLine(FileHeader);
            writer.WriteLine(Format((int)this.RouteType));
            writer.WriteLine(Format(this._nodes.Count));

            foreach (var node in this._nodes)
            {
                writer.WriteLine(Format(node.Type));
                writer.WriteLine(Format(node.Position.EastWest));
                writer.WriteLine(Format(node.Position.NorthSouth));
                writer.WriteLine(Format(node.Position.Z));
                writer.WriteLine(Format(0.0));

                foreach (var line in node.ExtraLines)
                {
                    writer.WriteLine(line);
                }
            }
        }

        return builder.ToString();
    }

    public void Save(string path)
    {
        File.WriteAllText(path, this.Serialize());
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

    private sealed class Node
    {
        public Node(int type, VTNavCoord position, params string[] extraLines)
        {
            this.Type = type;
            this.Position = position;
            this.ExtraLines = extraLines;
        }

        public int Type { get; }
        public VTNavCoord Position { get; }
        public string[] ExtraLines { get; }
    }
}
