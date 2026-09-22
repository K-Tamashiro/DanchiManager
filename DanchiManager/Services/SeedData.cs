using DanchiManager.Models;

namespace DanchiManager.Services;

public static class SeedData
{
    public static List<Building> DefaultBuildings()
    {
        var k1South = new List<FloorDef>
        {
            F(1, "101", "102", "103", "105", "106"),
            F(2, "201", "202", "203", "205", "206", "207"),
            F(3, "301", "302", "303", "305", "306", "307"),
            F(4, "401", "402", "403", "405", "406", "407"),
        };
        var k1East = new List<FloorDef>
        {
            F(1, "107", "108", "109"),
            F(2, "208", "209", "210", "211"),
            F(3, "308", "309", "310", "311"),
            F(4, "408", "409", "410", "411"),
        };

        return
        [
            B("錦林 第５棟", 1, Floors(3, 6)),
            B("錦林 第６棟", 2, Floors(3, 6)),
            B("錦林 第７棟", 3, Floors(1, 6)),
            B("錦林 第９棟", 4, Floors(3, 7)),
            B("錦林 第１０棟", 5, Floors(3, 7)),
            B("錦林 第１１棟", 6, Floors(3, 3)),
            B("錦林 第１２棟", 7, Floors(3, 3)),
            B("錦林 第１３棟", 8, Floors(3, 6)),
            B("錦林 第１６棟", 9, Floors(3, 6)),
            B("錦林 第１７棟", 10, Floors(3, 7)),
            B("錦林 第１８棟", 11, Floors(3, 7)),
            B("錦林 第１９棟", 12, Floors(3, 4)),
            B("錦林 第２０棟", 13, Floors(3, 2)),
            B("錦林 第２１棟", 14, Floors(3, 2)),
            B("錦林 第２２棟", 15, Floors(2, 2)),
            B("錦林 K1 南棟", 16, k1South),
            B("錦林 K1 東棟", 17, k1East, skip4: false),
            B("明照寺", 18, [F(1, "101")], skip4: false),
        ];
    }

    public static List<ParkingLot> DefaultLots()
    {
        return
        [
            Lot("第１駐車場", 5000, [1, 2, 3, 4, 5, 6, 7, 0, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20]),
            Lot("第２駐車場", 5000, Enumerable.Range(1, 28).ToArray()),
            Lot("第３駐車場", 5000, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]),
            Lot("第４駐車場", 5000, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 13, 14, 15, 16, 0, 18, 19, 20]),
            Lot("第５駐車場", 5000, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13]),
        ];
    }

    static Building B(string name, int order, List<FloorDef> floors, bool skip4 = true) => new()
    {
        Name = name,
        SortOrder = order,
        Floors = floors,
        Skip4 = skip4,
    };

    static FloorDef F(int level, params string[] rooms) => new() { Level = level, Rooms = [.. rooms] };

    static List<FloorDef> Floors(int levels, int perFloor, bool skip4 = true)
    {
        var list = new List<FloorDef>();
        for (var lv = 1; lv <= levels; lv++)
        {
            var rooms = new List<string>();
            var n = 1;
            while (rooms.Count < perFloor)
            {
                if (skip4 && n % 10 == 4) { n++; continue; }
                rooms.Add($"{lv * 100 + n}");
                n++;
            }
            list.Add(new FloorDef { Level = lv, Rooms = rooms });
        }
        return list;
    }

    static ParkingLot Lot(string name, int fee, int[] numbers)
    {
        var lot = new ParkingLot { Name = name, Fee = fee };
        foreach (var n in numbers)
        {
            var invalid = n == 0;
            lot.Slots.Add(new ParkingSlot
            {
                Occupant = invalid ? "利用不可" : "空き",
                IsInvalid = invalid,
            });
        }
        lot.Normalize();
        return lot;
    }
}
