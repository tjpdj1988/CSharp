using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using Utility.Reflection;
using System.Data;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;
using System.Data.Common;
using System.Text;
using Utility.Data;
using System.Data.OleDb;

namespace Commons_Utility_Test
{
    [TestClass]
    public class DataTest
    {
        #region Test DbUtils
        [TestMethod]
        public void GetIDbConnection()
        {
            DbConnection conn = (DbConnection)DbUtils.GetIDbConnection();
            Assert.IsNotNull(conn);
            conn.Close();

            conn = DbUtils.GetIDbConnection<SqlCeConnection>("Northwind");
            Assert.IsNotNull(conn);
            conn.Close();
        }
       
        [TestMethod]
        public void ToObjectAndToEntity()
        {
            DbConnection conn = DbUtils.GetIDbConnection<SqlCeConnection>("Northwind");

            string sql = "select * from Orders where ([Order ID]=@OrderID)";

            // ToObject(IDataRecord)、ToObject(IDataRecord,Func<IDataRecord,T> toObject)
            Orders order = DbUtils.Execute<Orders>(conn, (cmd) =>
            {
                DbUtils.PreparedIDbCommand(cmd, sql, new SqlCeParameter("OrderID", 10000));

                IDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return DbUtils.ToObject<Orders>(reader);
                }
                else
                {
                    return null;
                }
            });
            Assert.IsTrue(order.Order_ID == 10000);
            order = null;
            //ToEntity<T>(IDatarecord)
            order = DbUtils.Execute<Orders>(conn, (IDbCommand cmd) =>
            {
                DbUtils.PreparedIDbCommand(cmd, sql, new SqlCeParameter("OrderID", 10000));

                IDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    order = DbUtils.ToEntity<Orders>(reader)(reader);
                }
                return order;
            });
            Assert.IsTrue(order.Order_ID == 10000);

            order = null;
            order = DbUtils.Execute<Orders>(conn, (IDbCommand cmd) =>
            {
                DbUtils.PreparedIDbCommand(cmd, sql, new SqlCeParameter("OrderID", 10000));

                IDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    order = DbUtils.ToEntity<Orders>()(reader);
                }
                return order;
            });
            Assert.IsTrue(order.Order_ID == 10000);
            conn.Close();
        }
        #endregion

        #region LocalDb
        [TestMethod]
        public void GetConnectionStringSettingsOfCSV()
        {
            string file = @".\";
            DbUtils.RegisterConnectionStrings(LocalDb.GetConnectionStringSettingsOfCSV(file));
            IDbConnection csv = DbUtils.GetIDbConnection<IDbConnection>(file);
            Assert.IsNotNull(csv);
            csv.Close();
            DbUtils.RemoveConnectionStrings(file);
        }

        [TestMethod]
        public void GetConnectionStringSettingsOfSDF() {
            string file = @"Northwind.sdf";
            DbUtils.RegisterConnectionStrings(LocalDb.GetConnectionStringSettingsOfSDF(file));
            IDbConnection sdf = DbUtils.GetIDbConnection<SqlCeConnection>(file);
            Assert.IsNotNull(sdf);
            sdf.Close();
        }

        [TestMethod]
        public void GetConnectionStringSettingsOfXLS()
        {
            string file = "express.xls";
            DbUtils.RegisterConnectionStrings(LocalDb.GetConnectionStringSettingsOfXLS(file));
            IDbConnection xls = DbUtils.GetIDbConnection<IDbConnection>(file);
            Assert.IsNotNull(xls);
            xls.Close();
        }

        public void GetConnectionStringSettingsOfMDB() {
            string file = "";
            DbUtils.RegisterConnectionStrings(LocalDb.GetConnectionStringSettingsOfMDB(file));
            IDbConnection mdb = DbUtils.GetIDbConnection<IDbConnection>(file);
            Assert.IsNotNull(mdb);
            mdb.Close();
        }

        [TestMethod]
        public void Load() {
            string file = @".\";
            DbUtils.RegisterConnectionStrings(LocalDb.GetConnectionStringSettingsOfCSV(file));
            DbConnection csv = DbUtils.GetIDbConnection<DbConnection>(file);
            DataTable table = LocalDb.Load((OleDbConnection)csv,"express#csv");
            Assert.IsTrue(table.Rows.Count == 60);
            csv.Close();
        }
        #endregion
    }
}
