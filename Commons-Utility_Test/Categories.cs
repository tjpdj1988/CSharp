//------------------------------------------------------------------------------
// <auto-generated>
//    此代码是根据模板生成的。
//
//    手动更改此文件可能会导致应用程序中发生异常行为。
//    如果重新生成代码，则将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Commons_Utility_Test
{
    using System;
    using System.Collections.Generic;
    
    public partial class Categories
    {
        public Categories()
        {
            this.Products = new HashSet<Products>();
        }
    
        public int Category_ID { get; set; }
        public string Category_Name { get; set; }
        public string Description { get; set; }
        public byte[] Picture { get; set; }
    
        public virtual ICollection<Products> Products { get; set; }
    }
}
