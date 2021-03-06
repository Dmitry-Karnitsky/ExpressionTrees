﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using TestProject.Helpers;

namespace TestProject.Models
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FilterFields : ActionFilterAttribute
    {
        public const string FilterFieldsParameterFromQueryString = "fields";
        public const char ParametersSeparator = ',';

        private const string ActionExecutingTaskKey = "ActionExecutionTask";
        private const string RequestedPropertiesKey = "RequestedProperties";

        private readonly CachableSerializationFilterDecorator _filterDecorator;

        public FilterFields()
        {
            _filterDecorator = new CachableSerializationFilterDecorator();
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            object taskObject;
            var actionArguments = actionExecutedContext.ActionContext.ActionArguments;
            if (actionExecutedContext.Response == null || !actionArguments.TryGetValue(ActionExecutingTaskKey, out taskObject))
            {
                return;
            }

            var content = actionExecutedContext.Response.Content as ObjectContent;
            if (content != null)
            {
                WaitForActionExecutingTask((Task)taskObject);
                var requestedProperties = (HashSet<string>)actionArguments[RequestedPropertiesKey];
                if (requestedProperties == null)
                {
                    return;
                }

                var returnType = actionExecutedContext.ActionContext.ActionDescriptor.ReturnType;
                var contentValue = _filterDecorator.GetDecorator(content.Value, returnType, requestedProperties);

                actionExecutedContext.Response.Content = new ObjectContent(contentValue.GetDecoratorType(), contentValue, content.Formatter);
            }
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            actionContext.ActionArguments.Add(ActionExecutingTaskKey,
                Task.Factory.StartNew(context =>
                {
                    var httpActionContext = (HttpActionContext) context;
                    var requestedProperties = ParseFieldsNamesFromQueryString(httpActionContext.Request.RequestUri.Query);
                    if (requestedProperties == null)
                    {
                        return;
                    }

                    var returnType = httpActionContext.ActionDescriptor.ReturnType;
                    _filterDecorator.PrepareDecorator(returnType, requestedProperties);

                    httpActionContext.ActionArguments.Add(RequestedPropertiesKey, requestedProperties);
                    base.OnActionExecuting(actionContext);
                }, actionContext));
        }

        private static HashSet<string> ParseFieldsNamesFromQueryString(string queryString)
        {
            var queryParameters = HttpUtility.ParseQueryString(queryString);
            if (queryParameters[FilterFieldsParameterFromQueryString] != null)
            {
                var queryParams = queryParameters[FilterFieldsParameterFromQueryString].Split(ParametersSeparator);
                Array.Sort(queryParams);
                return new HashSet<string>(queryParams, new PropertiesComparer());
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

        #region PropertiesComparer

        private class PropertiesComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (x == null)
                    throw new ArgumentNullException("x");
                if (y == null)
                    throw new ArgumentNullException("y");

                return String.Equals(x.Trim(), y.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                if (obj == null)
                {
                    throw new ArgumentNullException("obj");
                }

                return obj.GetHashCode();
            }
        }

        #endregion
    }
}
