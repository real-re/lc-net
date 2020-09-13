namespace Re.LC
{
    public unsafe readonly struct LCRange
    {
        public readonly int Start;
        public readonly int End;

        public LCRange(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }

        public override string ToString()
        {
            return $"(Start: {Start}, End: {End})";
        }
    }
}
