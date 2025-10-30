using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Замените using alias на struct
public struct Point
{
    public int x;
    public int y;
    
    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public enum Direction { Left, Right, Up, Down }

public class Cell : Div
{
    private readonly Point p;
    private readonly List<(Point p, Direction d)> snake;
    private readonly Point food;

    public Cell(Point p, HashSet<Point> seen, List<(Point p, Direction d)> snake, Point food)
    {
        this.p = p;
        this.snake = snake;
        this.food = food;

        // В реальном приложении нужно будет адаптировать отрисовку
        Console.Write(seen.Contains(p) ? "*" : " ");
    }

    public bool IsHead => snake.First().p.Equals(p);
    public bool IsFood => p.Equals(food);
    public bool IsSnakeBody => snake.Any(y => y.p.Equals(p));
    public bool IsBorder => p.x == 0 || p.x == 20 || p.y == 0 || p.y == 20;
}

// Временная заглушка для Div (нужно будет реализовать GUI)
public class Div
{
    public List<object> Children { get; } = new();
    public string CssClass { get; set; }
    public Dictionary<string, string> Styles { get; } = new();
}

public class Button
{
    public event EventHandler Click;
    
    public void SimulateClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
}

public class Label
{
    public string Text { get; }
    
    public Label(string text)
    {
        Text = text;
    }
}

public class DumpContainer
{
    public object Content { get; set; }
    
    public void Dump(string name)
    {
        Console.WriteLine($"{name}: {Content}");
    }
}

public static class Util
{
    public static Div HtmlHead { get; } = new();
}

class Program
{
    static object _lock = new object();

    static async Task Main(string[] args)
    {
        await RunGame();
    }

    static async Task RunGame()
    {
        bool hasFood = false;
        bool isWallCollision = false;

        DumpContainer board = new();
        board.Dump("board");

        Random r = new();

        List<(Point Point, Direction d, Queue<(Point Point, Direction d)> moves)> snake =
            new List<(Point, Direction, Queue<(Point, Direction)>)>
            {
                (new Point(10, 10), Direction.Right, new Queue<(Point, Direction)>())
            };
        
        Point food = new Point(r.Next(1, 19), r.Next(1, 19));

        // Создание кнопок (в консольной версии они будут виртуальными)
        Button left = new Button();
        Button right = new Button();
        Button up = new Button();
        Button down = new Button();
        Button pause = new Button();

        bool isPaused = false;
        pause.Click += (o, e) => { isPaused = !isPaused; };

        HashSet<Point> seen = new HashSet<Point>();
        int refreshMilliseconds = 700;

        // Для консольной версии - эмуляция управления
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;
                lock (_lock)
                {
                    switch (key)
                    {
                        case ConsoleKey.LeftArrow:
                            snake.ForEach(snk => snk.moves.Enqueue((snake.First().Point, Direction.Left)));
                            break;
                        case ConsoleKey.RightArrow:
                            snake.ForEach(snk => snk.moves.Enqueue((snake.First().Point, Direction.Right)));
                            break;
                        case ConsoleKey.UpArrow:
                            snake.ForEach(snk => snk.moves.Enqueue((snake.First().Point, Direction.Up)));
                            break;
                        case ConsoleKey.DownArrow:
                            snake.ForEach(snk => snk.moves.Enqueue((snake.First().Point, Direction.Down)));
                            break;
                        case ConsoleKey.Spacebar:
                            isPaused = !isPaused;
                            break;
                    }
                }
                await Task.Delay(50);
            }
        });

        while (true)
        {
            if (isPaused)
            {
                await Task.Delay(100);
                continue;
            }

            seen = snake.Select(x => x.Point).Concat(seen).Distinct().ToHashSet();

            // Отрисовка игрового поля в консоли
            Console.Clear();
            Console.WriteLine($"Score: {snake.Count - 1}");
            Console.WriteLine($"Speed: {refreshMilliseconds}");
            Console.WriteLine("Controls: Arrow keys to move, Space to pause");

            for (int i = 0; i < 21; i++)
            {
                for (int j = 0; j < 21; j++)
                {
                    Point p = new Point(i, j);
                    char cellChar = ' ';
                    
                    if (snake.First().Point.Equals(p)) cellChar = 'H'; // Голова
                    else if (snake.Skip(1).Any(s => s.Point.Equals(p))) cellChar = 'S'; // Тело
                    else if (food.Equals(p)) cellChar = 'F'; // Еда
                    else if (i == 0 || i == 20 || j == 0 || j == 20) cellChar = '#'; // Граница
                    else if (seen.Contains(p)) cellChar = '.'; // Посещенные клетки
                    
                    Console.Write(cellChar);
                }
                Console.WriteLine();
            }

            await Task.Delay(refreshMilliseconds);

            lock (_lock)
            {
                var tail = snake.Last();
                
                snake = snake.Select((snk, i) =>
                {
                    Direction newDirection = snk.moves.Any() && snk.moves.Peek().Point.Equals(snk.Point) 
                        ? snk.moves.Dequeue().d 
                        : snk.d;
                    
                    Point newPoint = newDirection switch
                    {
                        Direction.Left => new Point(snk.Point.x, snk.Point.y - 1),
                        Direction.Right => new Point(snk.Point.x, snk.Point.y + 1),
                        Direction.Up => new Point(snk.Point.x - 1, snk.Point.y),
                        Direction.Down => new Point(snk.Point.x + 1, snk.Point.y),
                        _ => throw new InvalidOperationException("shouldn't happen")
                    };
                    
                    return (newPoint, newDirection, snk.moves);
                }).ToList();
                
                var head = snake.First();

                isWallCollision = head.Point.x == 0 || head.Point.x == 20 || head.Point.y == 0 || head.Point.y == 20;
                if (isWallCollision)
                {
                    Console.WriteLine("hit wall");
                    break;
                }

                bool isSelfCollision = snake.Skip(1).Select(x => x.Point).ToHashSet().Contains(head.Point);
                if (isSelfCollision)
                {
                    Console.WriteLine("self collision");
                    break;
                }
                
                hasFood = head.Point.Equals(food);
                if (hasFood)
                {
                    refreshMilliseconds = Math.Max(100, refreshMilliseconds - 25);
                    food = new Point(r.Next(1, 19), r.Next(1, 19));
                    Console.WriteLine("got food");
                    snake = snake.Append((tail.Point, snake.Last().d, 
                        new Queue<(Point, Direction)>(snake.Last().moves))).ToList();
                }
            }
        }
        
        Console.WriteLine("game over");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}