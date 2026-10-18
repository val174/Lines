using LogicLines;

namespace LogicLines.Tests
{
    // Заглушка UI: собирает события ShowItem/ShowScore,
    // чтобы тесты видели, что логика отдаёт наружу
    internal class TestView
    {
        public List<(Ball Ball, Item Item)> Events { get; } = new();

        public int Score { get; private set; }

        public void Show(Ball ball, Item item)
        {
            Events.Add((ball, item));
        }

        public void ShowScore(int score)
        {
            Score = score;
        }
    }
}
