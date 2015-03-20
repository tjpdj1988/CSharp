
namespace Utility.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// 反射相关方法:
    /// </summary>
    public static class Reflections
    {
        /// <summary>
        /// 获取实例对象的泛型类型参数列表
        /// </summary>
        /// <param name="o">实例对象</param>
        /// <returns>泛型参数的类型</returns>
        public static Type[] GetGenericTypes(object o)
        {
            return o.GetType().GetGenericArguments();
        }

        /// <summary>
        /// 获取当前应用程序域中指定接口的实现类
        /// </summary>
        /// <param name="interfaceName">接口名称</param>
        /// <returns>实现接口的类型列表</returns>
        public static List<Type> GetInterfaceImplementation(string interfaceName)
        {
            List<Type> list = new List<Type>();
            // 获取当前应用程序域所加载的所有程序集
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    // 查找类实现的接口中是否包含指定接口
                    if (type.FindInterfaces(delegate(Type _type, object _o) { return _type.ToString() == _o.ToString(); }, interfaceName).Length > 0)
                    {
                        list.Add(type);
                    }
                }
            }
            return list;
        }

        /// <summary>
        ///  从一个实例中复制字段值到另一个实例中,复制字段值时忽略目标实例的类型且仅复制同名字段.(反射实现)
        /// </summary>
        /// <param name="from">提供字段值的对象</param>
        /// <param name="to">被赋予字段值的对象</param>
        public static void CopyFields(Object from, Object to)
        {
            Type t1 = from.GetType();
            Type t2 = to.GetType();

            if (t1.IsClass && t2.IsClass)
            {
                foreach (FieldInfo fi1 in t1.GetFields())
                {
                    FieldInfo fi2 = t2.GetField(fi1.Name);
                    if ((fi2 != null) && (fi1.FieldType == fi2.FieldType) && (!fi2.IsInitOnly))
                        fi2.SetValue(to, fi1.GetValue(from));
                }
            }
        }

        /// <summary>
        /// 从一个实例中复制属性值到另一个实例中,复制属性值时忽略目标实例的类型且仅复制同名属性.
        /// </summary>
        /// <param name="from">提供属性值的对象</param>
        /// <param name="to">被赋予属性值的对象</param>
        public static void CopyPrperties(Object from, Object to)
        {
            Type t1 = from.GetType();
            Type t2 = to.GetType();

            if (t1.IsClass && t2.IsClass)
            {
                foreach (PropertyInfo pi1 in t1.GetProperties())
                {
                    PropertyInfo pi2 = t2.GetProperty(pi1.Name);
                    if (pi1.CanRead && pi2.CanWrite && (pi2 != null) && (pi1.PropertyType == pi2.PropertyType))
                        pi2.SetValue(to, pi1.GetValue(from, null), null);
                }
            }
        }

        /// <summary>
        /// 同类型之间( S, T => T : S)从实例source 复制属性值到实例target.eg:
        /// <code>
        /// Action&lt;User, Student&gt; CopyProperties = Reflections.CopyPropertiesFunc&lt;User, Student&gt;();
        /// CopyProperties(user, student);
        /// </code>
        /// </summary>
        /// <typeparam name="S">参数类型1</typeparam>
        /// <typeparam name="T">参数类型1或继承自参数类型1,即 T is S</typeparam>
        public static Action<S, T> CopyPropertiesFunc1<S, T>() where T : S
        {
            Type sourceType = typeof(S), targetType = typeof(T);

            ParameterExpression s = Expression.Parameter(sourceType, "s");
            ParameterExpression t = Expression.Parameter(targetType, "t");

            List<Expression> assigns = new List<Expression>();

            foreach (PropertyInfo sourceProperty in sourceType.GetProperties())
            {
                PropertyInfo targetPorperty = targetType.GetProperty(sourceProperty.Name);
                MethodCallExpression get = Expression.Call(s, sourceProperty.GetGetMethod());
                MethodCallExpression set = Expression.Call(t, targetPorperty.GetSetMethod(), get);
                assigns.Add(set);
            }

            Expression<Action<S, T>> lambda = Expression.Lambda<Action<S, T>>(Expression.Block(assigns), s, t);
            return lambda.Compile();
        }

        /// <summary>
        /// 从一个实例中复制属性值到另一个实例中,复制属性值时忽略目标实例的类型且仅复制同名属性.eg:
        /// <code>
        /// Action&lt;User, Student&gt; CopyProperties = Reflections.CopyPropertiesFunc&lt;User, Student&gt;();
        /// CopyProperties(user, student);
        /// </code>
        /// </summary>
        /// <typeparam name="S">参数类型</typeparam>
        /// <typeparam name="T">目标参数类型</typeparam>
        public static Action<S, T> CopyPropertiesFunc<S, T>()
        {
            Type sourceType = typeof(S), targetType = typeof(T);

            // 声明参数
            ParameterExpression s = Expression.Parameter(sourceType, "s");
            ParameterExpression t = Expression.Parameter(targetType, "t");

            List<Expression> assigns = new List<Expression>();

            // 创建临时变量
            ParameterExpression _ = Expression.Variable(targetType, "_");

            //将结果赋值给本地字符串变量
            BinaryExpression o = Expression.Assign(_, t);
            assigns.Add(o);

            foreach (PropertyInfo sourceProperty in sourceType.GetProperties())
            {
                PropertyInfo targetPorperty = targetType.GetProperty(sourceProperty.Name);
                if (targetPorperty != null)
                {
                    // 赋值操作 _.Name = s.Name;//_.Set_Name(s.Get_Name());
                    MethodCallExpression get = Expression.Call(s, sourceProperty.GetGetMethod());
                    MethodCallExpression set = Expression.Call(_, targetPorperty.GetSetMethod(), get);
                    assigns.Add(set);
                }
            }
            // 返回值
            //LabelTarget labelTarget = Expression.Label(targetType);
            //GotoExpression _goto = Expression.Return(labelTarget, _, targetType);
            //assigns.Add(Expression.Label(labelTarget, _goto));

            BlockExpression block = Expression.Block(new ParameterExpression[] { _ }, assigns);
            Expression<Action<S, T>> lambda = Expression.Lambda<Action<S, T>>(block, s, t);

            return lambda.Compile();
        }

        /// <summary>
        /// 克隆对象
        /// <code>
        /// Func&lt;Order, Order&gt; copy = Reflections.Clone&lt;Order&gt;();
        /// Order _order = copy(order);
        /// </code>
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>对象克隆体</returns>
        public static Func<T, T> Clone<T>()
        {
            Type type = typeof(T);
            // (T s) => new T { Prop1 = s.Prop1, ... }
            ParameterExpression source = Expression.Parameter(type, "s");

            List<MemberBinding> bindings = new List<MemberBinding>();

            foreach (PropertyInfo property in type.GetProperties())
            {
                MethodCallExpression value = Expression.Call(source, property.GetGetMethod());
                MemberBinding binding = Expression.Bind(property, value);
                bindings.Add(binding);
            }

            Expression body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
            Expression<Func<T, T>> lambda = Expression.Lambda<Func<T, T>>(body, source);

            return lambda.Compile();
        }
    }

    public class MappingInfo
    {
        private object source;
        private object target;
        private PropertyInfo sourceProperty;
        private PropertyInfo targetProperty;
        public MappingInfo(object source, object target, PropertyInfo sourceProperty, PropertyInfo targetProperty)
        {
            this.source = source;
            this.target = target;
            this.sourceProperty = sourceProperty;
            this.targetProperty = targetProperty;
        }
        public int ID
        {
            get
            {
                return SourceProperty.GetHashCode() ^ TargetType.GetHashCode();
            }
        }
        public object Source
        {
            get
            {
                return source;
            }
        }
        public object Target
        {
            get
            {
                return target;
            }
        }
        public Type SourceType
        {
            get
            {
                return source.GetType();
            }
        }
        public Type TargetType
        {
            get
            {
                return target.GetType();
            }
        }
        public PropertyInfo SourceProperty
        {
            get
            {
                return sourceProperty;
            }
        }
        public PropertyInfo TargetProperty
        {
            get
            {
                return targetProperty;
            }
        }


    }
}
