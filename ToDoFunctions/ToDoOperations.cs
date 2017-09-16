using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Diagnostics;
using static ToDoFunctions.Utility;
using System.Collections.Generic;

namespace ToDoFunctions
{
    public static class ToDoOperations
    {

        [FunctionName("GetAllToDos")]
        public static HttpResponseMessage GetAllToDos([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/todos")]HttpRequestMessage req, [Table("todotable", Connection = "MyTable")]IQueryable<ToDoItem> inTable, TraceWriter log)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var includeCompleted = !req.GetQueryNameValuePairs().Any(i => i.Key == "includecompleted" && i.Value == "false");
            var includeActive = !req.GetQueryNameValuePairs().Any(i => i.Key == "includeactive" && i.Value == "false");

            var items = inTable
                .Where(p => (p.IsComplete == false || includeCompleted) && (p.IsComplete == true || includeActive)).ToList()
                .Select(i => i.MapFromTableEntity()).ToList();

            stopWatch.Stop();

            var metrics = new Dictionary<string, double> {
                { "processingTime", stopWatch.Elapsed.TotalMilliseconds}
            };

            TelemetryClientInstance.TrackEvent("get-all-todos", metrics: metrics);
            return req.CreateResponse(HttpStatusCode.OK, items);
        }

        [FunctionName("GetToDo")]
        public static HttpResponseMessage GetToDo([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/todos/{id}")]HttpRequestMessage req, [Table("todotable", Connection = "MyTable")]CloudTable table, string id, TraceWriter log)
        {
            Stopwatch stopWatch = new Stopwatch();
            var item = Utility.GetToDoFromTable(table, id);

            stopWatch.Stop();

            var metrics = new Dictionary<string, double> {
                { "processingTime", stopWatch.Elapsed.TotalMilliseconds}
            };

            var props = new Dictionary<string, string> {
                { "todo-id", id}
            };

            TelemetryClientInstance.TrackEvent("get-todo", metrics: metrics);

            return req.CreateResponse(HttpStatusCode.OK, item);
        }

        [FunctionName("CreateToDo")]
        public static async Task<HttpResponseMessage> CreateToDo([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/todos")]HttpRequestMessage req, [Table("todotable", Connection = "MyTable")]CloudTable table, TraceWriter log, ExecutionContext context)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                var json = await req.Content.ReadAsStringAsync();
                var todo = JsonConvert.DeserializeObject<ToDo>(json);
                Utility.AddOrUpdateToDoToTable(table, todo);
                stopWatch.Stop();

                var metrics = new Dictionary<string, double> {
                    { "processingTime", stopWatch.Elapsed.TotalMilliseconds}
                };

                var props = new Dictionary<string, string> {
                     { "todo-id", todo.id}
                };

                TelemetryClientInstance.TrackEvent("create-todo", metrics: metrics);
                return req.CreateResponse(HttpStatusCode.Created, todo);
            }
            catch (Exception e)
            {
                TelemetryClientInstance.TrackException(e);
                return req.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [FunctionName("UpdateToDo")]
        public static async Task<HttpResponseMessage> UpdateToDo([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "api/todos/{id}")]HttpRequestMessage req, [Table("todotable", Connection = "MyTable")]CloudTable table, string id, TraceWriter log)
        {
            Stopwatch stopWatch = new Stopwatch();

            var json = await req.Content.ReadAsStringAsync();
            var item = JsonConvert.DeserializeObject<ToDo>(json);

            var oldItem = Utility.GetToDoFromTable(table, id);
            item.id = id; // ensure item id matches id passed in
            item.isComplete = oldItem.isComplete; // ensure we don't change isComplete

            Utility.AddOrUpdateToDoToTable(table, item);

            stopWatch.Stop();

            var metrics = new Dictionary<string, double> {
                { "processingTime", stopWatch.Elapsed.TotalMilliseconds}
            };

            var props = new Dictionary<string, string> {
                { "todo-id", id}
            };

            TelemetryClientInstance.TrackEvent("update-todo", properties: props, metrics: metrics);
            return req.CreateResponse(HttpStatusCode.OK, item);
        }

        [FunctionName("SetCompleteToDo")]
        public static async Task<HttpResponseMessage> SetCompleteToDo([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "api/todos/{id}")]HttpRequestMessage req, [Table("todotable", Connection = "MyTable")]CloudTable table, string id, TraceWriter log)
        {
            Stopwatch stopWatch = new Stopwatch();
            var json = await req.Content.ReadAsStringAsync();
            var item = JsonConvert.DeserializeObject<ToDo>(json);

            var oldItem = Utility.GetToDoFromTable(table, id);
            oldItem.isComplete = item.isComplete;

            Utility.AddOrUpdateToDoToTable(table, oldItem);
            stopWatch.Stop();

            var metrics = new Dictionary<string, double> {
                { "processingTime", stopWatch.Elapsed.TotalMilliseconds}
            };

            var props = new Dictionary<string, string> {
                { "todo-id", id}
            };

            TelemetryClientInstance.TrackEvent("complete-todo", properties: props, metrics: metrics);

            return req.CreateResponse(HttpStatusCode.OK, oldItem);
        }

        [FunctionName("DeleteToDo")]
        public static HttpResponseMessage DeleteToDo([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "api/todos/{id}")]HttpRequestMessage req, string id, [Table("todotable", Connection = "MyTable")]CloudTable table, TraceWriter log)
        {
            Stopwatch stopWatch = new Stopwatch();
            Utility.DeleteToDoFromTable(table, id);

            stopWatch.Stop();

            var metrics = new Dictionary<string, double> {
                { "processingTime", stopWatch.Elapsed.TotalMilliseconds}
            };

            var props = new Dictionary<string, string> {
                { "todo-id", id}
            };

            TelemetryClientInstance.TrackEvent("delete-todo", properties: props, metrics: metrics);

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}