using System;
using System.Collections.Generic;
using System.Web.Http;
using TestProject.Attributes;
using TestProject.Helpers2;
using TestProject.Models;

namespace TestProject.Controllers
{
    public class ValuesController : ApiController
    {
        //[FilterFields]
        //[HttpGet]
        //public ObjectWithNotSoManyProperties FilterObjectProperties()
        //{
        //    var ret = BuildObject();
        //    return ret;
        //}

        //[FilterFields]
        //[HttpGet]
        //public IEnumerable<Root> FilterObjectProperties()
        //{
        //    return Builder.GetEnumerableInstance();
        //}

        [FilterFields]
        [HttpGet]
        public Root1 FilterObjectProperties()
        {
            return Builder.GetInstance2();
        }
    }
}

