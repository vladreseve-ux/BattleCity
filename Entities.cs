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

    /// <summary>Повернуться и сделать шаг, если целевая клетка свободна.</summary>
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
    /// Создаёт снаряд в клетке танка. На первом такте движения снаряд сдвинется
    /// в соседнюю клетку, что обеспечивает корректное попадание в упор.
    /// </summary>
    public Projectile Shoot()
    {
        return new Projectile(X, Y, Direction, Team);
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
