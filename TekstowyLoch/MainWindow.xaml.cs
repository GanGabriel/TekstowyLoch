using System;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;

namespace TekstowyLoch
{
    public partial class MainWindow : Window
    {
        private GameLogic gameLogic;

        public MainWindow()
        {
            InitializeComponent();
            gameLogic = new GameLogic(gameCanvas, healthText, armorText);
        }

        private void OnClick1(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void StartNewGame()
        {
            menuPanel.Visibility = Visibility.Collapsed;
            movementPanel.Visibility = Visibility.Visible;
            gameMenuPanel.Visibility = Visibility.Visible;
            statsPanel.Visibility = Visibility.Visible;
            gameLogic.StartNewGame();
        }

        private void OnClickLoadGame(object sender, RoutedEventArgs e)
        {
            LoadGame();
        }

        private void LoadGameInGame(object sender, RoutedEventArgs e)
        {
            LoadGame();
        }

        private void LoadGame()
        {
            try
            {
                gameLogic.LoadGame();
                menuPanel.Visibility = Visibility.Collapsed;
                movementPanel.Visibility = Visibility.Visible;
                gameMenuPanel.Visibility = Visibility.Visible;
                statsPanel.Visibility = Visibility.Visible;
                MessageBox.Show("Gra została wczytana!", "Wczytywanie Gry", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wczytywania gry: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MovePlayerUp(object sender, RoutedEventArgs e) => gameLogic.MovePlayer('w');
        private void MovePlayerDown(object sender, RoutedEventArgs e) => gameLogic.MovePlayer('s');
        private void MovePlayerLeft(object sender, RoutedEventArgs e) => gameLogic.MovePlayer('a');
        private void MovePlayerRight(object sender, RoutedEventArgs e) => gameLogic.MovePlayer('d');

        private void SaveGame(object sender, RoutedEventArgs e)
        {
            try
            {
                gameLogic.SaveGame();
                string savePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TekstowyLoch_Save.txt");
                MessageBox.Show($"Gra zapisana w: {savePath}", "Zapis Gry", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisywania gry: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}