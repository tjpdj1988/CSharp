using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Utility.Models;
using Utility.Web;
using System.Reflection;
namespace Utility
{
    public partial class _default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            List<MethodInfo> methods = typeof(WebControl).GetRuntimeMethods().ToList();//("SetMappingValue", new[] { typeof(WebControl), typeof(object) });

            methods.ForEach(o => Literal1.Text += o.Name);
            if (!IsPostBack)
            {
                User user = new User();
                user.Name = "tongJianpeng";
                user.Age = 25;
                user.Birthday = DateTime.Now.AddDays(-5);
                user.LoginTime = DateTime.Now.AddDays(5);
                user.Remember = true;
                user.sex = Sex.男;
                this.EntityToControls<User>(user);
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            User user = new User();
            Page.ControlsToEntity<User>(user);
            user.ToString();
            
        }
    }
}