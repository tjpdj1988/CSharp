
namespace Utility.Json
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// 仿 Json 动态对象
    /// </summary>
    public class Json : DynamicObject
    {
        private static readonly Dictionary<int, Delegate> convertTo = new Dictionary<int, Delegate>();

        Dictionary<object, object> members = new Dictionary<object, object>();

        private int MembersHashCode = 7;

        /// <summary>
        /// 从实例对象获取信息构建Json对象
        /// </summary>
        /// <param name="o">实例对象</param>
        /// <returns>动态对象</returns>
        public static dynamic JsonFrom(object o)
        {
            return new JsonReadonly(o);
        }

        #region Override 的方法
        /// <summary>
        /// 获取动态对象的所有动态成员名
        /// </summary>
        /// <returns>动态成员枚举器</returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            foreach (KeyValuePair<object, object> item in members.AsEnumerable())
            {
                yield return item.Key as string;
            }
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="binder">ConvertBinder</param>
        /// <param name="result">转换后的目标类型实例</param>
        /// <returns>true</returns>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            Type targetType = binder.Type;
            int key = targetType.GetHashCode() ^ MembersHashCode;
            Func<Json, object> toReturnType = null;
            try
            {
                toReturnType = (Func<Json, object>)convertTo[key];
            }
            catch (KeyNotFoundException)
            {
                PropertyInfo[] properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(o => o.CanWrite && o.CanRead && (o.PropertyType.IsEnum || o.PropertyType.Namespace == "System"))
                    .ToArray();
                List<MemberBinding> bindings = new List<MemberBinding>();
                MemberBinding binding = null;
                foreach (PropertyInfo property in properties)
                {
                    if (members.ContainsKey(property.Name))
                    {
                        binding = Expression.Bind(property, Expression.Constant(members[property.Name], property.PropertyType));
                    }
                    else
                    {
                        binding = Expression.Bind(property, Expression.Default(property.PropertyType));
                    }
                    bindings.Add(binding);
                }

                ParameterExpression json = Expression.Parameter(typeof(Json), "json");

                Expression body = Expression.MemberInit(Expression.New(targetType), bindings);
                toReturnType = Expression.Lambda<Func<Json, object>>(body, json).Compile();
                convertTo[key] = toReturnType;
            }
            result = toReturnType(this);
            return true;
        }

        /// <summary>
        /// 获取成员值
        /// </summary>
        /// <param name="binder">GetMemberBinder</param>
        /// <param name="result">成员值</param>
        /// <returns>true</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = members.ContainsKey(binder.Name) ? members[binder.Name] : null;
            return true;
        }

        /// <summary>
        /// 设置成员值
        /// </summary>
        /// <param name="binder">SetMemberBinder</param>
        /// <param name="value">成员值</param>
        /// <returns>true</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (!members.ContainsKey(binder.Name))
            {
                MembersHashCode ^= (binder.Name.GetHashCode() >> 32);
            }
            members[binder.Name] = value;
            return true;
        }

        /// <summary>
        /// 设置索引值
        /// </summary>
        /// <param name="binder">SetIndexBinder</param>
        /// <param name="indexes">使用的索引</param>
        /// <param name="value">索引值</param>
        /// <returns>true</returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (!members.ContainsKey(indexes[0]))
            {
                MembersHashCode ^= (indexes[0].GetHashCode() >> 32);
            }
            members[indexes[0]] = value;
            return true;
        }

        /// <summary>
        /// 获取索引值
        /// </summary>
        /// <param name="binder">GetIndexBinder</param>
        /// <param name="indexes">使用的索引</param>
        /// <param name="value">索引值</param>
        /// <returns>true</returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = members.ContainsKey(indexes[0]) ? members[indexes[0]] : null;
            return true;
        }

        /// <summary>
        /// Json 格式字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder _ = new StringBuilder("{");
            IEnumerable<KeyValuePair<object, object>> list = members.AsEnumerable().Where(o => !(o.Value is Delegate)).AsEnumerable();

            foreach (KeyValuePair<object, object> item in list)
            {
                _.Append(string.Format("\"{0}\":\"{1}\",", item.Key, item.Value));
            }
            _.Replace(',', '}', _.Length - 1, 1);
            return Regex.Replace(_.ToString(), "\"(\\d+)\"", "$1");
        }
        #endregion

        class JsonReadonly : Json
        {
            private static readonly Dictionary<int, Delegate> convertFrom = new Dictionary<int, Delegate>();

            public JsonReadonly(object o)
            {
                Type sourceType = o.GetType(), targetType = typeof(Dictionary<object, object>);
                int key = sourceType.GetHashCode();
                Action<object, Dictionary<object, object>> toJsonType = null;
                try
                {
                    toJsonType = convertFrom[key] as Action<object, Dictionary<object, object>>;
                }
                catch (KeyNotFoundException)
                {
                    PropertyInfo[] properties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanWrite && p.CanRead)
                        .ToArray();

                    ParameterExpression members = Expression.Parameter(targetType, "members");
                    ParameterExpression _ = Expression.Parameter(typeof(object), "_");

                    ParameterExpression source = Expression.Variable(sourceType, "source");
                    BinaryExpression assign = Expression.Assign(source, Expression.TypeAs(_, sourceType));

                    MethodInfo method = targetType.GetMethod("Add");
                    List<Expression> expressions = new List<Expression>();
                    expressions.Add(assign);

                    foreach (PropertyInfo property in properties)
                    {
                        MethodCallExpression add = Expression.Call(members, method,
                            new Expression[]{
                                Expression.Constant(property.Name, typeof(string)),
                                Expression.TypeAs(
                                    Expression.Call(source, property.GetGetMethod()),
                                    typeof(object))
                            });
                        expressions.Add(add);
                    }
                    BlockExpression block = Expression.Block(new[] { source }, expressions.ToArray());

                    toJsonType = Expression.Lambda<Action<object, Dictionary<object, object>>>(block, _, members).Compile();
                    convertFrom[key] = toJsonType;
                }
                toJsonType(o, base.members);
            }
            #region Override 的方法
            public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
            {
                throw new NotSupportedException("此实例为只读实例,不支持修改索引操作！");
            }
            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                throw new NotSupportedException("此实例为只读实例,不支持修改成员操作！");
            }
            #endregion
        }
    }
}
