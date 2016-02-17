using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using TestProject.Helpers;

namespace TestProject.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FilterFields : ActionFilterAttribute
    {
        public const string FilterFieldsParameterFromQueryString = "fields";
        public const char ParametersSeparator = ',';
        public const char FieldsSeparator = '.';

        private const string ActionExecutingTaskKey = "ActionExecutionTask";
        private const string RequestedPropertiesKey = "RequestedProperties";

        private readonly SerializationFilterDecorator _filterDecorator;

        public FilterFields()
        {
            _filterDecorator = new SerializationFilterDecorator();
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            //object taskObject;
            //var actionArguments = actionExecutedContext.ActionContext.ActionArguments;
            //if (actionExecutedContext.Response == null || !actionArguments.TryGetValue(ActionExecutingTaskKey, out taskObject))
            //{
            //    return;
            //}

            //var content = actionExecutedContext.Response.Content as ObjectContent;
            //if (content != null)
            //{
            //    WaitForActionExecutingTask((Task)taskObject);
            //    var requestedProperties = (HashSet<string>)actionArguments[RequestedPropertiesKey];
            //    if (requestedProperties == null)
            //    {
            //        return;
            //    }

            //    var returnType = actionExecutedContext.ActionContext.ActionDescriptor.ReturnType;
            //    var contentValue = _filterDecorator.GetDecorator(content.Value, returnType, requestedProperties);

            //    actionExecutedContext.Response.Content = new ObjectContent(contentValue.GetDecoratorType(), contentValue, content.Formatter);
            //}

            var query = actionExecutedContext.Request.RequestUri.Query;
            var queryParameters = HttpUtility.ParseQueryString(query);
            if (queryParameters[FilterFieldsParameterFromQueryString] != null)
            {
                var queryString = queryParameters[FilterFieldsParameterFromQueryString];
                var routes = queryString
                .Split(new[] { ParametersSeparator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Split(new[] { FieldsSeparator }, StringSplitOptions.RemoveEmptyEntries)).ToArray();

                var instanceType = actionExecutedContext.ActionContext.ActionDescriptor.ReturnType;
                var content = actionExecutedContext.Response.Content as ObjectContent;
                if (content != null)
                {
                    var instance = content.Value;
                    object newContent;

                    var list = new List<object>();
                    var enumerable = instance as IEnumerable;
                    if (enumerable != null)
                    {
                        var undT = instanceType.GetGenericArguments()[0];
                        foreach (var item in enumerable)
                        {
                            list.Add(TreeSerializer.BuildTree("RootNode", routes, undT, item));
                        }
                        newContent = list;
                    }
                    else
                    {
                        newContent = TreeSerializer.BuildTree("RootNode", routes, instanceType, instance);
                    }

                    //actionExecutedContext.Response.Content = new ObjectContent(newContent.GetType(), newContent, content.Formatter);
                    content.Value = newContent;
                }
            }
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            //actionContext.ActionArguments.Add(ActionExecutingTaskKey,
            //    Task.Factory.StartNew(context =>
            //    {
            //        var httpActionContext = (HttpActionContext) context;
            //        var requestedProperties = ParseFieldsNamesFromQueryString(httpActionContext.Request.RequestUri.Query);
            //        if (requestedProperties == null)
            //        {
            //            return;
            //        }

            //        //var returnType = httpActionContext.ActionDescriptor.ReturnType;
            //        //_filterDecorator.PrepareDecorator(returnType, requestedProperties);

            //        httpActionContext.ActionArguments.Add(RequestedPropertiesKey, requestedProperties);
            //        base.OnActionExecuting(actionContext);
            //    }, actionContext));
        }

        private static HashSet<string> ParseFieldsNamesFromQueryString(string queryString)
        {
            var queryParameters = HttpUtility.ParseQueryString(queryString);
            if (queryParameters[FilterFieldsParameterFromQueryString] != null)
            {
                var queryParams = queryParameters[FilterFieldsParameterFromQueryString].Split(ParametersSeparator);
                Array.Sort(queryParams);
                return new HashSet<string>(queryParams);
            }
            return null;
        }

        private static void WaitForActionExecutingTask(Task task)
        {
            if (task != null)
            {
                task.GetAwaiter().GetResult();
            }
        }
    }
}
