using Lines.Shared.Settings;
using Xunit;

namespace LogicLines.Tests
{
    // In-memory реализация хранилища: вместо файлов/localStorage
    // данные живут в словаре, сервисы тестируются изолированно
    internal class MemoryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> data = new();

        public bool TryRead(string name, out string text)
        {
            if (data.TryGetValue(name, out string found))
            {
                text = found;
                return true;
            }
            text = null!;
            return false;
        }

        public void Write(string name, string text)
        {
            data[name] = text;
        }
    }

    public class SettingsServiceTests
    {
        [Fact]
        public void Load_BrokenJson_FallsBackToDefaults()
        {
            var store = new MemoryKeyValueStore();
            store.Write("settings", "{ это не json");

            var svc = new SettingsService(store);

            Assert.True(svc.Current.SoundEnabled);
            Assert.Equal(80, svc.Current.Volume);
            Assert.Equal("auto", svc.Current.Language);
        }

        [Fact]
        public void Update_PersistsAndRaisesChanged()
        {
            var store = new MemoryKeyValueStore();
            var svc = new SettingsService(store);
            int changed = 0;
            svc.Changed += () => changed++;

            svc.Update(s =>
            {
                s.Volume = 55;
                s.PlayerName = "Валя";
                s.SoundEnabled = false;
            });

            Assert.Equal(1, changed);

            // перечитываем из того же хранилища - значения сохранились
            var reloaded = new SettingsService(store);
            Assert.Equal(55, reloaded.Current.Volume);
            Assert.Equal("Валя", reloaded.Current.PlayerName);
            Assert.False(reloaded.Current.SoundEnabled);
        }

        [Fact]
        public void Update_VolumeOutOfRange_IsClamped()
        {
            var svc = new SettingsService(new MemoryKeyValueStore());

            svc.Update(s => s.Volume = 150);
            Assert.Equal(100, svc.Current.Volume);

            svc.Update(s => s.Volume = -5);
            Assert.Equal(0, svc.Current.Volume);
        }
    }

    public class ResultsServiceTests
    {
        [Fact]
        public void Qualifies_EmptyTable_OnlyPositiveScore()
        {
            var svc = new ResultsService(new MemoryKeyValueStore());

            Assert.False(svc.Qualifies(0));
            Assert.False(svc.Qualifies(-10));
            Assert.True(svc.Qualifies(1));
        }

        [Fact]
        public void Qualifies_FullTable_StrictlyAboveMinimum()
        {
            var svc = new ResultsService(new MemoryKeyValueStore());
            for (int i = 1; i <= ResultsService.MaxEntries; i++)
                svc.Add("Игрок" + i, i * 10); // 10..100, минимум 10

            Assert.False(svc.Qualifies(10));  // равный минимуму не проходит
            Assert.False(svc.Qualifies(5));
            Assert.True(svc.Qualifies(11));
        }

        [Fact]
        public void Add_SortsDescendingAndReturnsIndex()
        {
            var svc = new ResultsService(new MemoryKeyValueStore());

            Assert.Equal(0, svc.Add("Аня", 30));
            Assert.Equal(1, svc.Add("Боря", 10)); // второе место из двух
            Assert.Equal(1, svc.Add("Вера", 20)); // вклинилась между ними

            Assert.Equal(new[] { "Аня", "Вера", "Боря" },
                svc.Entries.Select(e => e.Name).ToArray());
            Assert.Equal(new[] { 30, 20, 10 },
                svc.Entries.Select(e => e.Score).ToArray());
        }

        [Fact]
        public void Add_SameName_KeepsBestScore()
        {
            var svc = new ResultsService(new MemoryKeyValueStore());

            svc.Add("Bob", 10);
            int index = svc.Add("bob", 20); // имя без учёта регистра

            Assert.Single(svc.Entries);
            Assert.Equal(20, svc.Entries[0].Score);
            Assert.Equal(0, index);

            svc.Add("BOB", 5); // худший результат не затирает лучший
            Assert.Single(svc.Entries);
            Assert.Equal(20, svc.Entries[0].Score);
        }

        [Fact]
        public void Add_RejectsEmptyNameAndZeroScore()
        {
            var svc = new ResultsService(new MemoryKeyValueStore());

            Assert.Equal(-1, svc.Add("", 100));
            Assert.Equal(-1, svc.Add("   ", 100));
            Assert.Equal(-1, svc.Add("Аня", 0));
            Assert.Empty(svc.Entries);
        }

        [Fact]
        public void Add_OverLimit_TrimsToTen()
        {
            var svc = new ResultsService(new MemoryKeyValueStore());
            for (int i = 1; i <= 12; i++)
                svc.Add("Игрок" + i, i); // 1..12

            Assert.Equal(ResultsService.MaxEntries, svc.Entries.Count);
            Assert.Equal(12, svc.Entries[0].Score);
            Assert.Equal(3, svc.Entries[9].Score); // 1 и 2 вытеснены
        }

        [Fact]
        public void Add_PersistsAcrossReload()
        {
            var store = new MemoryKeyValueStore();
            var svc = new ResultsService(store);
            svc.Add("Аня", 42);

            var reloaded = new ResultsService(store);
            Assert.Single(reloaded.Entries);
            Assert.Equal("Аня", reloaded.Entries[0].Name);
            Assert.Equal(42, reloaded.Entries[0].Score);
            Assert.False(string.IsNullOrEmpty(reloaded.Entries[0].Date));
        }

        [Fact]
        public void Load_BrokenJson_StartsEmpty()
        {
            var store = new MemoryKeyValueStore();
            store.Write("results", "%%%");

            var svc = new ResultsService(store);
            Assert.Empty(svc.Entries);
        }

        [Fact]
        public void Clear_EmptiesAndPersists()
        {
            var store = new MemoryKeyValueStore();
            var svc = new ResultsService(store);
            svc.Add("Аня", 42);

            svc.Clear();

            Assert.Empty(svc.Entries);
            Assert.Empty(new ResultsService(store).Entries);
        }
    }

    public class SavesServiceTests
    {
        private static GameSave MakeSave(int score)
        {
            var save = new GameSave { Score = score };
            save.Balls[0] = 1;
            save.NextBalls[0] = 2; save.NextBalls[1] = 3; save.NextBalls[2] = 4;
            return save;
        }

        [Fact]
        public void Fresh_HasTenEmptySlots()
        {
            var svc = new SavesService(new MemoryKeyValueStore());

            Assert.Equal(SavesService.SlotCount, svc.Slots.Count);
            Assert.All(svc.Slots, s => Assert.Null(s));
        }

        [Fact]
        public void Load_BrokenJson_TenEmptySlots()
        {
            var store = new MemoryKeyValueStore();
            store.Write("saves", "[битый");

            var svc = new SavesService(store);

            Assert.Equal(SavesService.SlotCount, svc.Slots.Count);
            Assert.All(svc.Slots, s => Assert.Null(s));
        }

        [Fact]
        public void Save_OutOfRangeSlots_AreIgnored()
        {
            var svc = new SavesService(new MemoryKeyValueStore());

            svc.Save(-1, MakeSave(1));
            svc.Save(SavesService.SlotCount, MakeSave(2));

            Assert.All(svc.Slots, s => Assert.Null(s));
        }

        [Fact]
        public void Save_FirstAndLastManualSlots_Work()
        {
            var svc = new SavesService(new MemoryKeyValueStore());

            svc.Save(1, MakeSave(11));
            svc.Save(SavesService.SlotCount - 1, MakeSave(99));

            Assert.Equal(11, svc.Slots[1]!.Score);
            Assert.Equal(99, svc.Slots[SavesService.SlotCount - 1]!.Score);
            Assert.False(string.IsNullOrEmpty(svc.Slots[1]!.Date)); // дата проставляется
        }

        [Fact]
        public void AutoSave_AlwaysGoesToSlotZero()
        {
            var svc = new SavesService(new MemoryKeyValueStore());

            svc.AutoSave(MakeSave(7));

            Assert.Equal(7, svc.Slots[SavesService.AutoSlot]!.Score);
            for (int i = 1; i < SavesService.SlotCount; i++)
                Assert.Null(svc.Slots[i]);
        }

        [Fact]
        public void Save_PersistsAcrossReload()
        {
            var store = new MemoryKeyValueStore();
            var svc = new SavesService(store);
            svc.Save(3, MakeSave(33));

            var reloaded = new SavesService(store);
            Assert.Equal(33, reloaded.Slots[3]!.Score);
            Assert.Equal(1, reloaded.Slots[3]!.Balls[0]);
            Assert.Equal(4, reloaded.Slots[3]!.NextBalls[2]);
            Assert.Null(reloaded.Slots[0]);
        }
    }
}
