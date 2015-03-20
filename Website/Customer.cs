using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Utility
{
    public class Customer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
    }
    public static class dd
    {
        public static IEnumerable<WebControl> FormValueControls(this Control control)
        {
            Queue<Control> WebControls = new Queue<Control>(control.Controls.Cast<Control>());
            while (WebControls.Count > 0)
            {
                Control child = WebControls.Dequeue();
                if (child is WebControl && child is IPostBackDataHandler && Regex.IsMatch((child as WebControl).CssClass, @"(\w+)@Customer"))
                {
                    yield return child as WebControl;
                }
                else
                {
                    child.Controls.Cast<Control>().ToList().ForEach(c => WebControls.Enqueue(c));
                }
            }

        }
    }
}
