using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TekstowyLoch
{
    public class DisplayMap
    {
        private int width;
        private int height;
        private bool[,] visitedRooms;
        private (int x, int y) playerPosition;

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
            switch (direction)
            {
                case 'w': UpdatePlayerPosition(x, y - 1); break;
                case 's': UpdatePlayerPosition(x, y + 1); break;
                case 'a': UpdatePlayerPosition(x - 1, y); break;
                case 'd': UpdatePlayerPosition(x + 1, y); break;
            }
        }

        public void MarkRoomAsVisited(int x, int y)
        {
            visitedRooms[x, y] = true;
        }

        public bool IsRoomVisited(int x, int y)
        {
            return visitedRooms[x, y];
        }

        public (int x, int y) GetPlayerPosition()
        {
            return playerPosition;
        }
    }

    public partial class MainWindow : Window
    {
        private int tileSize = 30;
        private DisplayMap map;

        public MainWindow()
        {
            InitializeComponent();
            map = new DisplayMap(10, 10);
            DrawMap();
            this.KeyDown += OnKeyDown;
        }

        private void DrawMap()
        {
            gameCanvas.Children.Clear();
            var (playerX, playerY) = map.GetPlayerPosition();

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    Rectangle room = new Rectangle
                    {
                        Width = tileSize,
                        Height = tileSize,
                        Stroke = Brushes.Black,
                        Fill = map.IsRoomVisited(x, y) ? Brushes.Gray : Brushes.White
                    };
                    Canvas.SetLeft(room, x * tileSize);
                    Canvas.SetTop(room, y * tileSize);
                    gameCanvas.Children.Add(room);
                }
            }

            Ellipse player = new Ellipse
            {
                Width = tileSize,
                Height = tileSize,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(player, playerX * tileSize);
            Canvas.SetTop(player, playerY * tileSize);
            gameCanvas.Children.Add(player);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            char direction = e.Key switch
            {
                Key.W => 'w',
                Key.S => 's',
                Key.A => 'a',
                Key.D => 'd',
                _ => '\0'
            };

            if (direction != '\0')
            {
                map.MovePlayer(direction);
                DrawMap();
            }
        }

        private void OnClick1(object sender, RoutedEventArgs e)
        {
            btn1.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void OnClick2(object sender, RoutedEventArgs e)
        {
            btn2.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}