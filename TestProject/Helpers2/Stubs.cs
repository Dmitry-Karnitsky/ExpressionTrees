// ReSharper Disable All

using System.Collections.Generic;

namespace TestProject.Helpers2
{
    public static class Builder
    {
        private static Root instance;

        static Builder()
        {
            var class1list = new List<Class1>();
            for (var i = 0; i < 300; i++)
            {
                var class1 = new Class1 { Class1Int = i, Class1String = "ValueString_" + i }; // Prop1
                class1list.Add(class1);
            }


            var class2list = new List<Class2>();
            for (var i = 0; i < 50; i++)
            {
                var class2_1_1 = new Class2_1_1 { Abc = "ValueAbc" };
                var class2_1_2 = new Class2_1_2 { Def = 5 };
                var class2_1 = new Class2_1 { Field1 = class2_1_1, Field2 = class2_1_2 };
                var class2_2 = new Class2_2 { Field1 = new Class2_2_1(), Field2 = new Class2_2_1() };
                var class2 = new Class2 { InnerProp1 = class2_1, InnerProp2 = class2_2, InnerProp3 = "InnerProp3StringValue" }; // Prop2
                var k = i;
                class2.InnerProp2.Field2.Class2_2_1Int = k;
                class2.InnerProp3 += "_" + i;
                class2list.Add(class2);
            }


            var class3_1_1 = new Class3_1_1 { Abc = 2 };
            var class3_1_2 = new Class3_1_2 { Def = "DefStringValue", Hkl = 3.6 };
            var class3_1 = new Class3_1 { IntVal = class3_1_1, DoubleVal = class3_1_2 };
            var class3_2 = new Class3_2 { Class3_2String = "Field2StringValue" };

            var class3 = new Class3 { Field1 = class3_1, Field2 = class3_2 }; // Prop3

            var class4 = new Class4 { Class4Double = 56.8989 };
            var class5 = new Class5 { Class5Double = 9898.11111, Class5Int = 88888, Class5String = "SomeString" };

            instance = new Root { Prop1 = class1list, Prop2 = class2list, Prop3 = class3, Prop4 = class4, Prop5 = class5 };
        }

        public static IEnumerable<Root> GetInstance()
        {
            var list = new List<Root>();
            for (var i = 0; i < 20; i++)
            {
                instance.Prop4.Class4Double = i;
                list.Add(instance);
            }
            return list;
        }
    }

    public class Root
    {
        public IEnumerable<Class1> Prop1 { get; set; }
        public IEnumerable<Class2> Prop2 { get; set; }
        public Class3 Prop3 { get; set; }
        public Class4 Prop4 { get; set; }
        public Class5 Prop5 { get; set; }
    }

    public class Class1
    {
        public string Class1String { get; set; }
        public int Class1Int { get; set; }
    }

    public class Class2
    {
        public Class2_1 InnerProp1 { get; set; }
        public Class2_2 InnerProp2 { get; set; }
        public string InnerProp3 { get; set; }
    }

    public class Class3
    {
        public Class3_1 Field1 { get; set; }
        public Class3_2 Field2 { get; set; }
    }

    public class Class4
    {
        public double Class4Double { get; set; }
    }

    public class Class5
    {
        public double Class5Double { get; set; }
        public int Class5Int { get; set; }
        public string Class5String { get; set; }
    }


    public class Class2_1
    {
        public Class2_1_1 Field1 { get; set; }
        public Class2_1_2 Field2 { get; set; }
    }

    public class Class2_1_1
    {
        public string Abc { get; set; }
    }

    public class Class2_1_2
    {
        public int Def { get; set; }
    }

    public class Class2_2
    {
        public Class2_2_1 Field1 { get; set; }
        public Class2_2_1 Field2 { get; set; }
    }

    public class Class2_2_1
    {
        public string Class2_2_1String { get; set; }
        public int Class2_2_1Int { get; set; }
    }

    public class Class3_1
    {
        public Class3_1_1 IntVal { get; set; }
        public Class3_1_2 DoubleVal { get; set; }
    }

    public class Class3_1_1
    {
        public int Abc { get; set; }
    }

    public class Class3_1_2
    {
        public string Def { get; set; }
        public double Hkl { get; set; }
    }

    public class Class3_2
    {
        public string Class3_2String { get; set; }
    }

}