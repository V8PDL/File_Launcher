using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Select_and_Launch_Files_WPF
{
    /// <summary>
    /// Логика взаимодействия для Files_Options.xaml
    /// </summary>
    public partial class Files_Options : Window
    {
        public ObservableCollection<Iconed_File> Files;
        public ObservableCollection<Group> Groups;
        public Files_Options(ObservableCollection<Iconed_File> Files, ObservableCollection<Group> Groups)
        {
            InitializeComponent();
            this.Files = Files;
            this.Groups = Groups;
        }
        private void File_Editing_Window_Loaded(object sender, RoutedEventArgs e)
        {
            Files_Combobox.ItemsSource = Files;
            Groups_Combobox.ItemsSource = Groups;
        }
        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            int index = Files_Combobox.SelectedIndex;
            if (index < 0)
                return;
            var result = MessageBox.Show("Вы уверены, что хотите безвозвратно удалить файл с компьютера?",
                "Подтверждение удаления", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                Iconed_File file = Files[index];
                Icon_Image.Source = null;
                File.Delete(file.Path);
                Files.Remove(file);
            }
        }
        private void Files_Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = Files_Combobox.SelectedIndex;
            if (index < 0)
            {
                Icon_Image.Source = null;
                Name_Textbox.Clear();
                Path_Textbox.Clear();
                Groups_Combobox.SelectedIndex = -1;
            }
            else
            {
                Icon_Image.Source = Files[index].ImageSource;
                Name_Textbox.Text = Files[index].Name;
                Path_Textbox.Text = Files[index].Path;
                Groups_Combobox.SelectedIndex = Groups_Combobox.Items.IndexOf(Files[index].Group);
            }
        }
        private void Change_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Files.Any(f => f.Name == Name_Textbox.Text))
                MessageBox.Show("Имя не уникально");
            else
            {
                int index = Files_Combobox.SelectedIndex;
                if (index < 0)
                    return;
                Files[index].Name = Name_Textbox.Text;
            }
        }
        private void Change_Group_Button_Click(object sender, RoutedEventArgs e)
        {
            int index = Files_Combobox.SelectedIndex;
            if (index < 0)
                return;
            Files[index].Group = (Group)Groups_Combobox.SelectedItem;
        }
        private void Open_Button_Click(object sender, RoutedEventArgs e)
        {
            int index = Files_Combobox.SelectedIndex;
            if (index < 0)
                return;

            string path = Files[index].Path;
            if (File.Exists(path))
                System.Diagnostics.Process.Start(path);
            else
                MessageBox.Show("Файл не существует!");
        }
        private void Save_As_Button_Click(object sender, RoutedEventArgs e)
        {
            int index = Files_Combobox.SelectedIndex;
            if (index < 0)
                return;
            var dialog = new Microsoft.Win32.SaveFileDialog();
            if (dialog.ShowDialog() != true)
                return;
            string path = dialog.FileName;
            if (Files[index].Path == path)
                return;
            if (File.Exists(path))
                File.Delete(path);
            File.Move(Files[index].Path, path);
            Files[index].Path = path;
        }
    }
}
