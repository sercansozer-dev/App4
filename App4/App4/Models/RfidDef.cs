namespace App4
{
    public enum RfidOperationMode
    {
        Mixed,
        Specific
    }

    public class RfidDef
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public override string ToString() => $"{Id} ({Description})";
    }
}
