using System;
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

        private HashSet<string> _desiredFields;
        private Type _returnType;

        private Task _actionExecutionTask;

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var content = actionExecutedContext.Response.Content as ObjectContent;
            if (content != null)
            {
                WaitForActionExecutingTask();
                var contentValue = SerializationDecoratorManager.PrepareDecorator(content.Value, _returnType, _desiredFields);
                actionExecutedContext.Response.Content = new ObjectContent(contentValue.GetDecoratorType(), contentValue, content.Formatter);
            }
            else
            {
                throw new Exception(); // for debug purposes only
            }
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            _actionExecutionTask =
                Task.Factory.StartNew(() =>
                {
                    ParseFieldsNamesFromQueryString(actionContext.Request.RequestUri.Query);
                    _returnType = actionContext.ActionDescriptor.ReturnType;
                    //PropertiesFilter.PrepareDelegate(_returnType, _desiredFields);
                })
                    .ContinueWith(prevTask =>
                    {
                        base.OnActionExecuting(actionContext);
                    });
        }

        private void ParseFieldsNamesFromQueryString(string queryString)
        {
            var param = HttpUtility.ParseQueryString(queryString);
            if (param[DesiredFieldsParameterFromQueryString] != null)
            {
                var queryParams = param[DesiredFieldsParameterFromQueryString].Split(',');
                Array.Sort(queryParams);
                _desiredFields = new HashSet<string>(queryParams);
            }
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
