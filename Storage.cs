using System.Text.Json;

namespace BattleCity;

/// <summary>Абстракция сохранения и загрузки игры (принцип инверсии зависимостей DIP).</summary>
public interface IStateStore
{
    /// <summary>Сохранить состояние игры в файл.</summary>
    void Save(GameEngine game, string path);

    /// <summary>Загрузить состояние игры из файла.</summary>
    GameEngine Load(string path);
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
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Файл сохранения пуст.");

        GameState? state;
        try
        {
            state = JsonSerializer.Deserialize<GameState>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Файл сохранения повреждён.", ex);
        }

        return GameEngine.FromState(state ?? throw new InvalidDataException("Файл сохранения повреждён."));
    }
}
