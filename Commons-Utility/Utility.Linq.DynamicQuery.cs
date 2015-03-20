using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Linq.SqlClient;
using System.Text;

// http://www.cnblogs.com/coolcode/archive/2011/06/08/2075749.html
// 此类需要更新，详情访问上面的地址
namespace Utility.Linq
{
    /// <summary>
    /// 
    /// </summary>
    public static class QueryBuilder
    {
        /// <summary>
        /// 创建查询构建器
        /// </summary>
        /// <typeparam name="T">查询对象类型</typeparam>
        /// <returns></returns>
        public static IQueryBuilder<T> Create<T>()
        {
            return new QueryBuilder<T>();
        }
    }

    class QueryBuilder<T> : IQueryBuilder<T>
    {
        private Expression<Func<T, bool>> predicate;
        Expression<Func<T, bool>> IQueryBuilder<T>.Expression
        {
            get
            {
                return predicate;
            }
            set
            {
                predicate = value;
            }
        }

        public QueryBuilder()
        {
            predicate = PredicateBuilder.True<T>();
        }
    }

    /// <summary>
    /// 动态查询条件构建器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQueryBuilder<T>
    {
        /// <summary>
        /// 查询表达式
        /// </summary>
        Expression<Func<T, bool>> Expression { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class IQueryBuilderExtensions
    {
        /// <summary>
        /// 建立 Between 查询条件
        /// </summary>
        /// <typeparam name="T">查询对象类型</typeparam>
        /// <typeparam name="P">查询参数值类型</typeparam>
        /// <param name="q">动态查询条件构建器</param>
        /// <param name="property">属性</param>
        /// <param name="from">开始值</param>
        /// <param name="to">结束值</param>
        /// <returns></returns>
        public static IQueryBuilder<T> Between<T, P>(this IQueryBuilder<T> q, Expression<Func<T, P>> property, P from, P to)
        {
            var parameter = property.GetParameters();
            var constantFrom = Expression.Constant(from);
            var constantTo = Expression.Constant(to);
            Type type = typeof(P);
            Expression nonNullProperty = property.Body;
            //如果是Nullable<X>类型，则转化成X类型
            if (IsNullableType(type))
            {
                type = GetNonNullableType(type);
                nonNullProperty = Expression.Convert(property.Body, type);
            }
            var c1 = Expression.GreaterThanOrEqual(nonNullProperty, constantFrom);
            var c2 = Expression.LessThanOrEqual(nonNullProperty, constantTo);
            var c = Expression.AndAlso(c1, c2);
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(c, parameter);

            q.Expression = q.Expression.And(lambda);
            return q;
        }

        /// <summary>
        /// 建立 Like ( 模糊 ) 查询条件
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="q">动态查询条件构建器</param>
        /// <param name="property">属性</param>
        /// <param name="value">查询值</param>
        /// <returns></returns>
        public static IQueryBuilder<T> Like<T>(this IQueryBuilder<T> q, Expression<Func<T, string>> property, string value)
        {
            value = value.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                var parameter = property.GetParameters();
                var constant = Expression.Constant("%" + value + "%");
                MethodCallExpression methodExp = Expression.Call(null, typeof(SqlMethods).GetMethod("Like",
                    new Type[] { typeof(string), typeof(string) }), property.Body, constant);
                Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(methodExp, parameter);

                q.Expression = q.Expression.And(lambda);
            }
            return q;
        }

        /// <summary>
        /// 建立 Equals ( 相等 ) 查询条件
        /// </summary>
        /// <typeparam name="T">查询对象类型</typeparam>
        /// <typeparam name="P">查询参数值类型</typeparam>
        /// <param name="q">动态查询条件构建器</param>
        /// <param name="property">属性</param>
        /// <param name="value">查询值</param>
        /// <returns></returns>
        public static IQueryBuilder<T> Equals<T, P>(this IQueryBuilder<T> q, Expression<Func<T, P>> property, P value)
        {
            var parameter = property.GetParameters();
            var constant = Expression.Constant(value);
            Type type = typeof(P);
            Expression nonNullProperty = property.Body;
            //如果是Nullable<X>类型，则转化成X类型
            if (IsNullableType(type))
            {
                type = GetNonNullableType(type);
                nonNullProperty = Expression.Convert(property.Body, type);
            }
            var methodExp = Expression.Equal(nonNullProperty, constant);
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(methodExp, parameter);
            q.Expression = q.Expression.And(lambda);
            return q;
        }

        /// <summary>
        /// 建立 In 查询条件
        /// </summary>
        /// <typeparam name="T">查询对象类型</typeparam>
        /// <typeparam name="P">查询参数值类型</typeparam>
        /// <param name="q">动态查询条件构建器</param>
        /// <param name="property">属性</param>
        /// <param name="values">查询值</param> 
        /// <returns></returns>
        public static IQueryBuilder<T> In<T, P>(this IQueryBuilder<T> q, Expression<Func<T, P>> property, params P[] values)
        {
            if (values != null && values.Length > 0)
            {
                var parameter = property.GetParameters();
                var constant = Expression.Constant(values);
                Type type = typeof(P);
                Expression nonNullProperty = property.Body;
                //如果是Nullable<X>类型，则转化成X类型
                if (IsNullableType(type))
                {
                    type = GetNonNullableType(type);
                    nonNullProperty = Expression.Convert(property.Body, type);
                }
                Expression<Func<P[], P, bool>> InExpression = (list, el) => list.Contains(el);
                var methodExp = InExpression;
                var invoke = Expression.Invoke(methodExp, constant, property.Body);
                Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(invoke, parameter);
                q.Expression = q.Expression.And(lambda);
            }
            return q;
        }

        private static ParameterExpression[] GetParameters<T, S>(this Expression<Func<T, S>> expr)
        {
            return expr.Parameters.ToArray();
        }

        static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        static Type GetNonNullableType(Type type)
        {
            return type.GetGenericArguments()[0];
            //return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public static class PredicateBuilder
    {
        /// <summary>
        /// 获取恒真表达式
        /// </summary>
        /// <typeparam name="T">相关查询对象类型</typeparam>
        /// <returns></returns>
        public static Expression<Func<T, bool>> True<T>() { return f => true; }

        /// <summary>
        /// 获取恒假表达式
        /// </summary>
        /// <typeparam name="T">相关查询对象类型</typeparam>
        /// <returns></returns>
        public static Expression<Func<T, bool>> False<T>() { return f => false; }

        /// <summary>
        /// 构建or表达式
        /// </summary>
        /// <typeparam name="T">相关查询对象类型</typeparam>
        /// <param name="expr1">表达式1</param>
        /// <param name="expr2">表达式2</param>
        /// <returns>or表达式</returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
                                                            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        /// <summary>
        /// 构建and表达式
        /// </summary>
        /// <typeparam name="T">相关查询对象类型</typeparam>
        /// <param name="expr1">表达式1</param>
        /// <param name="expr2">表达式2</param>
        /// <returns>and表达式</returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
                                                             Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }

        /// <summary>
        /// 构建not表达式
        /// </summary>
        /// <typeparam name="T">相关查询对象类型</typeparam>
        /// <param name="expr"></param>
        /// <returns>not 表达式</returns>
        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
        {
            var not = Expression.Not(expr.Body);
            return Expression.Lambda<Func<T, bool>>(not, expr.Parameters);
        }
    }
}
