using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RedoxExtensions.Location;

namespace RedoxExtensions.Tests.LocationTests
{
    [TestFixture]
    public class LocationDatabaseTests
    {
        [Test]
        public void VerifyDungeonsLoadedWithoutError()
        {
            LocationDatabase.LoadDungeonInfo(LocationDatabase.DefaultDataLocation);
        }

        [Test]
        public void CheckLocationsOfSingleLocationDungeon()
        {
            var dungeons = LocationDatabase.LoadDungeonInfo(LocationDatabase.DefaultDataLocation);

            Assert.Contains("green mire grave", dungeons.Keys);

            var greenMire = dungeons["green mire grave"];
            Assert.That(
                greenMire.Locations.Select(l => l.ToString()),
                Is.EquivalentTo(new []
                {
                    "27.8S, 71.6E"
                }));
        }

        [Test]
        public void CheckDropLocationsOfSingleLocationDungeon()
        {
            var dungeons = LocationDatabase.LoadDungeonInfo(LocationDatabase.DefaultDataLocation);

            Assert.Contains("green mire grave", dungeons.Keys);

            var greenMire = dungeons["green mire grave"];
            Assert.That(
                greenMire.DropLocations.Select(l => l.ToString()),
                Is.EquivalentTo(new[]
                {
                    "0x01E5020E 79.955452 -69.226143 0.005000 0.999816 0.000000 0.000000 -0.019198"
                }));
        }
    }
}
