using BattleCity;
using Xunit;

namespace BattleCity.Tests;

/// <summary>
/// Набор модульных тестов для проверки механики движка Battle City.
/// </summary>
public sealed class GameEngineTests
{
    /// <summary>Танк не проходит сквозь кирпичную стену.</summary>
    [Fact]
    public void TankCannotMoveThroughWall()
    {
        var game = new GameEngine { Walls = [new Wall(2, 1)] };
        var player = game.Tanks.First(t => t.Team == "player");
        player.X = 1;
        player.Y = 1;

        Assert.False(player.Move(Direction.Right, game));
        Assert.Equal((1, 1), (player.X, player.Y));
    }

    /// <summary>Танк не может выехать за пределы игрового поля (0..9).</summary>
    [Fact]
    public void TankCannotMoveOutOfBounds()
    {
        var game = new GameEngine();
        var player = game.Tanks.First(t => t.Team == "player");
        player.X = 0;
        player.Y = 0;

        Assert.False(player.Move(Direction.Up, game));
        Assert.False(player.Move(Direction.Left, game));
        Assert.Equal((0, 0), (player.X, player.Y));
    }

    /// <summary>Снаряд разрушает кирпичную стену.</summary>
    [Fact]
    public void ProjectileDestroysBrickWall()
    {
        var game = new GameEngine { Walls = [new Wall(2, 1, false, 1)] };
        game.Projectiles.Add(new Projectile(1, 1, Direction.Right, "player"));
        game.Step();

        Assert.Empty(game.Walls);
        Assert.Empty(game.Projectiles);
    }

    /// <summary>Бетонная стена не разрушается снарядами.</summary>
    [Fact]
    public void ConcreteWallIsIndestructible()
    {
        var game = new GameEngine { Walls = [new Wall(2, 1, true, 999)] };
        game.Projectiles.Add(new Projectile(1, 1, Direction.Right, "player"));
        game.Step();

        Assert.Single(game.Walls);
        Assert.Equal(999, game.Walls[0].Hp);
    }

    /// <summary>Попадание снаряда в базу игрока приводит к её разрушению и завершению игры.</summary>
    [Fact]
    public void ProjectileDestroyingBaseEndsGame()
    {
        var game = new GameEngine();
        game.Base.Hp = 1;
        game.Projectiles.Add(new Projectile(game.Base.X - 1, game.Base.Y, Direction.Right, "enemy"));
        game.Step();

        Assert.True(game.GameOver);
        Assert.False(game.Victory);
        Assert.Equal(0, game.Base.Hp);
    }

    /// <summary>Попадание снаряда игрока во вражеский танк наносит урон и начисляет очки при уничтожении.</summary>
    [Fact]
    public void ProjectileHittingEnemyTankDestroysItAndAddsScore()
    {
        var game = new GameEngine();
        game.Walls.Clear();
        var enemy = game.Tanks.First(t => t.Team == "enemy");
        enemy.X = 5;
        enemy.Y = 5;
        enemy.Hp = 1;
        var initialScore = game.Score;

        game.Projectiles.Add(new Projectile(4, 5, Direction.Right, "player"));
        game.Step();

        Assert.Equal(0, enemy.Hp);
        Assert.Equal(initialScore + 100, game.Score);
    }

    /// <summary>Стрельба в упор: снаряд танка поражает объект в соседней клетке на первом же такте.</summary>
    [Fact]
    public void ShootPointBlankHitsAdjacentTarget()
    {
        var game = new GameEngine();
        game.Walls.Clear();
        // Кирпичная стена на (3, 3)
        var wall = new Wall(3, 3, isConcrete: false, hp: 1);
        game.Walls.Add(wall);

        // Танк игрока на (2, 3) смотрит вправо прямо на стену
        var player = game.Tanks.First(t => t.Team == "player");
        player.X = 2;
        player.Y = 3;
        player.Direction = Direction.Right;

        // Выстрел в упор
        Assert.True(game.Fire(player));
        Assert.Single(game.Projectiles);

        // Первый такт — снаряд перемещается в (3, 3) и разрушает стену
        game.Step();

        Assert.Empty(game.Walls);
        Assert.Empty(game.Projectiles);
    }

    /// <summary>Искусственный интеллект врагов обновляет позиции и удерживает танки в границах поля.</summary>
    [Fact]
    public void EnemyTanksUpdateMovementAndStaysInBounds()
    {
        var game = new GameEngine();
        var enemy = game.Tanks.First(t => t.Team == "enemy");

        for (var i = 0; i < 10; i++)
        {
            game.UpdateEnemies();
            Assert.InRange(enemy.X, 0, GameEngine.MapSize - 1);
            Assert.InRange(enemy.Y, 0, GameEngine.MapSize - 1);
        }
    }

    /// <summary>Сохранение и загрузка через JSON восстанавливают полное состояние игры.</summary>
    [Fact]
    public void SaveLoadRestoresFullState()
    {
        var game = new GameEngine { Score = 300 };
        game.Projectiles.Add(new Projectile(3, 4, Direction.Up, "player"));
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        try
        {
            var store = new JsonStateStore();
            store.Save(game, path);
            var loaded = store.Load(path);

            Assert.Equal(game.Score, loaded.Score);
            Assert.Equal(game.Base.X, loaded.Base.X);
            Assert.Equal(game.Base.Y, loaded.Base.Y);
            Assert.Equal(game.Tanks.Count, loaded.Tanks.Count);
            Assert.Equal(game.Projectiles.Count, loaded.Projectiles.Count);
            Assert.Equal(game.Walls.Count, loaded.Walls.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Загрузка повреждённого или пустого файла вызывает исключение InvalidDataException.</summary>
    [Fact]
    public void CorruptedSaveFileThrowsOrHandled()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(path, "{ corrupted json syntax");
            var store = new JsonStateStore();
            Assert.Throws<InvalidDataException>(() => store.Load(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
