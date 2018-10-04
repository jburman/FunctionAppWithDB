using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FunctionAppWithDB.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FunctionAppWithDB
{
    public static class DBFunctions
    {

        static DBFunctions()
        {
            SQLitePCL.Batteries.Init();
        }

        private static IDbConnection _OpenDB(ExecutionContext context)
        {
            string dbPath = Path.Combine(context.FunctionAppDirectory, "dbfile.db");
            var sqlConn = new SqliteConnection("Data Source=" + dbPath);
            sqlConn.Open();
            return sqlConn;
        }

        [FunctionName("QuerySignups")]
        public static async Task<HttpResponseMessage> QuerySignups(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, 
            TraceWriter log, 
            ExecutionContext context)
        {
            try
            {
                string query = req.GetQueryNameValuePairs()
                    .FirstOrDefault(q => string.Compare(q.Key, "query", true) == 0)
                    .Value;

                if (!string.IsNullOrEmpty(query))
                {
                    IEnumerable<UserSignup> queryResult = null;

                    using (var db = _OpenDB(context))
                        queryResult = await db.QueryAsync<UserSignup>(query);

                    return req.CreateResponse(HttpStatusCode.OK, queryResult);
                }
                return req.CreateResponse(HttpStatusCode.OK, Array.Empty<UserSignup>());
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        [FunctionName("CreateSignup")]
        public static async Task<HttpResponseMessage> CreateSignup(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req,
            TraceWriter log,
            ExecutionContext context)
        {
            try
            {
                var requestContent = await req.Content.ReadAsStringAsync();
                var createSignup = JsonConvert.DeserializeObject<UserSignup>(requestContent);

                int insertId = 0;

                if (createSignup != null)
                {
                    using (var db = _OpenDB(context))
                        insertId = (int)await db.ExecuteScalarAsync<long>($@"INSERT INTO 
[UserSignups] ({nameof(UserSignup.ScreenName)},{nameof(UserSignup.Email)},{nameof(UserSignup.Token)},{nameof(UserSignup.Location)},{nameof(UserSignup.CreatedUTC)}) 
VALUES (@{nameof(UserSignup.ScreenName)},@{nameof(UserSignup.Email)},@{nameof(UserSignup.Token)},@{nameof(UserSignup.Location)},@{nameof(UserSignup.CreatedUTC)});
SELECT last_insert_rowid();",
                            createSignup);
                }
                return req.CreateResponse(HttpStatusCode.Created, insertId);
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        [FunctionName("CreateSignups")]
        public static async Task<HttpResponseMessage> CreateSignups(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req,
            TraceWriter log,
            ExecutionContext context)
        {
            try
            {
                var requestContent = await req.Content.ReadAsStringAsync();
                var createSignups = JsonConvert.DeserializeObject<UserSignup[]>(requestContent);

                int count = 0;

                if (createSignups != null)
                {
                    using (var db = _OpenDB(context))
                        count = await db.ExecuteAsync($@"INSERT INTO 
[UserSignups] ({nameof(UserSignup.ScreenName)},{nameof(UserSignup.Email)},{nameof(UserSignup.Token)},{nameof(UserSignup.Location)},{nameof(UserSignup.CreatedUTC)}) 
VALUES (@{nameof(UserSignup.ScreenName)},@{nameof(UserSignup.Email)},@{nameof(UserSignup.Token)},@{nameof(UserSignup.Location)},@{nameof(UserSignup.CreatedUTC)})",
                            createSignups);
                }
                return req.CreateResponse(HttpStatusCode.Created, count + " records created");
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        [FunctionName("CountSignups")]
        public static async Task<HttpResponseMessage> CountSignups(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req,
            TraceWriter log,
            ExecutionContext context)
        {
            try
            {
                int count = 0;
                using (var db = _OpenDB(context))
                    count = (int)await db.QueryFirstAsync<long>("SELECT COUNT(*) FROM [UserSignups]");

                return req.CreateResponse(HttpStatusCode.OK, new QueryResponse<int>(count));
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }

        [FunctionName("Reset")]
        public static async Task<HttpResponseMessage> Reset(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req,
            TraceWriter log,
            ExecutionContext context)
        {
            try
            {
                int count = 0;
                using (var db = _OpenDB(context))
                    count = await db.ExecuteAsync(@"DELETE FROM UserSignups;VACUUM;");

                return req.CreateResponse(HttpStatusCode.OK, new QueryResponse<int>(count));
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }
    }
}
