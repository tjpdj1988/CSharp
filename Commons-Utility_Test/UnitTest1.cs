using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Commons_Utility_Test
{
    //public delegate void Func<S, T>(S source, T target);
    //public delegate T Func<S, T>(S source, T target);
    public class User
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Age {get; set;}
    }
    public class stu:ren
    {
        public int ID { get; set; }
        string ren.Name{get; set;}
        public bool Sex { get; set; }
    }

    interface ren
    {
        string Name { get;set;}
        
    }

    [TestClass]
    public class UnitTest1
    {
        //public static Func<User, stu, stu> llll = DynamicCreateEntity<User, stu, stu>();
        [TestMethod]
        public void TestMethod1()
        {
            //Expression<Func<Type, object, object>> exp = (type, o) => {};
            object o = new User();
            User user = (User)o;
            //string s = Sex.男.ToString();
            
           //Sex s = Convert.ChangeType(
            //string i = Regex.Match("Name@User", @"(\w+)@User").Groups[0].Value;
            //ren s = new stu();
            //s.Name = "tong";
            //ren ss = new stu();
            //ss.Name = "dddd";
            //Console.Write("s.Name:"+s.Name);
            //Console.Write("ss.Name:" + ss.Name);
        }

        public static Func<S, T, R> DynamicCreateEntity<S, T, R>()
        {
            // Compiles a delegate of the form (IDataRecord r) => new T { Prop1 = r.Field<Prop1Type>("Prop1"), ... }
            
            Type sourceType = typeof(S), targetType = typeof(T);

            // 声明参数
            ParameterExpression s = Expression.Parameter(sourceType, "s");
            ParameterExpression t = Expression.Parameter(targetType, "t");

            List<Expression> assigns = new List<Expression>();

            // 创建临时变量
            ParameterExpression _ = Expression.Variable(targetType,"_");

            //将结果赋值给本地字符串变量
            BinaryExpression o = Expression.Assign(_, t);
            assigns.Add(o);

            foreach (PropertyInfo sourceProperty in sourceType.GetProperties())
            {
                PropertyInfo targetPorperty = targetType.GetProperty(sourceProperty.Name);
                if (targetPorperty != null)
                {
                    MethodCallExpression get = Expression.Call(s, sourceProperty.GetGetMethod());
                    MethodCallExpression set = Expression.Call(_, targetPorperty.GetSetMethod(), get);
                    assigns.Add(set);
                }
            }

            //LabelTarget labelTarget = Expression.Label(targetType);
            //assigns.Add(Expression.Return(labelTarget, _, targetType));
            //assigns.Add(Expression.Label(labelTarget, Expression.Constant(null)));

            BlockExpression block = Expression.Block(new ParameterExpression[]{_}, assigns);

            Expression<Func<S, T, R>> lambda = Expression.Lambda<Func<S, T, R>>(block, s, t);
            var _return =  lambda.Compile();
            return _return;

        }

        public static Func<T, T> DynamicCreateEntity1<T>()
        {
            // Compiles a delegate of the form (IDataRecord r) => new T { Prop1 = r.Field<Prop1Type>("Prop1"), ... }
            ParameterExpression r = Expression.Parameter(typeof(T), "r");

            // Create property assigns for all writable properties
            List<MemberBinding> bindings = new List<MemberBinding>();

            foreach (PropertyInfo property in (typeof(T).GetProperties()))
            {
                // Create expression representing r.Field<property.PropertyType>(property.Name)
                MethodInfo info = typeof(T).GetMethod(property.Name)/*.MakeGenericMethod(property.PropertyType)*/;
                info = property.GetGetMethod();
                Expression exp = Expression.Constant(property.Name);
                //MethodCallExpression propertyValue = Expression.Call(info
                ///*typeof(T).GetMethod(property.Name).MakeGenericMethod(property.PropertyType)*/,
                //r/*,exp*/ );
                MethodCallExpression propertyValue = Expression.Call(r,info);
                // Assign the property value to property through a member binding
                MemberBinding binding = Expression.Bind(property, propertyValue);
                bindings.Add(binding);
            }
            // Create the initializer, which instantiates an instance of T and sets property values

            // using the member assigns we just created
            Expression initializer = Expression.MemberInit(Expression.New(typeof(T)), bindings);

            // Create the lambda expression, which represents the complete delegate (r => initializer)
            Expression<Func<T, T>> lambda = Expression.Lambda<Func<T, T>>(
            initializer, r);
            Console.Write(lambda.ToString());
            return lambda.Compile();

        }
    }
}
