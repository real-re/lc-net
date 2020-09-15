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

    /** LC Features
     * 1. Operator .. and ...
     *     # NOTE: Generaly used for frames of no events
     *     frames = [
     *         # Generaly Way
     *         naurto_idle_001
     *         naurto_idle_002
     *         naurto_idle_005
     *         naurto_idle_007
     *
     *         # First Way
     *         naruto_idle_{001..007}
     *
     *         # Secound Way
     *         naruto_idle_001
     *         ...
     *         naruto_idle_007
     *     ]
     *
     * 2. Tag
     *     # NOTE: Used before key of key-value
     */
}
