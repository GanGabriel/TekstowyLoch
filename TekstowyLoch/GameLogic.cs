using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System.Windows;

namespace TekstowyLoch
{
    public class GameLogic
    {
        private const int TileSize = 30;
        private readonly Canvas gameCanvas;
        private readonly TextBlock healthText, armorText, damageText, goldText, weaponText, floorText, consoleLog;
        private readonly ComboBox inventoryComboBox;
        private readonly StackPanel combatPanel, inventoryPanel;
        private DisplayMap? map;
        private Enemy? currentEnemy;

        public GameLogic(Canvas canvas, TextBlock healthText, TextBlock armorText, TextBlock damageText, TextBlock goldText,
                         TextBlock weaponText, TextBlock floorText, TextBlock consoleLog,
                         ComboBox inventoryComboBox, StackPanel combatPanel, StackPanel inventoryPanel)
        {
            gameCanvas = canvas;
            this.healthText = healthText;
            this.armorText = armorText;
            this.damageText = damageText;
            this.goldText = goldText;
            this.weaponText = weaponText;
            this.floorText = floorText;
            this.consoleLog = consoleLog;
            this.inventoryComboBox = inventoryComboBox;
            this.combatPanel = combatPanel;
            this.inventoryPanel = inventoryPanel;
        }

        public void StartNewGame()
        {
            map = new DisplayMap(10, 10, new PlayerStats(100, 5, 10, "Drewniany kij", 0));
            consoleLog.Text = "";
            UpdateStatsDisplay();
            AddLog("Nowa gra rozpoczęta!");
            DrawMap();
            RefreshInventory();
        }

        public void LoadGame()
        {
            try
            {
                map = DisplayMap.LoadFromFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TekstowyLoch_Save.txt"), consoleLog);
                UpdateStatsDisplay();
                DrawMap();
                RefreshInventory(); // Odśwież ekwipunek po wczytaniu
                AddLog("Gra została wczytana.");
            }
            catch (Exception ex)
            {
                AddLog($"Błąd podczas wczytywania gry: {ex.Message}");
                throw;
            }
        }

        public void SaveGame()
        {
            if (map == null) throw new InvalidOperationException("Brak gry do zapisania!");
            map.SaveToFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TekstowyLoch_Save.txt"), consoleLog.Text);
        }

        public void MovePlayer(char dir)
        {
            if (map == null || currentEnemy != null) return;
            map.MovePlayer(dir);
            UpdateStatsDisplay();
            DrawMap();
            var room = map.CurrentRoom;
            if (room.Visited) return;
            room.Visited = true;
            if (room.Enemy != null)
            {
                currentEnemy = room.Enemy;
                combatPanel.Visibility = Visibility.Visible;
                AddLog($"Spotkałeś przeciwnika: {currentEnemy.Name} (HP: {currentEnemy.Health}, DMG: {currentEnemy.Damage})");
                DrawMap();
                return;
            }
            if (room.Loot != null)
            {
                map.Inventory.Add(room.Loot);
                AddLog($"Znalazłeś {room.Loot.Name}!");
                RefreshInventory();
                room.Loot = null; // Usuń przedmiot z pokoju po podniesieniu
            }
            if (room.IsStairs)
            {
                map.NextFloor();
                AddLog($"Znalazłeś zejście na niższy poziom. Przechodzisz na poziom {map.Floor}!");
                DrawMap();
            }
        }

        public void PlayerAttack()
        {
            if (map == null || currentEnemy == null) return;
            currentEnemy.Health -= map.PlayerStats.Damage;
            AddLog($"Zaatakowałeś {currentEnemy.Name}, zadając {map.PlayerStats.Damage} obrażeń!");
            if (currentEnemy.Health <= 0)
            {
                AddLog($"Pokonałeś {currentEnemy.Name}!");
                int goldDropped = new Random().Next(5, 15) * map.Floor + map.Floor * 5;
                map.PlayerStats.Gold += goldDropped;
                AddLog($"Zdobyłeś {goldDropped} złota!");
                if (new Random().NextDouble() < (0.1 + 0.03 * map.Floor))
                {
                    var loot = GenerateLoot(map.Floor);
                    map.Inventory.Add(loot);
                    AddLog($"Znalazłeś {loot.Name}!");
                    RefreshInventory();
                }
                map.CurrentRoom.Enemy = null;
                currentEnemy = null;
                combatPanel.Visibility = Visibility.Collapsed;
                UpdateStatsDisplay();
                DrawMap();
                return;
            }
            map.PlayerStats.TakeDamage(currentEnemy.Damage);
            AddLog($"{currentEnemy.Name} atakuje i zadaje {currentEnemy.Damage} obrażeń!");
            UpdateStatsDisplay();
            if (map.PlayerStats.Health <= 0)
            {
                AddLog("Zostałeś pokonany. Koniec gry.");
                combatPanel.Visibility = Visibility.Collapsed;
            }
        }

        public void TryToEscape()
        {
            if (map == null || currentEnemy == null) return;
            AddLog("Próbujesz uciec...");
            if (new Random().NextDouble() < 0.5)
            {
                AddLog("Udało Ci się uciec!");
                currentEnemy = null;
                combatPanel.Visibility = Visibility.Collapsed;
                DrawMap();
            }
            else
            {
                AddLog("Nie udało się uciec!");
                map.PlayerStats.TakeDamage(currentEnemy.Damage);
                AddLog($"{currentEnemy.Name} atakuje i zadaje {currentEnemy.Damage} obrażeń!");
                UpdateStatsDisplay();
            }
        }

        public void UseItem(string? itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName) || map == null) return;
            var item = map.Inventory.FirstOrDefault(i => i.Name == itemName);
            if (item == null) return;
            map.Inventory.Remove(item); // Usuń przedmiot po użyciu
            item.Use(map.PlayerStats);
            AddLog($"Użyto: {item.Name}");
            RefreshInventory();
            UpdateStatsDisplay();
        }

        public void ShowItemEffect(string itemName)
        {
            if (map == null) return;
            var item = map.Inventory.FirstOrDefault(i => i.Name == itemName);
            if (item == null) return;
            string effectMessage = $"Efekt przedmiotu {item.Name}:";
            if (item.HealthBonus > 0) effectMessage += $" +{item.HealthBonus} Życia";
            if (item.ArmorBonus > 0) effectMessage += $" +{item.ArmorBonus} Pancerza";
            if (item.DamageBonus > 0) effectMessage += $" +{item.DamageBonus} Obrażeń";
            if (item.ChangesWeapon) effectMessage += $" Zmienia broń na {item.NewWeaponName}";
            if (item.HealthBonus == 0 && item.ArmorBonus == 0 && item.DamageBonus == 0 && !item.ChangesWeapon) effectMessage += " Brak efektu.";
            AddLog(effectMessage);
        }

        private void RefreshInventory()
        {
            inventoryComboBox.Items.Clear();
            if (map?.Inventory != null)
            {
                foreach (var item in map.Inventory)
                    inventoryComboBox.Items.Add(item.Name);
            }
        }

        private void AddLog(string message)
        {
            consoleLog.Text += $"{DateTime.Now:HH:mm:ss} - {message}\n";
        }

        private void UpdateStatsDisplay()
        {
            if (map == null) return;
            var stats = map.PlayerStats;
            healthText.Text = $"Życie: {stats.Health}";
            armorText.Text = $"Pancerz: {stats.Armor}";
            damageText.Text = $"Obrażenia: {stats.Damage}";
            goldText.Text = $"Złoto: {stats.Gold}";
            weaponText.Text = $"Broń: {stats.WeaponName}";
            floorText.Text = $"Poziom: {map.Floor}";
        }

        private void DrawMap()
        {
            if (map == null) return;
            gameCanvas.Children.Clear();
            var (px, py) = map.GetPlayerPosition();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var room = map.Rooms[x, y];
                    Rectangle rect = new Rectangle
                    {
                        Width = TileSize,
                        Height = TileSize,
                        Stroke = new SolidColorBrush(Color.FromRgb(80, 0, 0)),
                        Fill = room.Visited ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : new SolidColorBrush(Color.FromRgb(30, 30, 30))
                    };
                    if (room.Enemy != null && room.Visited)
                        rect.Fill = new SolidColorBrush(Color.FromRgb(120, 0, 0));
                    Canvas.SetLeft(rect, x * TileSize);
                    Canvas.SetTop(rect, y * TileSize);
                    gameCanvas.Children.Add(rect);
                }
            }
            Ellipse player = new Ellipse
            {
                Width = TileSize,
                Height = TileSize,
                Fill = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
            Canvas.SetLeft(player, px * TileSize);
            Canvas.SetTop(player, py * TileSize);
            gameCanvas.Children.Add(player);
        }

        private Loot GenerateLoot(int floor)
        {
            var items = new List<Loot>
            {
                new Loot
                {
                    Name = $"Krwawa Mikstura (P{floor})",
                    HealthBonus = 20 + floor * 15,
                    Use = p => p.Health += 20 + floor * 15
                },
                new Loot
                {
                    Name = $"Zbroja Cieni (P{floor})",
                    ArmorBonus = 5 + floor * 3,
                    Use = p => p.Armor += 5 + floor * 3
                },
                new Loot
                {
                    Name = $"Miecz Przeklęty (P{floor})",
                    DamageBonus = 5 + floor * 7,
                    ChangesWeapon = true,
                    NewWeaponName = $"Miecz Przeklęty (P{floor})",
                    Use = p => { p.Damage += 5 + floor * 7; p.WeaponName = $"Miecz Przeklęty (P{floor})"; }
                }
            };
            return items[new Random().Next(items.Count)];
        }
    }

    public class PlayerStats
    {
        public int Health { get; set; }
        public int Armor { get; set; }
        public int Damage { get; set; }
        public int Gold { get; set; }
        public string WeaponName { get; set; }

        public PlayerStats(int hp, int armor, int dmg, string weapon, int gold)
        {
            Health = hp;
            Armor = armor;
            Damage = dmg;
            WeaponName = weapon;
            Gold = gold;
        }

        public void TakeDamage(int dmg)
        {
            Health = Math.Max(Health - Math.Max(dmg - Armor, 0), 0);
        }
    }

    public class Enemy
    {
        public string Name { get; set; } = "Przeciwnik";
        public int Health { get; set; }
        public int Damage { get; set; }
    }

    public class Loot
    {
        public string Name { get; set; } = string.Empty;
        public int HealthBonus { get; set; }
        public int ArmorBonus { get; set; }
        public int DamageBonus { get; set; }
        public bool ChangesWeapon { get; set; }
        public string NewWeaponName { get; set; } = string.Empty;
        public Action<PlayerStats> Use { get; set; } = _ => { };
    }

    public class Room
    {
        public bool Visited = false;
        public Enemy? Enemy;
        public Loot? Loot;
        public bool IsStairs = false;
    }

    public class DisplayMap
    {
        public int Width { get; } = 10;
        public int Height { get; } = 10;
        public int Floor { get; private set; } = 1;
        public Room[,] Rooms { get; private set; }
        public PlayerStats PlayerStats { get; set; }
        public List<Loot> Inventory { get; private set; } = new();
        private (int x, int y) playerPosition;
        public Room CurrentRoom => Rooms[playerPosition.x, playerPosition.y];

        public DisplayMap(int width, int height, PlayerStats stats)
        {
            Rooms = new Room[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    Rooms[x, y] = GenerateRoom();
            playerPosition = (width / 2, height / 2);
            Rooms[playerPosition.x, playerPosition.y].Visited = true;
            Rooms[new Random().Next(width), new Random().Next(height)].IsStairs = true;
            PlayerStats = stats;
        }

        private DisplayMap(int width, int height, PlayerStats stats, int floor, (int x, int y) playerPos, Room[,] rooms, List<Loot> inventory)
        {
            Width = width;
            Height = height;
            Floor = floor;
            Rooms = rooms;
            PlayerStats = stats;
            playerPosition = playerPos;
            Inventory = inventory;
        }

        public void NextFloor()
        {
            Floor++;
            Rooms = new Room[Width, Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Rooms[x, y] = GenerateRoom();
            playerPosition = (Width / 2, Height / 2);
            Rooms[playerPosition.x, playerPosition.y].Visited = true;
            Rooms[new Random().Next(Width), new Random().Next(Height)].IsStairs = true;
        }

        private Room GenerateRoom()
        {
            var rnd = new Random();
            var room = new Room();
            if (rnd.NextDouble() < 0.2)
            {
                var enemies = new[]
                {
                    new Enemy { Name = "Cień", Health = 15 + Floor * 10, Damage = 5 + Floor * 3 },
                    new Enemy { Name = "Przeklęty Rycerz", Health = 20 + Floor * 12, Damage = 7 + Floor * 4 },
                    new Enemy { Name = "Wampir", Health = 30 + Floor * 15, Damage = 10 + Floor * 5 },
                    new Enemy { Name = "Zjawa", Health = 10 + Floor * 8, Damage = 3 + Floor * 2 }
                };
                room.Enemy = enemies[rnd.Next(enemies.Length)];
            }
            else if (rnd.NextDouble() < 0.2)
            {
                var potions = new[]
                {
                    new Loot
                    {
                        Name = $"Krwawa Mikstura (P{Floor})",
                        HealthBonus = 20 + Floor * 15,
                        Use = p => p.Health += 20 + Floor * 15
                    },
                    new Loot
                    {
                        Name = $"Zbroja Cieni (P{Floor})",
                        ArmorBonus = 5 + Floor * 3,
                        Use = p => p.Armor += 5 + Floor * 3
                    },
                    new Loot
                    {
                        Name = $"Miecz Przeklęty (P{Floor})",
                        DamageBonus = 5 + Floor * 7,
                        ChangesWeapon = true,
                        NewWeaponName = $"Miecz Przeklęty (P{Floor})",
                        Use = p => { p.Damage += 5 + Floor * 7; p.WeaponName = $"Miecz Przeklęty (P{Floor})"; }
                    }
                };
                room.Loot = potions[rnd.Next(potions.Length)];
            }
            return room;
        }

        public void MovePlayer(char dir)
        {
            var (x, y) = playerPosition;
            switch (char.ToLower(dir))
            {
                case 'w': y--; break;
                case 's': y++; break;
                case 'a': x--; break;
                case 'd': x++; break;
            }
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                playerPosition = (x, y);
        }

        public (int x, int y) GetPlayerPosition() => playerPosition;

        public void SaveToFile(string path, string consoleLogText)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(Width);
                writer.WriteLine(Height);
                writer.WriteLine(Floor);
                writer.WriteLine(playerPosition.x);
                writer.WriteLine(playerPosition.y);
                writer.WriteLine(PlayerStats.Health);
                writer.WriteLine(PlayerStats.Armor);
                writer.WriteLine(PlayerStats.Damage);
                writer.WriteLine(PlayerStats.Gold);
                writer.WriteLine(PlayerStats.WeaponName);
                writer.WriteLine(Inventory.Count);
                foreach (var item in Inventory)
                {
                    writer.WriteLine(item.Name);
                    writer.WriteLine(item.HealthBonus);
                    writer.WriteLine(item.ArmorBonus);
                    writer.WriteLine(item.DamageBonus);
                    writer.WriteLine(item.ChangesWeapon);
                    writer.WriteLine(item.NewWeaponName);
                }
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var room = Rooms[x, y];
                        writer.WriteLine(room.Visited);
                        writer.WriteLine(room.IsStairs);
                        writer.WriteLine(room.Enemy != null);
                        if (room.Enemy != null)
                        {
                            writer.WriteLine(room.Enemy.Name);
                            writer.WriteLine(room.Enemy.Health);
                            writer.WriteLine(room.Enemy.Damage);
                        }
                        writer.WriteLine(room.Loot != null);
                        if (room.Loot != null)
                        {
                            writer.WriteLine(room.Loot.Name);
                            writer.WriteLine(room.Loot.HealthBonus);
                            writer.WriteLine(room.Loot.ArmorBonus);
                            writer.WriteLine(room.Loot.DamageBonus);
                            writer.WriteLine(room.Loot.ChangesWeapon);
                            writer.WriteLine(room.Loot.NewWeaponName);
                        }
                    }
                }
                writer.WriteLine(consoleLogText);
            }
        }

        public static DisplayMap LoadFromFile(string path, TextBlock consoleLog)
        {
            try
            {
                using (var reader = new StreamReader(path))
                {
                    int width = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak szerokości mapy"));
                    int height = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak wysokości mapy"));
                    int floor = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak poziomu"));
                    int playerX = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak pozycji X gracza"));
                    int playerY = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak pozycji Y gracza"));
                    int health = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak zdrowia"));
                    int armor = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak pancerza"));
                    int damage = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak obrażeń"));
                    int gold = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak złota"));
                    string weapon = reader.ReadLine() ?? throw new InvalidDataException("Brak nazwy broni");
                    var stats = new PlayerStats(health, armor, damage, weapon, gold);
                    int inventoryCount = int.Parse(reader.ReadLine() ?? throw new InvalidDataException("Brak liczby przedmiotów"));
                    var inventory = new List<Loot>();
                    for (int i = 0; i < inventoryCount; i++)
                    {
                        string itemName = reader.ReadLine() ?? throw new InvalidDataException("Brak nazwy przedmiotu");
                        int healthBonus = int.Parse(reader.ReadLine() ?? "0");
                        int armorBonus = int.Parse(reader.ReadLine() ?? "0");
                        int damageBonus = int.Parse(reader.ReadLine() ?? "0");
                        bool changesWeapon = bool.Parse(reader.ReadLine() ?? "false");
                        string newWeaponName = reader.ReadLine() ?? "";

                        var loot = new Loot
                        {
                            Name = itemName,
                            HealthBonus = healthBonus,
                            ArmorBonus = armorBonus,
                            DamageBonus = damageBonus,
                            ChangesWeapon = changesWeapon,
                            NewWeaponName = newWeaponName
                        };

                        // Odtworzenie akcji Use
                        if (itemName.Contains("Krwawa Mikstura"))
                        {
                            loot.Use = p => p.Health += healthBonus;
                        }
                        else if (itemName.Contains("Zbroja Cieni"))
                        {
                            loot.Use = p => p.Armor += armorBonus;
                        }
                        else if (itemName.Contains("Miecz Przeklęty"))
                        {
                            loot.Use = p =>
                            {
                                p.Damage += damageBonus;
                                p.WeaponName = newWeaponName;
                            };
                        }
                        else
                        {
                            loot.Use = p => { };
                            consoleLog.Text += $"{DateTime.Now:HH:mm:ss} - Nieznany przedmiot {itemName}, ustawiam brak efektu\n";
                        }
                        inventory.Add(loot);
                    }
                    var rooms = new Room[width, height];
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var room = new Room();
                            room.Visited = bool.Parse(reader.ReadLine() ?? "false");
                            room.IsStairs = bool.Parse(reader.ReadLine() ?? "false");
                            bool hasEnemy = bool.Parse(reader.ReadLine() ?? "false");
                            if (hasEnemy)
                            {
                                room.Enemy = new Enemy
                                {
                                    Name = reader.ReadLine() ?? "Przeciwnik",
                                    Health = int.Parse(reader.ReadLine() ?? "10"),
                                    Damage = int.Parse(reader.ReadLine() ?? "5")
                                };
                            }
                            bool hasLoot = bool.Parse(reader.ReadLine() ?? "false");
                            if (hasLoot)
                            {
                                string lootName = reader.ReadLine() ?? throw new InvalidDataException("Brak nazwy przedmiotu w pokoju");
                                int healthBonus = int.Parse(reader.ReadLine() ?? "0");
                                int armorBonus = int.Parse(reader.ReadLine() ?? "0");
                                int damageBonus = int.Parse(reader.ReadLine() ?? "0");
                                bool changesWeapon = bool.Parse(reader.ReadLine() ?? "false");
                                string newWeaponName = reader.ReadLine() ?? "";

                                room.Loot = new Loot
                                {
                                    Name = lootName,
                                    HealthBonus = healthBonus,
                                    ArmorBonus = armorBonus,
                                    DamageBonus = damageBonus,
                                    ChangesWeapon = changesWeapon,
                                    NewWeaponName = newWeaponName
                                };

                                // Odtworzenie akcji Use dla przedmiotu w pokoju
                                if (lootName.Contains("Krwawa Mikstura"))
                                {
                                    room.Loot.Use = p => p.Health += healthBonus;
                                }
                                else if (lootName.Contains("Zbroja Cieni"))
                                {
                                    room.Loot.Use = p => p.Armor += armorBonus;
                                }
                                else if (lootName.Contains("Miecz Przeklęty"))
                                {
                                    room.Loot.Use = p =>
                                    {
                                        p.Damage += damageBonus;
                                        p.WeaponName = newWeaponName;
                                    };
                                }
                                else
                                {
                                    room.Loot.Use = p => { };
                                    consoleLog.Text += $"{DateTime.Now:HH:mm:ss} - Nieznany przedmiot {lootName} w pokoju, ustawiam brak efektu\n";
                                }
                            }
                            rooms[x, y] = room;
                        }
                    }
                    string logText = reader.ReadToEnd().Trim();
                    consoleLog.Text = logText; // Nadpisz log, aby uniknąć duplikacji
                    return new DisplayMap(width, height, stats, floor, (playerX, playerY), rooms, inventory);
                }
            }
            catch (Exception ex)
            {
                consoleLog.Text += $"{DateTime.Now:HH:mm:ss} - Błąd wczytywania pliku: {ex.Message}\n";
                throw;
            }
        }
    }
}