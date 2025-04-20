using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TekstowyLoch
{
    public partial class MainWindow : Window
    {
        private readonly GameLogic gameLogic;

        public MainWindow()
        {
            InitializeComponent();
            gameLogic = new GameLogic(gameCanvas, healthText, armorText, damageText, goldText, weaponText, floorText, consoleLog, inventoryComboBox, combatPanel, inventoryPanel);
        }

        private void ShowGamePanels()
        {
            newGameButton.Visibility = Visibility.Collapsed;
            loadGameButton.Visibility = Visibility.Visible;
            saveGameButton.Visibility = Visibility.Visible;
            mapPanel.Visibility = Visibility.Visible;
            statsPanel.Visibility = Visibility.Visible;
            combatPanel.Visibility = Visibility.Collapsed;
            inventoryPanel.Visibility = Visibility.Visible;
            consoleScroll.Visibility = Visibility.Visible;
        }

        private void OnClick1(object sender, RoutedEventArgs e)
        {
            ShowGamePanels();
            gameLogic.StartNewGame();
        }

        private void OnClickLoadGame(object sender, RoutedEventArgs e)
        {
            try
            {
                gameLogic.LoadGame();
                ShowGamePanels();
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

        private void SaveGame(object sender, RoutedEventArgs e)
        {
            try
            {
                gameLogic.SaveGame();
                MessageBox.Show("Gra zapisana!", "Zapis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AttackEnemy(object sender, RoutedEventArgs e)
        {
            gameLogic.PlayerAttack();
        }

        private void RunFromEnemy(object sender, RoutedEventArgs e)
        {
            gameLogic.TryToEscape();
        }

        private void UseSelectedItem(object sender, RoutedEventArgs e)
        {
            if (inventoryComboBox.SelectedItem != null)
                gameLogic.UseItem(inventoryComboBox.SelectedItem.ToString());
        }

        private void InventoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inventoryComboBox.SelectedItem != null)
                gameLogic.ShowItemEffect(inventoryComboBox.SelectedItem.ToString());
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            char direction = e.Key switch
            {
                Key.W or Key.Up => 'w',
                Key.S or Key.Down => 's',
                Key.A or Key.Left => 'a',
                Key.D or Key.Right => 'd',
                _ => '\0'
            };
            if (direction != '\0')
                gameLogic.MovePlayer(direction);
        }
    }
}