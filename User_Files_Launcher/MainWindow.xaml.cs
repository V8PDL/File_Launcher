using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace User_Files_Launcher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly List<File_> Files = new List<File_>();
        static Dictionary<string, string[]> Types_Dictionary;
        static Dictionary<string, string[]>.KeyCollection Types;
        const string Types_Dictionary_Path = "Types Dictionary.txt";
        const string Files_List_Path = "Files List.txt";
        static bool File_Name_Changed = false;
        static bool Group_Changed = false;
        static Thread thread = null;
        static object locker = new object();
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Open_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Files_ComboBox.Text))
                return;
            string path = Files.Find(f => f.Name == Files_ComboBox.Text).Path;
            if (File.Exists(path))
                System.Diagnostics.Process.Start(path);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Types_Dictionary = new Dictionary<string, string[]>();
            Types = new Dictionary<string, string[]>.KeyCollection(Types_Dictionary);

            if (File.Exists(Types_Dictionary_Path))
                Parse_Dictionary(null, null);
            else
                Create_Dictionary();
            if (File.Exists(Files_List_Path))
                Parse_Files_List();
            else
            {
                using (StreamWriter sw = new StreamWriter(Files_List_Path)) ;
            }
            Types_ComboBox.ItemsSource = Types;
            Types_ComboBox_1.ItemsSource = Types;
        }
        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Path_TextBox.Text))
            {
                MessageBox.Show("Select file or directory, please");
                return;
            }
            string path;
            lock (locker)
            {
                if (Files.Any(f => f.Name == Name_Textbox.Text))
                {
                    MessageBox.Show("Enter unique name, please");
                    return;
                }
                if (Files.Any(f => f.Path == Path_TextBox.Text))
                {
                    MessageBox.Show("File already picked");
                    return;
                }
                path = Path_TextBox.Text;
            }
            //if (Directory_CheckBox.IsChecked == true)
            {
                if (!Directory.Exists(path))
                    MessageBox.Show("No such directory");
                else
                {
                    string[] extensions = new string[] { "" };
                    extensions = extensions.Concat(Extensions_To_Find_TextBox.Text.
                        Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
                    string type = Types_ComboBox.Text;
                    if (!string.IsNullOrWhiteSpace(type) && Types_Dictionary.ContainsKey(type))
                        extensions = extensions.Concat(Types_Dictionary[type]).ToArray();
                    thread = new Thread(() => Get_Files_From_Directory(path, extensions));
                    MessageBox.Show("If you've chosen large directory, maybe you'll need to wait before it ends" + Environment.NewLine
                        + "While loading, other functions will work slower, and risk of critical error is higher");
                    thread.Start();
                    Clear_Fields();
                }
                return;
            }
            if (!File.Exists(path))
            {
                MessageBox.Show("File doesn't exist. Maybe you ment directory?");
                return;
            }
            bool no_type = string.IsNullOrEmpty(Types_ComboBox.Text.ToString());
            bool no_name = string.IsNullOrWhiteSpace(Name_Textbox.Text);
            if (no_name && no_type)
                Add_File(new File_(path, Auto_Set_Type(path), Auto_Set_Name(path)));
            else
            {
                if (!no_name && !no_type)
                    Add_File(new File_(path, Types_ComboBox.Text.ToString(), Name_Textbox.Text));
                else
                {
                    if (no_name)
                        Add_File(new File_(path, Types_ComboBox.Text.ToString(), Auto_Set_Name(path)));
                    else
                        Add_File(new File_(path, Auto_Set_Type(path), Name_Textbox.Text));
                }
            }
            lock (locker)
                Files_ComboBox.ItemsSource = Files.Select(c => c.Name);
            Clear_Fields();
        }
        private void Get_Files_From_Directory(string path, string[] Extensions)
        {
            lock (locker)
                foreach (var file in Directory.GetFiles(path))
                    foreach (string ext in Extensions)
                        if (file.EndsWith(ext) || string.IsNullOrWhiteSpace(ext))
                            Add_File(new File_(file, Auto_Set_Type(file), Auto_Set_Name(file)));
            foreach (var dir in Directory.GetDirectories(path))
                Get_Files_From_Directory(dir, Extensions);
        }
        private void Create_Dictionary()
        {
            Types_Dictionary = new Dictionary<string, string[]>();

            Add_Type("Other", new string[] { "" });
            Add_Type("exe", new string[] { ".exe" });
            Add_Type("image", new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" });
            Add_Type("text", new string[] { ".txt", ".rtf", ".docx" });
            Add_Type("video", new string[] { ".mp4", ".avi", ".flv" });

            Types = new Dictionary<string, string[]>.KeyCollection(Types_Dictionary);
        }
        private void Parse_Files_List()
        {
            string error_format = ", can't upload files" + Environment.NewLine +
                "Continue without it? If no, starting from the beginning";
            lock (locker)
            {
                using (StreamReader sr = new StreamReader(Files_List_Path))
                {
                    for (int line = 1; !sr.EndOfStream; line++)
                    {
                        string[] splitted_line = sr.ReadLine().Split('|');
                        if (splitted_line.Length != 3 ||
                            !File.Exists(splitted_line[0]) ||
                            string.IsNullOrWhiteSpace(splitted_line[1]) ||
                            string.IsNullOrWhiteSpace(splitted_line[2]) ||
                            !Types_Dictionary.ContainsKey(splitted_line[2].Trim(' ')))
                        {
                            var dialog = MessageBox.Show("Wrong input at line " +
                                line.ToString() + error_format, "Wrong path", MessageBoxButton.YesNo);
                            if (dialog == MessageBoxResult.Yes)
                                continue;
                            else
                            {
                                sr.Close();
                                Parse_Files_List();
                                return;
                            }
                        }
                        if (Files.Any(f => f.Path == splitted_line[0]))
                            continue;
                        Files.Add(new File_(splitted_line[0], splitted_line[2], splitted_line[1]));
                    }
                }
                Files_ComboBox.ItemsSource = Files.Select(c => c.Name);
            }
        }
        private void Parse_Dictionary(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Types_Dictionary_Path))
                File.Create(Types_Dictionary_Path);
            using (StreamReader sr = new StreamReader(Types_Dictionary_Path))
            {
                int line = 1;
                for (; !sr.EndOfStream; line++)
                {
                    string[] splitted_line = sr.ReadLine().Split(':');
                    if (string.IsNullOrWhiteSpace(splitted_line[0]) && splitted_line.Length == 1)
                        continue;
                    if (splitted_line.Length == 2 && !string.IsNullOrWhiteSpace(splitted_line[0]))
                    {
                        if (!Types_Dictionary.ContainsKey(splitted_line[0].Trim(' ')))
                            Types_Dictionary.Add(splitted_line[0].Trim(' '),
                                splitted_line[1].Split(new char[] { ',', ' ' },
                                StringSplitOptions.RemoveEmptyEntries));
                    }
                    else
                    {
                        var dialog = MessageBox.Show("Wrong types in dictionary. Create (previous version will be REMOVED)? If no, you won't be able to use program properly",
                            "Wrong types!", MessageBoxButton.YesNo);
                        sr.Close();
                        if (dialog == MessageBoxResult.Yes)
                            Create_Dictionary();
                        else
                        {
                            MessageBox.Show("You can edit file by yourself; error in line " + line.ToString());
                            Add_Button.IsEnabled = false;
                        }
                        return;
                    }
                }
            }
            if (Types_Dictionary.Count == 0)
                Create_Dictionary();
            Add_Button.IsEnabled = true;
        }
        private void Add_File(File_ file)
        {
            bool append = File.Exists(Files_List_Path);
            if (!append && Files.Count > 1)
            {
                File.Create(Files_List_Path);
                foreach (File_ f in Files)
                    Update_File(f.Path, $"{f.Path}|{f.Name}|{f.File_type}", Files_List_Path, '|');
            }
            Files.Add(file);
            if (!Types_Dictionary.ContainsKey(file.File_type))
                Add_Type(file.File_type, Extensions_To_Find_TextBox.Text.Split(' '));
            lock (locker)
            {
                using (StreamWriter sw = new StreamWriter(Files_List_Path, append))
                    sw.WriteLine($"{file.Path}|{file.Name}|{file.File_type}");
            }
        }
        private void Add_Type(string type, string[] formats)
        {
            Types_Dictionary.Add(type, formats);
            using (StreamWriter sw = new StreamWriter(Types_Dictionary_Path, true))
            {
                sw.Write($"{type}:");
                foreach (string format in formats)
                    sw.Write($" {format}");
                sw.Write(Environment.NewLine);
            }
            //Types = Types_Dictionary.Keys;
            //Types_ComboBox_1.ItemsSource = Types_Dictionary.Keys;
        }
        private void Select_Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            var result = dialog.ShowDialog();
            if (result == true)
                Path_TextBox.Text = dialog.FileName;
        }
        private void Change_File_Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Files_ComboBox.Text.ToString()))
            {
                MessageBox.Show("Select file that you want to change");
                return;
            }
            lock (locker)
            {
                File_ file = Files.Find(f => f.Name == Files_ComboBox.Text.ToString());

                string old_path = file.Path;
                string name = New_Name_TextBox.Text;

                if (!string.IsNullOrWhiteSpace(name) &&
                    name != file.Name)
                {
                    if (Files.Any(f => f.Name == name && f.Name != file.Name))
                    {
                        MessageBox.Show("This name already exists");
                        return;
                    }
                    file.Name = New_Name_TextBox.Text;
                }
                string rename = Rename_TextBox.Text, path;
                if (!string.IsNullOrWhiteSpace(rename))
                {
                    if (Full_Path_CheckBox.IsChecked == true)
                        path = rename;
                    else
                        path = file.Path.Substring(0, file.Path.LastIndexOf('\\') + 1) + rename;
                    if (Files.Any(f => f.Path == path && f.Path != file.Path))
                    {
                        MessageBox.Show("This file already exists");
                        return;
                    }
                    if (path != file.Path)
                    {
                        File.Move(old_path, path);
                        file.Path = path;
                    }
                }
                string type = Types_ComboBox_1.Text;
                if (!string.IsNullOrEmpty(type))
                    file.File_type = type;

                File_Name_Changed = true;
                Files_ComboBox.ItemsSource = Files.Select(f => f.Name);
                Files_ComboBox.SelectedIndex = Files_ComboBox.Items.IndexOf(file.Name);
                Update_File(old_path, $"{file.Path}|{file.Name}|{file.File_type}", Files_List_Path, '|');
                Clear_Fields();
            }
        }
        private void Update_File(string first_part_from, string to, string what, char separator)
        {
            bool finded = false;
            lock (locker)
            {
                using (StreamReader sr = new StreamReader(what))
                using (StreamWriter sw = new StreamWriter(what + ".tmp"))
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.Split(separator)[0] == first_part_from && !finded)
                        {
                            finded = true;

                            if (!string.IsNullOrEmpty(to))
                                sw.WriteLine(to);
                        }
                        else
                            sw.WriteLine(line);
                    }

                File.Delete(what);
                File.Move(what + ".tmp", what);
                File.Delete(what + ".tmp");
            }
        }
        private string Auto_Set_Type(string path)
        {
            int start = path.LastIndexOf('.');
            string extension = "";
            if (start > 0)
                extension = path.Substring(start, path.Length - start);
            foreach (var pair in Types_Dictionary)
                foreach (string value in pair.Value)
                    if (extension == value)
                        return pair.Key;
            return "Other";
        }
        public string Auto_Set_Name(string path)
        {
            string file_name = path.Substring(path.LastIndexOf('\\') + 1);
            int index = file_name.LastIndexOf('.');
            string name;
            if (index > 0)
                name = file_name.Substring(0, index);
            else
                name = file_name;
            int count = Files.Count(f => f.Name == name);
            if (count == 0)
                return name;
            else
                return name + count.ToString();
        }
        private void Files_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (File_Name_Changed)
            {
                File_Name_Changed = false;
                return;
            }
            string name = !string.IsNullOrEmpty(Files_ComboBox.SelectedItem.ToString()) ? 
                Files_ComboBox.SelectedItem.ToString() : Files_ComboBox.Text;
            lock (locker)
            {
                File_ file = Files.Find(f => f.Name == name);

                Types_ComboBox_1.SelectedIndex = Types_ComboBox_1.Items.IndexOf(file.File_type);
                //Types_ComboBox_1.SelectedItem = file.File_type;

                if (Full_Path_CheckBox.IsChecked == true)
                    Rename_TextBox.Text = file.Path;
                else
                {
                    int start = file.Path.LastIndexOf('\\') + 1;
                    Rename_TextBox.Text = file.Path.Substring(start, file.Path.Length - start);
                }
                New_Name_TextBox.Text = file.Name;
            }
        }
        private void Full_Path_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            string name = Files_ComboBox.Text;
            if (string.IsNullOrWhiteSpace(name))
                return;
            lock (locker)
                Rename_TextBox.Text = Files.Find(f => f.Name == Files_ComboBox.SelectedItem.ToString()).Path;
        }
        private void Full_Path_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (locker)
            {
                File_ file = Files.Find(f => f.Name == Files_ComboBox.SelectedItem.ToString());
                int start = file.Path.LastIndexOf('\\') + 1;
                Rename_TextBox.Text = file.Path.Substring(start, file.Path.Length - start);
            }
        }
        private void Types_ComboBox_1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Group_Changed)
                Group_Changed = false;
            else
                Extensions_Of_Type.Text = string.Join(" ", Types_Dictionary[Types_ComboBox_1.SelectedItem.ToString()]);
        }
        private void Change_Group_Button_Click(object sender, RoutedEventArgs e)
        {
            string type = Types_ComboBox_1.Text;
            if (string.IsNullOrEmpty(type) || type == "Other")
            {
                MessageBox.Show("Select group to change it (but NOT 'Other')");
                return;
            }
            Types_Dictionary[type] = Extensions_Of_Type.Text.Split(' ');
            Update_File(type, $"{type}: {Extensions_Of_Type.Text} ", Types_Dictionary_Path, ':');

            //File
        }
        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            lock (locker)
            {
                File_ file = Files.Find(f => f.Name == Files_ComboBox.Text);
                Files.Remove(file);
                Update_File(file.Path, string.Empty, Files_List_Path, '|');
                Clear_Fields();
                File_Name_Changed = true;
                Files_ComboBox.ItemsSource = Files.Select(f => f.Name);
            }
        }
        private void Add_Group_Button_Click(object sender, RoutedEventArgs e)
        {
            string type = Types_ComboBox.Text;
            if (string.IsNullOrWhiteSpace(type) || Types_Dictionary.ContainsKey(type))
                MessageBox.Show("Enter unique group name");
            else
            {
                Add_Type(type, Extensions_To_Find_TextBox.Text.Split(' '));
                Refresh_Sourse();
            }
        }
        private void Delete_Group_Button_Click(object sender, RoutedEventArgs e)
        {
            string type = Types_ComboBox_1.Text;
            Types_Dictionary.Remove(type);
            lock (locker)
            {
                foreach (File_ f in Files)
                    if (f.File_type == type)
                    {
                        f.File_type = "Other";
                        Update_File(f.Path, $"{f.Path}|{f.Name}|Other", Files_List_Path, '|');
                    }
            }
            Group_Changed = true;
            Refresh_Sourse();
            Update_File(type, string.Empty, Types_Dictionary_Path, ':');
            Extensions_Of_Type.Clear();
        }
        private void Refresh_Sourse()
        {
            Types = Types_Dictionary.Keys;
            Types_ComboBox.ItemsSource = Types;
            Types_ComboBox_1.ItemsSource = Types;
        }

        private void File_Loader_Closed(object sender, EventArgs e)
        {
            if (thread != null && thread.IsAlive)
                thread.Abort();
        }
        private void Clear_Fields()
        {
            Path_TextBox.Clear();
            Name_Textbox.Clear();
            Extensions_To_Find_TextBox.Clear();
            New_Name_TextBox.Clear();
            Rename_TextBox.Clear();
            Extensions_Of_Type.Clear();
        }
        private void Delete_File_Button_Click(object sender, RoutedEventArgs e)
        {
            string name = Files_ComboBox.Text;
            if (!string.IsNullOrEmpty(name))
            {
                string path = Files.Find(f => f.Name == name).Path;
                Delete_Button_Click(null, null);
                File.Delete(path);
            }
        }
    }
}