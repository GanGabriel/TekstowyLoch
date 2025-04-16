using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TekstowyLoch
{
    public class GameLogic
    {
        private const int TileSize = 30;
        private DisplayMap? map;
        private readonly Canvas gameCanvas;
        private readonly TextBlock healthText;
        private readonly TextBlock armorText;

        public GameLogic(Canvas canvas, TextBlock healthText, TextBlock armorText)
        {
            gameCanvas = canvas;
            this.healthText = healthText;
            this.armorText = armorText;
        }

        public void StartNewGame()
        {
            map = new DisplayMap(10, 10);
            map.PlayerStats = new PlayerStats(100, 0);
            UpdateStatsDisplay();
            DrawMap();
        }

        public void LoadGame()
        {
            string savePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TekstowyLoch_Save.txt");
            map = DisplayMap.LoadFromFile(savePath);
            UpdateStatsDisplay();
            DrawMap();
        }

        public void SaveGame()
        {
            if (map == null)
            {
                throw new InvalidOperationException("Brak gry do zapisania!");
            }
            string savePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TekstowyLoch_Save.txt");
            map.SaveToFile(savePath);
        }

        public void MovePlayer(char direction)
        {
            if (map != null)
            {
                map.MovePlayer(direction);
                UpdateStatsDisplay();
                DrawMap();
            }
        }

        private void UpdateStatsDisplay()
        {
            if (map != null && map.PlayerStats != null)
            {
                healthText.Text = $"Życie: {map.PlayerStats.Health}";
                armorText.Text = $"Pancerz: {map.PlayerStats.Armor}";
            }
        }

        private void DrawMap()
        {
            if (map == null) return;

            gameCanvas.Children.Clear();
            var (playerX, playerY) = map.GetPlayerPosition();

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    Rectangle room = new Rectangle
                    {
                        Width = TileSize,
                        Height = TileSize,
                        Stroke = Brushes.Black,
                        Fill = map.IsRoomVisited(x, y) ? Brushes.Gray : Brushes.White
                    };
                    Canvas.SetLeft(room, x * TileSize);
                    Canvas.SetTop(room, y * TileSize);
                    gameCanvas.Children.Add(room);
                }
            }

            Ellipse player = new Ellipse
            {
                Width = TileSize,
                Height = TileSize,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(player, playerX * TileSize);
            Canvas.SetTop(player, playerY * TileSize);
            gameCanvas.Children.Add(player);
        }
    }

    public class PlayerStats
    {
        public int Health { get; private set; }
        public int Armor { get; set; }

        public PlayerStats(int health, int armor)
        {
            Health = health;
            Armor = armor;
        }

        public void TakeDamage(int damage)
        {
            int reducedDamage = Math.Max(damage - Armor, 0);
            Health = Math.Max(Health - reducedDamage, 0);
        }
    }

    public class DisplayMap
    {
        private readonly int width;
        private readonly int height;
        private readonly bool[,] visitedRooms;
        private (int x, int y) playerPosition;
        public PlayerStats PlayerStats { get; set; }

        public DisplayMap(int width, int height)
        {
            this.width = width;
            this.height = height;
            visitedRooms = new bool[width, height];
            playerPosition = (width / 2, height / 2);
            MarkRoomAsVisited(playerPosition.x, playerPosition.y);
        }

        public void UpdatePlayerPosition(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                playerPosition = (x, y);
                MarkRoomAsVisited(x, y);
            }
        }

        public void MovePlayer(char direction)
        {
            var (x, y) = playerPosition;
            switch (char.ToLower(direction))
            {
                case 'w': UpdatePlayerPosition(x, y - 1); break;
                case 's': UpdatePlayerPosition(x, y + 1); break;
                case 'a': UpdatePlayerPosition(x - 1, y); break;
                case 'd': UpdatePlayerPosition(x + 1, y); break;
            }
        }

        public void MarkRoomAsVisited(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                visitedRooms[x, y] = true;
            }
        }

        public bool IsRoomVisited(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height && visitedRooms[x, y];
        }

        public (int x, int y) GetPlayerPosition()
        {
            return playerPosition;
        }

        public void SaveToFile(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"{playerPosition.x},{playerPosition.y}");
                writer.WriteLine($"{PlayerStats.Health},{PlayerStats.Armor}"); // Zapis statystyk
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        writer.Write(visitedRooms[x, y] ? "1" : "0");
                    }
                    writer.WriteLine();
                }
            }
        }

        public static DisplayMap LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Plik zapisu gry nie istnieje.");
            }

            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length < 12)
            {
                throw new FormatException("Plik zapisu gry jest uszkodzony.");
            }

            string[] position = lines[0].Split(',');
            if (position.Length != 2 || !int.TryParse(position[0], out int x) || !int.TryParse(position[1], out int y))
            {
                throw new FormatException("Nieprawidłowy format pozycji gracza w pliku.");
            }

            string[] stats = lines[1].Split(',');
            if (stats.Length != 2 || !int.TryParse(stats[0], out int health) || !int.TryParse(stats[1], out int armor))
            {
                throw new FormatException("Nieprawidłowy format statystyk w pliku.");
            }

            DisplayMap map = new DisplayMap(10, 10);
            map.playerPosition = (x, y);
            map.PlayerStats = new PlayerStats(health, armor);

            for (int yMap = 0; yMap < 10; yMap++)
            {
                string row = lines[yMap + 2]; // +2, bo 2 linie na dane
                if (row.Length != 10)
                {
                    throw new FormatException("Nieprawidłowy format mapy w pliku.");
                }
                for (int xMap = 0; xMap < 10; xMap++)
                {
                    map.visitedRooms[xMap, yMap] = row[xMap] == '1';
                }
            }

            return map;
        }
    }
}