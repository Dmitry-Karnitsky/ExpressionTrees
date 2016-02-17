using System;
using System.Linq;
using System.Net.Http;
using System.Web;
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

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
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

                    var newContent = TreeSerializer.BuildFilteredObjectTree(instance, instanceType, routes);

                    //actionExecutedContext.Response.Content = new ObjectContent(newContent.GetType(), newContent, content.Formatter);
                    content.Value = newContent;
                }
            }
        }
    }
}
