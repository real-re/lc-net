namespace Re.LC
{
    public struct LC
    {
        public string Name { get; private set; }
        public string[] Lines { get; private set; }

        public LC(string name, string[] lines)
        {
            Name = name;
            Lines = lines;
        }

        public void SaveTo(string path)
        {
        }
    }
}
