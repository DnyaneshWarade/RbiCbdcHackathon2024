namespace BharatEpaisaApp.Database.Models
{
    internal class User
    {
        public string id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string mobileNo { get; set; }
        public string pin { get; set; }
        public string deviceId { get; set; }
        public string uniqueId { get; set; }
        public bool isAnonymousWCreated { get; set; }

        public User(string idValue, string fName, string lName, string mob, string pinValue, string uId, string devId, bool isAWCValue)
        {
            id = idValue;
            firstName = fName;
            lastName = lName;
            mobileNo = mob;
            pin = pinValue;
            uniqueId = uId;
            deviceId = devId;
            isAnonymousWCreated = isAWCValue;
        }
    }
}
