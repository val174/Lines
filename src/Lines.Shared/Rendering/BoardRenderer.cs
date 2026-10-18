using System;
using System.Collections.Generic;
using LogicLines;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Rendering
{
    // событие логики за один шаг (GameScreen накапливает их пакетом
    // и передаёт в BoardRenderer.ApplyEvents)
    internal struct ShowEvent
    {
        public Ball Ball;
        public Item Item;
    }

    // Отрисовка игрового поля и его визуальное состояние: клетки,
    // шарики, следы, анимации появления/перемещения/сжигания.
    // Логика (LogicLines.Game) шлёт пошаговые события ShowEvent,
    // ApplyEvents превращает их в состояние, Draw рисует кадр.
    // Звуки, счёт и предпросмотр «следующих» - забота GameScreen.
    internal class BoardRenderer
    {
        private const float StepSeconds = 0.09f;   // длительность одного шага анимации
        private const float AppearSeconds = 0.25f; // появление нового шарика
        private const float BurnSeconds = 0.36f;   // сжигание линии (4 фазы логики)

        private readonly int mapSize;
        private readonly BallRenderer balls;
        private readonly Texture2D pixel; // 1x1 белый - для заливок

        // визуальное состояние поля
        private readonly int[,] cellColor; // 0 - пусто, 1-6 - шарик
        private readonly int[,] cellHint;  // 0 - нет, 1-6 - подсказка цвета

        // анимации
        private readonly Dictionary<Point, float> appear = new Dictionary<Point, float>();
        private readonly HashSet<Point> burning = new HashSet<Point>();
        private float burnStart;
        private Point? selected; // выбранный шарик (подпрыгивает)
        private Glide glide;     // перемещение шарика по пути
        private readonly List<Point> trails = new List<Point>(); // следы шарика (Item.path)

        // полный сброс визуала (пакет пустых клеток от логики):
        // GameScreen дополнительно сбрасывает у себя отложенный клик
        // и предпросмотр «следующих» шариков
        private readonly Action onReset;

        private class Glide
        {
            public Point From;
            public Point To;
            public int Color;
            public float Start;
        }

        public BoardRenderer(BallRenderer balls, Texture2D pixel, int mapSize, Action onReset)
        {
            this.balls = balls;
            this.pixel = pixel;
            this.mapSize = mapSize;
            this.onReset = onReset;
            cellColor = new int[mapSize, mapSize];
            cellHint = new int[mapSize, mapSize];
        }

        // выбор шарика по клику: true - в клетке есть шарик (выбран,
        // GameScreen играет звук); клик по пустой клетке при активном
        // пути снимает выбор - шарик поехал по следу
        public bool TrySelect(int x, int y)
        {
            if (cellColor[x, y] > 0)
            {
                selected = new Point(x, y);
                return true;
            }
            if (trails.Count > 0)
                selected = null;
            return false;
        }

        // применение пакета событий логики к визуальному состоянию.
        // Флаги - какие звуки играть; nextColors - предпросмотр
        // «следующих» шариков на панели (заполняется последней тройкой)
        public void ApplyEvents(List<ShowEvent> events, float time,
            out bool playStrip, out bool playMove, out bool playAppear, int[] nextColors)
        {
            playStrip = false;
            playMove = false;
            playAppear = false;
            if (events.Count == 0)
                return;

            // инициализация поля приходит пакетом из mapSize*mapSize «empty» - сброс визуала
            int emptyCount = 0, jumpCount = 0;
            foreach (ShowEvent e in events)
            {
                if (e.Item == Item.empty) emptyCount++;
                if (e.Item == Item.jump) jumpCount++;
            }
            if (emptyCount >= mapSize * mapSize)
            {
                ResetVisuals();
                onReset?.Invoke();
            }

            // сжигание линии начинается с «подпрыгивания» сразу нескольких шариков
            if (jumpCount >= 2)
            {
                playStrip = true;
                burnStart = time;
                foreach (ShowEvent e in events)
                    if (e.Item == Item.jump)
                        burning.Add(new Point(e.Ball.x, e.Ball.y));
            }

            // шаг перемещения: ровно одна клетка опустела,
            // а в соседней появился шарик того же цвета
            Point emptyCell = Point.Zero, ballCell = Point.Zero;
            int emptyMoves = 0, ballMoves = 0, moveColor = 0;
            foreach (ShowEvent e in events)
            {
                Point c = new Point(e.Ball.x, e.Ball.y);
                if (burning.Contains(c))
                    continue;
                if (e.Item == Item.empty) { emptyMoves++; emptyCell = c; }
                if (e.Item == Item.ball) { ballMoves++; ballCell = c; moveColor = e.Ball.color; }
            }
            bool isMoveStep = emptyMoves == 1 && ballMoves == 1 &&
                Math.Abs(emptyCell.X - ballCell.X) + Math.Abs(emptyCell.Y - ballCell.Y) == 1 &&
                moveColor > 0 && cellColor[emptyCell.X, emptyCell.Y] == moveColor;
            if (isMoveStep)
                glide = new Glide { From = emptyCell, To = ballCell, Color = moveColor, Start = time };

            playMove = isMoveStep;

            List<int> nextBatch = new List<int>();
            foreach (ShowEvent e in events)
            {
                int x = e.Ball.x, y = e.Ball.y;
                if (x < 0 || y < 0 || x >= mapSize || y >= mapSize)
                    continue;
                Point cell = new Point(x, y);

                switch (e.Item)
                {
                    case Item.empty:
                        appear.Remove(cell);
                        trails.Remove(cell);
                        burning.Remove(cell);
                        cellColor[x, y] = 0;
                        cellHint[x, y] = 0;
                        break;

                    case Item.ball:
                        if (burning.Contains(cell))
                            break; // фаза сжигания - рисуем свою анимацию
                        if (cellColor[x, y] == e.Ball.color && cellHint[x, y] == 0)
                            break; // уже нарисован (финиш MoveBall дублирует клетку)
                        cellColor[x, y] = e.Ball.color;
                        cellHint[x, y] = 0;
                        if ((!isMoveStep || cell != ballCell) && !appear.ContainsKey(cell))
                        {
                            appear[cell] = time;
                            playAppear = true; // появился новый шарик
                        }
                        // шарик прибыл в конец следа - движение закончено, следы убираем
                        if (trails.Count > 0 && cell == trails[trails.Count - 1])
                            trails.Clear();
                        break;

                    case Item.next:
                        if (burning.Contains(cell))
                            break;
                        if (cellColor[x, y] == 0)
                            cellHint[x, y] = e.Ball.color;
                        nextBatch.Add(e.Ball.color);
                        break;

                    case Item.path:
                        if (!trails.Contains(cell))
                            trails.Add(cell);
                        break;

                    case Item.jump:
                        // одиночное «подпрыгивание» - выбранный шарик;
                        // выбор отслеживаем по кликам и рисуем сами
                        break;
                }
            }

            // next-события за шаг - это «следующие» шарики; на инициализации их две
            // тройки (появились + заготовлены), берём последнюю
            if (nextBatch.Count >= 3)
                for (int i = 0; i < 3; i++)
                    nextColors[i] = nextBatch[nextBatch.Count - 3 + i];
        }

        public void ResetVisuals()
        {
            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                {
                    cellColor[x, y] = 0;
                    cellHint[x, y] = 0;
                }
            appear.Clear();
            burning.Clear();
            trails.Clear();
            selected = null;
            glide = null;
        }

        // восстановленное из сохранения поле рисуем сразу,
        // без анимации «появления»
        public void ClearAppear() => appear.Clear();

        // bouncePhase - фаза [0, PI/3) подпрыгивания выбранного шарика
        // (обёрнутая в GameScreen, чтобы не терять точность float)
        public void Draw(SpriteBatch sb, BoardLayout layout, float time, bool showTrails,
            float bouncePhase)
        {
            // рамка и фон клеток - тёплый «песочный», как в оригинале
            Rectangle boardRect = new Rectangle(layout.BoardOriginX - 4, layout.BoardOriginY - 4,
                layout.BoardSize + 8, layout.BoardSize + 8);
            sb.Draw(pixel, boardRect, UiTheme.BoardFrame);

            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                {
                    Rectangle r = layout.CellRect(x, y);
                    r.Inflate(-1, -1);
                    sb.Draw(pixel, r, UiTheme.CellBack);
                }

            if (showTrails)
                DrawTrails(sb, layout);

            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                {
                    Point cell = new Point(x, y);

                    if (cellColor[x, y] > 0)
                    {
                        if (glide != null && cell == glide.To && time - glide.Start < StepSeconds)
                            continue; // шарик в пути - нарисуем летящим

                        float scale = 1f, alpha = 1f, yOffset = 0f, whiteFlash = 0f;

                        if (appear.TryGetValue(cell, out float appearT0))
                        {
                            float k = (time - appearT0) / AppearSeconds;
                            if (k >= 1f)
                                appear.Remove(cell);
                            else
                            {
                                scale = k;
                                alpha = k;
                            }
                        }

                        if (burning.Contains(cell))
                        {
                            float k = MathHelper.Clamp((time - burnStart) / BurnSeconds, 0f, 1f);
                            scale = 1f - 0.25f * k;
                            alpha = 1f - 0.5f * k;
                            whiteFlash = k;
                        }

                        if (selected.HasValue && selected.Value == cell)
                            yOffset = -Math.Abs(MathF.Sin(bouncePhase * 6f)) * layout.CellSize * 0.12f;

                        Rectangle dest = BallRect(layout, x, y, scale);
                        balls.DrawShadow(sb, dest, alpha * 0.6f);
                        balls.Draw(sb, cellColor[x, y], dest, alpha, yOffset);
                        if (whiteFlash > 0f)
                            balls.DrawTinted(sb, cellColor[x, y], dest, Color.White * (whiteFlash * 0.8f));
                    }
                    else if (cellHint[x, y] > 0)
                    {
                        // подсказка «следующего» шарика - маленький полупрозрачный
                        Rectangle dest = BallRect(layout, x, y, 0.35f);
                        balls.Draw(sb, cellHint[x, y], dest, 0.8f);
                    }
                }

            // летящий по пути шарик
            if (glide != null)
            {
                float k = (time - glide.Start) / StepSeconds;
                if (k >= 1f)
                {
                    glide = null; // дальше шарик нарисуется статично в клетке
                }
                else
                {
                    Vector2 from = layout.CellCenter(glide.From.X, glide.From.Y);
                    Vector2 to = layout.CellCenter(glide.To.X, glide.To.Y);
                    Vector2 pos = Vector2.Lerp(from, to, k);
                    int size = (int)(layout.CellSize * UiTheme.BallScale);
                    Rectangle dest = new Rectangle((int)pos.X - size / 2, (int)pos.Y - size / 2, size, size);
                    balls.DrawShadow(sb, dest, 0.6f);
                    balls.Draw(sb, glide.Color, dest);
                }
            }
        }

        private void DrawTrails(SpriteBatch sb, BoardLayout layout)
        {
            // следы шарика - чередующиеся точки «шагов» вдоль пути
            Color trailColor = new Color(120, 110, 80);
            int i = 0;
            foreach (Point cell in trails)
            {
                Vector2 c = layout.CellCenter(cell.X, cell.Y);
                int d = Math.Max(2, layout.CellSize / 8);
                int shift = (i % 2 == 0 ? -1 : 1) * layout.CellSize / 6;
                sb.Draw(pixel, new Rectangle((int)c.X + shift - d / 2, (int)c.Y - d / 2, d, d), trailColor);
                i++;
            }
        }

        private static Rectangle BallRect(BoardLayout layout, int x, int y, float scale)
        {
            int size = (int)(layout.CellSize * UiTheme.BallScale * scale);
            Vector2 c = layout.CellCenter(x, y);
            return new Rectangle((int)c.X - size / 2, (int)c.Y - size / 2, size, size);
        }
    }
}
