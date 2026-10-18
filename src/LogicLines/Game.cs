using System;
using System.Collections.Generic;

namespace LogicLines
{
    public delegate void ShowItem(Ball ball, Item item);
    public delegate void ShowScore(int Score);

    public enum Item
    {
        empty,
        ball,
        next,
        jump,
        hint,
        path
    }

    public struct Ball
    {
        public int x;
        public int y;
        public int color;
    }

    public class Game
    {
        public Game(int map_size, ShowItem Show, ShowScore ShowScore)
        {
            this.Map_size = map_size;
            this.Show = Show;
            this.ShowScore = ShowScore;
            map = new int[Map_size, Map_size];
            fmap = new int[Map_size, Map_size];
            status = Status.init;
            path = new List<Ball>();
            strip = new List<Ball>();
        }

        public void NewGame()
        {
            Score = 0;
            status = Status.init;
        }

        // поле заполнено - конец игры (для UI)
        public bool IsGameOver
        {
            get { return status == Status.stop; }
        }

        // логика занята (показ пути, перемещение, появление новых шариков,
        // сжигание линии): ClickBox в этих состояниях игнорируется,
        // поэтому UI должен поставить клик в очередь и повторить позже
        public bool IsBusy
        {
            get { return status != Status.wait && status != Status.ball_mark; }
        }

        // состояние можно сохранить только в ожидании хода (не занято,
        // не конец игры): в эти моменты map содержит 0 (пусто), -1
        // (резервация под подсказку следующего шарика) или 1..6 (шарик)
        public bool CanSave
        {
            get { return status == Status.wait || status == Status.ball_mark; }
        }

        // доступ к состоянию для сохранения (UI/сервис копирует данные)
        public int[,] FieldMap => map;
        public Ball[] NextBalls => balls;
        public int CurrentScore => Score;

        // восстановление сохранённой партии: поле (в т.ч. резервации -1
        // под подсказки), следующие шарики и счёт; free_fields
        // перестраивается по полю, анимационное состояние сбрасывается
        public void Restore(int[,] savedMap, Ball[] savedBalls, int score)
        {
            free_fields = new List<FieldAddress>(Map_size * Map_size);

            for (int x = 0; x < Map_size; x++)
                for (int y = 0; y < Map_size; y++)
                {
                    map[x, y] = Math.Clamp(savedMap[x, y], -1, max_colors);

                    Ball b;
                    b.x = x;
                    b.y = y;
                    b.color = map[x, y] > 0 ? map[x, y] : 0;
                    Show(b, map[x, y] > 0 ? Item.ball : Item.empty);

                    if (map[x, y] == 0)
                    {
                        FieldAddress field;
                        field.x = x;
                        field.y = y;
                        free_fields.Add(field);
                    }
                }

            for (int i = 0; i < 3; i++)
            {
                balls[i] = savedBalls[i];
                // защита от повреждённого сейва: координаты вне поля
                // или недопустимый цвет - шарик отбрасываем (x = -1),
                // иначе ShowNextBall упадёт с IndexOutOfRangeException
                if (balls[i].x < -1 || balls[i].x >= Map_size ||
                    balls[i].y < 0 || balls[i].y >= Map_size ||
                    balls[i].color < 1 || balls[i].color > max_colors)
                    balls[i].x = -1;
                if (balls[i].x >= 0)
                    Show(balls[i], Item.next);
            }

            Score = Math.Max(0, score);
            ShowScore(Score);

            path.Clear();
            strip.Clear();
            status = Status.wait;
        }

        public void ClickBox(int x, int y)
        {
            if (status == Status.wait || status == Status.ball_mark)
            {
                if (map[x, y] > 0)
                {

                    if (status == Status.ball_mark)
                        Show(marked_ball, Item.ball);

                    marked_ball.x = x;
                    marked_ball.y = y;
                    marked_ball.color = map[x, y];
                    status = Status.ball_mark;
                    return;
                }
            }

            if (status == Status.ball_mark)
            {
                if (map[x, y] <= 0)
                {
                    destin_ball.x = x;
                    destin_ball.y = y;
                    destin_ball.color = marked_ball.color;
                    if (FindPath())
                        status = Status.path_show;

                    return;
                }
            }
            // в статусе stop (конец игры) клики игнорируются: новая игра
            // начинается только явным NewGame() (UI проверяет IsGameOver)
        }

        public void Step()
        {
            switch (status)
            {

                case Status.init:
                    InitMap();
                    SelectNextBalls();
                    ShowNextBalls();
                    SelectNextBalls();
                    ShowScore(Score);
                    status = Status.wait;
                    break;

                case Status.wait:
                    break;

                case Status.ball_mark:
                    break; // шарик выбран, ждём цель; «подпрыгивание» рисует UI

                case Status.path_show:
                    PathShow();
                    break;

                case Status.ball_move:
                    MoveBall();
                    break;

                case Status.next_balls:
                    ShowNextBalls();
                    SelectNextBalls();
                    break;

                case Status.line_strip:
                    StripLines();
                    break;

                case Status.stop:
                    break;

            }
        }

        private int Map_size;
        internal int[,] map; // 0 - пусто, 1-6 шарик цвета N (internal - для юнит-тестов)
        private int max_colors = 6;
        private ShowItem Show;
        private ShowScore ShowScore;
        internal Status status; // internal - для юнит-тестов
        internal Ball[] balls = new Ball[3]; // internal - для юнит-тестов
        private Random rand = new Random();
        private Ball marked_ball;
        private Ball destin_ball;
        private int[,] fmap;
        private List<Ball> path;
        private int paths;
        private int path_step;
        private List<Ball> strip;
        private int strip_step;
        private int Score = 0;
        internal List<FieldAddress> free_fields = new List<FieldAddress>(); // internal - для юнит-тестов
        public struct FieldAddress
        {
            public int x;
            public int y;
        }

        internal enum Status // internal - для юнит-тестов
        {
            init, // самое начало
            wait, // ожидание выбора первого шарика
            ball_mark, // шарик выбран - отмечен, ожидаем выбора назначения
            path_show, // показать путь следования шарика
            ball_move, // процесс движения шарика
            next_balls, // вывод подсказки по следующим шарикам
            line_strip, // "взрыв" собранных линий
            stop // поле заполнено, конец игры
        }

        private void InitMap()
        {
            Ball none;
            none.color = 0;

            FieldAddress fieldAddress;

            free_fields = new List<FieldAddress>(Map_size * Map_size);

            for (int x = 0; x < Map_size; x++)
                for (int y = 0; y < Map_size; y++)
                {
                    map[x, y] = 0;
                    none.x = x;
                    none.y = y;

                    Show(none, Item.empty);

                    fieldAddress.x = x;
                    fieldAddress.y = y;

                    free_fields.Add(fieldAddress);
                }

            Score = 0;
        }

        private void SelectNextBalls()
        {

            balls[0] = SelectNextBall();
            balls[1] = SelectNextBall();
            balls[2] = SelectNextBall();

        }

        private Ball SelectNextBall()
        {
            return SelectNextBall(rand.Next(1, max_colors + 1));

        }

        private Ball SelectNextBall(int color)
        {
            Ball next;
            next.x = -1;
            next.y = -1;
            next.color = 0;

            // выкидываем рассинхронизированные записи (клетки, где
            // map != 0) - защита от бесконечного цикла, если список
            // когда-нибудь разойдётся с полем
            for (int i = free_fields.Count - 1; i >= 0; i--)
                if (map[free_fields[i].x, free_fields[i].y] != 0)
                    free_fields.RemoveAt(i);

            if (free_fields.Count == 0)
                return next;

            // верхняя граница Next() исключительная - не вычитаем 1,
            // иначе последнее свободное поле никогда не выбиралось
            int indexField = rand.Next(free_fields.Count);
            next.x = free_fields[indexField].x;
            next.y = free_fields[indexField].y;

            map[next.x, next.y] = -1; // занимаем поле

            free_fields.RemoveAt(indexField);

            next.color = color;

            Show(next, Item.next);

            return next;
        }

        private void AddFreeField(int x, int y)
        {
            FieldAddress field;
            field.x = x;
            field.y = y;

            if (free_fields.Contains(field))
                return;

            free_fields.Add(field);
        }

        private void ShowNextBalls()
        {
            ShowNextBall(0);
            ShowNextBall(1);
            ShowNextBall(2);

            if (FindStripLines())
                status = Status.line_strip;
            else
            {
                if (IsMapFull())
                    status = Status.stop;
                else
                    status = Status.wait;
            }
        }

        private void ShowNextBall(int i)
        {
            Ball next = balls[i];
            if (next.x < 0) return;
            if (map[next.x, next.y] > 0)
            {
                next = SelectNextBall(next.color);
                if (next.x < 0) return;
                balls[i] = next;
            }

            // клетка могла остаться в free_fields: это бывшая позиция
            // подсказки, перезаписанная ходом и потом сгоревшая в линии
            // (map снова 0). Без удаления список свободных полей
            // расходится с полем и SelectNextBall зацикливается
            FieldAddress field;
            field.x = next.x;
            field.y = next.y;
            free_fields.Remove(field);

            map[next.x, next.y] = next.color;
            Show(next, Item.ball);

        }

        private void HintNextBalls()
        {
            HintNextBall(balls[0]);
            HintNextBall(balls[1]);
            HintNextBall(balls[2]);
        }

        private void HintNextBall(Ball next)
        {
            if (next.x < 0) return;
            Show(next, Item.next);
        }

        private void MoveBall()
        {
            if (status != Status.ball_move) return;

            if (map[marked_ball.x, marked_ball.y] > 0 &&
                map[destin_ball.x, destin_ball.y] <= 0)
            {
                map[marked_ball.x, marked_ball.y] = 0;
                map[destin_ball.x, destin_ball.y] = marked_ball.color;

                AddFreeField(marked_ball.x, marked_ball.y);

                // удаляем поле назначения из списка свободных
                FieldAddress field;
                field.x = destin_ball.x;
                field.y = destin_ball.y;

                free_fields.Remove(field);

                Show(marked_ball, Item.empty);
                Show(destin_ball, Item.ball);
                if (FindStripLines())
                    status = Status.line_strip;
                else
                    status = Status.next_balls;

            }

        }

        private bool FindPath()
        {
            if (!(map[marked_ball.x, marked_ball.y] > 0 &&
                map[destin_ball.x, destin_ball.y] <= 0))
                return false;
            for (int x = 0; x < Map_size; x++)
                for (int y = 0; y < Map_size; y++)
                    fmap[x, y] = 0;

            fmap[marked_ball.x, marked_ball.y] = 1; // Отмечаем начало пути

            bool added;
            bool found = false;
            int nr = 1;

            do
            {
                added = false;
                for (int x = 0; x < Map_size; x++)
                    for (int y = 0; y < Map_size; y++)
                        if (fmap[x, y] == nr)
                        {
                            MarkPath(x + 1, y, nr + 1);
                            MarkPath(x - 1, y, nr + 1);
                            MarkPath(x, y + 1, nr + 1);
                            MarkPath(x, y - 1, nr + 1);
                            added = true;
                        }
                if (fmap[destin_ball.x, destin_ball.y] > 0)
                {
                    found = true;
                    break;
                }

                nr++;

            } while (added);

            if (!found) return false;

            paths = nr; // число шагов от старта до цели

            path.Clear();

            // собираем путь от цели к началу, затем разворачиваем:
            // идём от клетки назначения по соседям с убывающим номером
            // волны (волна клетки назначения = nr + 1). Сама клетка
            // назначения в path НЕ входит: path[0] - стартовое поле,
            // path[paths-1] - клетка перед назначением, финальный шаг
            // в destin_ball PathShow делает отдельно
            int px = destin_ball.x;
            int py = destin_ball.y;
            int wave = nr + 1;
            while (wave > 1)
            {
                if (IsPath(px + 1, py, wave - 1)) px++;
                else if (IsPath(px - 1, py, wave - 1)) px--;
                else if (IsPath(px, py + 1, wave - 1)) py++;
                else if (IsPath(px, py - 1, wave - 1)) py--;
                else break; // не должно случаться: волна непрерывна

                wave--;
                Ball step;
                step.x = px;
                step.y = py;
                step.color = 0;
                path.Add(step);
            }

            path.Reverse();

            path_step = 0;
            return true;
        }

        private void MarkPath(int x, int y, int k)
        {
            if (x < 0 || x >= Map_size) return;
            if (y < 0 || y >= Map_size) return;
            if (map[x, y] > 0) return;
            if (fmap[x, y] > 0) return; // проверяем, если мы тут уже были то выходим

            fmap[x, y] = k;
        }

        private bool IsPath(int x, int y, int k)
        {
            if (x < 0 || x >= Map_size) return false;
            if (y < 0 || y >= Map_size) return false;
            return (fmap[x, y] == k);
        }

        private void PathShow()
        {
            if (path_step == 0)
            {
                // следы на всех клетках пути кроме стартовой; клетка
                // назначения в path не входит (см. FindPath) - последний
                // след ставим на destin_ball
                for (int nr = 1; nr <= paths; nr++)
                {
                    Ball cell;
                    if (nr < paths)
                        cell = path[nr];
                    else
                    {
                        cell.x = destin_ball.x;
                        cell.y = destin_ball.y;
                        cell.color = 0;
                    }

                    Show(cell, Item.path);
                }

                path_step++;
                return;

            }

            Ball moving_ball;

            moving_ball = path[path_step - 1];
            Show(moving_ball, Item.empty);

            if (path_step < paths)
            {
                moving_ball = path[path_step];
                moving_ball.color = marked_ball.color;
            }
            else
            {
                // последний шаг - в клетку назначения (в path не входит)
                moving_ball.x = destin_ball.x;
                moving_ball.y = destin_ball.y;
                moving_ball.color = marked_ball.color;
            }

            Show(moving_ball, Item.ball);

            path_step++;

            if (path_step > paths)
            {
                HintNextBalls();
                status = Status.ball_move;
            }
        }

        private bool FindStripLines()
        {
            strip.Clear();
            for (int x = 0; x < Map_size; x++)
                for (int y = 0; y < Map_size; y++)
                {
                    CheckLine(x, y, 1, 0);
                    CheckLine(x, y, 1, 1);
                    CheckLine(x, y, 0, 1);
                    CheckLine(x, y, -1, 1);
                }
            if (strip.Count == 0)
                return false;
            strip_step = 4;
            return true;
        }

        private void CheckLine(int x, int y, int sx, int sy)
        {
            int p = 4; // сколько шагов проверяем
            if (x < 0 || x >= Map_size) return;
            if (y < 0 || y >= Map_size) return;

            if (x + p * sx < 0 || x + p * sx >= Map_size) return;
            if (y + p * sy < 0 || y + p * sy >= Map_size) return;

            int color = map[x, y];

            // пустые клетки (0) и резервации под подсказки (-1)
            // линий не образуют
            if (color <= 0) return;

            for (int k = 1; k <= p; k++)
                if (map[x + k * sx, y + k * sy] != color)
                    return;

            for (int k = 0; k <= p; k++)
            {
                Ball ball;
                ball.x = x + k * sx;
                ball.y = y + k * sy;
                ball.color = color;
                strip.Add(ball);
            }
        }

        private void StripLines()
        {
           if (strip_step <= 0)
           {
                int freeFieldsCount = free_fields.Count;

                for (int i = 0; i < strip.Count; i++)
                {
                    map[strip[i].x, strip[i].y] = 0;
                    AddFreeField(strip[i].x, strip[i].y);
                }

                HintNextBalls();

                status = Status.wait;

                Score += free_fields.Count - freeFieldsCount;

                ShowScore(Score);
                return;
           }

           strip_step--;

            for (int i = 0; i < strip.Count; i++)
            {
                switch (strip_step)
                {
                    case 3: Show(strip[i], Item.jump); break;
                    case 2: Show(strip[i], Item.ball); break;
                    case 1: Show(strip[i], Item.next); break;
                    case 0: Show(strip[i], Item.empty); break;
                }
            }
        }

        private bool IsMapFull()
        {
            for (int x = 0; x < Map_size; x++)
                for (int y = 0; y < Map_size; y++)
                    if (map[x, y] <= 0) return false;
            return true;
        }

    }
}
