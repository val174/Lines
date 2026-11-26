using LogicLines;
using Xunit;

namespace LogicLines.Tests
{
    // Отмена последнего хода: снимок перед ходом, штрафы по счётчику
    // отмен, запрет двойной отмены, отмена хода, заполнившего поле,
    // сохранение/восстановление состояния отмены
    public class UndoTests
    {
        private const int N = 9;

        private static (Game Game, TestView View) CreateGame()
        {
            var view = new TestView();
            var game = new Game(N, view.Show, view.ShowScore);
            game.Step(); // init
            view.Events.Clear();
            return (game, view);
        }

        // полный контроль над полем (как в GameTests)
        private static void ClearBoard(Game game)
        {
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    game.map[x, y] = 0;

            game.free_fields.Clear();
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    game.free_fields.Add(new Game.FieldAddress { x = x, y = y });

            for (int i = 0; i < 3; i++)
            {
                game.balls[i].x = -1;
                game.balls[i].y = -1;
                game.balls[i].color = 0;
            }

            game.status = Game.Status.wait;
        }

        private static void PlaceBall(Game game, int x, int y, int color)
        {
            game.map[x, y] = color;
            game.free_fields.RemoveAll(f => f.x == x && f.y == y);
        }

        // раскраска без линий 5+: соседние клетки во всех 4 направлениях разного цвета
        private static int SafeColor(int x, int y)
        {
            return 1 + (x + 2 * y) % 6;
        }

        private static void RunStepsUntil(Game game, Func<Game, bool> condition, int maxSteps = 1000)
        {
            for (int i = 0; i < maxSteps && !condition(game); i++)
                game.Step();
            Assert.True(condition(game), "условие не наступило за " + maxSteps + " шагов логики");
        }

        private static int CountBalls(Game game)
        {
            int n = 0;
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    if (game.map[x, y] > 0)
                        n++;
            return n;
        }

        // простой ход без линий: шарик (0,0) -> (8,8)
        private static void MakeSimpleMove(Game game)
        {
            game.ClickBox(0, 0);
            game.ClickBox(8, 8);
            Assert.True(game.IsBusy, "путь не найден - ход не начался");
            RunStepsUntil(game, g => !g.IsBusy);
        }

        [Fact]
        public void NoMovesYet_CannotUndo()
        {
            (Game game, TestView _) = CreateGame();
            Assert.False(game.CanUndo);
            Assert.False(game.Undo());
            Assert.Equal(0, game.UndoCount);
        }

        [Fact]
        public void Undo_AfterSimpleMove_RestoresFieldAndScore()
        {
            (Game game, TestView view) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);

            MakeSimpleMove(game);
            Assert.True(game.CanUndo);
            Assert.Equal(1, game.map[8, 8]);
            Assert.Equal(0, game.map[0, 0]);

            view.Events.Clear();
            Assert.True(game.Undo());

            // поле и счёт - как до хода; появившиеся после хода шарики убраны
            Assert.Equal(1, game.map[0, 0]);
            Assert.Equal(0, game.map[8, 8]);
            Assert.Equal(1, CountBalls(game));
            Assert.Equal(0, game.CurrentScore);
            Assert.Equal(0, view.Score);
            Assert.Equal(Game.Status.wait, game.status);

            // первая отмена - без штрафа, счётчик вырос
            Assert.Equal(1, game.UndoCount);

            // UI получил полный перерендер (empty/ball на каждую клетку)
            int rendered = 0;
            foreach ((Ball _, Item item) in view.Events)
                if (item == Item.empty || item == Item.ball)
                    rendered++;
            Assert.Equal(N * N, rendered);
        }

        [Fact]
        public void Undo_TwiceForbidden_UntilNewMove()
        {
            (Game game, TestView _) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);

            MakeSimpleMove(game);
            Assert.True(game.Undo());
            Assert.False(game.CanUndo);
            Assert.False(game.Undo()); // дважды один ход отменить нельзя
            Assert.Equal(1, game.UndoCount);

            // новый ход снова разрешает отмену (снимок заменяется)
            MakeSimpleMove(game);
            Assert.True(game.CanUndo);
            Assert.True(game.Undo());
            Assert.Equal(2, game.UndoCount);
            Assert.Equal(1, game.map[0, 0]);
        }

        // границы штрафов: 1-я отмена бесплатна; 2-10 -> -1; 11-20 -> -2;
        // 21-30 -> -3; 31-40 -> -4; 41+ -> -5 (максимум)
        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(10, 1)]
        [InlineData(11, 2)]
        [InlineData(20, 2)]
        [InlineData(21, 3)]
        [InlineData(30, 3)]
        [InlineData(31, 4)]
        [InlineData(40, 4)]
        [InlineData(41, 5)]
        [InlineData(100, 5)]
        public void UndoPenalty_Boundaries(int n, int expected)
        {
            Assert.Equal(expected, Game.UndoPenalty(n));
        }

        [Fact]
        public void Undo_PenaltySubtractedFromRestoredScore()
        {
            (Game game, TestView view) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);

            MakeSimpleMove(game);
            Assert.Equal(0, game.CurrentScore);

            // счёт в снимке 0, но сделаем его ненулевым через второй ход
            // (после первой отмены ход повторяем: снимок заменяется)
            game.Undo();
            game.Restore(game.map.Clone() as int[,], new Ball[3]
            {
                new Ball { x = -1, y = -1, color = 1 },
                new Ball { x = -1, y = -1, color = 2 },
                new Ball { x = -1, y = -1, color = 3 },
            }, 7); // партия со счётом 7, без отмены
            Assert.Equal(0, game.UndoCount);

            MakeSimpleMove(game); // снимок со счётом 7
            game.UndoCount = 4;   // следующая отмена - 5-я, штраф -1

            Assert.True(game.Undo());
            Assert.Equal(6, game.CurrentScore); // 7 - 1
            Assert.Equal(6, view.Score);
            Assert.Equal(5, game.UndoCount);
        }

        [Fact]
        public void Undo_ScoreNeverBelowZero()
        {
            (Game game, TestView _) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);

            MakeSimpleMove(game); // снимок со счётом 0
            game.UndoCount = 40;  // следующая отмена - 41-я, штраф -5

            Assert.True(game.Undo());
            Assert.Equal(0, game.CurrentScore); // 0 - 5 ограничено нулём
        }

        [Fact]
        public void Undo_AfterLineStrip_RestoresPointsAndField()
        {
            (Game game, TestView view) = CreateGame();
            ClearBoard(game);

            // 4 шарика в ряд + 5-й подъезжает ходом (как в GameTests)
            for (int k = 0; k < 4; k++) PlaceBall(game, k, 0, 3);
            PlaceBall(game, 8, 8, 3);
            game.ClickBox(8, 8);
            game.ClickBox(4, 0);
            RunStepsUntil(game, g => g.status == Game.Status.wait);

            Assert.Equal(5, view.Score); // линия сгорела, +5
            for (int k = 0; k < 5; k++)
                Assert.Equal(0, game.map[k, 0]);

            Assert.True(game.Undo());

            // снимок был со счётом 0 (1-я отмена - без штрафа)
            Assert.Equal(0, view.Score);
            Assert.Equal(3, game.map[8, 8]); // шарик вернулся
            for (int k = 0; k < 4; k++)
                Assert.Equal(3, game.map[k, 0]); // сгоревшие вернулись
            Assert.Equal(0, game.map[4, 0]);
        }

        [Fact]
        public void Undo_AfterGameOver_ContinuesGame()
        {
            (Game game, TestView _) = CreateGame();
            ClearBoard(game);

            // поле заполнено, кроме трёх клеток: (0,1),(0,2) - путь,
            // (0,3) - назначение. Ход (0,0) -> (0,3) освобождает (0,0);
            // три «следующих» шарика (заданы детерминированно) встают в
            // (0,0),(0,1),(0,2) - поле заполнено, конец игры
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    if ((x, y) != (0, 1) && (x, y) != (0, 2) && (x, y) != (0, 3))
                        PlaceBall(game, x, y, SafeColor(x, y));

            game.balls[0] = new Ball { x = 0, y = 0, color = 2 };
            game.balls[1] = new Ball { x = 0, y = 1, color = 4 };
            game.balls[2] = new Ball { x = 0, y = 2, color = 6 };

            game.ClickBox(0, 0);
            game.ClickBox(0, 3);
            RunStepsUntil(game, g => g.IsGameOver);
            Assert.True(game.IsGameOver);

            // ход, приведший к заполнению поля, можно отменить
            // (до записи результата в таблицу рекордов)
            Assert.True(game.CanUndo);
            Assert.True(game.Undo());

            Assert.False(game.IsGameOver);
            Assert.Equal(Game.Status.wait, game.status);
            Assert.Equal(SafeColor(0, 0), game.map[0, 0]);
            Assert.Equal(0, game.map[0, 1]);
            Assert.Equal(0, game.map[0, 2]);
            Assert.Equal(0, game.map[0, 3]);
            Assert.Equal(78, CountBalls(game));
        }

        [Fact]
        public void Restore_ClearsUndoState()
        {
            (Game game, TestView _) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);
            MakeSimpleMove(game);
            Assert.True(game.CanUndo);

            var m = new int[N, N];
            m[0, 0] = 1;
            var nb = new Ball[3]
            {
                new Ball { x = -1, y = -1, color = 1 },
                new Ball { x = -1, y = -1, color = 2 },
                new Ball { x = -1, y = -1, color = 3 },
            };
            game.Restore(m, nb, 10);

            Assert.False(game.CanUndo);
            Assert.Equal(0, game.UndoCount);
        }

        [Fact]
        public void RestoreUndoState_Roundtrip_SnapshotAndCounterSurvive()
        {
            (Game game, TestView view) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);
            MakeSimpleMove(game);
            game.UndoCount = 3;

            // «сохраняем» состояние отмены через публичные аксессоры
            int[,] snapMap = (int[,])game.UndoSnapshotMap.Clone();
            Ball[] snapBalls = (Ball[])game.UndoSnapshotBalls.Clone();
            int snapScore = game.UndoSnapshotScore;
            int undoCount = game.UndoCount;

            // «загружаем» партию: сначала Restore (отмена очищается),
            // затем состояние отмены - как из полей сохранения
            var m = new int[N, N];
            m[8, 8] = 1;
            var nb = new Ball[3]
            {
                new Ball { x = -1, y = -1, color = 1 },
                new Ball { x = -1, y = -1, color = 2 },
                new Ball { x = -1, y = -1, color = 3 },
            };
            game.Restore(m, nb, 0);
            Assert.False(game.CanUndo);

            game.RestoreUndoState(snapMap, snapBalls, snapScore, false, undoCount);

            Assert.True(game.CanUndo);
            Assert.Equal(3, game.UndoCount);

            // 4-я отмена: штраф -1 от счёта снимка (снимок со счётом 0)
            Assert.True(game.Undo());
            Assert.Equal(4, game.UndoCount);
            Assert.Equal(0, view.Score);
            Assert.Equal(1, game.map[0, 0]); // снимок - поле до хода
            Assert.Equal(0, game.map[8, 8]);
        }

        [Fact]
        public void RestoreUndoState_UsedSnapshot_StaysBlocked()
        {
            (Game game, TestView _) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);
            MakeSimpleMove(game);
            game.Undo();
            Assert.False(game.CanUndo);

            // состояние «снимок использован» переживает сохранение
            game.RestoreUndoState(
                (int[,])game.UndoSnapshotMap.Clone(),
                (Ball[])game.UndoSnapshotBalls.Clone(),
                game.UndoSnapshotScore, true, game.UndoCount);

            Assert.False(game.CanUndo);
            Assert.False(game.Undo());
        }
    }
}
