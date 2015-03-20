using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Utility.Reflection
{
    /// <summary>
    /// 
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// 打印DataSet信息
        /// </summary>
        /// <param name="ds"></param>
        public static void getList(DataSet ds)
        {
            int tables = ds.Tables.Count;
            System.Console.WriteLine("此DataSet中共有{0}个数据表", tables);

            foreach (DataTable table in ds.Tables)
            {
                Console.WriteLine("表名：{0}", table.TableName);
                Console.WriteLine("共有{0}行，{1}列", table.Rows.Count, table.Columns.Count);
                foreach (DataRow row in table.Rows)
                {
                    foreach (object obj in row.ItemArray)
                    {
                        Console.Write(Convert.ToString(obj) + "\t");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ds"></param>
        public static void AnalyzeDataSet(DataSet ds)
        {
            Console.WriteLine(ds.DataSetName);
            for (int i = 0; i < ds.Tables.Count; i++)
            {
                DataTable dt = ds.Tables[i];
                AnalyzeDataTable(dt);
            }
        }

        /// <summary>
        /// 打印DataTable信息
        /// </summary>
        /// <param name="dt"></param>
        public static void AnalyzeDataTable(DataTable dt)
        {
            Console.WriteLine(dt.TableName);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                for (int j = 0; j < dr.ItemArray.Length; j++)
                {
                    Object obj = dr.ItemArray[j];
                    Console.Write(obj.ToString());
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 打印SqlDataReader信息
        /// </summary>
        /// <param name="sdr"></param>
        public static void AnalyzeDataReader(SqlDataReader sdr)
        {
            while (sdr.Read())
            {
                for (int i = 0; i < sdr.FieldCount; i++)
                {
                    Console.Write(sdr[i]);
                }
                Console.WriteLine();
            }
            sdr.Close();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="file"></param>
        public static void DebugToHTML(IDataReader reader, string file)
        {
            StringBuilder html = new StringBuilder("<html><header>");
            html.Append("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/></header><body>");
            html.Append("<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"1\" bgcolor=\"b5d6e6\">");

            int columns = reader.FieldCount;

            while (reader.Read())
            {
                html.Append("<tr bgcolor=\"#FFFFFF\">");

                for (int i = 0; i < columns; i++)
                {
                    html.Append(string.Format("<td>{0}</td>", reader.GetValue(i)));
                }

                html.Append("</tr>");
            }
            html.Append("</table></body></html>");
            FileStream fs = new FileStream(file, FileMode.Create);
            byte[] data = new UTF8Encoding().GetBytes(html.ToString());
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
        }
        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="ds"></param> 
        /// <param name="file"></param> 
        public static void DebugToHTML(DataSet ds, string file)
        {
            StringBuilder html = new StringBuilder("<html><header>");
            html.Append("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/></header><body>");

            foreach (DataTable table in ds.Tables)
            {
                html.Append("<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"1\" bgcolor=\"b5d6e6\">");
                html.Append(string.Format("<caption>{0}{1}\\{2}</caption>", table.TableName, table.Rows.Count, table.Columns.Count));
                foreach (DataRow row in table.Rows)
                {
                    html.Append("<tr>");
                    foreach (object obj in row.ItemArray)
                    {
                        html.Append(String.Format("<td>{0}</td>", obj));
                    }
                    html.Append("</tr>");
                }
                html.Append("</table>");
            }
            html.Append("</body></html>");
            FileStream fs = new FileStream(file, FileMode.Create);
            byte[] data = new UTF8Encoding().GetBytes(html.ToString());
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
        }
    }
}
