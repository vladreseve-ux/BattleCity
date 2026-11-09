using System.Text.Json;

namespace BattleCity;

/// <summary>
/// Направление движения на сетке.
/// </summary>
public enum Direction
{
    /// <summary>Вверх.</summary>
    Up,
    /// <summary>Вниз.</summary>
    Down,
    /// <summary>Влево.</summary>
    Left,
    /// <summary>Вправо.</summary>
    Right
}

/// <summary>
/// Объект, занимающий координаты на игровой сетке.
/// </summary>
public interface IGridEntity
{
    /// <summary>Горизонтальная координата (0..MapSize-1).</summary>
    int X { get; }
    /// <summary>Вертикальная координата (0..MapSize-1).</summary>
    int Y { get; }
}

/// <summary>
/// Объект, которому может быть нанесён урон.
/// </summary>
public interface IDamageable
{
    /// <summary>Наносит объекту урон.</summary>
    /// <param name="amount">Количество единиц урона.</param>
    void TakeDamage(int amount = 1);
}

/// <summary>
/// Абстракция сохранения и загрузки состояния игры.
/// </summary>
public interface IStateStore
{
    /// <summary>Сохраняет игровой движок в указанный файл.</summary>
    /// <param name="game">Экземпляр игрового движка.</param>
    /// <param name="path">Путь к файлу.</param>
    void Save(GameEngine game, string path);

    /// <summary>Загружает игровой движок из файла.</summary>
    /// <param name="path">Путь к файлу.</param>
    /// <returns>Восстановленный игровой движок.</returns>
    GameEngine Load(string path);
}

/// <summary>
/// Стена карты: кирпичная разрушается при попадании, бетонная является неразрушимой.
/// </summary>
public sealed class Wall : IGridEntity, IDamageable
{
    /// <summary>Создаёт стену с заданными координатами, признаком бетона и здоровьем.</summary>
    /// <param name="x">Координата X.</param>
    /// <param name="y">Координата Y.</param>
    /// <param name="isConcrete">Флаг бетонной неразрушимой стены.</param>
    /// <param name="hp">Запас прочности.</param>
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

    /// <summary>Признак неразрушаемой бетонной стены.</summary>
    public bool IsConcrete { get; set; }

    /// <summary>Текущая прочность стены.</summary>
    public int Hp { get; set; }

    /// <inheritdoc/>
    public void TakeDamage(int amount = 1)
    {
        if (!IsConcrete)
        {
            Hp = Math.Max(0, Hp - amount);
        }
    }
}

/// <summary>
/// База (штаб) игрока. Попадание вражеского снаряда приводит к поражению.
/// </summary>
public sealed class Base : IGridEntity, IDamageable
{
    /// <summary>Создаёт базу в указанных координатах.</summary>
    /// <param name="x">Координата X.</param>
    /// <param name="y">Координата Y.</param>
    /// <param name="hp">Запас прочности базы.</param>
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

    /// <summary>Прочность базы.</summary>
    public int Hp { get; set; }

    /// <inheritdoc/>
    public void TakeDamage(int amount = 1)
    {
        Hp = Math.Max(0, Hp - amount);
    }
}

/// <summary>
/// Танк (игрок или противник) со здоровьем, направлением и возможностью стрельбы.
/// </summary>
public sealed class Tank : IGridEntity, IDamageable
{
    /// <summary>Создаёт танк с начальными параметрами.</summary>
    /// <param name="x">Координата X.</param>
    /// <param name="y">Координата Y.</param>
    /// <param name="direction">Начальное направление.</param>
    /// <param name="team">Команда ("player" или "enemy").</param>
    /// <param name="hp">Запас здоровья.</param>
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

    /// <summary>Текущее направление танка.</summary>
    public Direction Direction { get; set; }

    /// <summary>Команда танка ("player" или "enemy").</summary>
    public string Team { get; set; }

    /// <summary>Текущее здоровье танка.</summary>
    public int Hp { get; set; }

    /// <inheritdoc/>
    public void TakeDamage(int amount = 1)
    {
        Hp = Math.Max(0, Hp - amount);
    }

    /// <summary>
    /// Поворачивает танк в указанном направлении и пробует переместиться на 1 клетку.
    /// </summary>
    /// <param name="direction">Новое направление.</param>
    /// <param name="game">Игровой движок для проверки препятствий.</param>
    /// <returns>True, если перемещение успешно; иначе false.</returns>
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

    /// <summary>
    /// Создаёт снаряд в соседней клетке по направлению ствола танка.
    /// </summary>
    /// <returns>Экземпляр снаряда.</returns>
    public Projectile Shoot()
    {
        var (dx, dy) = Vector(Direction);
        return new Projectile(X + dx, Y + dy, Direction, Team);
    }

    /// <summary>
    /// Возвращает смещение по координатам для заданного направления.
    /// </summary>
    /// <param name="direction">Направление.</param>
    /// <returns>Кортеж смещения (dX, dY).</returns>
    public static (int X, int Y) Vector(Direction direction) => direction switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        _ => (1, 0)
    };
}

/// <summary>
/// Снаряд, летящий по прямой линии до столкновения.
/// </summary>
public sealed class Projectile : IGridEntity
{
    /// <summary>Создаёт снаряд с заданными параметрами.</summary>
    /// <param name="x">Координата X.</param>
    /// <param name="y">Координата Y.</param>
    /// <param name="direction">Направление полёта.</param>
    /// <param name="team">Команда, выпустившая снаряд.</param>
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

    /// <summary>Команда, выпустившая снаряд.</summary>
    public string Team { get; set; }
}

/// <summary>
/// Игровой движок: карта, правила столкновений, перемещение снарядов и логика врагов.
/// </summary>
public sealed class GameEngine
{
    /// <summary>Размер квадратной карты (10 клеток).</summary>
    public const int MapSize = 10;

    private readonly Random _random = new();

    /// <summary>Создаёт игровое поле со стенами, базой и танками по умолчанию.</summary>
    public GameEngine()
    {
        Walls =
        [
            // Защитный периметр вокруг базы (база на 4, 9)
            new Wall(3, 9, false),
            new Wall(3, 8, false),
            new Wall(4, 8, false),
            new Wall(5, 8, false),
            new Wall(5, 9, false),

            // Кирпичные укрытия на карте
            new Wall(1, 4, false),
            new Wall(2, 4, false),
            new Wall(7, 4, false),
            new Wall(8, 4, false),
            new Wall(3, 2, false),
            new Wall(6, 2, false),

            // Бетонные неразрушаемые колонны по центру
            new Wall(4, 5, true, 999),
            new Wall(5, 5, true, 999)
        ];

        Base = new Base(4, 9);
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

    /// <summary>Список стен на карте.</summary>
    public List<Wall> Walls { get; set; } = [];

    /// <summary>База (штаб) игрока.</summary>
    public Base Base { get; set; } = new(4, 9);

    /// <summary>Список танков (игрок и враги).</summary>
    public List<Tank> Tanks { get; set; } = [];

    /// <summary>Список активных снарядов в полёте.</summary>
    public List<Projectile> Projectiles { get; set; } = [];

    /// <summary>Текущий счёт игрока.</summary>
    public int Score { get; set; }

    /// <summary>Признак окончания игры.</summary>
    public bool GameOver { get; set; }

    /// <summary>Признак победы игрока.</summary>
    public bool Victory { get; set; }

    /// <summary>Последнее информационное сообщение движка.</summary>
    public string LastMessage { get; set; } = string.Empty;

    /// <summary>Возвращает танк игрока или null, если он уничтожен.</summary>
    public Tank? PlayerTank => Tanks.FirstOrDefault(t => t.Team == "player" && t.Hp > 0);

    /// <summary>Количество живых вражеских танков.</summary>
    public int AliveEnemiesCount => Tanks.Count(t => t.Team == "enemy" && t.Hp > 0);

    /// <summary>
    /// Проверяет, заблокирована ли клетка для перемещения танка.
    /// </summary>
    /// <param name="x">Координата X.</param>
    /// <param name="y">Координата Y.</param>
    /// <param name="except">Танк, перемещение которого проверяется (игнорирует сам себя).</param>
    /// <returns>True, если клетка заблокирована; иначе false.</returns>
    public bool IsBlocked(int x, int y, Tank? except = null)
    {
        if (x < 0 || y < 0 || x >= MapSize || y >= MapSize) return true;
        if (Walls.Any(w => w.X == x && w.Y == y)) return true;
        if (Base.Hp > 0 && Base.X == x && Base.Y == y) return true;
        if (Tanks.Any(t => t != except && t.Hp > 0 && t.X == x && t.Y == y)) return true;
        return false;
    }

    /// <summary>
    /// Выпускает снаряд из указанного танка, если это разрешено правилами.
    /// </summary>
    /// <param name="tank">Стреляющий танк.</param>
    /// <returns>True, если выстрел произведён; иначе false.</returns>
    public bool Fire(Tank tank)
    {
        if (GameOver || tank.Hp <= 0) return false;
        // Ограничение: максимум 2 снаряда от одной команды одновременно в воздухе
        var activeShotsFromTeam = Projectiles.Count(p => p.Team == tank.Team);
        if (activeShotsFromTeam >= 2) return false;

        var shot = tank.Shoot();
        // Если выстрел прямо в край карты — снаряд сразу поглощается
        if (shot.X < 0 || shot.Y < 0 || shot.X >= MapSize || shot.Y >= MapSize)
        {
            return false;
        }

        Projectiles.Add(shot);
        if (tank.Team == "player")
        {
            LastMessage = "Игрок выстрелил!";
        }
        return true;
    }

    /// <summary>
    /// Выполняет один такт симуляции: полёт снарядов, коллизии, разрушение стен и базы.
    /// </summary>
    public void Step()
    {
        if (GameOver) return;

        var remaining = new List<Projectile>();
        foreach (var shot in Projectiles)
        {
            var (dx, dy) = Tank.Vector(shot.Direction);
            shot.X += dx;
            shot.Y += dy;

            // Вылет за границы карты
            if (shot.X < 0 || shot.Y < 0 || shot.X >= MapSize || shot.Y >= MapSize)
            {
                continue;
            }

            // Попадание в стену
            var wall = Walls.FirstOrDefault(w => w.X == shot.X && w.Y == shot.Y);
            if (wall is not null)
            {
                wall.TakeDamage(1);
                if (wall.Hp <= 0)
                {
                    Walls.Remove(wall);
                    LastMessage = wall.IsConcrete ? "Снаряд отрикошетил от бетона." : "Кирпичная стена разрушена!";
                }
                continue;
            }

            // Попадание в базу
            if (Base.Hp > 0 && Base.X == shot.X && Base.Y == shot.Y)
            {
                Base.TakeDamage(1);
                GameOver = true;
                Victory = false;
                LastMessage = "ШТАБ УНИЧТОЖЕН! Игра окончена.";
                continue;
            }

            // Попадание в танк противоположной команды
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
                else
                {
                    LastMessage = target.Team == "enemy" ? "Попадание во врага!" : "Ваш танк подбит!";
                }
                continue;
            }

            remaining.Add(shot);
        }

        Projectiles = remaining;
    }

    /// <summary>
    /// Автоматическое действие вражеских танков (искусственный интеллект).
    /// </summary>
    public void UpdateEnemies()
    {
        if (GameOver) return;

        foreach (var enemy in Tanks.Where(t => t.Team == "enemy" && t.Hp > 0))
        {
            // С вероятностью 25% стреляет
            if (_random.Next(100) < 25)
            {
                Fire(enemy);
            }

            // Выбор направления: 60% продолжать или повернуть к базе/игроку, 40% случайный поворот
            var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            Direction preferred;

            if (_random.Next(100) < 60)
            {
                // Приоритет — движение вниз (к базе) или к игроку
                var targetY = Base.Y;
                var targetX = Base.X;
                if (enemy.Y < targetY && _random.Next(2) == 0) preferred = Direction.Down;
                else if (enemy.X < targetX) preferred = Direction.Right;
                else if (enemy.X > targetX) preferred = Direction.Left;
                else preferred = Direction.Down;
            }
            else
            {
                preferred = directions[_random.Next(directions.Length)];
            }

            // Пробуем шагнуть в выбранном направлении, если заблокировано — пробуем случайное
            if (!enemy.Move(preferred, this))
            {
                var alt = directions[_random.Next(directions.Length)];
                enemy.Move(alt, this);
            }
        }
    }

    /// <summary>
    /// Создаёт DTO состояния для последующей сериализации в JSON.
    /// </summary>
    /// <returns>Объект GameState.</returns>
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

    /// <summary>
    /// Восстанавливает игровой движок из сохранённого состояния DTO.
    /// </summary>
    /// <param name="state">Сохранённое состояние.</param>
    /// <returns>Восстановленный игровой движок.</returns>
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

/// <summary>
/// DTO полного состояния игры для JSON-сериализации.
/// </summary>
public sealed class GameState
{
    /// <summary>Сохранённые стены.</summary>
    public List<Wall> Walls { get; set; } = [];

    /// <summary>Сохранённая база.</summary>
    public Base Base { get; set; } = new(4, 9);

    /// <summary>Сохранённые танки.</summary>
    public List<Tank> Tanks { get; set; } = [];

    /// <summary>Сохранённые снаряды.</summary>
    public List<Projectile> Projectiles { get; set; } = [];

    /// <summary>Сохранённый счёт игрока.</summary>
    public int Score { get; set; }

    /// <summary>Признак завершения игры.</summary>
    public bool GameOver { get; set; }

    /// <summary>Признак победы.</summary>
    public bool Victory { get; set; }

    /// <summary>Текст последнего сообщения.</summary>
    public string LastMessage { get; set; } = string.Empty;
}

/// <summary>
/// Реализация хранилища состояния в формате JSON.
/// </summary>
public sealed class JsonStateStore : IStateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc/>
    public void Save(GameEngine game, string path)
    {
        var json = JsonSerializer.Serialize(game.ToState(), Options);
        File.WriteAllText(path, json);
    }

    /// <inheritdoc/>
    public GameEngine Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Файл сохранения не найден: {path}");
        }

        var json = File.ReadAllText(path);
        var state = JsonSerializer.Deserialize<GameState>(json)
            ?? throw new InvalidDataException("Файл сохранения повреждён.");
        return GameEngine.FromState(state);
    }
}

/// <summary>
/// Консольный интерфейс: отрисовка карты в псевдографике, игровой цикл и управление.
/// </summary>
public static class Program
{
    private const string SavePath = "save.json";

    /// <summary>Точка входа в игру.</summary>
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;

        IStateStore store = new JsonStateStore();
        GameEngine game;

        if (File.Exists(SavePath))
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("       BATTLE CITY — C# CONSOLE         ");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine("Обнаружено сохранение игры.");
            Console.WriteLine("  [1] Загрузить сохранённую игру");
            Console.WriteLine("  [2] Начать новую игру");
            Console.Write("Ваш выбор: ");

            var key = Console.ReadKey(true).KeyChar;
            if (key == '1')
            {
                try
                {
                    game = store.Load(SavePath);
                }
                catch
                {
                    game = new GameEngine();
                }
            }
            else
            {
                game = new GameEngine();
            }
        }
        else
        {
            game = new GameEngine();
        }

        RunGameLoop(game, store);

        Console.CursorVisible = true;
    }

    /// <summary>
    /// Основной цикл интерактивной аркадной игры в реальном времени.
    /// </summary>
    public static void RunGameLoop(GameEngine game, IStateStore store)
    {
        Console.Clear();
        var enemyTickCounter = 0;
        var running = true;

        while (running)
        {
            // Обработка пользовательского ввода
            var player = game.PlayerTank;
            while (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow or ConsoleKey.W when player is not null:
                        player.Move(Direction.Up, game);
                        break;
                    case ConsoleKey.DownArrow or ConsoleKey.S when player is not null:
                        player.Move(Direction.Down, game);
                        break;
                    case ConsoleKey.LeftArrow or ConsoleKey.A when player is not null:
                        player.Move(Direction.Left, game);
                        break;
                    case ConsoleKey.RightArrow or ConsoleKey.D when player is not null:
                        player.Move(Direction.Right, game);
                        break;
                    case ConsoleKey.Spacebar or ConsoleKey.F when player is not null:
                        game.Fire(player);
                        break;
                    case ConsoleKey.F5:
                        store.Save(game, SavePath);
                        game.LastMessage = "Игра сохранена в " + SavePath;
                        break;
                    case ConsoleKey.R:
                        game = new GameEngine();
                        game.LastMessage = "Новая игра начата!";
                        break;
                    case ConsoleKey.Escape or ConsoleKey.Q:
                        store.Save(game, SavePath);
                        Console.Clear();
                        Console.ResetColor();
                        Console.WriteLine($"Состояние сохранено в {SavePath}. До встречи!");
                        return;
                }
            }

            // Обновление состояния снарядов
            game.Step();

            // Действия врагов (каждые 3 такта)
            enemyTickCounter++;
            if (enemyTickCounter >= 3)
            {
                enemyTickCounter = 0;
                game.UpdateEnemies();
            }

            // Отрисовка поля и интерфейса
            Render(game);

            if (game.GameOver)
            {
                Console.WriteLine();
                if (game.Victory)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("╔═══════════════════════════════════════════════╗");
                    Console.WriteLine("║            ПОЗДРАВЛЯЕМ! ПОБЕДА!               ║");
                    Console.WriteLine("╚═══════════════════════════════════════════════╝");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("╔═══════════════════════════════════════════════╗");
                    Console.WriteLine("║            ИГРА ОКОНЧЕНА (GAME OVER)          ║");
                    Console.WriteLine("╚═══════════════════════════════════════════════╝");
                }
                Console.ResetColor();
                Console.WriteLine("Нажмите [R] для перезапуска или [Q] для выхода.");

                while (true)
                {
                    var exitKey = Console.ReadKey(true).Key;
                    if (exitKey == ConsoleKey.R)
                    {
                        game = new GameEngine();
                        Console.Clear();
                        break;
                    }
                    if (exitKey is ConsoleKey.Q or ConsoleKey.Escape)
                    {
                        return;
                    }
                }
            }

            Thread.Sleep(120);
        }
    }

    /// <summary>
    /// Отрисовывает игровое поле 10x10 в консоли псевдографикой с цветовым оформлением.
    /// </summary>
    public static void Render(GameEngine game)
    {
        Console.SetCursorPosition(0, 0);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║           BATTLE CITY — CONSOLE ARCADE       ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.ResetColor();

        var player = game.PlayerTank;
        var hpText = player is not null ? new string('♥', player.Hp) : "МЁРТВ";

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  Счёт: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{game.Score,-6}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(" Жизни: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{hpText,-6}");
        Console.ForegroundColor = ConsoleColor.White;
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
                // Проверяем снаряд
                var projectile = game.Projectiles.FirstOrDefault(p => p.X == x && p.Y == y);
                // Проверяем танк
                var tank = game.Tanks.FirstOrDefault(t => t.Hp > 0 && t.X == x && t.Y == y);
                // Проверяем базу
                var isBase = game.Base.X == x && game.Base.Y == y;
                // Проверяем стену
                var wall = game.Walls.FirstOrDefault(w => w.X == x && w.Y == y);

                if (projectile is not null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(" ●  ");
                }
                else if (tank is not null)
                {
                    if (tank.Team == "player")
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        var symbol = tank.Direction switch
                        {
                            Direction.Up => "▲",
                            Direction.Down => "▼",
                            Direction.Left => "◄",
                            _ => "►"
                        };
                        Console.Write($"[{symbol}] ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        var symbol = tank.Direction switch
                        {
                            Direction.Up => "▲",
                            Direction.Down => "▼",
                            Direction.Left => "◄",
                            _ => "►"
                        };
                        Console.Write($"<{symbol}> ");
                    }
                }
                else if (isBase)
                {
                    if (game.Base.Hp > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(" ⚑  ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(" ☠  ");
                    }
                }
                else if (wall is not null)
                {
                    if (wall.IsConcrete)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("████");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("▓▓▓▓");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write(" ·  ");
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("│");
        }

        // Нижняя граница сетки
        Console.WriteLine("  └────────────────────────────────────────┘");
        Console.ResetColor();

        // Легенда
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  Легенда: [▲] Игрок  <▲> Враг  ▓ Кирпич  █ Бетон  ⚑ Штаб  ● Снаряд");
        Console.ResetColor();

        // Панель управления
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Управление: [W/A/S/D/Стрелки] - Ход | [Пробел] - Огонь");
        Console.WriteLine("              [F5] - Сохранить | [R] - Рестарт | [Q/Esc] - Выход");
        Console.ResetColor();

        // Информационная строка
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  Событие: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{game.LastMessage,-40}");
        Console.ResetColor();
    }
}
