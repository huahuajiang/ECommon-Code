using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ECommon.Dapper
{
    public static class SqlMapperExtensions
    {
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> _paramCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();

        public static long Insert(this IDbConnection connection,dynamic data,string table,IDbTransaction transaction=null,int? commandTimeout = null)
        {
            var obj = data as object;
            var properties = GetProperties(obj);
            var columns = string.Join(",", properties);
            var values = string.Join(",", properties.Select(p => "@" + p));
            var sql = string.Format("insert into {0} ({1}) values ({2})", table, columns, values);
            return connection.ExecuteScalar<long>(sql, obj, transaction, commandTimeout);
        }

        public static Task<long> InsertAsync(this IDbConnection connection, dynamic data, string table, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var obj = data as object;
            var properties = GetProperties(obj);
            var columns = string.Join(",", properties);
            var values = string.Join(",", properties.Select(p => "@" + p));
            var sql = string.Format("insert into {0} ({1}) values ({2})", table, columns, values);

            return connection.ExecuteScalarAsync<long>(sql, obj, transaction, commandTimeout);
        }

        public static int Update(this IDbConnection connection,dynamic data,dynamic condition,string table, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var obj = data as object;
            var conditionObj = condition as object;

            var updatePropertyInfos = GetPropertyInfos(obj);
            var wherePropertyInfos = GetPropertyInfos(conditionObj);

            var updateProperties = updatePropertyInfos.Select(p => p.Name);
            var whereProperties = wherePropertyInfos.Select(p => p.Name);

            var updateFields = string.Join(",", updateProperties.Select(p => p + " = @" + p));
            var whereFields = string.Empty;

            if (whereProperties.Any())
            {
                whereFields = " where " + string.Join(" and ", whereProperties.Select(p => p + " = @w_" + p));
            }

            var sql = string.Format("update {0} set {1}{2}", table, updateFields, whereFields);
            var parameters = new DynamicParameters(data);
            var expandoObject = new ExpandoObject() as IDictionary<string, object>;
            wherePropertyInfos.ForEach(p => expandoObject.Add("w_" + p.Name, p.GetValue(conditionObj, null)));
            parameters.AddDynamicParams(expandoObject);

            return connection.Execute(sql, parameters, transaction, commandTimeout);
        }

        public static Task<int> UpdateAsync(this IDbConnection connection, dynamic data, dynamic condition, string table, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var obj = data as object;
            var conditionObj = condition as object;

            var updatePropertyInfos = GetPropertyInfos(obj);
            var wherePropertyInfos = GetPropertyInfos(conditionObj);

            var updateProperties = updatePropertyInfos.Select(p => p.Name);
            var whereProperties = wherePropertyInfos.Select(p => p.Name);

            var updateFields = string.Join(",", updateProperties.Select(p => p + " = @" + p));
            var whereFields = string.Empty;

            if (whereProperties.Any())
            {
                whereFields = " where " + string.Join(" and ", whereProperties.Select(p => p + " = @w_" + p));
            }

            var sql = string.Format("update {0} set {1}{2}", table, updateFields, whereFields);

            var parameters = new DynamicParameters(data);
            var expandoObject = new ExpandoObject() as IDictionary<string, object>;
            wherePropertyInfos.ForEach(p => expandoObject.Add("w_" + p.Name, p.GetValue(conditionObj, null)));
            parameters.AddDynamicParams(expandoObject);

            return connection.ExecuteAsync(sql, parameters, transaction, commandTimeout);
        }

        public static int Delete(this IDbConnection connection, dynamic condition, string table, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var conditionObj = condition as object;
            var whereFields = string.Empty;
            var whereProperties = GetProperties(conditionObj);
            if (whereProperties.Count > 0)
            {
                whereFields = " where " + string.Join(" and ", whereProperties.Select(p => p + " = @" + p));
            }

            var sql = string.Format("delete from {0}{1}", table, whereFields);

            return connection.Execute(sql, conditionObj, transaction, commandTimeout);
        }

        public static Task<int> DeleteAsync(this IDbConnection connection, dynamic condition, string table, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var conditionObj = condition as object;
            var whereFields = string.Empty;
            var whereProperties = GetProperties(conditionObj);
            if (whereProperties.Count > 0)
            {
                whereFields = " where " + string.Join(" and ", whereProperties.Select(p => p + " = @" + p));
            }

            var sql = string.Format("delete from {0}{1}", table, whereFields);

            return connection.ExecuteAsync(sql, conditionObj, transaction, commandTimeout);
        }

        private static List<string> GetProperties(object obj)
        {
            if (obj == null)
            {
                return new List<string>();
            }
            if(obj is DynamicParameters)
            {
                return (obj as DynamicParameters).ParameterNames.ToList();
            }
            return GetPropertyInfos(obj).Select(x => x.Name).ToList();
        }

        private static List<PropertyInfo> GetPropertyInfos(object obj)
        {
            if (obj == null)
            {
                return new List<PropertyInfo>();
            }
            List<PropertyInfo> properties;
            if (_paramCache.TryGetValue(obj.GetType(), out properties)) return properties.ToList();
            properties = obj.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public).ToList();
            _paramCache[obj.GetType()] = properties;
            return properties;
        }
    }
}
