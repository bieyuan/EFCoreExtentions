using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EFCoreExtentions
{
    /// <summary>
    /// EFCore批量操作扩展
    /// </summary>
    public static class BatchExtentions
    {
        /// <summary>
        /// 自增更新表的某列字段 列 UPDATE User SET Name=Name+"zhang"   表达式为 context.User.RestValue(x=>x.Name==(x.Name+"zhang"))
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="updateValues"></param>
        /// <returns></returns>
        public static int RestValue<T>(this IQueryable<T> query, Expression<Func<T, bool>> updateValues) where T : class, new()
        {
            var context = EFCoreHelper.GetDbContext(query);
            var (sql, sp) = GetSqlUpdate(query, updateValues);
            return context.Database.ExecuteSqlCommand(sql, sp);
        }

        /// <summary>
        /// 自增更新表的某列字段 列 UPDATE User SET Name=Name+"zhang"   表达式为 context.User.RestValue(x=>x.Name==(x.Name+"zhang"))
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="updateValues"></param>
        /// <returns></returns>
        public static async Task<int> RestValueAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> updateValues) where T : class, new()
        {
            var context = EFCoreHelper.GetDbContext(query);
            var (sql, sp) = GetSqlUpdate(query, updateValues);
            return await context.Database.ExecuteSqlCommandAsync(sql, sp);
        }

        /// <summary>
        /// 获取更新的sql语句
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="updateValues"></param>
        /// <returns></returns>
        public static (string, List<SqlParameter>) GetSqlUpdate<T>(IQueryable<T> query, Expression<Func<T, bool>> updateValues) where T : class, new()
        {
            (string sql, string tableAlias) = GetBatchSql(query);
            var sb = new StringBuilder();
            var sp = new List<SqlParameter>();
            CreateUpdateBody(tableAlias, updateValues.Body, ref sb, ref sp);
            return ($"UPDATE [{tableAlias}] SET {sb.ToString()} {sql}", sp);
        }

        /// <summary>
        /// 递归解析表达式 
        /// </summary>
        /// <param name="tableAlias"></param>
        /// <param name="expression"></param>
        /// <param name="sb"></param>
        /// <param name="sp"></param>
        internal static void CreateUpdateBody(string tableAlias, Expression expression, ref StringBuilder sb, ref List<SqlParameter> sp)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                CreateUpdateBody(tableAlias, binaryExpression.Left, ref sb, ref sp);

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

                CreateUpdateBody(tableAlias, binaryExpression.Right, ref sb, ref sp);
            }

            if (expression is ConstantExpression constantExpression)
            {
                var parmName = $"param_{sp.Count}";
                sp.Add(new SqlParameter(parmName, constantExpression.Value));
                sb.Append($" @{parmName}");
            }

            if (expression is MemberExpression memberExpression)
            {
                sb.Append($"[{tableAlias}].[{memberExpression.Member.Name}]");
            }
        }

        /// <summary>
        /// 获取查询语句
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        internal static (string, string) GetBatchSql<T>(IQueryable<T> query) where T : class, new()
        {
            string sqlQuery = query.ToSql();
            string tableAlias = sqlQuery.Substring(8, sqlQuery.IndexOf("]") - 8);
            int indexFROM = sqlQuery.IndexOf(Environment.NewLine);
            string sql = sqlQuery.Substring(indexFROM, sqlQuery.Length - indexFROM);
            sql = sql.Contains("{") ? sql.Replace("{", "{{") : sql; // Curly brackets have to escaped:
            sql = sql.Contains("}") ? sql.Replace("}", "}}") : sql; // https://github.com/aspnet/EntityFrameworkCore/issues/8820
            return (sql, tableAlias);
        }
    }
}
