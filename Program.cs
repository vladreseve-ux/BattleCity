namespace BattleCity;

/// <summary>Главный класс программы: инициализация, игровой цикл и диспетчеризация ввода.</summary>
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

        // Если обнаружен файл сохранения — предлагаем выбор пользователю
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
            if (choice == '1')
            {
                try
                {
                    game = store.Load(SaveFile);
                    game.LastMessage = "Игра загружена.";
                }
                catch
                {
                    game = new GameEngine();
                    game.LastMessage = "Файл сохранения повреждён. Начата новая игра.";
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
        try { Console.CursorVisible = true; } catch { }
    }

    /// <summary>Основной игровой цикл аркады (Game Loop).</summary>
    public static void RunGameLoop(GameEngine game, IStateStore store)
    {
        Console.Clear();
        var enemyTimer = 0;

        while (true)
        {
            Direction? moveDirection = null;
            var shootRequested = false;
            var restartRequested = false;
            var quitRequested = false;

            // 1. Считывание всех накопившихся нажатий за кадр с фильтрацией автоповтора
            while (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.W or ConsoleKey.UpArrow:
                        moveDirection = Direction.Up;
                        break;
                    case ConsoleKey.S or ConsoleKey.DownArrow:
                        moveDirection = Direction.Down;
                        break;
                    case ConsoleKey.A or ConsoleKey.LeftArrow:
                        moveDirection = Direction.Left;
                        break;
                    case ConsoleKey.D or ConsoleKey.RightArrow:
                        moveDirection = Direction.Right;
                        break;
                    case ConsoleKey.Spacebar or ConsoleKey.F:
                        shootRequested = true;
                        break;
                    case ConsoleKey.F5:
                        store.Save(game, SaveFile);
                        game.LastMessage = "Игра сохранена в " + SaveFile;
                        break;
                    case ConsoleKey.R:
                        restartRequested = true;
                        break;
                    case ConsoleKey.Q or ConsoleKey.Escape:
                        quitRequested = true;
                        break;
                }
            }

            if (quitRequested)
            {
                store.Save(game, SaveFile);
                Console.Clear();
                Console.WriteLine($"Игра сохранена в {SaveFile}. До свидания!");
                return;
            }

            if (restartRequested)
            {
                game = new GameEngine();
                game.LastMessage = "Новая игра начата!";
                while (!Console.IsInputRedirected && Console.KeyAvailable) Console.ReadKey(true);
                Console.Clear();
            }

            // Применяем ход и выстрел строго один раз за кадр
            var player = game.PlayerTank;
            if (player is not null)
            {
                if (moveDirection.HasValue) player.Move(moveDirection.Value, game);
                if (shootRequested) game.Fire(player);
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
            ConsoleUi.Render(game);

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
                    if (exitKey == ConsoleKey.R)
                    {
                        game = new GameEngine();
                        while (!Console.IsInputRedirected && Console.KeyAvailable) Console.ReadKey(true);
                        Console.Clear();
                        break;
                    }
                    if (exitKey is ConsoleKey.Q or ConsoleKey.Escape) return;
                }
            }

            // 6. Задержка между кадрами (120 мс ~ 8 кадров/сек)
            Thread.Sleep(120);
        }
    }
}
