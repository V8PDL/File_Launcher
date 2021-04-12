using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Select_and_Launch_Files_WPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public class Iconed_File
    {
        public ImageSource ImageSource { get; set; }
        public string Path { get; set; }
        public Group Group { get; set; }
        public string Name { get; set; }
        public Iconed_File(ImageSource imgsrc, string path, Group group, string name)
        {
            ImageSource = imgsrc;
            Path = path;
            Group = group;
            Name = name;
        }
    }
    public class Group
    {
        public string Name { get; set; }
        public List<string> Extensions { get; set; }
        public Group(string name, List<string> ext)
        {
            Name = name;
            Extensions = ext;
        }
    }
    public partial class MainWindow : Window
    {

        ObservableCollection<Group> Groups;
        ObservableCollection<Iconed_File> Files;
        Thread thread;

        public MainWindow()
        {
            InitializeComponent();
            Groups = new ObservableCollection<Group>
            {
                new Group("Другое", new List<string>()),
                new Group("Исполняемый", new List<string>() { ".exe" }),
                new Group("Изображение", new List<string>()
            {
                ".png",
                ".jpg",
                ".bmp",
                ".jpeg",
                ".gif"
            }),
                new Group("Видео", new List<string>()
            { ".avi",
              ".3gp",
              ".mp4"
            }),
                new Group("Текст", new List<string>()
            {
                ".txt",
                ".docx",
                ".doc",
                ".rtf"
            })
            };
        }
        static System.Timers.Timer timer;
        private void Add_Files_Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true
            };
            int missing = 0;
            if (dialog.ShowDialog() != true)
                return;
            string[] filenames = dialog.FileNames;
            foreach (string path in filenames)
            {
                if (!File.Exists(path))
                    continue;
                if (Files.Any(f => f.Path == path))
                    missing++;
                else
                    Add_File(path, filenames.Length > 1);
            }
            if (missing > 0)
                MessageBox.Show($"{missing} файлов не было добавлено, поскольку уже существуют");
            Name_Textbox.Clear();
        }
        private void Add_File(string path, bool Multiselected)
        {
            lock (Files)
            {
                string TB_name = "";
                if (!Multiselected)
                    TB_name = Name_Textbox.Text;

                ImageSource imgsrc;
                using (System.Drawing.Bitmap bitmap = System.Drawing.Icon.ExtractAssociatedIcon(path).ToBitmap())
                {
                    var stream = new MemoryStream();
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    imgsrc = BitmapFrame.Create(stream);
                }
                string name;
                if (string.IsNullOrWhiteSpace(TB_name))
                    name = Set_Name(path);
                else
                    name = Set_Name(TB_name.Replace('.', '|')).Replace('|', '.');
                Group g;
                if (Multiselected)
                    g = Set_Group(path);
                else
                {
                    g = Get_Group();
                    if (g == null)
                    {
                        var result = MessageBox.Show($"Для файла {name} не выбрана группа. Установить автоматически?",
                            "Группа не найдена", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                            g = Set_Group(path);
                        else
                            return;
                    }
                }
                Dispatcher.BeginInvoke((Action)(() => Files.Add(new Iconed_File(imgsrc, path, g, name))));
            }
        }
        private string Set_Name(string path)
        {
            lock (Files)
            {
                string file_name = path.Substring(path.LastIndexOf('\\') + 1);
                int index = file_name.LastIndexOf('.');
                string name;
                if (index > 0)
                    name = file_name.Substring(0, index);
                else
                    name = file_name;
                int count = Files.ToList().Count(f => f.Name == name);
                if (count == 0)
                    return name;
                else
                    return name + count.ToString();
            }
        }
        private Group Set_Group(string path)
        {
            int start = path.LastIndexOf('.');
            string extension = "";
            if (start > 0)
                extension = path.Substring(start, path.Length - start);
            foreach (var g in Groups)
                foreach (string ext in g.Extensions)
                    if (extension == ext)
                        return g;
            return Groups[0];
        }
        private void Add_Group_Button_Click(object sender, RoutedEventArgs e)
        {
            Group g = Get_Group();
            string groupname = Groups_Textbox.Text;
            if (string.IsNullOrWhiteSpace(groupname))
            {
                MessageBox.Show("Введите, пожалуйста, название группы");
                return;
            }
            List<string> ext = Extensions_Textbox.Text.Split(' ').ToList();
            if (g != null)
                if (g.Name != "Other")
                    g.Extensions = ext;
                else
                    MessageBox.Show("Группу 'Другое' нельзя менять и удалять");
            else
            {
                Groups.Add(new Group(groupname, ext));
                Groups_Textbox.Clear();
                Extensions_Textbox.Clear();
            }
        }
        private void Go_Next_Button_Click(object sender, RoutedEventArgs e)
        {
            Files_Options files_Options = new Files_Options(Files, Groups);
            files_Options.ShowDialog();
        }
        private void Remove_File_Click(object sender, RoutedEventArgs e)
        {
            int index = Files_Listbox.SelectedIndex;
            if (index > -1)
                lock (Files)
                    Files.RemoveAt(index);
        }
        private void Add_Directory_Button_Click(object sender, RoutedEventArgs e)
        {
            string[] groups = Groups_Textbox.Text.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            List<string> filters = new List<string>();
            foreach (string gname in groups)
            {
                Group group = Groups.ToList().Find(g => g.Name == gname);
                if (group != null)
                    filters.AddRange(group.Extensions);
            }
            filters.AddRange(Extensions_Textbox.Text.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries).ToList());
            string path = Get_Directory();
            if (path == null)
                return;
            thread = new Thread(() => Files_From_Dir(path, filters));
            Go_Next_Button.IsEnabled = false;
            timer.Start();
            thread.Start();
        }
        private void Files_From_Dir(string path, List<string> filters)
        {
            var all_files = Directory.GetFiles(path);
            List<string> files = new List<string>();
            if (!filters.Any())
                filters.Add("");
            lock (Files)
                foreach (var file in all_files)
                    if (filters.Any(f => file.EndsWith(f)) &&
                        !Files.ToList().Any(f => file == f.Path))
                        files.Add(file);

            foreach (var file in files)
                Add_File(file, true);

            foreach (var dir in Directory.GetDirectories(path))
                Files_From_Dir(dir, filters);
        }
        private static string Get_Directory()
        {
            // Create a "Save As" dialog for selecting a directory 
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Выберите папку", // instead of default "Save As"
                Filter = "Directory|*.this.directory", // Prevents displaying files
                FileName = "PICK_FOLDER" // Filename will then be "PICK_FOLDER.this.directory"
            };
            if (dialog.ShowDialog() != true)
                return null;

            string path = dialog.FileName;
            // Remove fake filename from resulting path
            if (path.EndsWith("PICK_FOLDER.this.directory.this.directory"))
            {
                path = path.Substring(0, path.LastIndexOf('\\'));
            }
            if (!System.IO.Directory.Exists(path))
            {
                MessageBox.Show("Неверный путь");
                return null;
            }
            // Our final value is in path
            return path;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Files = new ObservableCollection<Iconed_File>();
            Files_Listbox.ItemsSource = Files;
            timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 1000;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (thread != null && thread.IsAlive)
            {
                switch (timer.Interval)
                {
                    case 1000:
                        timer.Interval = 3000;
                        break;
                    case 3000:
                        timer.Interval = 5000;
                        break;
                    case 5000:
                        timer.Interval = 10000;
                        break;
                    case 10000:
                        timer.Interval = 15000;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                timer.Stop();
                Dispatcher.Invoke((Action)(() =>
                {
                    Go_Next_Button.IsEnabled = true;
                    if (timer.Interval > 5000)
                        MessageBox.Show("Файлы добавлены!");
                }));
            }
        }

        private void Delete_Group_Button_Click(object sender, RoutedEventArgs e)
        {
            Group g = Get_Group();
            if (g != null && g.Name != "Other")
                Groups.Remove(g);
            else
            {
                MessageBox.Show("Группа не существует");
                return;
            }
            Groups_Textbox.Clear();
            Extensions_Textbox.Clear();
        }
        private Group Get_Group()
        {
            string groupname = Groups_Textbox.Text;
            Group group;
            if (string.IsNullOrWhiteSpace(groupname))
                return null;
            else
            {
                if (groupname.Split(' ').Length != 1)
                    return null;
                else
                {
                    group = Groups.ToList().Find(g => g.Name == groupname);
                    if (group == null)
                        return null;
                    else
                        return group;
                }
            }
        }
        private void Main_Window_Closed(object sender, EventArgs e)
        {
            if (thread != null && thread.IsAlive)
                thread.Abort();
        }

        private void Show_Groups_Button_Click(object sender, RoutedEventArgs e)
        {
            string output = "";
            foreach (var g in Groups)
                output += g.Name + ": " + string.Join(" ", g.Extensions) + Environment.NewLine;
            MessageBox.Show(output.Remove(output.LastIndexOf(Environment.NewLine)));
        }

        private void Groups_Textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Group g = Get_Group();
                if (g != null)
                    Extensions_Textbox.Text = string.Join(" ", g.Extensions);
            }
        }
    }
}
