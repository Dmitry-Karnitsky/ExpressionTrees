using System;
using System.Collections.Generic;
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
        private const string DesiredFieldsParameterFromQueryString = "fields";

        private readonly CachableSerializationFilterDecorator _filterDecorator;

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
            object task;
            if (actionExecutedContext.ActionContext.ActionArguments.TryGetValue("ActionExecutionTask", out task))
            {
                var content = actionExecutedContext.Response.Content as ObjectContent;
                if (content != null)
                {
                    WaitForActionExecutingTask((Task)task);
                    var returnValue = (Type)actionExecutedContext.ActionContext.ActionArguments["ReturnType"];
                    var requestedProperties = (HashSet<string>)actionExecutedContext.ActionContext.ActionArguments["RequestedProperties"];
                    var contentValue = _filterDecorator.GetDecorator(content.Value, returnValue, requestedProperties);
                    actionExecutedContext.Response.Content = new ObjectContent(contentValue.GetDecoratorType(), contentValue, content.Formatter);
                }
                else
                {
                    throw new Exception(); // for debug purposes only
                }
            }
        }

        // if not in cache:
        // Action executing 64.0064ms
        // if cached
        // Action executing 3.0003ms
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            actionContext.ActionArguments.Add("ActionExecutionTask",
                Task.Factory.StartNew(context =>
                {
                    var httpActionContext = (HttpActionContext)context;
                    var requestedProperties = ParseFieldsNamesFromQueryString(httpActionContext.Request.RequestUri.Query);
                    var returnType = httpActionContext.ActionDescriptor.ReturnType;
                    _filterDecorator.PrepareDecorator(returnType, requestedProperties);

                    httpActionContext.ActionArguments.Add("RequestedProperties", requestedProperties);
                    httpActionContext.ActionArguments.Add("ReturnType", returnType);
                }, actionContext)
                    .ContinueWith(prevTask =>
                    {
                        base.OnActionExecuting(actionContext);
                    }));
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

        private void WaitForActionExecutingTask(Task task)
        {
            if (task != null)
            {
                task.GetAwaiter().GetResult();
            }
        }
    }

}
