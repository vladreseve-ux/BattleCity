namespace BattleCity;

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

        Projectiles.Add(tank.Shoot());
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

            // Вылет за пределы поля — снаряд исчезает
            if (shot.X < 0 || shot.Y < 0 || shot.X >= MapSize || shot.Y >= MapSize) continue;

            // Столкновение со стеной
            var wall = Walls.FirstOrDefault(w => w.X == shot.X && w.Y == shot.Y);
            if (wall is not null)
            {
                wall.TakeDamage(1);
                if (wall.Hp <= 0) Walls.Remove(wall);
                LastMessage = wall.IsConcrete ? "Снаряд поглощён бетоном." : "Кирпичная стена разрушена!";
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

    /// <summary>Действия вражеских танков (случайная стрельба и движение к штабу).</summary>
    public void UpdateEnemies()
    {
        if (GameOver) return;

        var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        foreach (var enemy in Tanks.Where(t => t.Team == "enemy" && t.Hp > 0))
        {
            // Вероятность выстрела 25%
            if (_random.Next(100) < 25) Fire(enemy);

            // 60% выбор пути в сторону штаба, 40% случайный поворот
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
