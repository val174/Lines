using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lines.Shared.Rendering
{
    // Раскладка игрового поля: размеры считаются от текущего Viewport
    // при каждом кадре - никаких фиксированных 800x571, как в старой версии.
    // Верхняя панель - счёт и подсказки, нижняя (StatusBarRect, всегда,
    // в обеих ориентациях) - панель кнопок-иконок; поле размещается
    // между ними.
    public struct BoardLayout
    {
        public int MapSize;
        public int CellSize;      // размер клетки, px
        public int BoardSize;     // размер поля, px (CellSize * MapSize)
        public int BoardOriginX;  // левый верхний угол поля
        public int BoardOriginY;
        public Rectangle PanelRect;     // верхняя панель со счётом
        public Rectangle StatusBarRect; // нижняя панель с кнопками
        public bool IsPortrait;

        public static BoardLayout Compute(Viewport viewport, int mapSize)
        {
            bool portrait = viewport.Height > viewport.Width;
            int panelHeight = Math.Max(48, viewport.Height / 10);
            int statusHeight = panelHeight; // нижняя панель - всегда
            // зазор между панелью и полем: рамка поля рисуется на 4px выше
            // начала поля, поэтому без явного зазора она перекрывает панель
            int gap = Math.Max(8, panelHeight / 4);
            int board = Math.Min(viewport.Width,
                viewport.Height - panelHeight - statusHeight - gap * 2);
            int cell = Math.Max(1, board / mapSize);
            board = cell * mapSize;

            BoardLayout layout;
            layout.MapSize = mapSize;
            layout.CellSize = cell;
            layout.BoardSize = board;
            layout.BoardOriginX = (viewport.Width - board) / 2;
            layout.BoardOriginY = panelHeight + gap +
                (viewport.Height - panelHeight - statusHeight - gap * 2 - board) / 2;
            layout.PanelRect = new Rectangle(0, 0, viewport.Width, panelHeight);
            layout.StatusBarRect = new Rectangle(0, viewport.Height - statusHeight,
                viewport.Width, statusHeight);
            layout.IsPortrait = portrait;
            return layout;
        }

        public Rectangle CellRect(int x, int y)
        {
            return new Rectangle(BoardOriginX + x * CellSize, BoardOriginY + y * CellSize,
                CellSize, CellSize);
        }

        public Vector2 CellCenter(int x, int y)
        {
            return new Vector2(BoardOriginX + (x + 0.5f) * CellSize,
                BoardOriginY + (y + 0.5f) * CellSize);
        }

        // экранные координаты -> координаты клетки (с учётом масштаба и смещения поля)
        public bool TryGetCell(Point screen, out int x, out int y)
        {
            x = (screen.X - BoardOriginX) / CellSize;
            y = (screen.Y - BoardOriginY) / CellSize;
            return x >= 0 && x < MapSize && y >= 0 && y < MapSize;
        }
    }
}
