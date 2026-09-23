using System.IO;
using System.Text.Json;
using DanchiManager.Models;
using Microsoft.Data.Sqlite;

namespace DanchiManager.Services;

public sealed class DatabaseService : IDisposable
{
    static readonly JsonSerializerOptions JsonOpt = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    readonly SqliteConnection _cnn;
    public string FilePath { get; }

    DatabaseService(string path)
    {
        FilePath = path;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        _cnn = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString());
        _cnn.Open();
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        cmd.ExecuteNonQuery();
        EnsureSchema();
    }

    public static DatabaseService Open(string path)
    {
        var db = new DatabaseService(path);
        db.SeedIfEmpty();
        return db;
    }

    public void Dispose() => _cnn.Dispose();

    void EnsureSchema()
    {
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS buildings (
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL UNIQUE,
              sort_order INTEGER NOT NULL,
              floors_json TEXT NOT NULL,
              skip4 INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE IF NOT EXISTS danchi (
              "棟" TEXT NOT NULL,
              "部屋番号" TEXT NOT NULL,
              "名前" TEXT,
              "フリガナ" TEXT,
              "性別" INTEGER DEFAULT 2,
              "人数" INTEGER DEFAULT 0,
              "バイク" INTEGER DEFAULT 0,
              "自転車" INTEGER DEFAULT 0,
              "電話" TEXT,
              "空きフラグ" INTEGER DEFAULT 0,
              "備考" TEXT,
              "更新日" TEXT,
              "入居順" INTEGER,
              "会員" TEXT,
              PRIMARY KEY ("棟", "部屋番号")
            );
            CREATE TABLE IF NOT EXISTS kaihi (
              "棟" TEXT NOT NULL,
              "部屋番号" TEXT NOT NULL,
              "年度" INTEGER NOT NULL,
              "月" INTEGER NOT NULL,
              "会費" INTEGER DEFAULT 0,
              "名前" TEXT,
              "順" INTEGER,
              PRIMARY KEY ("棟", "部屋番号", "年度", "月")
            );
            CREATE TABLE IF NOT EXISTS parking_lots (
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL,
              fee INTEGER NOT NULL,
              slots_json TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    void SeedIfEmpty()
    {
        var buildingCount = ScalarInt("SELECT COUNT(*) FROM buildings");
        var roomCount = ScalarInt("SELECT COUNT(*) FROM danchi");
        if (buildingCount == 0 && roomCount > 0)
        {
            InferBuildingsFromRooms();
            return;
        }
        if (buildingCount > 0) return;

        var buildings = SeedData.DefaultBuildings();
        using var tx = _cnn.BeginTransaction();
        foreach (var b in buildings)
        {
            SaveBuildingCore(b, tx);
            foreach (var no in b.AllRoomNos)
                UpsertRoomCore(RoomRecord.Empty(b.Name, no, b.SortOrder), tx);
        }
        foreach (var lot in SeedData.DefaultLots())
            SaveLotCore(lot, tx);
        tx.Commit();
    }

    void InferBuildingsFromRooms()
    {
        var defaults = SeedData.DefaultBuildings().ToDictionary(b => b.Name, b => b);
        var found = new List<(string Name, int Ord)>();
        using (var cmd = _cnn.CreateCommand())
        {
            cmd.CommandText = """SELECT DISTINCT "棟","入居順" FROM danchi ORDER BY "入居順","棟" """;
            using var r = cmd.ExecuteReader();
            while (r.Read())
                found.Add((r.GetString(0), r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1))));
        }
        using var tx = _cnn.BeginTransaction();
        foreach (var (name, ord) in found)
        {
            Building b;
            if (defaults.TryGetValue(name, out var known))
            {
                b = known;
                b.SortOrder = ord == 0 ? known.SortOrder : ord;
            }
            else
            {
                b = new Building
                {
                    Name = name,
                    SortOrder = ord,
                    Floors = InferFloors(name),
                };
            }
            SaveBuildingCore(b, tx);
        }
        tx.Commit();
    }

    List<FloorDef> InferFloors(string buildingName)
    {
        var nos = new List<string>();
        using (var cmd = _cnn.CreateCommand())
        {
            cmd.CommandText = """SELECT "部屋番号" FROM danchi WHERE "棟" = $b ORDER BY "部屋番号" """;
            cmd.Parameters.AddWithValue("$b", buildingName);
            using var r = cmd.ExecuteReader();
            while (r.Read()) nos.Add(Convert.ToString(r.GetValue(0)) ?? "");
        }

        var grouped = new SortedDictionary<int, List<string>>();
        foreach (var n in nos)
        {
            var level = 1;
            if (int.TryParse(n, out var v))
                level = Math.Max(1, v / 100);
            if (!grouped.TryGetValue(level, out var rooms))
            {
                rooms = [];
                grouped[level] = rooms;
            }
            rooms.Add(n);
        }
        return grouped.Select(kv => new FloorDef { Level = kv.Key, Rooms = kv.Value }).ToList();
    }

    public IReadOnlyList<Building> LoadBuildings()
    {
        var list = new List<Building>();
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = "SELECT id, name, sort_order, floors_json, skip4 FROM buildings ORDER BY sort_order, name";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Building
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                SortOrder = r.GetInt32(2),
                Floors = JsonSerializer.Deserialize<List<FloorDef>>(r.GetString(3), JsonOpt) ?? [],
                Skip4 = r.GetInt32(4) != 0,
            });
        }
        return list;
    }

    public IReadOnlyList<RoomRecord> LoadRooms(string? buildingName = null)
    {
        var list = new List<RoomRecord>();
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = buildingName is null
            ? """SELECT "棟","部屋番号","名前","フリガナ","性別","人数","バイク","自転車","電話","空きフラグ","備考","更新日","入居順","会員" FROM danchi"""
            : """SELECT "棟","部屋番号","名前","フリガナ","性別","人数","バイク","自転車","電話","空きフラグ","備考","更新日","入居順","会員" FROM danchi WHERE "棟" = $b""";
        if (buildingName is not null) cmd.Parameters.AddWithValue("$b", buildingName);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(ReadRoom(r));
        return list;
    }

    public RoomRecord? LoadRoom(string buildingName, string roomNo)
    {
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = """SELECT "棟","部屋番号","名前","フリガナ","性別","人数","バイク","自転車","電話","空きフラグ","備考","更新日","入居順","会員" FROM danchi WHERE "棟" = $b AND "部屋番号" = $r""";
        cmd.Parameters.AddWithValue("$b", buildingName);
        cmd.Parameters.AddWithValue("$r", roomNo);
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadRoom(r) : null;
    }

    static RoomRecord ReadRoom(SqliteDataReader r) => new()
    {
        BuildingName = r.GetString(0),
        RoomNo = Convert.ToString(r.GetValue(1)) ?? "",
        Name = r.IsDBNull(2) ? "" : r.GetString(2).Trim(),
        Furigana = r.IsDBNull(3) ? "" : r.GetString(3).Trim(),
        Gender = (GenderFlag)(r.IsDBNull(4) ? 2 : r.GetInt32(4)),
        People = r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5)),
        Bikes = r.IsDBNull(6) ? 0 : Convert.ToInt32(r.GetValue(6)),
        Bicycles = r.IsDBNull(7) ? 0 : Convert.ToInt32(r.GetValue(7)),
        Phone = r.IsDBNull(8) ? "" : r.GetString(8).Trim(),
        Vacancy = (VacancyFlag)(r.IsDBNull(9) ? 0 : r.GetInt32(9)),
        Note = r.IsDBNull(10) ? "" : r.GetString(10),
        UpdatedAt = r.IsDBNull(11) ? "" : r.GetString(11),
        SortOrder = r.IsDBNull(12) ? 0 : Convert.ToInt32(r.GetValue(12)),
        Member = !r.IsDBNull(13) && string.Equals(Convert.ToString(r.GetValue(13)), "True", StringComparison.OrdinalIgnoreCase),
    };

    public void UpsertRoom(RoomRecord room)
    {
        using var tx = _cnn.BeginTransaction();
        UpsertRoomCore(room, tx);
        tx.Commit();
    }

    void UpsertRoomCore(RoomRecord room, SqliteTransaction tx)
    {
        room.UpdatedAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        using var del = _cnn.CreateCommand();
        del.Transaction = tx;
        del.CommandText = """DELETE FROM danchi WHERE "棟" = $b AND "部屋番号" = $r""";
        del.Parameters.AddWithValue("$b", room.BuildingName);
        del.Parameters.AddWithValue("$r", room.RoomNo);
        del.ExecuteNonQuery();

        using var ins = _cnn.CreateCommand();
        ins.Transaction = tx;
        ins.CommandText = """
            INSERT INTO danchi ("棟","部屋番号","名前","フリガナ","性別","人数","バイク","自転車","電話","空きフラグ","備考","更新日","入居順","会員")
            VALUES ($b,$r,$n,$f,$g,$p,$k,$c,$t,$v,$note,$u,$s,$m)
            """;
        ins.Parameters.AddWithValue("$b", room.BuildingName);
        ins.Parameters.AddWithValue("$r", room.RoomNo);
        ins.Parameters.AddWithValue("$n", room.Name);
        ins.Parameters.AddWithValue("$f", room.Furigana);
        ins.Parameters.AddWithValue("$g", (int)room.Gender);
        ins.Parameters.AddWithValue("$p", room.People);
        ins.Parameters.AddWithValue("$k", room.Bikes);
        ins.Parameters.AddWithValue("$c", room.Bicycles);
        ins.Parameters.AddWithValue("$t", room.Phone);
        ins.Parameters.AddWithValue("$v", (int)room.Vacancy);
        ins.Parameters.AddWithValue("$note", room.Note);
        ins.Parameters.AddWithValue("$u", room.UpdatedAt);
        ins.Parameters.AddWithValue("$s", room.SortOrder);
        ins.Parameters.AddWithValue("$m", room.Member ? "True" : "False");
        ins.ExecuteNonQuery();
    }

    public void ClearRoom(string buildingName, string roomNo, int sortOrder, string keepNote)
    {
        var empty = RoomRecord.Empty(buildingName, roomNo, sortOrder);
        empty.Note = keepNote;
        UpsertRoom(empty);
    }

    public void SaveBuilding(Building building, string? oldName)
    {
        using var tx = _cnn.BeginTransaction();
        if (!string.IsNullOrEmpty(oldName) && oldName != building.Name)
        {
            Exec(tx, """UPDATE danchi SET "棟" = $n WHERE "棟" = $o""", ("$n", building.Name), ("$o", oldName));
            Exec(tx, """UPDATE kaihi SET "棟" = $n WHERE "棟" = $o""", ("$n", building.Name), ("$o", oldName));
        }
        SaveBuildingCore(building, tx);

        var wanted = building.AllRoomNos.ToHashSet();
        using var exist = _cnn.CreateCommand();
        exist.Transaction = tx;
        exist.CommandText = """SELECT "部屋番号" FROM danchi WHERE "棟" = $b""";
        exist.Parameters.AddWithValue("$b", building.Name);
        var have = new HashSet<string>();
        using (var r = exist.ExecuteReader())
        {
            while (r.Read()) have.Add(Convert.ToString(r.GetValue(0)) ?? "");
        }
        foreach (var extra in have.Where(x => !wanted.Contains(x)))
            Exec(tx, """DELETE FROM danchi WHERE "棟" = $b AND "部屋番号" = $r""", ("$b", building.Name), ("$r", extra));
        foreach (var no in wanted.Where(x => !have.Contains(x)))
            UpsertRoomCore(RoomRecord.Empty(building.Name, no, building.SortOrder), tx);
        tx.Commit();
    }

    void SaveBuildingCore(Building b, SqliteTransaction tx)
    {
        using var cmd = _cnn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO buildings (id, name, sort_order, floors_json, skip4)
            VALUES ($id, $name, $ord, $floors, $skip)
            ON CONFLICT(id) DO UPDATE SET name=$name, sort_order=$ord, floors_json=$floors, skip4=$skip
            """;
        cmd.Parameters.AddWithValue("$id", b.Id);
        cmd.Parameters.AddWithValue("$name", b.Name);
        cmd.Parameters.AddWithValue("$ord", b.SortOrder);
        cmd.Parameters.AddWithValue("$floors", JsonSerializer.Serialize(b.Floors, JsonOpt));
        cmd.Parameters.AddWithValue("$skip", b.Skip4 ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void DeleteBuilding(string id, string name)
    {
        using var tx = _cnn.BeginTransaction();
        Exec(tx, "DELETE FROM buildings WHERE id = $id", ("$id", id));
        Exec(tx, """DELETE FROM danchi WHERE "棟" = $n""", ("$n", name));
        Exec(tx, """DELETE FROM kaihi WHERE "棟" = $n""", ("$n", name));
        tx.Commit();
    }

    public FeeYear LoadFees(string buildingName, string roomNo, int year, string name, int sortOrder)
    {
        var fee = new FeeYear
        {
            BuildingName = buildingName,
            RoomNo = roomNo,
            Year = year,
            Name = name,
            SortOrder = sortOrder,
        };
        var settings = PathService.LoadSettings();
        var standard = settings.MonthlyFee > 0 ? settings.MonthlyFee : AppConstants.DefaultFee;
        var start = settings.EnvelopeStartMonth is >= 1 and <= 12 ? settings.EnvelopeStartMonth : 4;
        for (var i = 0; i < 12; i++)
        {
            var calendar = (start - 1 + i) % 12 + 1;
            var fiscal = (calendar + 8) % 12;
            fee.Months.Add(new FeeMonth { Month = fiscal, Amount = 0, Standard = standard });
        }

        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = """SELECT "月","会費" FROM kaihi WHERE "棟"=$b AND "部屋番号"=$r AND "年度"=$y ORDER BY "月" """;
        cmd.Parameters.AddWithValue("$b", buildingName);
        cmd.Parameters.AddWithValue("$r", roomNo);
        cmd.Parameters.AddWithValue("$y", year);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var m = Convert.ToInt32(reader.GetValue(0));
            var amt = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1));
            if (fee.Months.FirstOrDefault(x => x.Month == m) is { } slot)
                slot.Amount = amt;
        }
        return fee;
    }

    public void SaveFees(FeeYear fee)
    {
        using var tx = _cnn.BeginTransaction();
        Exec(tx, """DELETE FROM kaihi WHERE "棟"=$b AND "部屋番号"=$r AND "年度"=$y""",
            ("$b", fee.BuildingName), ("$r", fee.RoomNo), ("$y", fee.Year));
        foreach (var m in fee.Months)
        {
            using var ins = _cnn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = """INSERT INTO kaihi ("棟","部屋番号","年度","月","会費","名前","順") VALUES ($b,$r,$y,$m,$a,$n,$s)""";
            ins.Parameters.AddWithValue("$b", fee.BuildingName);
            ins.Parameters.AddWithValue("$r", fee.RoomNo);
            ins.Parameters.AddWithValue("$y", fee.Year);
            ins.Parameters.AddWithValue("$m", m.Month);
            ins.Parameters.AddWithValue("$a", m.Amount);
            ins.Parameters.AddWithValue("$n", fee.Name);
            ins.Parameters.AddWithValue("$s", fee.SortOrder);
            ins.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public IReadOnlyList<(string Building, string RoomNo, string Name, int Total, bool Member, int Sort)> FeeList(int year)
    {
        var list = new List<(string, string, string, int, bool, int)>();
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = """
            SELECT k."棟", k."部屋番号", k."名前", SUM(k."会費"), d."会員", k."順"
            FROM kaihi k
            LEFT JOIN danchi d ON d."棟" = k."棟" AND d."部屋番号" = k."部屋番号"
            WHERE k."年度" = $y
            GROUP BY k."棟", k."部屋番号", k."名前", d."会員", k."順"
            ORDER BY k."順", k."部屋番号"
            """;
        cmd.Parameters.AddWithValue("$y", year);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add((
                r.GetString(0),
                Convert.ToString(r.GetValue(1)) ?? "",
                r.IsDBNull(2) ? "" : r.GetString(2),
                r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
                !r.IsDBNull(4) && string.Equals(Convert.ToString(r.GetValue(4)), "True", StringComparison.OrdinalIgnoreCase),
                r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5))
            ));
        }
        return list;
    }

    public IReadOnlyList<ParkingLot> LoadLots()
    {
        var list = new List<ParkingLot>();
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = "SELECT id, name, fee, slots_json FROM parking_lots ORDER BY name";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var lot = new ParkingLot
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                Fee = r.GetInt32(2),
            };
            var slots = JsonSerializer.Deserialize<List<ParkingSlot>>(r.GetString(3), JsonOpt) ?? [];
            foreach (var s in slots) lot.Slots.Add(s);
            list.Add(lot);
        }
        foreach (var lot in list)
        {
            if (lot.Normalize())
                SaveLot(lot);
        }
        if (list.Count == 0)
        {
            foreach (var lot in SeedData.DefaultLots())
            {
                SaveLot(lot);
                list.Add(lot);
            }
        }
        return list;
    }

    public void SaveLot(ParkingLot lot)
    {
        using var tx = _cnn.BeginTransaction();
        SaveLotCore(lot, tx);
        tx.Commit();
    }

    void SaveLotCore(ParkingLot lot, SqliteTransaction tx)
    {
        using var cmd = _cnn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO parking_lots (id, name, fee, slots_json)
            VALUES ($id, $name, $fee, $slots)
            ON CONFLICT(id) DO UPDATE SET name=$name, fee=$fee, slots_json=$slots
            """;
        cmd.Parameters.AddWithValue("$id", lot.Id);
        cmd.Parameters.AddWithValue("$name", lot.Name);
        cmd.Parameters.AddWithValue("$fee", lot.Fee);
        cmd.Parameters.AddWithValue("$slots", JsonSerializer.Serialize(lot.Slots.ToList(), JsonOpt));
        cmd.ExecuteNonQuery();
    }

    public void DeleteLot(string id)
    {
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = "DELETE FROM parking_lots WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public BuildingStats Stats(string? buildingName)
    {
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = buildingName is null
            ? """SELECT COUNT(*), SUM(CASE WHEN "空きフラグ"=0 THEN 1 ELSE 0 END), SUM(CASE WHEN "空きフラグ"=1 THEN 1 ELSE 0 END), IFNULL(SUM("人数"),0), IFNULL(SUM("バイク"),0), IFNULL(SUM("自転車"),0) FROM danchi"""
            : """SELECT COUNT(*), SUM(CASE WHEN "空きフラグ"=0 THEN 1 ELSE 0 END), SUM(CASE WHEN "空きフラグ"=1 THEN 1 ELSE 0 END), IFNULL(SUM("人数"),0), IFNULL(SUM("バイク"),0), IFNULL(SUM("自転車"),0) FROM danchi WHERE "棟"=$b""";
        if (buildingName is not null) cmd.Parameters.AddWithValue("$b", buildingName);
        using var r = cmd.ExecuteReader();
        r.Read();
        return new BuildingStats(
            r.GetInt32(0),
            r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1)),
            r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2)),
            r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
            r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
            r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5)));
    }

    public void BackupTo(string destPath)
    {
        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var dest = new SqliteConnection($"Data Source={destPath}");
        dest.Open();
        _cnn.BackupDatabase(dest);
    }

    int ScalarInt(string sql)
    {
        using var cmd = _cnn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    void Exec(SqliteTransaction tx, string sql, params (string Name, object Value)[] args)
    {
        using var cmd = _cnn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (n, v) in args) cmd.Parameters.AddWithValue(n, v);
        cmd.ExecuteNonQuery();
    }
}
