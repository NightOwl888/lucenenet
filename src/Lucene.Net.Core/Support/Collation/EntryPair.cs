namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// This is used for building contracting character tables.  entryName
    /// is the contracting character name and value is its collation
    /// order.
    /// </summary>
    internal sealed class EntryPair
    {
        public string EntryName { get; set; }
        public int Value { get; set; }
        public bool Fwd { get; set; }

        public EntryPair(string name, int value)
            : this(name, value, true)
        {
        }

        public EntryPair(string name, int value, bool fwd)
        {
            this.EntryName = name;
            this.Value = value;
            this.Fwd = fwd;
        }
    }
}
