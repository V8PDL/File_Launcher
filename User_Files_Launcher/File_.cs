namespace User_Files_Launcher
{
    class File_
    {
        public string Path { get; set; }
        public string File_type { get; set; }
        public string Name { get; set; }
        public File_(string path, string type, string name)
        {
            this.Path = path;
            File_type = type;
            this.Name = name;
        }
    }
}
