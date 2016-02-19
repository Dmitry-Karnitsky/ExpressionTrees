using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http.Filters;
using TestProject.Helpers;

namespace TestProject.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FilterFields : ActionFilterAttribute
    {
        public const string FilterFieldsParameterFromQueryString = "fields";
        public const char ParametersSeparator = ',';
        public const char FieldsSeparator = '.';

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext == null)
                throw new ArgumentNullException("actionExecutedContext");
            if (actionExecutedContext.ActionContext == null)
                throw new ArgumentException("Action executed context must have action context.");
            var actionDescriptor = actionExecutedContext.ActionContext.ActionDescriptor;
            if (actionDescriptor == null)
                throw new ArgumentException("Action context must have descriptor.");
            var response = actionExecutedContext.Response;
            if (response == null || !response.IsSuccessStatusCode)
                return;
            var content = response.Content as ObjectContent;
            if (content == null)
                throw new ArgumentException("Filtering requires object content.");


            var query = actionExecutedContext.Request.RequestUri.Query;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var queryParameters = HttpUtility.ParseQueryString(query);
            if (queryParameters[FilterFieldsParameterFromQueryString] != null)
            {
                var queryString = queryParameters[FilterFieldsParameterFromQueryString];
                var routes = queryString
                    .Split(new[] { ParametersSeparator }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Split(new[] { FieldsSeparator }, StringSplitOptions.RemoveEmptyEntries));

                var instanceType = actionExecutedContext.ActionContext.ActionDescriptor.ReturnType;
                var instance = content.Value;
                stopwatch.Stop();
                Type newContentType;
                stopwatch.Start();
                var newContent = TreeSerializer.BuildFilteredObjectTree(instance, instanceType, routes, out newContentType);
                stopwatch.Stop();
                //actionExecutedContext.Response.Content = new ObjectContent(newContentType, newContent, content.Formatter);
                content.Value = newContent;
            }
        }
    }
}
