﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;

namespace cli_life
{
    public class LifeProperty
    {
        public LifeProperty() {}
        public LifeProperty(int BoardWidth, int BoardHeight, int BoardCellSize, double LifeDensity)
        {
            this.BoardWidth = BoardWidth;
            this.BoardHeight = BoardHeight;
            this.BoardCellSize = BoardCellSize;
            this.LifeDensity = LifeDensity;
        }
        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }
        public int BoardCellSize { get; set; }
        public double LifeDensity { get; set; }
    }

    public class JSONController
    {
        public static LifeProperty DeserializeFromJSON(string file_name)
        {
            string jsonString = File.ReadAllText(file_name);

            return JsonSerializer.Deserialize<LifeProperty>(jsonString)!;
        }
    }
    public class TextController
    {
        public static void Save_life(Cell[,] the_cells, string file_name)
        {
            using StreamWriter save_life = new StreamWriter(file_name);

            for (int row = 0; row < the_cells.GetLength(1); row++)
            {
                for (int col = 0; col < the_cells.GetLength(0); col++)
                {
                    save_life.Write(the_cells[col, row].IsAlive ? '1' : '0');
                }
                save_life.Write("\n");
            }
        }

        public static void Read_life(Cell[,] the_cells, string file_name)
        {
            var lines = File.ReadAllLines(file_name);
            int rows = the_cells.GetLength(1);
            int col = the_cells.GetLength(0);

            for (int y = 0; y < rows && y < lines.Length; y++)
            {
                for (int x = 0; x < col && x < lines[y].Length; x++)
                {
                    the_cells[x, y].IsAlive = lines[y][x] == '1';
                }
            }
        }

        public static void Load_figure(Cell[,] the_cells, string figureFilePath)
        {
            var lines = File.ReadAllLines(figureFilePath);
            int figureHeight = lines.Length;
            int figureWidth = lines[0].Length;

            Random rand = new();
            int startX = rand.Next(0, the_cells.GetLength(0) - figureWidth + 1);
            int startY = rand.Next(0, the_cells.GetLength(1) - figureHeight + 1);

            for (int y = 0; y < figureHeight; y++)
            {
                for (int x = 0; x < figureWidth; x++)
                {
                    if (startX + x < the_cells.GetLength(0) && startY + y < the_cells.GetLength(1))
                    {
                        the_cells[startX + x, startY + y].IsAlive = lines[y][x] == '1';
                    }
                }
            }
        }
    }
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = [];
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
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

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
    }
    class Program
    {
        static Board board;
        static readonly Dictionary<ConsoleKey, string> figureMap = new()
        {
            { ConsoleKey.D1, "glider.txt" },
            { ConsoleKey.D2, "blinker.txt" },
            { ConsoleKey.D3, "block.txt" },
            { ConsoleKey.D4, "ellipse.txt" }, 
            { ConsoleKey.D5, "hive.txt" }
        };

        static void Main(string[] args)
        {
            InitializeGame();
            RunGameLoop();
        }

        static void InitializeGame()
        {
            string proj_path = Directory.GetCurrentDirectory();
            string prop_path = Path.Combine(proj_path, "Property.json");
            string file_path = Path.Combine(proj_path, "LifeBoard.txt");
            
            LifeProperty life_property = JSONController.DeserializeFromJSON(prop_path);
            Reset(life_property);
        }

        static void RunGameLoop()
        {
            string file_path = Path.Combine(Directory.GetCurrentDirectory(), "LifeBoard.txt");
            
            while (true)
            {
                if (!Click_handler(file_path))
                    break;
                    
                UpdateGame();
            }
        }

        static void UpdateGame()
        {
            Console.Clear();
            Render();
            board.Advance();
            Thread.Sleep(1000);
        }

        static private void Reset(LifeProperty LifeProperty)
        {
            board = new Board(
                LifeProperty.BoardWidth,
                LifeProperty.BoardHeight,
                LifeProperty.BoardCellSize,
                LifeProperty.LifeDensity);
        }

        static void Render()
        {
            var output = new StringBuilder();
            
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    output.Append(board.Cells[col, row].IsAlive ? '*' : ' ');
                }
                output.AppendLine();
            }
            
            Console.Write(output);
        }

        static bool Click_handler(string file_path) 
        {
            if (!Console.KeyAvailable) 
                return true;

            var button = Console.ReadKey(true).Key;

            switch (button)
            {
                case ConsoleKey.L:
                    TextController.Read_life(board.Cells, file_path);
                    break;
                case ConsoleKey.S:
                    TextController.Save_life(board.Cells, file_path);
                    break;
                case ConsoleKey.E:
                    return false;
                default:
                    HandleFigureLoading(button);
                    break;
            }

            return true;
        }

        static void HandleFigureLoading(ConsoleKey button)
        {
            if (figureMap.TryGetValue(button, out string figureName))
            {
                string figurePath = Path.Combine(Directory.GetCurrentDirectory(), "figures", figureName);
                TextController.Load_figure(board.Cells, figurePath);
            }
        }
    }
}