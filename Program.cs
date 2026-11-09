using System.Text.Json;

namespace BattleCity;

/// <summary>Направление движения: вверх, вниз, влево, вправо.</summary>
public enum Direction
{
    /// <summary>Вверх (Y уменьшается).</summary>
    Up,
    /// <summary>Вниз (Y увеличивается).</summary>
    Down,
    /// <summary>Влево (X уменьшается).</summary>
    Left,
    /// <summary>Вправо (X увеличивается).</summary>
    Right
}

/// <summary>Объект с координатами на сетке 10x10.</summary>
public interface IGridEntity
{
    /// <summary>Координата X (по горизонтали, 0..9).</summary>
    int X { get; }
    /// <summary>Координата Y (по вертикали, 0..9).</summary>
    int Y { get; }
}

/// <summary>Объект, который может получать урон (стена, база, танк).</summary>
public interface IDamageable
{
    /// <summary>Нанести объекту урон.</summary>
    void TakeDamage(int amount = 1);
}

/// <summary>Абстракция сохранения и загрузки игры (принцип инверсии зависимостей DIP).</summary>
public interface IStateStore
{
    /// <summary>Сохранить состояние игры в файл.</summary>
    void Save(GameEngine game, string path);

    /// <summary>Загрузить состояние игры из файла.</summary>
    GameEngine Load(string path);
}

/// <summary>Стена: кирпичная разрушается с 1 попадания, бетонная неразрушима.</summary>
public sealed class Wall : IGridEntity, IDamageable
{
    /// <summary>Создать стену.</summary>
    public Wall(int x, int y, bool isConcrete = false, int hp = 1)
    {
        X = x;
        Y = y;
        IsConcrete = isConcrete;
        Hp = hp;
    }

    /// <inheritdoc/>
    public int X { get; set; }
    /// <inheritdoc/>
    public int Y { get; set; }
    /// <summary>Является ли стена неразрушимой бетонной.</summary>
    public bool IsConcrete { get; set; }
    /// <summary>Текущая прочность стены.</summary>
    public int Hp { get; set; }

    /// <inheritdoc/>
    public void TakeDamage(int amount = 1)
    {
        // Бетонная стена не получает урона
        if (!IsConcrete) Hp = Math.Max(0, Hp - amount);
    }
}

/// <summary>Штаб (база) игрока. Попадание по базе завершает игру.</summary>
public sealed class Base : IGridEntity, IDamageable
{
    /// <summary>Создать штаб.</summary>
    public Base(int x, int y, int hp = 1)
    {
        X = x;
        Y = y;
        Hp = hp;
    }

    /// <inheritdoc/>
    public int X { get; set; }
    /// <inheritdoc/>
    public int Y { get; set; }
    /// <summary>Прочность штаба.</summary>
    public int Hp { get; set; }

    /// <inheritdoc/>
    public void TakeDamage(int amount = 1) => Hp = Math.Max(0, Hp - amount);
}

/// <summary>Танк (игрок или противник).</summary>
public sealed class Tank : IGridEntity, IDamageable
{
    /// <summary>Создать танк.</summary>
    public Tank(int x, int y, Direction direction, string team, int hp = 2)
    {
        X = x;
        Y = y;
        Direction = direction;
        Team = team;
        Hp = hp;
    }

    /// <inheritdoc/>
    public int X { get; set; }
    /// <inheritdoc/>
    public int Y { get; set; }
    /// <summary>Куда направлено дуло танка.</summary>
    public Direction Direction { get; set; }
    /// <summary>Команда ("player" или "enemy").</summary>
    public string Team { get; set; }
    /// <summary>Очки здоровья (жизни).</summary>
    public int Hp { get; set; }

    /// <inheritdoc/>
    public void TakeDamage(int amount = 1) => Hp = Math.Max(0, Hp - amount);

    /// <summary>Повернуться и сделать шаг, если клетка свободна.</summary>
    public bool Move(Direction direction, GameEngine game)
    {
        Direction = direction;
        var (dx, dy) = Vector(direction);
        var nx = X + dx;
        var ny = Y + dy;

        if (game.IsBlocked(nx, ny, this)) return false;

        X = nx;
        Y = ny;
        return true;
    }

    /// <summary>Выпустить снаряд из ствола танка.</summary>
    public Projectile Shoot()
    {
        var (dx, dy) = Vector(Direction);
        return new Projectile(X + dx, Y + dy, Direction, Team);
    }

    /// <summary>Смещение (dx, dy) по направлению.</summary>
    public static (int X, int Y) Vector(Direction direction) => direction switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        _ => (1, 0)
    };
}

/// <summary>Снаряд, летящий по прямой линии.</summary>
public sealed class Projectile : IGridEntity
{
    /// <summary>Создать снаряд.</summary>
    public Projectile(int x, int y, Direction direction, string team)
    {
        X = x;
        Y = y;
        Direction = direction;
        Team = team;
    }

    /// <inheritdoc/>
    public int X { get; set; }
    /// <inheritdoc/>
    public int Y { get; set; }
    /// <summary>Направление полёта.</summary>
    public Direction Direction { get; set; }
    /// <summary>Кто выстрелил ("player" или "enemy").</summary>
    public string Team { get; set; }
}

/// <summary>Движок игры: правила, столкновения, такты симуляции и ИИ врагов.</summary>
public sealed class GameEngine
{
    /// <summary>Размер квадратной карты (10х10).</summary>
    public const int MapSize = 10;
    private readonly Random _random = new();

    /// <summary>Инициализация стандартной расстановки уровня.</summary>
    public GameEngine()
    {
        Base = new Base(4, 9);
        Walls =
        [
            // Кирпичный периметр вокруг штаба
            new Wall(3, 9), new Wall(3, 8), new Wall(4, 8), new Wall(5, 8), new Wall(5, 9),
            // Укрытия на карте
            new Wall(1, 4), new Wall(2, 4), new Wall(7, 4), new Wall(8, 4),
            new Wall(3, 2), new Wall(6, 2),
            // Неразрушимые бетонные колонны в центре
            new Wall(4, 5, isConcrete: true, hp: 999),
            new Wall(5, 5, isConcrete: true, hp: 999)
        ];
        Tanks =
        [
            new Tank(1, 8, Direction.Up, "player", hp: 3),
            new Tank(1, 1, Direction.Down, "enemy", hp: 1),
            new Tank(8, 1, Direction.Down, "enemy", hp: 1)
        ];
        Projectiles = [];
        Score = 0;
        GameOver = false;
        Victory = false;
        LastMessage = "Игра началась! Защитите штаб!";
    }

    /// <summary>Стены на карте.</summary>
    public List<Wall> Walls { get; set; } = [];
    /// <summary>Штаб игрока.</summary>
    public Base Base { get; set; } = new(4, 9);
    /// <summary>Танки на карте.</summary>
    public List<Tank> Tanks { get; set; } = [];
    /// <summary>Летящие снаряды.</summary>
    public List<Projectile> Projectiles { get; set; } = [];
    /// <summary>Набранные очки.</summary>
    public int Score { get; set; }
    /// <summary>Флаг завершения игры.</summary>
    public bool GameOver { get; set; }
    /// <summary>Флаг победы игрока.</summary>
    public bool Victory { get; set; }
    /// <summary>Информационное сообщение последнего события.</summary>
    public string LastMessage { get; set; } = string.Empty;

    /// <summary>Танк игрока (null, если уничтожен).</summary>
    public Tank? PlayerTank => Tanks.FirstOrDefault(t => t.Team == "player" && t.Hp > 0);
    /// <summary>Количество живых противников.</summary>
    public int AliveEnemiesCount => Tanks.Count(t => t.Team == "enemy" && t.Hp > 0);

    /// <summary>Проверка, занята ли клетка препятствием или танком.</summary>
    public bool IsBlocked(int x, int y, Tank? except = null)
    {
        if (x < 0 || y < 0 || x >= MapSize || y >= MapSize) return true;
        if (Walls.Any(w => w.X == x && w.Y == y)) return true;
        if (Base.Hp > 0 && Base.X == x && Base.Y == y) return true;
        if (Tanks.Any(t => t != except && t.Hp > 0 && t.X == x && t.Y == y)) return true;
        return false;
    }

    /// <summary>Выстрел орудия танка.</summary>
    public bool Fire(Tank tank)
    {
        if (GameOver || tank.Hp <= 0) return false;
        if (Projectiles.Count(p => p.Team == tank.Team) >= 2) return false;

        var shot = tank.Shoot();
        if (shot.X < 0 || shot.Y < 0 || shot.X >= MapSize || shot.Y >= MapSize) return false;

        Projectiles.Add(shot);
        if (tank.Team == "player") LastMessage = "Игрок выстрелил!";
        return true;
    }

    /// <summary>Один такт физики мира: перемещение снарядов и проверка попаданий.</summary>
    public void Step()
    {
        if (GameOver) return;

        var activeShots = new List<Projectile>();
        foreach (var shot in Projectiles)
        {
            var (dx, dy) = Tank.Vector(shot.Direction);
            shot.X += dx;
            shot.Y += dy;

            // Вылет за пределы поля
            if (shot.X < 0 || shot.Y < 0 || shot.X >= MapSize || shot.Y >= MapSize) continue;

            // Столкновение со стеной
            var wall = Walls.FirstOrDefault(w => w.X == shot.X && w.Y == shot.Y);
            if (wall is not null)
            {
                wall.TakeDamage(1);
                if (wall.Hp <= 0) Walls.Remove(wall);
                LastMessage = wall.IsConcrete ? "Снаряд отрикошетил от бетона." : "Кирпичная стена разрушена!";
                continue;
            }

            // Попадание в штаб
            if (Base.Hp > 0 && Base.X == shot.X && Base.Y == shot.Y)
            {
                Base.TakeDamage(1);
                GameOver = true;
                Victory = false;
                LastMessage = "ШТАБ УНИЧТОЖЕН! Игра окончена.";
                continue;
            }

            // Попадание во вражеский или союзный танк
            var target = Tanks.FirstOrDefault(t => t.Team != shot.Team && t.Hp > 0 && t.X == shot.X && t.Y == shot.Y);
            if (target is not null)
            {
                target.TakeDamage(1);
                if (target.Hp <= 0)
                {
                    if (target.Team == "enemy")
                    {
                        Score += 100;
                        LastMessage = $"Вражеский танк уничтожен! +100 очков (Счёт: {Score})";
                        if (AliveEnemiesCount == 0)
                        {
                            GameOver = true;
                            Victory = true;
                            LastMessage = "ПОБЕДА! Все враги уничтожены!";
                        }
                    }
                    else
                    {
                        GameOver = true;
                        Victory = false;
                        LastMessage = "Ваш танк уничтожен! Поражение.";
                    }
                }
                continue;
            }

            activeShots.Add(shot);
        }

        Projectiles = activeShots;
    }

    /// <summary>Действия искусственного интеллекта врагов (шаг и выстрел).</summary>
    public void UpdateEnemies()
    {
        if (GameOver) return;

        var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        foreach (var enemy in Tanks.Where(t => t.Team == "enemy" && t.Hp > 0))
        {
            // Вероятность выстрела 25%
            if (_random.Next(100) < 25) Fire(enemy);

            // 60% выбор пути в сторону штаба/игрока, 40% случайный поворот
            var preferred = (_random.Next(100) < 60)
                ? (enemy.Y < Base.Y ? Direction.Down : (enemy.X < Base.X ? Direction.Right : Direction.Left))
                : directions[_random.Next(directions.Length)];

            if (!enemy.Move(preferred, this))
            {
                enemy.Move(directions[_random.Next(directions.Length)], this);
            }
        }
    }

    /// <summary>Преобразовать текущее состояние в DTO для сериализации JSON.</summary>
    public GameState ToState() => new()
    {
        Walls = Walls,
        Base = Base,
        Tanks = Tanks,
        Projectiles = Projectiles,
        Score = Score,
        GameOver = GameOver,
        Victory = Victory,
        LastMessage = LastMessage
    };

    /// <summary>Восстановить движок из десериализованного объекта состояния.</summary>
    public static GameEngine FromState(GameState state) => new()
    {
        Walls = state.Walls ?? [],
        Base = state.Base ?? new Base(4, 9),
        Tanks = state.Tanks ?? [],
        Projectiles = state.Projectiles ?? [],
        Score = state.Score,
        GameOver = state.GameOver,
        Victory = state.Victory,
        LastMessage = state.LastMessage ?? "Игра загружена."
    };
}

/// <summary>DTO (Data Transfer Object) состояния игры для сериализации.</summary>
public sealed class GameState
{
    /// <summary>Стены.</summary>
    public List<Wall> Walls { get; set; } = [];
    /// <summary>Штаб.</summary>
    public Base Base { get; set; } = new(4, 9);
    /// <summary>Танки.</summary>
    public List<Tank> Tanks { get; set; } = [];
    /// <summary>Снаряды.</summary>
    public List<Projectile> Projectiles { get; set; } = [];
    /// <summary>Очки.</summary>
    public int Score { get; set; }
    /// <summary>Флаг конца игры.</summary>
    public bool GameOver { get; set; }
    /// <summary>Флаг победы.</summary>
    public bool Victory { get; set; }
    /// <summary>Сообщение.</summary>
    public string LastMessage { get; set; } = string.Empty;
}

/// <summary>Хранилище состояния в JSON-файле.</summary>
public sealed class JsonStateStore : IStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <inheritdoc/>
    public void Save(GameEngine game, string path)
    {
        var json = JsonSerializer.Serialize(game.ToState(), JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <inheritdoc/>
    public GameEngine Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Файл сохранения не найден", path);
        var state = JsonSerializer.Deserialize<GameState>(File.ReadAllText(path));
        return GameEngine.FromState(state ?? throw new InvalidDataException("Файл сохранения повреждён."));
    }
}

/// <summary>Интерфейс пользователя: Game Loop, клавиатурный ввод и консольная графика.</summary>
public static class Program
{
    private const string SaveFile = "save.json";

    /// <summary>Точка входа.</summary>
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        try { Console.Title = "Battle City — Console Arcade"; } catch { }
        try { Console.CursorVisible = false; } catch { }

        IStateStore store = new JsonStateStore();
        GameEngine game;

        // Если есть сохранение — предлагаем загрузить
        if (File.Exists(SaveFile))
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("       BATTLE CITY — C# CONSOLE         ");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine("Найдено сохранение. [1] Продолжить  [2] Новая игра");
            Console.Write("Выбор: ");
            var choice = !Console.IsInputRedirected ? Console.ReadKey(true).KeyChar : '2';
            game = (choice == '1') ? store.Load(SaveFile) : new GameEngine();
        }
        else
        {
            game = new GameEngine();
        }

        RunGameLoop(game, store);
        try { Console.CursorVisible = true; } catch { }
    }

    /// <summary>Основной игровой цикл аркады (Game Loop).</summary>
    public static void RunGameLoop(GameEngine game, IStateStore store)
    {
        Console.Clear();
        var enemyTimer = 0;

        while (true)
        {
            // 1. Неблокирующий ввод с клавиатуры
            var player = game.PlayerTank;
            while (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.W or ConsoleKey.UpArrow when player is not null:
                        player.Move(Direction.Up, game);
                        break;
                    case ConsoleKey.S or ConsoleKey.DownArrow when player is not null:
                        player.Move(Direction.Down, game);
                        break;
                    case ConsoleKey.A or ConsoleKey.LeftArrow when player is not null:
                        player.Move(Direction.Left, game);
                        break;
                    case ConsoleKey.D or ConsoleKey.RightArrow when player is not null:
                        player.Move(Direction.Right, game);
                        break;
                    case ConsoleKey.Spacebar or ConsoleKey.F when player is not null:
                        game.Fire(player);
                        break;
                    case ConsoleKey.F5:
                        store.Save(game, SaveFile);
                        game.LastMessage = "Игра сохранена в " + SaveFile;
                        break;
                    case ConsoleKey.R:
                        game = new GameEngine();
                        game.LastMessage = "Новая игра начата!";
                        break;
                    case ConsoleKey.Q or ConsoleKey.Escape:
                        store.Save(game, SaveFile);
                        Console.Clear();
                        Console.WriteLine($"Игра сохранена в {SaveFile}. До свидания!");
                        return;
                }
            }

            // 2. Движение снарядов
            game.Step();

            // 3. Шаг врагов каждые 3 такта
            if (++enemyTimer >= 3)
            {
                enemyTimer = 0;
                game.UpdateEnemies();
            }

            // 4. Отрисовка кадра
            Render(game);

            // 5. Обработка окончания игры
            if (game.GameOver)
            {
                Console.ForegroundColor = game.Victory ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine(game.Victory ? "\n>>> ПОБЕДА! ВСЕ ВРАГИ УНИЧТОЖЕНЫ! <<<" : "\n>>> GAME OVER! ВЫ ПРОИГРАЛИ! <<<");
                Console.ResetColor();
                Console.WriteLine("Нажмите [R] — перезапуск, [Q] — выход.");

                while (true)
                {
                    var exitKey = Console.ReadKey(true).Key;
                    if (exitKey == ConsoleKey.R) { game = new GameEngine(); Console.Clear(); break; }
                    if (exitKey is ConsoleKey.Q or ConsoleKey.Escape) return;
                }
            }

            // 6. Задержка между кадрами (120 мс ~ 8 кадров/сек)
            Thread.Sleep(120);
        }
    }

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

        // Сетка 10x10
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
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("│");
        }
        Console.WriteLine("  └────────────────────────────────────────┘");
        Console.ResetColor();

        // Подсказки управления и статус
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  Легенда: [▲] Игрок  <▲> Враг  ▓ Кирпич  █ Бетон  ⚑ Штаб  ● Снаряд");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Управление: [W/A/S/D/Стрелки] - Ход | [Пробел] - Огонь | [F5] - Save");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  Событие: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{game.LastMessage,-40}");
        Console.ResetColor();
    }

    /// <summary>Отрисовка содержимого одной клетки (x, y).</summary>
    private static void DrawCell(int x, int y, GameEngine game)
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
