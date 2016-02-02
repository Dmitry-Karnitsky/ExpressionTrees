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
        private const string DesiredFieldsParameterFromQueryString = "fields";

        private readonly CachableSerializationFilterDecorator _filterDecorator;

        private HashSet<string> _requestedProperties;
        private Type _returnType;
        private Task _actionExecutionTask;

        public FilterFields()
        {
            _filterDecorator = new CachableSerializationFilterDecorator();
        }

        // if not in cache:
        // Action executed in 7.0007ms
        // if cached
        // Action executed in 6.0006ms
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var content = actionExecutedContext.Response.Content as ObjectContent;
            if (content != null)
            {
                WaitForActionExecutingTask();
                var contentValue = _filterDecorator.GetDecorator(content.Value, _returnType, _requestedProperties);
                actionExecutedContext.Response.Content = new ObjectContent(contentValue.GetDecoratorType(), contentValue, content.Formatter);
            }
            else
            {
                throw new Exception(); // for debug purposes only
            }
        }

        // if not in cache:
        // Action executing 64.0064ms
        // if cached
        // Action executing 3.0003ms
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            _actionExecutionTask =
                Task.Factory.StartNew(() =>
                {
                    _requestedProperties = ParseFieldsNamesFromQueryString(actionContext.Request.RequestUri.Query);
                    _returnType = actionContext.ActionDescriptor.ReturnType;
                    _filterDecorator.PrepareDecorator(_returnType, _requestedProperties);
                })
                    .ContinueWith(prevTask =>
                    {
                        base.OnActionExecuting(actionContext);
                    });
        }

        private HashSet<string> ParseFieldsNamesFromQueryString(string queryString)
        {
            var param = HttpUtility.ParseQueryString(queryString);
            if (param[DesiredFieldsParameterFromQueryString] != null)
            {
                var queryParams = param[DesiredFieldsParameterFromQueryString].Split(',');
                Array.Sort(queryParams);
                return new HashSet<string>(queryParams);
            }
            return null;
        }

        private void WaitForActionExecutingTask()
        {
            if (_actionExecutionTask != null)
            {
                _actionExecutionTask.GetAwaiter().GetResult();
            }
        }
    }

}
