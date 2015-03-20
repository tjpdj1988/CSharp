using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utility.Json;
using System.Reflection;

namespace Commons_Utility_Test
{
    [TestClass]
    public class JsonTest
    {
        [TestMethod]
        public void JsonToString()
        {
            Action fun = () => Console.Write("ddddddd");
            dynamic json = new Json();
            json.ID = 10;
            json.Name = "TongJianPeng";
            json["age"] = 25;

            Assert.IsTrue("{\"ID\":10,\"Name\":\"TongJianPeng\",\"age\":25}" == json.ToString());
        }

        [TestMethod]
        public void JsonConvertTo()
        {
            dynamic json = new Json();
            json.ID = 10;
            json.Name = "TongJianPeng";
            json["age"] = 25;
            User user = (User)json;

            Assert.IsTrue(user.ID == json.ID);
            Assert.IsTrue(user.Age == 0);
        }

        [TestMethod]
        public void JsonFrom()
        {
            User user = new User();
            user.ID = 50;
            user.Name = "TJP";
            user.Age = 26;

            dynamic json = Json.JsonFrom(user);
            Assert.IsTrue(json.ID == 50);
            string jsonstring = json.ToString();
            Assert.IsTrue(jsonstring == "{\"ID\":50,\"Name\":\"TJP\",\"Age\":26}");
        }
    }
}
