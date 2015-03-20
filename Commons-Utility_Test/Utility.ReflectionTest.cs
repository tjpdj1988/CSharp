using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utility.Reflection;
using System.Collections.Generic;

namespace Commons_Utility_Test
{
    [TestClass]
    public class ReflectionsTest
    {
        [TestMethod]
        public void GetGenericTypes()
        {
            Type[] types = Reflections.GetGenericTypes(new Orders());
            Assert.IsTrue(types.Length == 0);

            types = Reflections.GetGenericTypes(new List<string>());
            Assert.IsTrue(types.Length == 1 && types[0] == typeof(string));

            types = Reflections.GetGenericTypes(new Dictionary<string, int>());
            Assert.IsTrue(types.Length == 2 && types[1] == typeof(int));
        }

        [TestMethod]
        public void CopyPropertiesFunc()
        {
            Orders order = new Orders();
            order.Customer_ID = "999999";
            order.Freight = new decimal(10.0);
            order.Order_Date = DateTime.Now;

            Orders order1 = new Orders();

            Reflections.CopyPropertiesFunc1<Orders, Orders>()(order, order1);
            Assert.IsTrue(order1.Customer_ID == order.Customer_ID);

            order1 = new Orders();
            Reflections.CopyPropertiesFunc<Orders, Orders>()(order, order1);
            Assert.IsTrue(order1.Customer_ID == order.Customer_ID);
        }

        [TestMethod]
        public void Clone() {
            Orders order = new Orders();
            order.Customer_ID = "999999";
            order.Freight = new decimal(10.0);
            order.Order_Date = DateTime.Now;

             Orders copy =  Reflections.Clone<Orders>()(order);

             Assert.IsTrue(order.Order_Date==copy.Order_Date);
             Assert.IsFalse(order.GetHashCode() == copy.GetHashCode());        
        }
    }
}
