using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace TestProject.Controllers
{
    public class HomeController : Controller
    {
        private const string RequestUrl = @"http://localhost:50194/api/Values/?fields=Prop1,Prop2.InnerProp2.Field1,Prop2.InnerProp2.Field2,Prop2.InnerProp3,Prop3.Field1.IntVal.Abc,Prop3.Field1.DoubleVal.Def,Prop3.Field1.DoubleVal.Hkl,Prop3.Field2,Prop4,Prop5&attr1=somevalue&attr2=somevalue2";

        public ActionResult Index()
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

            string result;
            using (var client = new WebClient())
            {
                var request = (HttpWebRequest)WebRequest.Create(RequestUrl);
                var localResponse = (HttpWebResponse)request.GetResponse();
                var resStream = localResponse.GetResponseStream();
                var content = new byte[80000];
                int numberOfReadedBytes = 0;
                if (resStream != null)
                {
                    numberOfReadedBytes = resStream.Read(content, 0, 80000);
                }

                var localResult = Encoding.UTF8.GetString(content, 0, numberOfReadedBytes);

                client.Headers["Accept"] = "application/json, text/javascript, */*; q=0.01";
                var response =
                client.UploadValues("https://jsonformatter.curiousconcept.com/process", new NameValueCollection
                       {
                           { "jsondata", localResult },
                           { "jsonstandard", "0" },
                           { "jsontemplate", "0" }
                       });
                result = Encoding.UTF8.GetString(response);
            }

            string resultJson = string.Empty;

            var responseResult = serializer.DeserializeObject(result) as Dictionary<string, object>;
            if (responseResult != null)
            {
                var innerResult = responseResult["result"] as Dictionary<string, object>;
                if (innerResult != null)
                {
                    resultJson = innerResult["json"] as string;
                }
            }

            ViewBag.FormattedJson = resultJson;

            return View();
        }
    }
}
