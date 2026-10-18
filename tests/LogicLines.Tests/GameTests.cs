using LogicLines;
using Xunit;

namespace LogicLines.Tests
{
    // Ключевая логика Game: поиск пути, сжигание линий, счёт,
    // конец игры, выбор случайных полей (регрессия на баг rand.Next).
    // Состояние Game доступно через internal (InternalsVisibleTo).
    public class GameTests
    {
        private const int N = 9;

        private static (Game Game, TestView View) CreateGame()
        {
            var view = new TestView();
            var game = new Game(N, view.Show, view.ShowScore);
            game.Step(); // init: поле очищено, выставлены 3 шарика и 3 подсказки
            view.Events.Clear();
            return (game, view);
        }

        // Полный контроль над полем: убираем случайные шарики и подсказки,
        // приводим free_fields в соответствие с пустым полем
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

        [Fact]
        public void ClickWhileBusy_IsIgnored_ThenLogicIdleAgain()
        {
            (Game game, TestView _) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);
            PlaceBall(game, 8, 8, 2);

            game.ClickBox(0, 0); // выбрали шарик - ожидание цели
            Assert.False(game.IsBusy);

            game.ClickBox(5, 5); // цель - начался показ пути/движение
            Assert.True(game.IsBusy);

            // клик по другому шарику во время движения игнорируется
            // (UI такие клики ставит в очередь и повторяет позже)
            game.ClickBox(8, 8);
            Assert.True(game.IsBusy);

            RunStepsUntil(game, g => !g.IsBusy);
            Assert.False(game.IsBusy);
        }

        [Fact]
        public void MoveBall_PathExists_BallMovesToDestination()
        {
            (Game game, TestView _) = CreateGame();
            ClearBoard(game);
            PlaceBall(game, 0, 0, 1);

            game.ClickBox(0, 0); // выбрали шарик
            Assert.Equal(Game.Status.ball_mark, game.status);

            game.ClickBox(8, 8); // назначение - путь есть
            Assert.Equal(Game.Status.path_show, game.status);

            RunStepsUntil(game, g => g.status == Game.Status.next_balls);

            Assert.Equal(1, game.map[8, 8]);
            Assert.Equal(0, game.map[0, 0]);
        }

        [Fact]
        public void MoveBall_NoPath_BallStaysMarked()
        {
            (Game game, TestView _) = CreateGame();
            ClearBoard(game);

            // заполняем всё поле, кроме шарика и клетки назначения - пути нет
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    if ((x, y) != (0, 0) && (x, y) != (8, 8))
                        PlaceBall(game, x, y, SafeColor(x, y));
            PlaceBall(game, 0, 0, 1);

            game.ClickBox(0, 0);
            Assert.Equal(Game.Status.ball_mark, game.status);

            game.ClickBox(8, 8);
            Assert.Equal(Game.Status.ball_mark, game.status); // не поехал - ждём дальше
            Assert.Equal(1, game.map[0, 0]);
            Assert.Equal(0, game.map[8, 8]);
        }

        [Theory]
        [InlineData("horizontal")]
        [InlineData("vertical")]
        [InlineData("diagonal")]
        public void StripLine_FiveInARow_LineBurnsAndScores(string direction)
        {
            (Game game, TestView view) = CreateGame();
            ClearBoard(game);

            int color = 3;
            switch (direction)
            {
                case "horizontal":
                    for (int k = 0; k < 4; k++) PlaceBall(game, k, 0, color);
                    PlaceBall(game, 8, 8, color);
                    game.ClickBox(8, 8);
                    game.ClickBox(4, 0);
                    break;
                case "vertical":
                    for (int k = 0; k < 4; k++) PlaceBall(game, 0, k, color);
                    PlaceBall(game, 8, 8, color);
                    game.ClickBox(8, 8);
                    game.ClickBox(0, 4);
                    break;
                case "diagonal":
                    for (int k = 0; k < 4; k++) PlaceBall(game, k, k, color);
                    PlaceBall(game, 8, 0, color);
                    game.ClickBox(8, 0);
                    game.ClickBox(4, 4);
                    break;
            }

            RunStepsUntil(game, g => g.status == Game.Status.wait);

            for (int k = 0; k < 5; k++)
            {
                int x = direction == "vertical" ? 0 : k;
                int y = direction == "horizontal" ? 0 : k;
                Assert.Equal(0, game.map[x, y]); // линия сожжена
            }
            Assert.Equal(5, view.Score); // 5 шариков = 5 очков
        }

        [Fact]
        public void StripLine_FourInARow_NothingBurns()
        {
            (Game game, TestView view) = CreateGame();
            ClearBoard(game);

            for (int k = 0; k < 3; k++) PlaceBall(game, k, 0, 1);
            PlaceBall(game, 8, 8, 1);

            game.ClickBox(8, 8);
            game.ClickBox(3, 0); // получится 4 в ряд - меньше пяти

            RunStepsUntil(game, g => g.status == Game.Status.next_balls);

            for (int k = 0; k < 4; k++)
                Assert.Equal(1, game.map[k, 0]); // всё на месте
            Assert.Equal(0, view.Score);
        }

        [Fact]
        public void GameOver_FullBoard_Stops_AndNewGameRestarts()
        {
            (Game game, TestView view) = CreateGame();
            ClearBoard(game);

            // заполняем всё поле без единой линии 5+
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    PlaceBall(game, x, y, SafeColor(x, y));

            game.status = Game.Status.next_balls;
            game.Step(); // ShowNextBalls: линий нет, поле полное -> конец игры

            Assert.Equal(Game.Status.stop, game.status);
            Assert.True(game.IsGameOver);

            // скрытого перезапуска кликом больше нет: в статусе stop
            // клики игнорируются (UI проверяет IsGameOver до вызова)
            game.ClickBox(0, 0);
            game.Step();
            Assert.Equal(Game.Status.stop, game.status);

            // новая игра - только явным NewGame()
            game.NewGame();
            game.Step();

            Assert.Equal(Game.Status.wait, game.status);
            Assert.False(game.IsGameOver);
            Assert.Equal(0, view.Score);

            int ballsCount = 0;
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    if (game.map[x, y] > 0)
                        ballsCount++;
            Assert.Equal(3, ballsCount);
        }

        [Fact]
        public void Restore_RebuildsFieldNextBallsAndScore()
        {
            (Game game, TestView view) = CreateGame();

            // сохранённое состояние: 3 шарика, 2 резервации (-1) под
            // подсказки следующих шариков, счёт 42
            var saved = new int[N, N];
            saved[0, 0] = 1;
            saved[4, 4] = 3;
            saved[8, 8] = 6;
            saved[2, 2] = -1;
            saved[7, 1] = -1;

            var next = new Ball[3];
            next[0] = new Ball { x = 2, y = 2, color = 4 };
            next[1] = new Ball { x = 7, y = 1, color = 2 };
            next[2] = new Ball { x = -1, y = -1, color = 5 };

            view.Events.Clear();
            game.Restore(saved, next, 42);

            Assert.Equal(Game.Status.wait, game.status);
            Assert.False(game.IsGameOver);
            Assert.False(game.IsBusy);
            Assert.Equal(42, game.CurrentScore);
            Assert.Equal(42, view.Score);
            Assert.Equal(1, game.map[0, 0]);
            Assert.Equal(3, game.map[4, 4]);
            Assert.Equal(6, game.map[8, 8]);
            Assert.Equal(-1, game.map[2, 2]);
            Assert.Equal(4, game.balls[0].color);
            Assert.Equal(2, game.balls[1].color);
            Assert.Equal(7, game.balls[1].x);
            Assert.Equal(-1, game.balls[2].x);

            // инвариант: free_fields == клетки с map == 0
            int freeOnMap = 0;
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    if (game.map[x, y] == 0)
                        freeOnMap++;
            Assert.Equal(freeOnMap, game.free_fields.Count);
            foreach (Game.FieldAddress f in game.free_fields)
                Assert.Equal(0, game.map[f.x, f.y]);

            // UI получил полный перерендер: empty/ball на каждую клетку
            // и подсказки для двух зарезервированных позиций
            int rendered = 0, hints = 0;
            foreach ((Ball ball, Item item) in view.Events)
            {
                if (item == Item.empty || item == Item.ball)
                    rendered++;
                if (item == Item.next)
                    hints++;
            }
            Assert.Equal(N * N, rendered);
            Assert.Equal(2, hints);
        }

        [Fact]
        public void SelectNextBall_LastFreeFieldIsReachable()
        {
            // регрессия на баг rand.Next(0, Length - 1): верхняя граница
            // исключительная, последнее свободное поле не выбиралось никогда
            (Game game, TestView view) = CreateGame();
            ClearBoard(game);

            // 4 свободные клетки; (8,8) - всегда последняя в списке free_fields
            var free = new[] { (0, 0), (3, 3), (6, 6), (8, 8) };
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    if (!free.Contains((x, y)))
                        PlaceBall(game, x, y, SafeColor(x, y));

            var picked = new HashSet<(int, int)>();
            for (int trial = 0; trial < 50; trial++)
            {
                foreach ((int fx, int fy) in free)
                    game.map[fx, fy] = 0;
                game.free_fields.Clear();
                foreach ((int fx, int fy) in free)
                    game.free_fields.Add(new Game.FieldAddress { x = fx, y = fy });
                for (int i = 0; i < 3; i++)
                {
                    game.balls[i].x = -1;
                    game.balls[i].y = -1;
                    game.balls[i].color = 0;
                }

                view.Events.Clear();
                game.status = Game.Status.next_balls;
                game.Step(); // SelectNextBalls выберет 3 из 4 свободных

                foreach ((Ball ball, Item item) in view.Events)
                    if (item == Item.next)
                        picked.Add((ball.x, ball.y));
            }

            // со старым багом (8,8) не выпала бы ни разу;
            // без бага вероятность не выпасть за 50 попыток - (1/4)^50
            Assert.Contains((8, 8), picked);
        }
    }
}
