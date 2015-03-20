namespace Utility.Data
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using System.Text;
    using System.Data.Common;
    using Utility.Reflection;
    using System.Data.OleDb;

    #region Class DbUtils(数据库操作相关方法)
    /// <summary> 
    /// 数据库操作相关方法:获取数据库连接、数据库查询、数据库事务、IDataReader 数据封装(反射)、 IDataReader数据封装Lambda生成
    /// </summary> 
    public static class DbUtils
    {
        #region 数据库连接信息缓存
        private static readonly Dictionary<string, Type> drivers = new Dictionary<string, Type>(2);
        private static readonly Dictionary<string, ConnectionStringSettings> settings = new Dictionary<string, ConnectionStringSettings>(2);
        private static readonly Dictionary<Type, Delegate> toEntityCache = new Dictionary<Type, Delegate>();

        static DbUtils()
        {
            // 缓存实现 System.Data.IDbConnection 接口的类 
            List<Type> list = Reflections.GetInterfaceImplementation("System.Data.IDbConnection");
            foreach (Type type in list)
            {
                drivers.Add(type.Namespace, type);
            }

            HashSet<string> csss = new HashSet<string> { "default" };

            // 配置文件代码段：
            // <appSettings> 
            //     <add name="DataSources" value="Northwind"/> 
            // </appSettings> 
            // <connectionStrings> 
            //     <add name="default" connectionString="DATA SOURCE=127.0.0.1;UID=sa;PWD=123456;DATABASE=default" providerName="System.Data.SqlClient"/> 
            //     <add name="Northwind" connectionString="DATA SOURCE=127.0.0.1;UID=sa;PWD=123456;DATABASE=northwind" providerName="System.Data.SqlClient"/> 
            // </connectionStrings> 

            // 获取数据库配置信息 
            string dataSource = ConfigurationManager.AppSettings["DataSources"];
            if (!string.IsNullOrEmpty(dataSource))
            {
                foreach (string item in dataSource.Split(',', ' ', '|'))
                {
                    csss.Add(item);
                }
            }

            foreach (string name in csss)
            {
                ConnectionStringSettings css = ConfigurationManager.ConnectionStrings[name];
                RegisterConnectionStrings(css);
            }
        }

        /// <summary> 
        /// 注册数据库连接信息 
        /// </summary> 
        /// <param name="css">数据库连接信息对象</param> 
        /// <returns>注册数据库连接信息名称<seealso cref="ConnectionStringSettings.Name"/></returns> 
        public static string RegisterConnectionStrings(ConnectionStringSettings css)
        {

            if (string.IsNullOrEmpty(css.Name))
            {
                throw new Exception(String.Format("数据库连接信息无效,属性Name 不能为空/Null!!"));
            }

            if (string.IsNullOrEmpty(css.ConnectionString))
            {
                throw new Exception(String.Format("数据库连接信息无效,属性ConnectionString 不能为空/Null!!"));
            }

            if (string.IsNullOrEmpty(css.ProviderName))
            {
                throw new Exception(String.Format("数据库连接信息配置无效,属性ProviderName 不能为空/Null!!!"));
            }

            string name = css.Name;
            if (settings.ContainsKey(name))
            {
                if (string.IsNullOrEmpty(css.ProviderName))
                {
                    throw new Exception(String.Format("数据库连接信息配置已存在!!!"));
                }
            }
            settings.Add(name, css);
            return name;
        }

        /// <summary> 
        /// 移除数据库连接信息 
        /// </summary> 
        /// <param name="name">数据库连接信息名称<seealso cref="ConnectionStringSettings.Name"/></param> 
        /// <returns>被移除的数据库连接信息<seealso cref="ConnectionStringSettings"/> </returns> 
        public static ConnectionStringSettings RemoveConnectionStrings(string name)
        {
            ConnectionStringSettings css = settings[name];
            settings.Remove(name);
            return css;
        }

        /// <summary> 
        /// 获取 IDbConnection. 注意:因未显式声明数据库驱动类型,可能因数据库驱动未加载而出现异常(如：SqlCeConnection).
        /// 此时建议使用泛型方法,通过参数 T 指定连接类型.
        /// </summary> 
        /// <returns>default数据库连接信息构建的实例</returns> 
        public static IDbConnection GetIDbConnection()
        {
            return GetIDbConnection<IDbConnection>("default");
        }


        /// <summary> 
        /// 获取 IDbConnection
        /// </summary> 
        /// <param name="name">数据库连接信息名称<seealso cref="ConnectionStringSettings.Name"/></param> 
        /// <returns>数据库连接信息构建的实例</returns> 
        public static T GetIDbConnection<T>(string name) where T : IDbConnection
        {
            ConnectionStringSettings setting = settings[name];
            T conn = (T)Activator.CreateInstance(drivers[setting.ProviderName], setting.ConnectionString);
            conn.Open();
            return conn;
        }
        #endregion

        #region 数据库操作相关方法
        /// <summary> 
        /// 执行查询操作 
        /// </summary> 
        /// <typeparam name="T">返回值类型</typeparam> 
        /// <param name="conn">IDbConnection 实例</param>
        /// <param name="queryRunner">委托方法:(IDbCommand cmd) => { return default(T); }</param> 
        /// <returns>查询结果(T 实例)</returns> 
        public static T Execute<T>(IDbConnection conn, Func<IDbCommand, T> queryRunner)
        {
            using (IDbCommand cmd = conn.CreateCommand())
            {
                return queryRunner(cmd);
            }
        }

        /// <summary> 
        /// 执行事务操作 
        /// </summary> 
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="conn">IDbConnection 实例</param>
        /// <param name="queryRunner">委托方法:(IDbCommand cmd) => { return default(T); }</param> 
        /// <returns>执行结果(T 实例)</returns> 
        public static T Transcation<T>(IDbConnection conn, Func<IDbCommand, T> queryRunner)
        {
            T _return = default(T);

            using (IDbTransaction trans = conn.BeginTransaction())
            {
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trans;
                    try
                    {
                        _return = queryRunner(cmd);
                        trans.Commit();
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
            return _return;
        }

        #endregion

        #region IDbCommand 查询参数设置
        /// <summary> 
        /// 对 IDbCommand 对象的相关属性进行赋值 
        /// </summary> 
        /// <param name="cmd">IDbCommand 对象</param> 
        /// <param name="cmdText">CommandText</param> 
        /// <param name="dps">IDbDataParameter参数</param> 
        public static IDbCommand PreparedIDbCommand(IDbCommand cmd, string cmdText, params IDbDataParameter[] dps)
        {

            if (string.IsNullOrEmpty(cmdText))
            {
                throw new Exception("参数 cmdText 不能为空！！");
            }
            cmd.CommandText = cmdText;

            // 填充IDbCommand 查询参数 
            if (dps != null && dps.Length > 0)
            {
                foreach (IDbDataParameter p in dps)
                {
                    if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && (p.Value == null))
                    {
                        p.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(p);
                }

            }
            return cmd;
        }

        #endregion

        #region IDataReader数据到实体对象的封装相关方法
        /// <summary> 
        /// 将 IDataReader 数据封装到泛型类型实例(反射实现)
        /// </summary> 
        /// <typeparam name="T">泛型类型</typeparam> 
        /// <param name="reader">IDataReader</param> 
        /// <param name="toObject">委托方法:(IDataReader reader) => { return default(T); }</param> 
        /// <returns>T 实例</returns> 
        public static T ToObject<T>(IDataReader reader, Func<IDataRecord, T> toObject) where T : class, new()
        {
            return toObject(reader);
        }

        /// <summary> 
        /// 将 IDataReader 数据封装到泛型类型实例(反射实现)
        /// </summary> 
        /// <typeparam name="T">泛型类型</typeparam> 
        /// <param name="reader">IDataReader</param> 
        /// <returns>T 实例</returns> 
        public static T ToObject<T>(IDataReader reader) where T : class, new()
        {
            return ToObject(reader, (_reader) =>
            {
                Type type = typeof(T);

                T _return = (T)Activator.CreateInstance(type);

                int count = reader.FieldCount;
                for (int i = 0; i < count; i++)
                {
                    string name = reader.GetName(i);
                    PropertyInfo property = type.GetProperty(name.Replace(" ", "_"));
                    if (property != null)
                    {
                        object _ = _reader.GetValue(i);
                        property.SetValue(_return, _ is DBNull ? null : _, null);
                    }
                }
                return _return;
            });
        }

        /// <summary>
        /// 生成对象属性赋值 Lambda代码并编译代码(编译代码未缓存),由IDataRecord 字段映射 T 对象属性.eg:
        /// <code>
        /// Func&lt;IDataRecord, Orders&gt; toOrder = DbUtils.ToEntity&lt;Orders&gt;(reader);
        /// Order order = toOrder(reader);
        /// </code>
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="record">IDataRecord</param>
        /// <param name="patternAndReplacement">列名映射对象属性的匹配替换参数.默认为将表名中的空格替换成下划线：
        /// propertyName = Regex.IsMatch(fieldName, @"(\w+)\s(\w+)") ? Regex.Replace(fieldName, @"(\w+)\s(\w+)", "$1_$2") : fieldName;</param>
        /// <returns>ToEntityFunc 委托</returns>
        public static Func<IDataRecord, T> ToEntity<T>(IDataRecord record, params string[] patternAndReplacement) where T : class, new()
        {
            // T o = new T();
            // int count = record.FieldCount;
            // for (int i = 0; i < count; i++)
            // {
            //      object value = record.getValue(i);
            //      o.property_i = value is DBNull? default(PT): (PT)value;
            // }
            //
            Type sourceType = typeof(IDataRecord), targetType = typeof(T);
            ParameterExpression source = Expression.Parameter(sourceType, "source");

            List<MemberBinding> bindings = new List<MemberBinding>();

            string pattern = @"(\w+)\s(\w+)", replacement = "$1_$2";
            if (patternAndReplacement != null && patternAndReplacement.Length == 2)
            {
                pattern = patternAndReplacement[0];
                replacement = patternAndReplacement[1];
            }

            int count = record.FieldCount;
            for (int i = 0; i < count; i++)
            {
                string name = record.GetName(i);
                name = Regex.IsMatch(name, pattern) ? Regex.Replace(name, pattern, replacement) : name;
                PropertyInfo property = targetType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.CanRead)
                {
                    // 声明临时变量
                    ParameterExpression value = Expression.Variable(typeof(object), "value");
                    ParameterExpression _value = Expression.Variable(property.PropertyType, "_value");

                    // 获取第 i 列的值，如果列值为 DBNull 类型，则返回 default(T) 为默认值否则返回将列值转换为目标属性类型
                    LabelTarget labelTarget = Expression.Label(property.PropertyType);
                    GotoExpression _goto = Expression.Return(labelTarget, _value, property.PropertyType);

                    // (record.getValue(i) is DBNull ? default(T) : (T)record.getValue(i))
                    BlockExpression block = Expression.Block(new[] { value, _value },
                        Expression.Assign(value, Expression.Call(source, sourceType.GetMethod("GetValue"), Expression.Constant(i, typeof(int)))),
                        Expression.IfThenElse(
                            Expression.TypeIs(value, typeof(DBNull)),
                            Expression.Assign(_value, Expression.Default(property.PropertyType)),
                            Expression.Assign(_value, Expression.Convert(value, property.PropertyType))
                        ),
                        Expression.Label(labelTarget, _goto)
                    );

                    MemberBinding binding = Expression.Bind(property, block);
                    bindings.Add(binding);
                }
            }

            Expression body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
            Expression<Func<IDataRecord, T>> lambda = Expression.Lambda<Func<IDataRecord, T>>(body, source);

            return lambda.Compile();
        }

        /// <summary>
        /// 生成对象属性赋值 Lambda代码并编译代码(编译代码已缓存),由T 对象属性映射 IDataRecord 字段.eg:
        /// <code>
        /// Func&lt;IDataRecord, Orders&gt; toOrder = DbUtils.ToEntity&lt;Orders&gt;();
        /// Order order = toOrder(reader);
        /// </code>
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="patternAndReplacement">对象属性映射列名的匹配替换参数.默认为将属性中的下划线替换成空格：
        /// fieldName = Regex.IsMatch(propertyName, @"(\w+)_(\w+)") ? Regex.Replace(propertyName, @"(\w+)_(\w+)", "$1 $2") : propertyName;</param>
        /// <returns>ToEntityFunc 委托</returns>
        public static Func<IDataRecord, T> ToEntity<T>(params string[] patternAndReplacement) where T : class, new()
        {
            Func<IDataRecord, T> toEntity = null;
            try
            {
                toEntity = toEntityCache[typeof(T)] as Func<IDataRecord, T>;
            }
            catch (KeyNotFoundException)
            {
                // T o = new T();
                // object value;
                // PT _value;
                // try{
                //     value = reader[fieldName];
                //     value = value is DbNull ? default(PT) as object : value;
                //     _value = (PT)value;
                // }catch(exception){
                //     _value = default(PT) as object;
                // }
                // o.propery1 = _value;            
                //
                Type sourceType = typeof(IDataRecord), targetType = typeof(T);
                ParameterExpression source = Expression.Parameter(sourceType, "source");

                List<MemberBinding> bindings = new List<MemberBinding>();

                string pattern = @"(\w+)_(\w+)", replacement = "$1 $2";
                if (patternAndReplacement != null && patternAndReplacement.Length == 2)
                {
                    pattern = patternAndReplacement[0];
                    replacement = patternAndReplacement[1];
                }

                PropertyInfo[] properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(o => o.CanWrite && o.CanRead && (o.PropertyType.IsEnum || o.PropertyType.Namespace == "System"))
                    .ToArray();
                foreach (PropertyInfo property in properties)
                {
                    string name = property.Name;
                    name = Regex.IsMatch(name, pattern) ? Regex.Replace(name, pattern, replacement) : name;

                    // 声明临时变量
                    ParameterExpression value = Expression.Variable(typeof(object), "value");
                    ParameterExpression _value = Expression.Variable(property.PropertyType, "_value");

                    // 获取第 i 列的值，如果列值为 DBNull 类型，则返回 default(T) 为默认值否则返回将列值转换为目标属性类型
                    LabelTarget labelTarget = Expression.Label(property.PropertyType);
                    GotoExpression _goto = Expression.Return(labelTarget, _value, property.PropertyType);

                    // (record.getValue(i) is DBNull ? default(T) : (T)record.getValue(i))
                    BlockExpression block = Expression.Block(new[] { value, _value },
                        Expression.TryCatch(
                            Expression.Block(
                                Expression.Assign(value,
                                    Expression.MakeIndex(source,
                                        sourceType.GetProperty("Item", new[] { typeof(string) }),
                                        new[] { Expression.Constant(name) }
                                    )
                                ),
                                Expression.IfThen(
                                    Expression.TypeIs(value, typeof(DBNull)),
                                    Expression.Assign(value, Expression.TypeAs(Expression.Default(property.PropertyType), value.Type))
                                ),
                                Expression.Assign(_value, Expression.Convert(value, property.PropertyType))
                            ),
                            Expression.Catch(
                                typeof(IndexOutOfRangeException),
                                Expression.Block(
                                    Expression.Assign(_value, Expression.Default(property.PropertyType))
                                )
                            )
                        ),
                        Expression.Label(labelTarget, _goto)
                    );

                    MemberBinding binding = Expression.Bind(property, block);
                    bindings.Add(binding);

                }

                Expression body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
                Expression<Func<IDataRecord, T>> lambda = Expression.Lambda<Func<IDataRecord, T>>(body, source);

                toEntityCache[targetType] = (toEntity = lambda.Compile());
            }
            return toEntity;
        }
        #endregion

        #region DataTable 数据输出到HTML table 的简单方法
        /// <summary>
        /// 根据 table 中数据输出 HTML 表格
        /// </summary>
        /// <param name="table">数据表</param>
        /// <returns>html 字符串</returns>
        private static string ToHTMLString(DataTable table)
        {
            StringBuilder html = new StringBuilder("<!DOCTYPE html>");
            html.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
            html.Append("<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" /></head>");
            html.Append(string.Format("<body><table style=\"width:600px\"><caption>{0}</caption>", table.TableName));

            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow row = table.Rows[i];
                if (i == 0)
                {
                    html.Append("<thead><tr>");
                    foreach (DataColumn col in table.Columns)
                    {
                        html.Append(String.Format("<td>{0}</td>", col.ColumnName));
                    }
                    html.Append("</tr></thead>");
                }
                html.Append("<tr>");
                foreach (DataColumn col in table.Columns)
                {
                    html.Append(String.Format("<td>{0}</td>", row[col]));
                }
                html.Append("</tr>");
            }
            html.Append("</table></body></html>");

            return html.ToString();
        }
        #endregion
    }
    #endregion

    #region Class Schemas(获取数据库 Schema 信息)
    /// <summary>
    /// 获取数据库 Schema 信息
    /// </summary>
    public static class Schemas
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly string MetaDataCollections = "MetaDataCollections";
        /// <summary>
        /// 
        /// </summary>
        public static readonly string DataSourcesInformation = "DataSourceInformation";
        /// <summary>
        /// 
        /// </summary>
        public static readonly string DataTypes = "DataTypes";
        /// <summary>
        /// 
        /// </summary>
        public static readonly string Restrictions = "Restrictions";
        /// <summary>
        /// 
        /// </summary>
        public static readonly string ReservedWords = "ReservedWords";
        /// <summary>
        /// 列
        /// </summary>
        public static readonly string Columns = "Columns";
        /// <summary>
        /// 索引
        /// </summary>
        public static readonly string Indexes = "Indexes";
        /// <summary>
        /// 存储过程
        /// </summary>
        public static readonly string Procedures = "Procedures";
        /// <summary>
        /// 表
        /// </summary>
        public static readonly string Tables = "Tables";
        /// <summary>
        /// 视图
        /// </summary>
        public static readonly string Views = "Views";

        /// <summary>
        /// 获取数据库指定的Schema信息
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="collectionName">信息名称</param>
        /// <returns>指定的Schema信息</returns>
        public static DataTable GetSchema(DbConnection conn, string collectionName)
        {
            return conn.GetSchema(collectionName);
        }

        /// <summary>
        /// 获取数据库 Table Schame
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <returns>Table Schame 信息</returns>
        public static DataTable GetTablesSchema(DbConnection conn)
        {
            return conn.GetSchema(Tables);
        }

        /// <summary>
        /// 获取数据库Column Schame
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <returns>Column Schame 信息</returns>
        public static DataTable GetColumnsSchema(DbConnection conn)
        {
            return conn.GetSchema(Columns);
        }

        /// <summary>
        /// 获取Table Column Schame
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="tableName">表名</param>
        /// <returns>Table Column Schame 信息</returns>
        public static DataRow[] GetTableColumnsSchema(DbConnection conn, string tableName)
        {
            return conn.GetSchema(Columns).Select(string.Format("TABLE_NAME='{0}'", tableName));
        }
    }
    #endregion

    #region Class DataExtensions(数据库操作相关扩展方法)
    /// <summary>
    /// 数据库操作相关扩展方法
    /// </summary>
    public static class DataExtensions
    {

        #region IDbConnection 扩展方法

        /// <summary> 
        /// 执行查询操作 
        /// </summary> 
        /// <typeparam name="T">返回值类型</typeparam> 
        /// <param name="conn">IDbConnection 实例</param>
        /// <param name="queryRunner">委托方法</param> 
        /// <returns>查询结果</returns> 
        public static T Execute<T>(this IDbConnection conn, Func<IDbCommand, T> queryRunner)
        {
            T _return = default(T);

            using (IDbCommand cmd = conn.CreateCommand())
            {
                _return = queryRunner(cmd);
            }

            return _return;
        }

        /// <summary> 
        /// 执行事务操作 
        /// </summary> 
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="conn">IDbConnection 实例</param>
        /// <param name="queryRunner">委托方法</param> 
        /// <returns>执行结果</returns> 
        public static T Transcation<T>(this IDbConnection conn, Func<IDbCommand, T> queryRunner)
        {
            T _return = default(T);

            using (IDbTransaction trans = conn.BeginTransaction())
            {
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trans;
                    try
                    {
                        _return = queryRunner(cmd);
                        trans.Commit();
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
            return _return;
        }
        #endregion

        #region IDbCommand 扩展方法
        /// <summary>
        /// 设置 IDbCommand 相关属性
        /// </summary>
        /// <param name="cmd">IDbCommand 实例</param>
        /// <param name="cmdText">sql 语句</param>
        /// <param name="dps">sql 语句参数列表</param>
        public static void PreparedIDbCommand(this IDbCommand cmd, string cmdText, params IDbDataParameter[] dps)
        {
            if (string.IsNullOrEmpty(cmdText))
            {
                throw new Exception("参数 sql 不能为空！！");
            }
            cmd.CommandText = cmdText;

            // 填充IDbCommand 查询参数 
            if (dps != null && dps.Length > 0)
            {
                foreach (IDbDataParameter p in dps)
                {
                    if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && (p.Value == null))
                    {
                        p.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(p);
                }
            }
        }

        #endregion


        #region IDataRecord 扩展方法
        /// <summary>
        /// 封装IDataReader 数据到对象实例列表
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="reader">IDataReader</param>
        /// <returns>对象实例列表</returns>
        public static List<T> ToList<T>(this IDataReader reader) where T : class, new()
        {
            Func<IDataRecord, T> toEntity = DbUtils.ToEntity<T>();
            List<T> list = new List<T>();
            while (reader.Read())
            {
                list.Add(toEntity(reader));
            }
            return list;
        }
        #endregion
    }
    #endregion

    #region Class LocalDb(文件型数据库相关)
    /// <summary>
    /// 文件型数据库相关
    /// </summary>
    public static class LocalDb
    {
        /// <summary>
        /// 设置并获取csv 文件的连接信息
        /// </summary>
        /// <param name="localFileDir">csv 文件所在目录</param>
        /// <returns>连接信息</returns>
        public static ConnectionStringSettings GetConnectionStringSettingsOfCSV(string localFileDir)
        {
            return new ConnectionStringSettings(
                localFileDir,
                string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=Text;", localFileDir),
                "System.Data.OleDb");
        }

        /// <summary>
        /// 设置并获取sdf 文件的连接信息
        /// </summary>
        /// <param name="localFile">sdf 文件</param>
        /// <returns>连接信息</returns>
        public static ConnectionStringSettings GetConnectionStringSettingsOfSDF(string localFile)
        {
            return new ConnectionStringSettings(
                localFile,
                string.Format("Data Source={0};Persist Security Info=False;", localFile),
                "System.Data.SqlServerCe");
        }

        ///// <summary>
        ///// 设置并获取mdf 文件的连接信息
        ///// </summary>
        ///// <param name="localFile">mdf 文件</param>
        ///// <returns>连接信息</returns>
        //public static ConnectionStringSettings GetConnectionStringSettingsOfMDF(string localFile)
        //{
        //    return new ConnectionStringSettings(
        //        localFile,
        //        string.Format("Data Source=(LocalDB)\v11.0;AttachDbFilename={0};Integrated Security=SSPI;Connect Timeout=45", localFile),
        //        "System.Data.SqlClient");
        //}

        /// <summary>
        /// 设置并获取xls 文件的连接信息
        /// </summary>
        /// <param name="localFile">excel文件</param>
        /// <returns>连接信息</returns>
        public static ConnectionStringSettings GetConnectionStringSettingsOfXLS(string localFile)
        {
            return new ConnectionStringSettings(
                localFile,
                string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=Excel 8.0;", localFile),
                "System.Data.OleDb");
        }


        /// <summary>
        /// 设置并获取Access 文件的连接信息
        /// </summary>
        /// <param name="localFile">mdb 文件</param>
        /// <returns>连接信息</returns>
        public static ConnectionStringSettings GetConnectionStringSettingsOfMDB(string localFile)
        {
            return new ConnectionStringSettings(
                localFile,
                string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};", localFile),
                "System.Data.OleDb");
        }

        /// <summary>
        /// 加载表数据到DataTable
        /// </summary>
        /// <param name="conn">OleDbConnection</param>
        /// <param name="table">表名</param>
        /// <returns>DataTable</returns>
        public static DataTable Load(OleDbConnection conn, string table)
        {
            string sql = "Select * FROM [{0}]";
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            DataTable dt = new DataTable(table);
            adapter.SelectCommand = new OleDbCommand(String.Format(sql, table), conn);
            adapter.Fill(dt);
            return dt;
        }

        /// <summary>
        /// 加载表数据到DataTable
        /// </summary>
        /// <param name="conn">OleDbConnection</param>
        /// <returns>DataTable</returns>
        private static DataTable LoadCSV(OleDbConnection conn)
        {
            string sql = "Select * FROM [{0}]";
            Regex regex = new Regex(@"(.*(\\|/)([A-Za-z0-9\u4e00-\u9fa5_-]+)((\.\w+)?));(.*)");
            string table = regex.Replace(conn.ConnectionString, "[$3#csv]");
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            DataTable dt = new DataTable(table);
            adapter.SelectCommand = new OleDbCommand(String.Format(sql, table), conn);
            adapter.Fill(dt);
            return dt;
        }

        /// <summary>
        /// 加载Excel 数据DataSet
        /// </summary>
        /// <param name="conn">OleDbConnection</param>
        /// <returns></returns>
        private static DataSet loadExcel(OleDbConnection conn)
        {
            return loadExcel(conn, (string tableName) => { return true; });
        }



        /// <summary>
        /// 加载Excel 数据到DataSet
        /// </summary>
        /// <param name="conn">OleDbConnection</param>
        /// <param name="loadSheet">判断是否加载工作表.eg:(string sheet)=&gt; sheet == "Customer"</param>
        /// <returns></returns>
        private static DataSet loadExcel(OleDbConnection conn, Func<string, bool> loadSheet)
        {
            DataSet ds = new DataSet();

            // 获取数据源的表定义元数据                        
            DataTable Schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

            // 加载表数据
            foreach (DataRow row in Schema.Rows)
            {
                string name = (string)row["TABLE_NAME"];

                if (!loadSheet(name))
                {
                    continue;
                }

                ds.Tables.Add(Load(conn, name));
            }
            return ds;
        }

    }
    #endregion

    #region Class PagedStatus(分页状态对象)
    /// <summary>
    /// 分页状态对象
    /// </summary>
    public class PagedStatus
    {
        private int pageIndex = 1;

        private int pageSize = 10;

        private int pageCount;

        private int dataSourceCount;

        /// <summary>
        /// 当前页索引
        /// </summary>
        public int PageIndex
        {
            get { return pageIndex; }
            set { pageIndex = (value > 1) ? value : pageIndex; }
        }

        /// <summary>
        /// 页记录数
        /// </summary>
        public int PageSize
        {
            get { return pageSize; }
            set { pageSize = (value > 1) ? value : pageSize; }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int PageCount
        {
            get { return pageCount; }
            set { pageCount = value; }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int DataSourceCount
        {
            get { return dataSourceCount < 0 ? 0 : dataSourceCount; }
            set
            {
                //设置记录总行数
                dataSourceCount = value;
                if (dataSourceCount > 0)
                {
                    //设置总页数
                    pageCount = (int)Math.Ceiling(dataSourceCount / (double)pageSize);

                    //检查请求页是否正确,当请求页大于总页数时,请求页将被自动设为最大值。
                    pageIndex = (pageIndex > pageCount) ? pageCount : pageIndex;
                }
            }
        }
    }
    #endregion
}