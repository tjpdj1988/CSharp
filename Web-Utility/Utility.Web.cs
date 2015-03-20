namespace Utility.Web
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.UI;
    using System.Web.UI.HtmlControls;
    using System.Web.UI.WebControls;
    using Utility.Reflection;

    /// <summary>
    /// Page 扩展方法(获取页面所有匹配指定条件的控件、控件与对象实例之间的属性相互映射、页面Alert信息)
    /// </summary>
    public static class Pages
    {
        #region Page 扩展函数（表单数据封装,表单控件赋值）

        static readonly Dictionary<int, KeyValuePair<Delegate, Delegate>> MappingFunctions = new Dictionary<int, KeyValuePair<Delegate, Delegate>>(10);


        /// <summary>
        /// 表单数据赋值到对象.映射模式：WebControl.CssClass="Property@Type".eg:&lt;asp:TextBox CssClass="Password@User" ……/> 映射到User类的Password属性。
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="page">Page</param>
        /// <param name="o">对象实例</param>
        public static void ControlsToEntity<T>(this Control control, T o)
        {
            Action<Control, T> toEntity = null;

            int key = control.Page.GetType().GetHashCode() ^ control.GetType().GetHashCode() ^ o.GetType().GetHashCode();
            try
            {
                toEntity = (Action<Control, T>)MappingFunctions[key].Key;
            }
            catch (KeyNotFoundException)
            {
                CreateMappingFunctions<T>(control, o);
                toEntity = (Action<Control, T>)MappingFunctions[key].Key;
            }

            toEntity(control, o);
        }

        /// <summary>
        /// 表单数据赋值到对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="control">Page</param>
        /// <param name="o">对象实例</param>
        public static void EntityToControls<T>(this Control control, T o)
        {
            Action<Control, T> toControls = null;

            int key = control.Page.GetType().GetHashCode() ^ control.GetType().GetHashCode() ^ o.GetType().GetHashCode();
            try
            {
                toControls = (Action<Control, T>)MappingFunctions[key].Value;
            }
            catch (KeyNotFoundException)
            {
                CreateMappingFunctions<T>(control, o);
                toControls = CreateEntityToControlsDelegate<T>(GetEntityMappingControlsInfo(typeof(T), control));
            }

            toControls(control, o);
        }

        /// <summary>
        /// 构建由控件值属性赋值到映射的对象实例属性
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="mappingInfos">对象属性映射控件的信息(Dictionary&lt;对象属性, KeyValuePair&lt;控件, 控件值属性&gt;&gt;)</param>
        /// <returns>控件赋值委托</returns>
        public static Action<Control, T> CreateControlsToEntityDelegate<T>(List<MappingInfo> mappingInfos)
        {
            Type controlType = typeof(Control), targetType = typeof(T);

            ParameterExpression c = Expression.Parameter(controlType, "c");
            ParameterExpression o = Expression.Parameter(targetType, "o");

            ParameterExpression _ = Expression.Variable(controlType, "_");
            ParameterExpression _c = Expression.Variable(typeof(Converter<object, object>), "_c");
            ParameterExpression _v = Expression.Variable(typeof(object));

            List<Expression> list = new List<Expression>();

            foreach (MappingInfo item in mappingInfos)
            {
                // WebControl _ = c.FindControl(item.Source.ID);
                // Converter<object, object> _c = item.Converter();
                // object _v = _.[Value];// 获取控件值
                // _v = _c.DynamicInvoke(new[]{_v}); // 类型转换
                // o.[PropertyName](([PropertyType])_v);// 设置实体属性值
                Expression block = Expression.Block(
                    Expression.Assign(_, Expression.Call(c, controlType.GetMethod("FindControl"), Expression.Constant(item.WebControl.ID))),
                    Expression.Assign(_c, Expression.Constant(item.ToEntityPropertyValue)),
                    BlockExpression.Block(
                        new[] { _v },
                        Expression.Assign(_v,
                            Expression.Convert(
                                Expression.Call(
                                    Expression.TypeAs(_, item.WebControl.GetType()),
                                    item.WebControlProperty.GetGetMethod()
                                ),
                                typeof(object)
                            )
                        ),
                        Expression.Assign(_v,
                            Expression.Call(_c,
                                typeof(Delegate).GetMethod("DynamicInvoke"),
                                Expression.NewArrayInit(typeof(object),
                                new[] { _v })
                            )
                        ),
                        Expression.Call(o,
                            item.EntityProperty.GetSetMethod(),
                            Expression.Convert(_v, item.EntityProperty.PropertyType)
                        )
                    )
                );
                list.Add(block);
            }

            BlockExpression body = Expression.Block(new[] { _, _c, _v }, list);
            Expression<Action<Control, T>> lambda = Expression.Lambda<Action<Control, T>>(body, c, o);
            return lambda.Compile();
        }

        /// <summary>
        /// 构建由对象实例属性赋值到映射的控件值属性
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="mappingInfos">对象属性映射控件的信息(Dictionary&lt;对象属性, KeyValuePair&lt;控件, 控件值属性&gt;&gt;)</param>
        /// <returns>控件赋值委托</returns>
        public static Action<Control, T> CreateEntityToControlsDelegate<T>(List<MappingInfo> mappingInfos)
        {
            Type controlType = typeof(Control), targetType = typeof(T);

            ParameterExpression o = Expression.Parameter(targetType, "o");

            ParameterExpression c = Expression.Variable(controlType, "c");
            ParameterExpression _ = Expression.Variable(controlType, "_");
            ParameterExpression _c = Expression.Variable(typeof(Converter<object, object>), "_c");
            ParameterExpression _v = Expression.Variable(typeof(object));

            List<Expression> list = new List<Expression>();

            foreach (MappingInfo item in mappingInfos)
            {
                Expression block = Expression.Block(
                    Expression.Assign(_, Expression.Call(c, controlType.GetMethod("FindControl"), Expression.Constant(item.WebControl.ID))),
                    Expression.Assign(_c, Expression.Constant(item.ToWebControlPropertyValue)),
                    BlockExpression.Block(
                       new[] { _v },
                       Expression.Assign(_v,
                           Expression.Convert(
                           Expression.Call(o, item.EntityProperty.GetGetMethod()),
                           typeof(object)
                       )
                    ),
                    Expression.Assign(_v,
                        Expression.Call(_c,
                            typeof(Delegate).GetMethod("DynamicInvoke"),
                            Expression.NewArrayInit(typeof(object),
                                new[] { _v })
                            )
                        ),
                        Expression.Call(
                            Expression.TypeAs(_, item.WebControl.GetType()),
                            item.WebControlProperty.GetSetMethod(),
                            Expression.Convert(_v, item.WebControlProperty.PropertyType)
                        )
                    )
                );
                list.Add(block);
            }


            BlockExpression body = Expression.Block(new[] { _, _c, _v }, list);
            Expression<Action<Control, T>> lambda = Expression.Lambda<Action<Control, T>>(body, c, o);
            return lambda.Compile();
        }

        /// <summary>
        /// 获取所有匹配的子控件以及间接子控件
        /// </summary>
        /// <param name="control">容器控件</param>
        /// <param name="filter">过滤条件</param>
        /// <returns>匹配控件的枚举数</returns>
        public static IEnumerable<Control> ControlFilter(this Control control, Func<Control, bool> filter)
        {
            Queue<Control> controls = new Queue<Control>(control.Controls.Cast<Control>());

            while (controls.Count > 0)
            {
                Control child = controls.Dequeue();
                if (filter(child))
                {
                    yield return child;
                }
                else
                {
                    child.Controls.Cast<Control>().ToList().ForEach(c => controls.Enqueue(c));
                }
            }
        }
        #endregion

        #region GridView 扩展函数
        /// <summary>
        /// 启用 GridView 编辑模式
        /// </summary>
        /// <param name="grid">GridView</param>
        /// <param name="BindingData">GridView 数据绑定函数</param>
        public static void UseEditMode(this GridView grid, Action BindingData)
        {
            grid.RowEditing += (object control, GridViewEditEventArgs e) =>
            {
                grid.EditIndex = e.NewEditIndex;
                BindingData();
            };

            grid.RowCancelingEdit += (object control, GridViewCancelEditEventArgs e) =>
            {
                grid.EditIndex = -1;
                BindingData();
            };
        }
        #endregion

        /// <summary>
        /// 添加alert信息提示
        /// </summary>
        /// <param name="page">Page</param>
        /// <param name="alertInfo">信息提示</param>
        public static void Alert(this Page page, string alertInfo)
        {
            page.ClientScript.RegisterStartupScript(page.GetType(), "", string.Format("alert('{0}');", alertInfo), true);
        }

        /// <summary>
        /// 添加alert信息提示,并在确认后跳转至指定路径
        /// </summary>
        /// <param name="page">Page</param>
        /// <param name="alertInfo">信息提示</param>
        /// <param name="forword">跳转路径</param>
        public static void Alert(this Page page, string alertInfo, string forword)
        {
            page.ClientScript.RegisterStartupScript(page.GetType(), "", string.Format("alert('{0}');location.href='{1}'", alertInfo, forword), true);
        }

        #region Private

        private static readonly Func<Type, Control, List<MappingInfo>> GetEntityMappingControlsInfo = (Type @Type, Control control) =>
        {
            string pattern = @"(\w+)@" + @Type.Name;

            List<MappingInfo> mappingInfos = new List<MappingInfo>();
            IEnumerable<Control> controls = control.ControlFilter(c => c is WebControl && Regex.IsMatch((c as WebControl).CssClass, pattern));
            foreach (Control c in controls)
            {
                WebControl webControl = c as WebControl;
                string propertyName = Regex.Match(webControl.CssClass, pattern).Groups[1].Value;
                PropertyInfo instanceProperty = @Type.GetProperty(propertyName);
                PropertyInfo controlProperty = null;
                switch (webControl.GetType().Name)
                {
                    case "RadioButtonList":
                    case "DropDownList":
                    case "ListBox":
                        controlProperty = typeof(ListControl).GetProperty("SelectedValue");
                        break;
                    case "Calendar":
                        controlProperty = typeof(Calendar).GetProperty("SelectedDate");
                        break;
                    case "RadioButton":
                    case "CheckBox":
                        controlProperty = typeof(CheckBox).GetProperty("Checked");
                        break;
                    case "TextBox":
                        controlProperty = typeof(TextBox).GetProperty("Text");
                        break;
                    default:
                        controlProperty = typeof(HiddenField).GetProperty("Value");
                        break;
                }
                if (instanceProperty.PropertyType.IsEnum)
                {
                    mappingInfos.Add(
                        new MappingInfo(webControl, @Type, controlProperty, instanceProperty,
                            (object o) => (int)Convert.ChangeType(o, typeof(int)),
                            (object o) => Convert.ChangeType((int)o, typeof(string))
                        )
                    );
                }
                else
                {
                    mappingInfos.Add(new MappingInfo(webControl, @Type, controlProperty, instanceProperty));
                }
            }
            return mappingInfos;
        };

        /// <summary>
        /// 创建映射函数并缓存
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="control">Control</param>
        /// <param name="o">实体对象</param>
        private static void CreateMappingFunctions<T>(Control control, T o)
        {
            lock (control.Page.GetType())
            {
                List<MappingInfo> mappings = GetEntityMappingControlsInfo(typeof(T), control);
                Delegate controlToEntity = CreateControlsToEntityDelegate<T>(mappings);// key
                Delegate entityToControl = CreateEntityToControlsDelegate<T>(mappings);// value
                int key = control.Page.GetType().GetHashCode() ^ control.GetType().GetHashCode() ^ o.GetType().GetHashCode();
                MappingFunctions[key] = new KeyValuePair<Delegate, Delegate>(controlToEntity, entityToControl);
            }
        }

        #endregion
    }


    /// <summary>
    /// Form WebControl 值属性和实体对象属性之间间的映射信息
    /// </summary>
    public class MappingInfo
    {
        private WebControl webControl;
        private Type entityType;
        private PropertyInfo webControlProperty;
        private PropertyInfo entityProperty;
        private Converter<object, object> toEntityPropertyValue;
        private Converter<object, object> toWebControlPropertyValue;

        /// <summary>
        /// 构造函数。映射值默认使用Convert.ChangeType()方法进行转换
        /// </summary>
        /// <param name="webControl">WebControl</param>
        /// <param name="entityType">映射的实体类</param>
        /// <param name="webControlProperty">WebControl 值属性</param>
        /// <param name="entityProperty">映射的实体属性</param>
        public MappingInfo(WebControl webControl, Type entityType, PropertyInfo webControlProperty, PropertyInfo entityProperty)
            : this(webControl, entityType, webControlProperty, entityProperty, 
                (object value) => Convert.ChangeType(value, entityProperty.PropertyType),
                (object value) => Convert.ChangeType(value, webControlProperty.PropertyType)
            )
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="webControl">WebControl</param>
        /// <param name="entityType">映射的实体类</param>
        /// <param name="webControlProperty">WebControl 值属性</param>
        /// <param name="entityProperty">映射的实体属性</param>
        /// <param name="toEntityPropertyValue">WebControl值转换成实体属性类型的函数</param>
        /// <param name="toWebControlPropertyValue">实体属性值转换成WebControl值属性类型的函数</param>
        public MappingInfo(WebControl webControl, Type entityType, PropertyInfo webControlProperty, PropertyInfo entityProperty, Converter<object, object> toEntityPropertyValue, Converter<object, object> toWebControlPropertyValue)
        {
            this.webControl = webControl;
            this.entityType = entityType;
            this.webControlProperty = webControlProperty;
            this.entityProperty = entityProperty;
            this.toEntityPropertyValue = toEntityPropertyValue;
            this.toWebControlPropertyValue = toWebControlPropertyValue;
        }

        /// <summary>
        /// Form WebControl对象(只读)
        /// </summary>
        public WebControl WebControl
        {
            get { return webControl; }
        }

        /// <summary>
        /// 实体对象类型(只读)
        /// </summary>
        public Type EntityType
        {
            get { return entityType; }
        }

        /// <summary>
        /// WebControl 值属性(只读)
        /// </summary>
        public PropertyInfo WebControlProperty
        {
            get { return webControlProperty; }
        }

        /// <summary>
        /// 实体对象属性
        /// </summary>
        public PropertyInfo EntityProperty
        {
            get { return entityProperty; }
        }

        /// <summary>
        /// WebControl 值属性的值转换成实体对象属性值的转换函数
        /// </summary>
        public Converter<object, object> ToEntityPropertyValue
        {
            get { return toEntityPropertyValue; }
        }

        /// <summary>
        /// 实体对象属性值转换成WebControl 值属性的值的转换函数
        /// </summary>
        public Converter<object, object> ToWebControlPropertyValue
        {
            get { return toWebControlPropertyValue; }
        }

    }
}