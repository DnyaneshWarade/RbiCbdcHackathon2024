namespace BharatEpaisaApp.Database.Models
{
    public class Denomination
    {
        public string Name { get; set; }
        public string ImageName { get; set; }
        public int Value { get; set; }
        public int Quantity { get; set; }
        public int MaxLimit { get; set; }

        public Denomination(string name, string imageName, int value, int maxLimit = 0, int qty = 0)
        {
            Name = name;
            ImageName = imageName;
            Value = value;
            MaxLimit = maxLimit;
            Quantity = qty;
        }
    }
}
