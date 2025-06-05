using Xunit;
using GameOfLifeSimulator;

namespace LifeTests
{
    public class LifeCellTests
    {
        [Fact]
        public void Cell_NextState_Underpopulation_Dies()
        {
            var cell = new LifeCell { Active = true };
            cell.AdjacentCells.AddRange(Enumerable.Repeat(new LifeCell { Active = true }, 1));
            cell.CalculateNextState();
            Assert.False(cell.NextState);
        }

        [Fact]
        public void Cell_NextState_Survival_LivesOn()
        {
            var cell = new LifeCell { Active = true };
            cell.AdjacentCells.AddRange(Enumerable.Repeat(new LifeCell { Active = true }, 2));
            cell.CalculateNextState();
            Assert.True(cell.NextState);
        }

        [Fact]
        public void Cell_NextState_Reproduction_BecomesAlive()
        {
            var cell = new LifeCell { Active = false };
            cell.AdjacentCells.AddRange(Enumerable.Repeat(new LifeCell { Active = true }, 3));
            cell.CalculateNextState();
            Assert.True(cell.NextState);
        }

        [Fact]
        public void Cell_NextState_Overpopulation_Dies()
        {
            var cell = new LifeCell { Active = true };
            cell.AdjacentCells.AddRange(Enumerable.Repeat(new LifeCell { Active = true }, 4));
            cell.CalculateNextState();
            Assert.False(cell.NextState);
        }

        [Fact]
        public void Cell_UpdateState_CorrectlyUpdates()
        {
            var cell = new LifeCell { NextState = true };
            cell.UpdateState();
            Assert.True(cell.Active);
        }
    }

    public class LifeGridTests
    {
        [Fact]
        public void Grid_InitializesCorrectSize()
        {
            var grid = new LifeGrid(100, 100, 10);
            Assert.Equal(10, grid.Columns);
            Assert.Equal(10, grid.Rows);
        }

        [Fact]
        public void Grid_ConnectNeighbors_CorrectCount()
        {
            var grid = new LifeGrid(3, 3, 1);
            var centerCell = grid.Grid[1, 1];
            Assert.Equal(8, centerCell.AdjacentCells.Count);
        }

        [Fact]
        public void Grid_Randomize_CreatesAliveCells()
        {
            var grid = new LifeGrid(100, 100, 10);
            grid.Randomize(0.5);
            Assert.Contains(grid.Grid.Cast<LifeCell>(), c => c.Active);
        }

        [Fact]
        public void Grid_AnalyzeGrid_CountsCorrectly()
        {
            int size = 6;
            var grid = new LifeGrid(size, size, 1);

            // Clear all cells
            foreach (var cell in grid.Grid)
                cell.Active = false;

            // Create a block
            grid.Grid[0, 0].Active = true;
            grid.Grid[0, 1].Active = true;
            grid.Grid[1, 0].Active = true;
            grid.Grid[1, 1].Active = true;

            // Create a blinker
            grid.Grid[3, 1].Active = true;
            grid.Grid[3, 2].Active = true;
            grid.Grid[3, 3].Active = true;

            var (total, patterns) = grid.AnalyzeGrid();
            Assert.Equal(7, total);
            Assert.Equal(2, patterns);
        }
    }

    public class FileOperationsTests
    {
        private readonly string testDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

        public FileOperationsTests()
        {
            Directory.CreateDirectory(testDir);
        }

        [Fact]
        public void ExportState_CreatesFile()
        {
            var grid = new LifeGrid(4, 4, 1);
            string filePath = Path.Combine(testDir, "export_test.txt");
            FileOperations.ExportState(grid.Grid, filePath);
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void ImportState_LoadsCorrectly()
        {
            var grid = new LifeGrid(2, 2, 1);
            string filePath = Path.Combine(testDir, "import_test.txt");
            File.WriteAllText(filePath, "11\n00");

            FileOperations.ImportState(grid.Grid, filePath);

            Assert.True(grid.Grid[0, 0].Active);
            Assert.True(grid.Grid[1, 0].Active);
            Assert.False(grid.Grid[0, 1].Active);
            Assert.False(grid.Grid[1, 1].Active);
        }
    }

    public class PatternAnalyzerTests : IDisposable
    {
        private readonly string patternsDir;

        public PatternAnalyzerTests()
        {
            // Создаем временную директорию для тестов
            patternsDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(patternsDir);

            // Создаем тестовые файлы паттернов
            CreateTestPatterns();
        }

        public void Dispose()
        {
            // Удаляем временную директорию после тестов
            try
            {
                Directory.Delete(patternsDir, true);
            }
            catch
            {
                // Игнорируем ошибки удаления
            }
        }

        private void CreateTestPatterns()
        {
            // Блок 2x2
            File.WriteAllText(Path.Combine(patternsDir, "block.txt"), "11\n11");

            // Мигалка 3x1
            File.WriteAllText(Path.Combine(patternsDir, "blinker.txt"), "1\n1\n1");

            // Улей 3x3
            File.WriteAllText(Path.Combine(patternsDir, "hive.txt"), "0110\n1001\n0110");

            // Планер 3x3
            File.WriteAllText(Path.Combine(patternsDir, "glider.txt"), "010\n001\n111");

            // Лодка 3x3
            File.WriteAllText(Path.Combine(patternsDir, "ellipse.txt"), "110\n101\n011");
        }

        [Fact]
        public void Constructor_LoadsAllPatterns()
        {
            var analyzer = new PatternAnalyzer(patternsDir);
            var patterns = analyzer.GetKnownPatternsForTesting();

            Assert.Equal(5, patterns.Count);
            Assert.Contains("Block", patterns.Keys);
            Assert.Contains("Oscillator", patterns.Keys);
        }

        [Fact]
        public void IdentifyPattern_RecognizesBlock()
        {
            var analyzer = new PatternAnalyzer(patternsDir);
            var pattern = new bool[,] { { true, true }, { true, true } };

            Assert.Equal("Block", analyzer.IdentifyPattern(pattern));
        }

        [Fact]
        public void IdentifyPattern_RecognizesBlinker()
        {
            var analyzer = new PatternAnalyzer(patternsDir);
            var pattern = new bool[,] { { true }, { true }, { true } };

            Assert.Equal("Oscillator", analyzer.IdentifyPattern(pattern));
        }

        [Fact]
        public void IdentifyPattern_ReturnsOther_ForUnknownPattern()
        {
            var analyzer = new PatternAnalyzer(patternsDir);
            var pattern = new bool[,] { { true, false }, { true, true } };

            Assert.Equal("Other", analyzer.IdentifyPattern(pattern));
        }

        [Fact]
        public void AnalyzePatterns_FindsBlockAndBlinker()
        {
            int size = 6;
            var grid = new LifeGrid(size, size, 1);

            // Очищаем сетку
            foreach (var cell in grid.Grid)
                cell.Active = false;

            // Добавляем блок
            grid.Grid[0, 0].Active = true;
            grid.Grid[0, 1].Active = true;
            grid.Grid[1, 0].Active = true;
            grid.Grid[1, 1].Active = true;

            // Добавляем мигалку
            grid.Grid[3, 1].Active = true;
            grid.Grid[3, 2].Active = true;
            grid.Grid[3, 3].Active = true;

            var analyzer = new PatternAnalyzer(patternsDir);
            var results = analyzer.AnalyzePatterns(grid);

            Assert.Equal(1, results["Block"]);
            Assert.Equal(1, results["Oscillator"]);
            Assert.Equal(0, results["Other"]);
        }
    }
}