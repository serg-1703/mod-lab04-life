using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace GameOfLifeSimulator
{
    public class SimulationSettings
    {
        public SimulationSettings() { }
        public SimulationSettings(int Width, int Height, int CellDimension, double Density)
        {
            this.Width = Width;
            this.Height = Height;
            this.CellDimension = CellDimension;
            this.Density = Density;
        }
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellDimension { get; set; }
        public double Density { get; set; }
    }

    public class JsonDataHandler
    {
        public static SimulationSettings ImportSettings(string filename)
        {
            string jsonContent = File.ReadAllText(filename);
            return JsonSerializer.Deserialize<SimulationSettings>(jsonContent)!;
        }
    }

    public class FileOperations
    {
        public static void ExportState(LifeCell[,] grid, string filename)
        {
            using StreamWriter writer = new StreamWriter(filename);

            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    writer.Write(grid[x, y].Active ? '1' : '0');
                }
                writer.Write("\n");
            }
        }

        public static void ImportState(LifeCell[,] grid, string filename)
        {
            var fileContent = File.ReadAllLines(filename);
            int rows = grid.GetLength(1);
            int cols = grid.GetLength(0);

            for (int y = 0; y < rows && y < fileContent.Length; y++)
            {
                for (int x = 0; x < cols && x < fileContent[y].Length; x++)
                {
                    grid[x, y].Active = fileContent[y][x] == '1';
                }
            }
        }

        public static void InsertPattern(LifeCell[,] grid, string patternFile)
        {
            var patternData = File.ReadAllLines(patternFile);
            int patternRows = patternData.Length;
            int patternCols = patternData[0].Length;

            Random rnd = new();
            int startX = rnd.Next(0, grid.GetLength(0) - patternCols + 1);
            int startY = rnd.Next(0, grid.GetLength(1) - patternRows + 1);

            for (int y = 0; y < patternRows; y++)
            {
                for (int x = 0; x < patternCols; x++)
                {
                    if (startX + x < grid.GetLength(0) && startY + y < grid.GetLength(1))
                    {
                        grid[startX + x, startY + y].Active = patternData[y][x] == '1';
                    }
                }
            }
        }
    }

    public class LifeCell
    {
        public bool Active;
        public bool NextState;
        public readonly List<LifeCell> AdjacentCells = new List<LifeCell>();

        public void CalculateNextState()
        {
            int liveNeighbors = AdjacentCells.Count(c => c.Active);
            NextState = Active ? (liveNeighbors == 2 || liveNeighbors == 3) 
                            : (liveNeighbors == 3);
        }

        public void UpdateState()
        {
            Active = NextState;
        }
    }

    public class LifeGrid
    {
        public readonly LifeCell[,] Grid;
        public readonly int CellSize;
        private bool[,] visited;
        private readonly Random random = new Random();

        public int Columns => Grid.GetLength(0);
        public int Rows => Grid.GetLength(1);
        public int Width => Columns * CellSize;
        public int Height => Rows * CellSize;

        public LifeGrid(int width, int height, int cellSize, double liveDensity = 0.1)
        {
            if (cellSize <= 0)
                throw new ArgumentException("Cell size must be greater than zero", nameof(cellSize));

            CellSize = cellSize;
            Grid = new LifeCell[width / cellSize, height / cellSize];
            visited = new bool[Columns, Rows];

            // Инициализация всех ячеек
            InitializeCells();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        private void InitializeCells()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    Grid[x, y] = new LifeCell();
                }
            }
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Grid[x, y].AdjacentCells.Add(Grid[xL, yT]);
                    Grid[x, y].AdjacentCells.Add(Grid[x, yT]);
                    Grid[x, y].AdjacentCells.Add(Grid[xR, yT]);
                    Grid[x, y].AdjacentCells.Add(Grid[xL, y]);
                    Grid[x, y].AdjacentCells.Add(Grid[xR, y]);
                    Grid[x, y].AdjacentCells.Add(Grid[xL, yB]);
                    Grid[x, y].AdjacentCells.Add(Grid[x, yB]);
                    Grid[x, y].AdjacentCells.Add(Grid[xR, yB]);
                }
            }
        }

        public void Randomize(double liveDensity)
        {
            foreach (var cell in Grid)
            {
                cell.Active = random.NextDouble() < liveDensity;
            }
        }

        public void Advance()
        {
            foreach (var cell in Grid)
            {
                cell.CalculateNextState();
            }
            foreach (var cell in Grid)
            {
                cell.UpdateState();
            }
        }

        public (int totalCells, int combinations) AnalyzeGrid()
        {
            int totalCells = 0;
            int combinations = 0;
            Array.Clear(visited, 0, visited.Length);

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (!visited[x, y] && Grid[x, y].Active)
                    {
                        int groupSize = ExploreCellGroup(x, y);
                        totalCells += groupSize;

                        if (groupSize > 1)
                        {
                            combinations++;
                        }
                    }
                }
            }

            return (totalCells, combinations);
        }

        private int ExploreCellGroup(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows || 
                visited[x, y] || !Grid[x, y].Active)
                return 0;

            visited[x, y] = true;
            int size = 1;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = (x + dx + Columns) % Columns;
                    int ny = (y + dy + Rows) % Rows;

                    size += ExploreCellGroup(nx, ny);
                }
            }

            return size;
        }
    }

    public class PatternAnalyzer
    {
        private readonly string patternsDirectory;
        private static readonly Dictionary<string, string> PatternFiles = new()
        {
            ["Block"] = "block.txt",
            ["Oscillator"] = "blinker.txt",
            ["Hexagon"] = "hive.txt",
            ["Spaceship"] = "glider.txt",
            ["Diamond"] = "ellipse.txt"
        };

        private readonly Dictionary<string, bool[,]> knownPatterns;
        private bool[,] visitedCells;

        public PatternAnalyzer(string patternsDir)
        {
            if (!Directory.Exists(patternsDir))
                throw new DirectoryNotFoundException($"Patterns directory not found: {patternsDir}");

            this.patternsDirectory = patternsDir;
            knownPatterns = LoadKnownPatterns();
            
            if (!knownPatterns.ContainsKey("Block"))
                throw new FileNotFoundException("Required pattern file (block.txt) not found or invalid");
        }

        private Dictionary<string, bool[,]> LoadKnownPatterns()
        {
            var patterns = new Dictionary<string, bool[,]>();

            foreach (var kvp in PatternFiles)
            {
                string fullPath = Path.Combine(patternsDirectory, kvp.Value);
                
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"Pattern file not found: {fullPath}");
                    continue;
                }

                try
                {
                    var pattern = ReadPatternFile(fullPath);
                    if (pattern.Length > 0)
                    {
                        patterns[kvp.Key] = pattern;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading {kvp.Key}: {ex.Message}");
                }
            }

            return patterns;
        }

        private bool[,] ReadPatternFile(string path)
        {
            var lines = File.ReadAllLines(path)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => l.Trim())
                        .ToArray();

            if (lines.Length == 0)
                return new bool[0, 0];

            int width = lines[0].Length;
            int height = lines.Length;

            var pattern = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                if (lines[y].Length != width)
                    throw new InvalidDataException($"Line {y+1} has incorrect length (expected {width}, got {lines[y].Length})");

                for (int x = 0; x < width; x++)
                {
                    if (lines[y][x] != '0' && lines[y][x] != '1')
                        throw new InvalidDataException($"Invalid character at line {y+1}, position {x+1}");

                    pattern[x, y] = lines[y][x] == '1';
                }
            }

            return pattern;
        }

        public Dictionary<string, int> AnalyzePatterns(LifeGrid grid)
        {
            var results = knownPatterns.Keys.ToDictionary(k => k, _ => 0);
            results["Other"] = 0;
            visitedCells = new bool[grid.Columns, grid.Rows];

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    if (!visitedCells[x, y] && grid.Grid[x, y].Active)
                    {
                        var cellGroup = FindCellGroup(grid, x, y);
                        if (cellGroup.Count > 1)
                        {
                            var pattern = CreatePatternMatrix(cellGroup);
                            var patternType = IdentifyPattern(pattern);
                            results[patternType]++;
                        }
                    }
                }
            }

            return results;
        }

        private List<(int x, int y)> FindCellGroup(LifeGrid grid, int startX, int startY)
        {
            var cells = new List<(int x, int y)>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (x < 0 || x >= grid.Columns || y < 0 || y >= grid.Rows ||
                    visitedCells[x, y] || !grid.Grid[x, y].Active) 
                    continue;

                visitedCells[x, y] = true;
                cells.Add((x, y));

                // Проверяем всех 8 соседей
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Пропускаем текущую клетку

                        int nx = (x + dx + grid.Columns) % grid.Columns;
                        int ny = (y + dy + grid.Rows) % grid.Rows;
                        
                        queue.Enqueue((nx, ny));
                    }
                }
            }
            return cells;
        }

        private bool[,] CreatePatternMatrix(List<(int x, int y)> cells)
        {
            int minX = cells.Min(c => c.x), maxX = cells.Max(c => c.x);
            int minY = cells.Min(c => c.y), maxY = cells.Max(c => c.y);
            var matrix = new bool[maxX - minX + 1, maxY - minY + 1];

            foreach (var (x, y) in cells)
                matrix[x - minX, y - minY] = true;

            return matrix;
        }

        public string IdentifyPattern(bool[,] pattern)
        {
            if (pattern == null || pattern.Length == 0)
                return "Other";

            foreach (var kvp in knownPatterns)
            {
                if (ComparePatterns(pattern, kvp.Value))
                    return kvp.Key;
            }

            return "Other";
        }

        private bool ComparePatterns(bool[,] patternA, bool[,] patternB)
        {
            if (patternA.Length == 0 || patternB.Length == 0)
                return false;

            // Проверяем все возможные ориентации
            return ArePatternsEqual(patternA, patternB) ||
                ArePatternsEqual(Rotate90(patternA), patternB) ||
                ArePatternsEqual(Rotate180(patternA), patternB) ||
                ArePatternsEqual(Rotate270(patternA), patternB) ||
                ArePatternsEqual(Mirror(patternA), patternB);
        }

        private bool ArePatternsEqual(bool[,] a, bool[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
                return false;

            for (int x = 0; x < a.GetLength(0); x++)
                for (int y = 0; y < a.GetLength(1); y++)
                    if (a[x, y] != b[x, y])
                        return false;

            return true;
        }

        private bool[,] Rotate90(bool[,] pattern)
        {
            int w = pattern.GetLength(0);
            int h = pattern.GetLength(1);
            var result = new bool[h, w];

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    result[y, w - 1 - x] = pattern[x, y];

            return result;
        }

        private bool[,] Rotate180(bool[,] pattern) => Rotate90(Rotate90(pattern));
        private bool[,] Rotate270(bool[,] pattern) => Rotate90(Rotate180(pattern));

        private bool[,] Mirror(bool[,] pattern)
        {
            int w = pattern.GetLength(0);
            int h = pattern.GetLength(1);
            var result = new bool[w, h];

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    result[w - 1 - x, y] = pattern[x, y];

            return result;
        }

        public Dictionary<string, bool[,]> GetKnownPatternsForTesting() => 
            new Dictionary<string, bool[,]>(knownPatterns);
    }

    public class LifeSimulation
    {
        public static LifeGrid gameGrid;
        public static int stableGenerations = 0;
        public static int patternCount = 0;
        public static int generationCount = 0;
        public static int requiredStableGenerations = 10;
        public static readonly Dictionary<ConsoleKey, string> patternKeys = new()
        {
            { ConsoleKey.D1, "glider.txt" },
            { ConsoleKey.D2, "blinker.txt" },
            { ConsoleKey.D3, "block.txt" },
            { ConsoleKey.D4, "ellipse.txt" },
            { ConsoleKey.D5, "hive.txt" }
        };

        static void Main(string[] args)
        {
            string projectDir = Directory.GetCurrentDirectory();
            string settingsPath = Path.Combine(projectDir, "config.json");
            string statePath = Path.Combine(projectDir, "game_state.txt");
            SimulationSettings settings = JsonDataHandler.ImportSettings(settingsPath);
            InitializeSimulation(settings);
            RunSimulationLoop(statePath);

            // Для сбора статистики по стабильности:
            //CalculateStabilityAverages(settingsPath, statePath);
    
            // Для сбора данных о поколениях:
            //CollectStatistics(settingsPath, statePath);
        }

        public static void InitializeSimulation(SimulationSettings settings)
        {
            gameGrid = new LifeGrid(
                settings.Width,
                settings.Height,
                settings.CellDimension,
                settings.Density);
        }

        static bool RunSimulationLoop(string statePath)
        {
            while (true)
            {
                if (!ProcessInput(statePath))
                    break;

                if (UpdateSimulation()) return true;
            }
            return false;
        }

        static bool UpdateSimulation()
        {
            DisplayGrid();
            generationCount++;
            Console.WriteLine($"\n Generation: {generationCount}");
            (int totalCells, int currentPatterns) = DisplayAnalysis();
            DisplayPatternAnalysis();
            
            if (CheckStability(currentPatterns))
            {
                Console.WriteLine("\n Stable state reached");
                return true;
            }
            
            gameGrid.Advance();
            Thread.Sleep(1000);
            return false;
        }

        static void DisplayGrid()
        {
            Console.Clear();
            var display = new StringBuilder();
            
            for (int y = 0; y < gameGrid.Rows; y++)
            {
                for (int x = 0; x < gameGrid.Columns; x++)
                {
                    display.Append(gameGrid.Grid[x, y].Active ? '■' : ' ');
                }
                display.AppendLine();
            }
            Console.Write(display);
        }

        static bool ProcessInput(string statePath)
        {
            if (!Console.KeyAvailable)
                return true;
                
            var key = Console.ReadKey(true).Key;
            
            switch (key)
            {
                case ConsoleKey.L:
                    FileOperations.ImportState(gameGrid.Grid, statePath);
                    stableGenerations = 0;
                    patternCount = 0;
                    generationCount = 0;
                    break;
                case ConsoleKey.S:
                    FileOperations.ExportState(gameGrid.Grid, statePath);
                    break;
                case ConsoleKey.E:
                    return false;
                default:
                    HandlePatternInsertion(key);
                    break;
            }
            return true;
        }

        static void HandlePatternInsertion(ConsoleKey key)
        {
            if (patternKeys.TryGetValue(key, out string patternFile))
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "figures", patternFile);
                FileOperations.InsertPattern(gameGrid.Grid, fullPath);
            }
        }

        static (int total, int patterns) DisplayAnalysis()
        {
            (int total, int patterns) = gameGrid.AnalyzeGrid();
            Console.WriteLine($"Single cells: {total}, Patterns: {patterns}");
            return (total, patterns);
        }

        static void DisplayPatternAnalysis()
        {
            string patternsDir = Path.Combine(Directory.GetCurrentDirectory(), "figures");
            var analyzer = new PatternAnalyzer(patternsDir);
            var results = analyzer.AnalyzePatterns(gameGrid);

            Console.WriteLine("\n Detected patterns:");
            foreach (var kvp in results.Where(r => r.Value > 0))
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        static bool CheckStability(int currentPatterns)
        {
            if (stableGenerations == 0)
            {
                stableGenerations++;
                patternCount = currentPatterns;
            }
            else
            {
                if (patternCount == currentPatterns)
                {
                    stableGenerations++;
                    if (stableGenerations == requiredStableGenerations)
                    {
                        return true;
                    }
                }
                else if (generationCount >= 1000 && Math.Abs(patternCount - currentPatterns) == 1)
                {
                    stableGenerations++;
                    if (stableGenerations == requiredStableGenerations)
                    {
                        return true;
                    }
                }
                else
                {
                    stableGenerations = 1;
                    patternCount = currentPatterns;
                }
            }
            return false;
        }
        static void CollectStatistics(string settingsPath, string statePath)
        {
            double density = 0;
            double densityStep = 0.02;
            SimulationSettings settings = JsonDataHandler.ImportSettings(settingsPath);
            string outputFile = Path.Combine(Directory.GetCurrentDirectory(), "data.txt");
            
            File.WriteAllText(outputFile, "Density\tGeneration\n");

            while (density < 1.01)
            {
                settings.Density = density;
                InitializeSimulation(settings);
                generationCount = 0;
                stableGenerations = 0;
                patternCount = 0;

                while (true)
                {
                    generationCount++;
                    var analysis = gameGrid.AnalyzeGrid();
                    
                    if (CheckStability(analysis.combinations) || generationCount >= 1000)
                        break;
                        
                    gameGrid.Advance();
                }

                File.AppendAllText(outputFile, $"{Math.Round(density, 2)}\t{generationCount}\n");
                density += densityStep;
                
                Console.WriteLine($"Completed density: {Math.Round(density, 2)}"); 
            }
        }
        static void CalculateStabilityAverages(string settingsPath, string statePath)
        {
            double density = 0.1;
            int simulationRuns = 10;
            List<int> generationsList = new List<int>();
            SimulationSettings settings = JsonDataHandler.ImportSettings(settingsPath);

            string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "StabilityAverages");
            Directory.CreateDirectory(outputFolder);

            while (density < 0.9)
            {
                generationsList.Clear();
                string fileName = $"density_{density:0.0}.txt";
                string fullPath = Path.Combine(outputFolder, fileName);

                for (int i = 0; i < simulationRuns; i++)
                {
                    InitializeSimulation(settings);
                    generationCount = 0;
                    stableGenerations = 0;
                    patternCount = 0;

                    while (true)
                    {
                        generationCount++;
                        var analysis = gameGrid.AnalyzeGrid();

                        if (CheckStability(analysis.combinations))
                            break;

                        gameGrid.Advance();
                    }

                    generationsList.Add(generationCount);
                    File.AppendAllText(fullPath, $"{i + 1} - run: generations: {generationCount}\n");
                }

                File.AppendAllText(fullPath, $"Average generations: {Math.Round(generationsList.Average())}\n");
                density += 0.1;
            }
        }
    }
}