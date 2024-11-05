using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BharatEpaisaApp.Database.Models
{
    internal class User
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string mobileNo { get; set; }
        public string pin { get; set; }
        public string deviceId { get; set; }
        public string cloudMsgToken { get; set; }
        public string publicKey { get; set; }
        public bool isActive { get; set; }

        public User(string fName, string lName, string mob, string pinValue, string devId, string token, string key, bool isActiveValue)
        {
            firstName = fName;
            lastName = lName;
            mobileNo = mob;
            pin = pinValue;
            deviceId = devId;
            cloudMsgToken = token;
            publicKey = key;
            isActive = isActiveValue;
        }
    }
}
