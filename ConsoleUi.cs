namespace BattleCity;

/// <summary>Консольный интерфейс: отрисовка карты, цветовая палитра и статусная панель.</summary>
public static class ConsoleUi
{
    /// <summary>Отрисовка карты и интерфейса в консоли без мерцания.</summary>
    public static void Render(GameEngine game)
    {
        try { Console.SetCursorPosition(0, 0); } catch { }

        // Верхняя панель
        var player = game.PlayerTank;
        var hp = player is not null ? new string('♥', player.Hp) : "DEAD";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║           BATTLE CITY — CONSOLE ARCADE       ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.Write("  Счёт: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{game.Score,-6}");
        Console.ResetColor();
        Console.Write(" Жизни: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{hp,-6}");
        Console.ResetColor();
        Console.Write(" Врагов: ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"{game.AliveEnemiesCount,-2}");
        Console.ResetColor();

        // Верхняя граница сетки
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ┌────────────────────────────────────────┐");

        for (var y = 0; y < GameEngine.MapSize; y++)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  │");

            for (var x = 0; x < GameEngine.MapSize; x++)
            {
                DrawCell(x, y, game);
            }

            // Правая рамка строго цвета DarkGray
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("│");
        }

        // Нижняя граница сетки
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  └────────────────────────────────────────┘");
        Console.ResetColor();

        // Подсказки управления и легенда
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  Легенда: [▲] Игрок  <▲> Враг  ▓ Кирпич  █ Бетон  ⚑ Штаб  ● Снаряд");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Управление: [W/A/S/D/Стрелки] - Ход | [Пробел] - Огонь | [F5] - Save");
        Console.WriteLine("              [R] - Новая игра | [Q/Esc] - Выход с сохранением");
        Console.ResetColor();

        // Информационная строка события (выравнивание 60 символов стирает старые длинные сообщения)
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  Событие: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{game.LastMessage,-60}");
        Console.ResetColor();
    }

    /// <summary>Отрисовка содержимого одной клетки (x, y).</summary>
    public static void DrawCell(int x, int y, GameEngine game)
    {
        var shot = game.Projectiles.FirstOrDefault(p => p.X == x && p.Y == y);
        if (shot is not null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" ●  ");
            return;
        }

        var tank = game.Tanks.FirstOrDefault(t => t.Hp > 0 && t.X == x && t.Y == y);
        if (tank is not null)
        {
            var symbol = tank.Direction switch
            {
                Direction.Up => "▲",
                Direction.Down => "▼",
                Direction.Left => "◄",
                _ => "►"
            };
            Console.ForegroundColor = tank.Team == "player" ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(tank.Team == "player" ? $"[{symbol}] " : $"<{symbol}> ");
            return;
        }

        if (game.Base.X == x && game.Base.Y == y)
        {
            Console.ForegroundColor = game.Base.Hp > 0 ? ConsoleColor.Cyan : ConsoleColor.DarkRed;
            Console.Write(game.Base.Hp > 0 ? " ⚑  " : " ☠  ");
            return;
        }

        var wall = game.Walls.FirstOrDefault(w => w.X == x && w.Y == y);
        if (wall is not null)
        {
            Console.ForegroundColor = wall.IsConcrete ? ConsoleColor.DarkGray : ConsoleColor.DarkYellow;
            Console.Write(wall.IsConcrete ? "████" : "▓▓▓▓");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write(" ·  ");
    }
}
