using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LogicLines;
using Xunit;

namespace LogicLines.Tests
{
    // Фуззинг с инвариантом: free_fields ровно = клетки с map==0
    // (нарушение - источник бесконечного цикла в SelectNextBall).
    // Ходы - как у живого игрока: клик по шарику, затем по пустой клетке.
    // При нарушении тест падает с дампом состояния и логом кликов.
    public class FuzzTests
    {
        private const int N = 9;

        private class InvariantException : Exception
        {
            public InvariantException(string message) : base(message) { }
        }

        private static void CheckInvariant(Game game, string context)
        {
            var free = game.free_fields;
            int mapFree = 0;
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                {
                    if (game.map[x, y] == 0) mapFree++;
                    bool inList = free.Exists(f => f.x == x && f.y == y);
                    if (game.map[x, y] == 0 && !inList)
                        throw new InvariantException(
                            $"{context}: клетка ({x},{y}) map=0, но НЕТ в free_fields, status={game.status}");
                    if (game.map[x, y] != 0 && inList)
                        throw new InvariantException(
                            $"{context}: клетка ({x},{y}) map={game.map[x, y]}, но ЕСТЬ в free_fields, status={game.status}");
                }
            if (free.Count != mapFree)
                throw new InvariantException(
                    $"{context}: free_fields.Count={free.Count}, map==0: {mapFree}, status={game.status}");
        }

        private static string DumpMap(Game game)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                    sb.Append(game.map[x, y] switch
                    {
                        0 => ".",
                        -1 => "o",
                        _ => game.map[x, y].ToString()
                    });
                sb.AppendLine();
            }
            return sb.ToString();
        }

        [Fact]
        public void Invariant_Holds_RandomGames()
        {
            int seedBase = Environment.TickCount;
            for (int gameIndex = 0; gameIndex < 60; gameIndex++)
            {
                var random = new Random(seedBase + gameIndex);
                var view = new TestView();
                var game = new Game(N, view.Show, view.ShowScore);
                var clickLog = new StringBuilder();
                int clicks = 0;

                try
                {
                    game.Step(); // init
                    CheckInvariant(game, $"игра{gameIndex} init");

                    int steps = 0;
                    while (steps < 4000)
                    {
                        steps++;
                        if (game.IsGameOver)
                        {
                            game.NewGame();
                            game.Step();
                            CheckInvariant(game, $"игра{gameIndex} шаг{steps} после NewGame");
                            continue;
                        }

                        // ход игрока: сначала клик по занятой клетке (выбор),
                        // затем по пустой или подсказочной (map <= 0)
                        if (random.Next(2) == 0)
                        {
                            for (int tries = 0; tries < 20; tries++)
                            {
                                int x = random.Next(N), y = random.Next(N);
                                if (game.map[x, y] > 0)
                                {
                                    clicks++;
                                    clickLog.AppendLine($"ClickBox({x}, {y}); Step(); // выбор, status был {game.status}");
                                    game.ClickBox(x, y);
                                    game.Step();
                                    CheckInvariant(game, $"игра{gameIndex} клик{clicks} выбор");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (int tries = 0; tries < 20; tries++)
                            {
                                int x = random.Next(N), y = random.Next(N);
                                if (game.map[x, y] <= 0)
                                {
                                    clicks++;
                                    clickLog.AppendLine($"ClickBox({x}, {y}); Step(); // цель map={game.map[x, y]}, status был {game.status}");
                                    game.ClickBox(x, y);
                                    game.Step();
                                    CheckInvariant(game, $"игра{gameIndex} клик{clicks} цель");
                                    break;
                                }
                            }
                        }

                        // доводим анимационные состояния
                        for (int guard = 0; guard < 300; guard++)
                        {
                            var before = game.status;
                            game.Step();
                            CheckInvariant(game, $"игра{gameIndex} шаг{steps} guard{guard}");
                            if (game.status == before &&
                                (game.status == Game.Status.wait || game.status == Game.Status.stop))
                                break;
                        }
                    }
                }
                catch (InvariantException ex)
                {
                    Assert.Fail(
                        $"seedBase={seedBase} gameIndex={gameIndex}\n" +
                        $"{ex.Message}\n--- map ---\n{DumpMap(game)}\n--- клики ---\n{clickLog}");
                }
            }
        }
    }
}
