using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Utility.Data;
using Utility.Models;
using Utility.Web;

namespace Utility
{
    public partial class GridView : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            GridView1.UseEditMode(() => getInfo());
            if (!IsPostBack)
                GridView1.EditIndex = 1;
                getInfo();
        }
        
        protected void getInfo()
        {
            DataTable dt = new DataTable();//new CustomerDAO().getALl();
            SqlCeConnection conn = (SqlCeConnection)DbUtils.GetIDbConnection();
            conn.Execute<DataTable>((IDbCommand cmd) =>
            {
                cmd.CommandText = "select * from Customers";
                SqlCeDataAdapter adapter = new SqlCeDataAdapter(cmd as SqlCeCommand);
                adapter.Fill(dt);
                return dt;
            });

            this.GridView1.DataSource = dt;
            this.GridView1.DataBind();

        }

        protected void GridView1_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            Dictionary<PropertyInfo, PropertyInfo> MappingInfo = new Dictionary<PropertyInfo, PropertyInfo>();
            List<WebControl> list = new List<WebControl>();
            foreach (WebControl c in GridView1.FormValueControls())
            {
                list.Add(c);
            }

            
            //1获得更新行的索引。
            int i = e.RowIndex;
            //2 获取更新行的主键。
            string customerId = Convert.ToString(this.GridView1.DataKeys[i].Value);
            //2 获得修改后的内容、
            string contactName = string.Empty;
            var row = GridView1.Rows[i].Controls;
            //row.AsQueryable();
            //TextBox tbox = (TextBox)GridView1.Rows[i].Cells[1].Controls[0]; 
            //if (tbox != null)
            //{
            //    contactName = tbox.Text;
            //}

            //3 update aaaa set name='asdf' where id=111


            //4


        }
       

    }
}
