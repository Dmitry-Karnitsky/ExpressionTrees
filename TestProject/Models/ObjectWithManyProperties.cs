namespace TestProject.Models
{
    public class ObjectWithManyProperties : ObjectWithNotSoManyProperties
    {
        public string Property11 { get; set; }
        public double Property22 { get; set; }
        public int Property33 { get; set; }
        public ObjectWithManyProperties Property44 { get; set; }
        public string Property55 { get; set; }
        public string Property66 { get; set; }
        public string Property77 { get; set; }
        public string Property88 { get; set; }
        public string Property99 { get; set; }
    }

    public class ObjectWithNotSoManyProperties
    {
        public string Property1 { get; set; }
        public double Property2 { get; set; }
        public int Property3 { get; set; }
        public ObjectWithManyProperties Property4 { get; set; }
        public string Property5 { get; set; }
        public string Property6 { get; set; }
        public string Property7 { get; set; }
        public string Property8 { get; set; }
        public string Property9 { get; set; }
    }
}
