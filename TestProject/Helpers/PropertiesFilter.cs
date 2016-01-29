using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace TestProject.Helpers
{
    public static class PropertiesFilter
    {
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

        private static LambdaExpression GetObjectFields(Type parameterType, HashSet<string> propertiesNames, string trash)
        {
            var propertyInfos = GetObjectProperties(parameterType);

            var numberOfProperties = propertyInfos.Length;
            var expressionBlockStatements = new List<Expression>(numberOfProperties);

            var listConstructor = Expression.New(typeof(List<>).GetConstructor(new Type[0]));
            var listVariable = Expression.Parameter(typeof(List<>), "listParam");
            var listAssigment = Expression.Assign(listVariable, listConstructor);

            //var foreachLoop = Expression.Loop();

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

            //TypeOfEnumerableWrapper = typeof(EnumerableWrapper<SingleObjectWrapper<>>);
            //EnumerableWrapperConstructor;

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

    public class Wrapper<T> : IWrapper<T>
    {
        protected Type WrappedType;
        protected T WrappedObject;

        public Wrapper(T instance)
        {
            WrappedType = typeof(T);
            WrappedObject = instance;

            _disposable = instance as IDisposable;
            if (_disposable == null)
            {
                GC.SuppressFinalize(this);
            }
        }

        #region IWrapper implementation

        public virtual TReturn Call<TReturn>(Func<T, TReturn> expression)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Instance");
            return expression(WrappedObject);
        }

        public virtual void Call(Action<T> expression)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Instance");
            expression(WrappedObject);
        }

        public virtual T Instance
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException("Instance");
                return WrappedObject;
            }
        }

        public virtual Type InstanceType
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException("Instance");
                return WrappedType;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Dispose(true);
            }
        }

        protected bool IsDisposed;

        private readonly IDisposable _disposable;
        protected IDisposable Disposable
        {
            get { return _disposable; }
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (Disposable != null)
            {
                Disposable.Dispose();
                WrappedObject = default(T);
                WrappedType = null;

                IsDisposed = true;
            }
        }

        #endregion

        #region Finalizer

        ~Wrapper()
        {
            Dispose(false);
        }

        #endregion
    }

    public class SerializationFilterWrapper<T> : Wrapper<T>, ISerializable
    {
        private readonly HashSet<string> _implementedProperties;
        private Action<SerializationInfo> _fillSerializationInfo;
        private bool _isFilterUnderlyingObjectIfEnumerable;
        private bool _isIEnumerableWrapped;
        private Type _underlyingObjectType;

        public SerializationFilterWrapper(T wrappedInstance, HashSet<string> propertiesNames)
            : this(wrappedInstance, propertiesNames, true)
        {
        }

        public SerializationFilterWrapper(T wrappedInstance, HashSet<string> propertiesNames, bool isFilterUnderlyingObjectIfEnumerable)
            : base(wrappedInstance)
        {
            _implementedProperties = propertiesNames;
            _isFilterUnderlyingObjectIfEnumerable = isFilterUnderlyingObjectIfEnumerable;

            Initialize();
        }

        protected void Initialize()
        {
            InitializeMembers();
            var lambdaExpression = BuildExpressionTree();
            CompileDelegate(lambdaExpression);
        }

        public HashSet<string> GetImplementedProperties()
        {
            return _implementedProperties;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            FillSerializationInfo(info);
        }

        protected void FillSerializationInfo(SerializationInfo info)
        {
            if (_fillSerializationInfo != null && info != null)
            {
                _fillSerializationInfo(info);
            }
        }

        protected LambdaExpression BuildExpressionTree()
        {
            var propertyInfos = GetObjectProperties(WrappedType);

            var numberOfProperties = propertyInfos.Length;
            var expressionBlockStatements = new List<Expression>(numberOfProperties);

            var listConstructor = Expression.New(typeof(List<>).GetConstructor(new Type[0]));
            var listVariable = Expression.Parameter(typeof(List<>), "listParam");
            var listAssigment = Expression.Assign(listVariable, listConstructor);

            //var foreachLoop = Expression.Loop();

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

        protected virtual List<Expression> AddObjectToSerializationInfoStatements(ParameterExpression collection, ParameterExpression instance)
        {
            var propertyInfos = GetObjectProperties(WrappedType);
            var statements = new List<Expression>(propertyInfos.Length);

            foreach (var propertyInfo in propertyInfos.Where(pi => _implementedProperties.Contains(pi.Name)))
            {
                var propertyGetterInfo = propertyInfo.GetGetMethod(false);
                if (propertyGetterInfo != null && propertyGetterInfo.GetParameters().Length == 0)
                {
                    var propertyNameConstant = Expression.Constant(propertyInfo.Name);
                    var getterCall = Expression.Call(instance, propertyGetterInfo);
                    var convertedGetterValueToObject = Expression.Convert(getterCall, TypeOfObject);
                    var propertyTypeConstant = Expression.Constant(propertyInfo.PropertyType, typeof(Type));
                    var addToDictionary = Expression.Call(instance, SerializationInfoAddObjectMethodInfo,
                        propertyNameConstant, convertedGetterValueToObject, propertyTypeConstant);

                    statements.Add(addToDictionary);
                }
            }
            return statements;
        }

        protected void InitializeMembers()
        {
            _isIEnumerableWrapped = TypeOfIEnumerable.IsAssignableFrom(WrappedType);
            _underlyingObjectType = GetRealType(WrappedType);


        }

        protected void CompileDelegate(LambdaExpression expression)
        {
            _fillSerializationInfo = (Action<SerializationInfo>)expression.Compile();
        }

        private PropertyInfo[] GetObjectProperties(Type objectType)
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
            return argumentType;
        }

        static SerializationFilterWrapper()
        {
            TypeOfIEnumerable = typeof(IEnumerable<>);
            TypeOfString = typeof(string);
            TypeOfString = typeof(object);

            SerializationInfoAddObjectMethodInfo = TypeofSerializationInfo.GetMethod("Add", new[] { TypeOfString, TypeOfObject });
        }

        private static readonly Type TypeOfIEnumerable;
        private static readonly Type TypeOfString;
        private static readonly Type TypeOfObject;
        private static readonly Type TypeofSerializationInfo;

        private static readonly MethodInfo SerializationInfoAddObjectMethodInfo;

        protected class BriefPropertyInfo
        {
            public string PropertyName { get; set; }
            public Type PropertyType { get; set; }
            public object PropertyValue { get; set; }

            public BriefPropertyInfo(string propertyName, object propertyValue, Type propertyType)
            {
                PropertyName = propertyName;
                PropertyValue = PropertyValue;
                PropertyType = propertyType;
            }
        }
    }

    //public class EnumerableWrapper<T, TUnderl> : Wrapper<T>
    //{
    //    protected readonly Type UnderlyingType;

    //    public EnumerableWrapper(Type underlyingType, IEnumerable<SingleObjectWrapper<TUnderl>> wrappedObject) : base(wrappedObject)
    //    {
    //        UnderlyingType = typeof(TUnderl);
    //    }

    //    public Type GetUnderlyingType()
    //    {
    //        return UnderlyingType;
    //    }
    //}
}