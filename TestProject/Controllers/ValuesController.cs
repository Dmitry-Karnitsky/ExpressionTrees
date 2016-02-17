﻿using System;
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

        [FilterFields]
        [HttpGet]
        public IEnumerable<Root> FilterObjectProperties()
        {
            return Builder.GetInstance();

            //var list = new List<ObjectWithManyProperties>();
            //for (var i = 0; i < 5; i++)
            //{
            //    list.Add(BuildObject());
            //}
            //return new ObjectWithEnumerableInside
            //{
            //    Prop1 = "SomeString", 
            //    Prop2 = 10.5,
            //    Prop3 = 6,
            //    Prop4 = list
            //};
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        private ObjectWithManyProperties BuildObject()
        {
            return new ObjectWithManyProperties
            {
                Property1 = "Property1",
                Property2 = 10.5,
                Property3 = 4,
                Property4 = new ObjectWithManyProperties { Property3 = 199 },
                Property5 = "Property5",
                Property6 = "Property6",
                Property7 = "Property7",
                Property8 = "Property8",
                Property9 = "Property9",
                Property11 = "Property11",
                Property22 = 10000.1,
                Property33 = 888888,
            };
        }
    }
}
