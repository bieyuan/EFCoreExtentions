# EFCore扩展Update方法(实现 Update User SET Id = Id + 1)
- [源码地址(github)](https://github.com/bieyuan/EFCoreExtentions)

## 前言
1.  EFCore在操作更新的时候往往需要先查询一遍数据，再去更新相应的字段，如果针对批量更新的话会很麻烦，效率也很低。 
2.  目前github上 [EFCore.Extentions](https://github.com/borisdj/EFCore.BulkExtensions) 项目,实现批量更新挺方便的，但是针对 Update User SET Id = Id + 1 这种操作还是没有解决
3. 本文主要就是扩展自更新Update
 
## 实现原理
1. 先根据IQuaryable 获取到SQL语句

```cs
        private static readonly TypeInfo QueryCompilerTypeInfo = typeof(QueryCompiler).GetTypeInfo();
        private static readonly FieldInfo QueryCompilerField = typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");
        private static readonly FieldInfo QueryModelGeneratorField = QueryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryModelGenerator");
        private static readonly FieldInfo DataBaseField = QueryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");
        private static readonly PropertyInfo DatabaseDependenciesField = typeof(Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");

        internal static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        {
            var queryCompiler = (QueryCompiler)QueryCompilerField.GetValue(query.Provider);
            var modelGenerator = (QueryModelGenerator)QueryModelGeneratorField.GetValue(queryCompiler);
            var queryModel = modelGenerator.ParseQuery(query.Expression);
            var database = (IDatabase)DataBaseField.GetValue(queryCompiler);
            var databaseDependencies = (DatabaseDependencies)DatabaseDependenciesField.GetValue(database);
            var queryCompilationContext = databaseDependencies.QueryCompilationContextFactory.Create(false);
            var modelVisitor = (RelationalQueryModelVisitor)queryCompilationContext.CreateQueryModelVisitor();

            modelVisitor.CreateQueryExecutor<TEntity>(queryModel);
            string sql = modelVisitor.Queries.First().ToString();
            return sql;
        }
 ```

 2. 把获取的查询语句的，From之前的语句砍掉，然后拼接
 ```cs
        public static (string, string) GetBatchSql<T>(IQueryable<T> query) where T : class, new()
        {
            string sqlQuery = query.ToSql();
            string tableAlias = sqlQuery.Substring(8, sqlQuery.IndexOf("]") - 8);
            int indexFROM = sqlQuery.IndexOf(Environment.NewLine);
            string sql = sqlQuery.Substring(indexFROM, sqlQuery.Length - indexFROM);
            sql = sql.Contains("{") ? sql.Replace("{", "{{") : sql; // Curly brackets have to escaped:
            sql = sql.Contains("}") ? sql.Replace("}", "}}") : sql; // https://github.com/aspnet/EntityFrameworkCore/issues/8820
            return (sql, tableAlias);
        }
 ```

 3. 根据传入的表达式Expression<Func<T,bool> 生成(Update [a] SET) 之后要更新的部分.列如：Expression<Func<T,bool> expression=a=>a.Id = a.Id + 1 生成[a].[Id]=[a].[Id]+parm_0  pram_0=1
>  通过分析Expression的节点[NodeType]来生成相应的操作符，递归拼接sql语句和参数
  ```cs
      internal static void CreateUpdateBody(string Param, Expression expression, ref StringBuilder sb, ref List<SqlParameter> sp)
        {
            if (expression is BinaryExpression binaryExpression)//表示一元操作符，BinaryExpression 都有左右两部分
            {
                CreateUpdateBody(Param, binaryExpression.Left, ref sb, ref sp);//递归到节点是常量或者属性的节点

                switch (binaryExpression.NodeType)
                {
                    case ExpressionType.Add:
                        sb.Append(" +");
                        break;
                    case ExpressionType.Divide:
                        sb.Append(" /");
                        break;
                    case ExpressionType.Multiply:
                        sb.Append(" *");
                        break;
                    case ExpressionType.Subtract:
                        sb.Append(" -");
                        break;
                    case ExpressionType.And:
                        sb.Append(" ,");
                        break;
                    case ExpressionType.AndAlso:
                        sb.Append(" ,");
                        break;
                    case ExpressionType.Or:
                        sb.Append(" ,");
                        break;
                    case ExpressionType.OrElse:
                        sb.Append(" ,");
                        break;
                    case ExpressionType.Equal:
                        sb.Append(" =");
                        break;
                    default: break;
                }

                CreateUpdateBody(Param, binaryExpression.Right, ref sb, ref sp);
            }

            if (expression is ConstantExpression constantExpression)//常量节点进行拼接，添加参数
            {
                var parmName = $"param_{sp.Count}";
                sp.Add(new SqlParameter(parmName, constantExpression.Value));
                sb.Append($" @{parmName}");
            }

            if (expression is MemberExpression memberExpression)//属性节点拼接
            {
                sb.Append($"{Param}.[{memberExpression.Member.Name}]");
            }
        }
 ```

 4. 最后执行生成SQL语句，详情请看[源码github](https://github.com/bieyuan/EFCoreExtentions)


 5. 调用
```cs
 static void Main(string[] args)
        {
            using (var context = new TestContext())
            {

                var user1 = context.User.AsNoTracking().FirstOrDefault(x=>x.Id==2);
                Console.WriteLine($"-----------Before Update --------------------");
                Console.WriteLine($"{user1.Id}:{user1.Name}:{user1.RoleId}");
                
                //更新语句
                context.User.Where(x => x.Id == 2).RestValue(x => x.Name == (x.Name + " Add Bob") && x.RoleId == (x.RoleId + 1));

                var user2 = context.User.AsNoTracking().FirstOrDefault(x => x.Id == 2);
                Console.WriteLine($"-----------After Update --------------------");
                Console.WriteLine($"{user2.Id}:{user2.Name}:{user2.RoleId}");
            }
            Console.WriteLine($"------------结束--------------------");
            Console.ReadLine();
        }
 ```
6. 生成的SQL如下
 
 ```SQL
      UPDATE [x] SET x.[Name] =x.[Name] + @param_0 ,x.[RoleId] =x.[RoleId] + @param_1
      FROM [User] AS [x]
      WHERE [x].[Id] = 2
 ```