using CitUp.Sim;
using Xunit;

namespace Tests;

public sealed class BuildingCatalogGeometryTests
{
    [Fact]
    public void DefaultCatalog_FootprintsNeverIntersectOwnClearance()
    {
        var catalog = BuildingCatalog.LoadDefault();

        foreach (var building in catalog.Buildings)
        {
            var origin = new Cell(10, 10);
            var footprint = BuildingGeometry.GetFootprintCells(building, origin);
            var clearance = BuildingGeometry.GetClearanceCells(building, origin);

            Assert.Empty(footprint.Intersect(clearance));
        }
    }

    [Fact]
    public void Catalog_FootprintOverlapsClearance_Throws()
    {
        const string json = """
                            {
                              "buildings": [
                                {
                                  "id": "bad",
                                  "name": "Bad",
                                  "cost": { "money": 0, "resources": {} },
                                  "constructionTimeTicks": 0,
                                  "footprint": ["FF", "FF"],
                                  "clearance": ["CC", "CC"]
                                }
                              ]
                            }
                            """;

        Assert.Throws<InvalidDataException>(() => BuildingCatalog.FromJson(json));
    }

    [Fact]
    public void Catalog_MasksThatCannotBeAligned_Throws()
    {
        const string json = """
                            {
                              "buildings": [
                                {
                                  "id": "bad",
                                  "name": "Bad",
                                  "cost": { "money": 0, "resources": {} },
                                  "constructionTimeTicks": 0,
                                  "footprint": ["FF", "FF"],
                                  "clearance": ["CCC", "C.C", "CCC"]
                                }
                              ]
                            }
                            """;

        Assert.Throws<InvalidDataException>(() => BuildingCatalog.FromJson(json));
    }
}