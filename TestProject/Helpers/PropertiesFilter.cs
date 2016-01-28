using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TestProject.Helpers
{
    public static class PropertiesFilter
    {
        /*
         * Timings from browser (waiting for response step):
         * first start: 35-40 ms
         * next starts: 3-5 ms
        */

        public static void PrepareDelegate(Type objectType, HashSet<string> propertiesNames)
        {
            var argumentType = GetRealType(objectType);
            GetCompiledLambda(argumentType, propertiesNames);
        }

        public static object FilterFields(this object instance, Type instanceType, HashSet<string> propertiesNames)
        {
            return GetFilteredObject(instance, instanceType, propertiesNames);
        }

        private static object GetFilteredObject(object instance, Type instanceType, HashSet<string> propertiesNames)
        {
            var enumerable = instance as IEnumerable;
            if (enumerable != null)
            {
                var argumentType = GetRealType(instanceType);

                var list = new List<Dictionary<string, object>>();
                if (argumentType != null)
                {
                    var lambda = GetCompiledLambda(argumentType, propertiesNames);
                    try
                    {
                        list.AddRange(from object item in enumerable select lambda.Invoke(item));

                        // to retrive some properties if expression tree was constructed to get all of it
                        //list.AddRange(from object item in enumerable select lambda.Invoke(item) into props select props.Where(property => propertiesNames.Contains(property.Key)));
                    }
                    catch (Exception e)
                    {
                        var exc = e; // for debug purposes only
                    }
                }

                var retVal = list.ToArray();
                return retVal;
            }
            else
            {
                var lambda = GetCompiledLambda(instanceType, propertiesNames).Invoke(instance);
                return lambda;
            }
        }

        private static Func<object, Dictionary<string, object>> GetCompiledLambda(Type instanceType,
            HashSet<string> propertiesNames)
        {
            var propertieskey = String.Join("|", propertiesNames);
            if (IsInCache(instanceType, propertieskey))
            {
                return Cache[instanceType][propertieskey];
            }

            var func = (Func<object, Dictionary<string, object>>)GetObjectFields(instanceType, propertiesNames).Compile();
            if (!IsInCache(instanceType))
            {
                Cache[instanceType] = new Dictionary<string, Func<object, Dictionary<string, object>>>();
            }

            Cache[instanceType][propertieskey] = func;
            return func;
        }

        private static LambdaExpression GetObjectFields(Type parameterType, HashSet<string> propertiesNames)
        {
            var propertyInfos = GetObjectProperties(parameterType);

            var numberOfProperties = propertyInfos.Length;
            var expressionBlockStatements = new List<Expression>(numberOfProperties);

            var dictionaryConstructorCapacity = Expression.Constant(numberOfProperties, TypeOfInt);
            var dictionaryConstructor = Expression.New(DictionaryConstructorInfo, dictionaryConstructorCapacity);
            var dictionaryVariable = Expression.Parameter(TypeOfDictionary, "fieldsDictionary");
            var dictionaryAssigment = Expression.Assign(dictionaryVariable, dictionaryConstructor);

            var parameter = Expression.Parameter(TypeOfObject, "obj");

            var castedParameterVariable = Expression.Parameter(parameterType, "castedObj");
            var castedParameter = Expression.TypeAs(parameter, parameterType);
            var castAssigment = Expression.Assign(castedParameterVariable, castedParameter);

            var returnTarget = Expression.Label(TypeOfDictionary, "returnLabel");
            var returnExpression = Expression.Label(returnTarget, dictionaryVariable);
            var returnStatement = Expression.Return(returnTarget, dictionaryVariable, TypeOfDictionary);

            var nullConstant = Expression.Constant(null);

            var ifCastedsuccessfullyAddToDictionaryElseGoToReturn = Expression.IfThen(Expression.Equal(castedParameterVariable, nullConstant),
                returnStatement);

            expressionBlockStatements.Add(dictionaryVariable);
            expressionBlockStatements.Add(dictionaryAssigment);
            expressionBlockStatements.Add(castedParameterVariable);
            expressionBlockStatements.Add(castAssigment);
            expressionBlockStatements.Add(ifCastedsuccessfullyAddToDictionaryElseGoToReturn);

            foreach (var propertyInfo in propertyInfos.Where(pi => propertiesNames.Contains(pi.Name)))
            {
                var propertyGetterInfo = propertyInfo.GetGetMethod(false);
                if (propertyGetterInfo != null && propertyGetterInfo.GetParameters().Length == 0)
                {
                    var propertyName = propertyInfo.Name;
                    var propertyNameConstant = Expression.Constant(propertyName);
                    var getterCall = Expression.Call(castedParameterVariable, propertyGetterInfo);
                    var convertGetterValueToObject = Expression.Convert(getterCall, TypeOfObject);
                    var addToDictionary = Expression.Call(dictionaryVariable, DictionaryAddMethodInfo,
                        propertyNameConstant, convertGetterValueToObject);

                    expressionBlockStatements.Add(addToDictionary);
                }
            }

            expressionBlockStatements.Add(returnStatement);
            expressionBlockStatements.Add(returnExpression);

            var expressionBlock = Expression.Block(TypeOfDictionary, new[] { dictionaryVariable, castedParameterVariable },
                expressionBlockStatements);

            var lambda = Expression.Lambda(expressionBlock, parameter);

            return lambda;
        }

        private static PropertyInfo[] GetObjectProperties(Type objectType)
        {
            return objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        private static Type GetRealType(Type objectType)
        {
            Type argumentType;
            if (TypeOfIEnumerable.IsAssignableFrom(objectType))
            {
                argumentType = objectType.IsGenericType
                    ? objectType.GetGenericArguments()[0]
                    : TypeOfObject;
            }
            else
            {
                argumentType = objectType;
            }
            return argumentType.IsClass ? argumentType : TypeOfObject;
        }

        private static bool IsInCache(Type instanceType)
        {
            return Cache.ContainsKey(instanceType);
        }

        private static bool IsInCache(Type instanceType, string propertiesKey)
        {
            if (Cache.ContainsKey(instanceType))
            {
                return (Cache[instanceType].ContainsKey(propertiesKey));
            }
            return false;
        }

        #region Private Fields and Static Constructor

        static PropertiesFilter()
        {
            TypeOfIEnumerable = typeof(IEnumerable);
            TypeOfInt = typeof(int);
            TypeOfString = typeof(string);
            TypeOfObject = typeof(object);
            TypeOfDictionary = typeof(Dictionary<string, object>);

            DictionaryConstructorInfo = TypeOfDictionary.GetConstructor(new[] { TypeOfInt });
            DictionaryAddMethodInfo = TypeOfDictionary.GetMethod("Add", new[] { TypeOfString, TypeOfObject });
        }


        private static readonly Type TypeOfIEnumerable;
        private static readonly Type TypeOfInt;
        private static readonly Type TypeOfString;
        private static readonly Type TypeOfObject;
        private static readonly Type TypeOfDictionary;

        private static readonly ConstructorInfo DictionaryConstructorInfo;

        private static readonly MethodInfo DictionaryAddMethodInfo;

        private static readonly Dictionary<Type, Dictionary<string, Func<object, Dictionary<string, object>>>> Cache = new Dictionary<Type, Dictionary<string, Func<object, Dictionary<string, object>>>>();

        #endregion

        #region Reflection implementation

        //public static Dictionary<string, object> FilterFields(this object instance, params string[] propertiesName)
        //{
        //    var properties = instance.GetType().GetProperties();
        //    var dict = properties.Where(prop => propertiesName.Contains(prop.Name))
        //        .ToDictionary(prop => prop.Name, prop => prop.GetValue(instance));
        //    return dict;
        //}

        #endregion
    }

    #region TODO

    // TODO
    // make changes to expression tree create to return below type
    // expression tree should create one delegate for return type of object
    // it will be !!!fantastic!!! if class ReturnWrapper will have actual return type as T
    public class ReturnWrapper<T>
    {
        // todo: class should wrap any object
        // it should contain properties such as WrappedType, 
    }

    public class ObjectWrapper<T> : ReturnWrapper<T>
    {
        // todo: class should wrap dictionary with properties for one object
        // it should contain properties such as WrappedType, Indexer, ImplementedProperties
    }

    public class EnumerableWrapper<T> : ReturnWrapper<T>, IEnumerable<T>
    {
        // todo: class should wrap list of dictionary with properties of real object as type of ObjectWrapper
        // it should contain properties such as WrappedType, UnderlyingObjectsType, Indexer


        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}