using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace TestProject.Helpers
{
    public class SerializationFilterDecorator
    {
        public DecoratorBase GetDecorator(object instance, Type instanceType)
        {
            return GetDecorator(instance, instanceType, null);
        }
        
        public DecoratorBase GetDecorator(object instance, Type instanceType, HashSet<string> propertiesNames)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            if (instanceType == null)
                throw new ArgumentNullException("instanceType");

            var underlyingType = GetUnderlyingTypeIfEnumerable(instanceType);

            if (underlyingType != null)
            {
                var compiledLambda = GetDelegate(underlyingType, propertiesNames);
                return new EnumerableObjectSerializationDecorator(instance, instanceType, underlyingType, compiledLambda);
            }
            else
            {
                var compiledLambda = GetDelegate(instanceType, propertiesNames);
                return new ObjectSerializationDecorator(instance, instanceType, compiledLambda);
            }
        }

        protected virtual Action<object, SerializationInfo> GetDelegate(Type instanceType, HashSet<string> propertiesNames)
        {
            if (instanceType == null)
                throw new ArgumentNullException("instanceType");
            
            var lambdaExpression = GetLambdaExpression(instanceType, propertiesNames);
            return (Action<object, SerializationInfo>)lambdaExpression.Compile();
        }

        private static LambdaExpression GetLambdaExpression(Type instanceType, HashSet<string> propertiesNames)
        {
            var expressions = new List<Expression>();

            var serializationInfo = Expression.Parameter(TypeofSerializationInfo, "serializationInfo");
            var decoratedObject = Expression.Parameter(TypeOfObject, "decoratedObject");

            var castedInstanceVariable = Expression.Parameter(instanceType, "castedObject");
            var castedInstance = Expression.TypeAs(decoratedObject, instanceType);
            var castInstanceAssigment = Expression.Assign(castedInstanceVariable, castedInstance);

            var variableEqualsNull = Expression.Equal(castedInstanceVariable, Expression.Constant(null));

            var exceptionThrow =
                Expression.Throw(
                    Expression.Constant(new InvalidCastException("instance is not of type of " + instanceType)));

            var checkCastIsSucessfull = Expression.IfThen(variableEqualsNull, exceptionThrow);

            expressions.Add(castInstanceAssigment);
            expressions.Add(checkCastIsSucessfull);

            expressions.AddRange(AddFieldsToSerializationInfoStatements(instanceType, serializationInfo, castedInstanceVariable, propertiesNames));

            var expressionsBlock = Expression.Block(new[] { castedInstanceVariable }, expressions);

            var lambda = Expression.Lambda(expressionsBlock, decoratedObject, serializationInfo);

            return lambda;
        }

        private static List<Expression> AddFieldsToSerializationInfoStatements(Type objectType, Expression serializationInfo, Expression instance, HashSet<string> propertiesNames)
        {
            var propertyInfos = GetObjectProperties(objectType, propertiesNames);
            var statements = new List<Expression>();

            foreach (var propertyInfo in propertyInfos)
            {
                var propertyGetterInfo = propertyInfo.GetGetMethod(false);
                if (propertyGetterInfo != null && propertyGetterInfo.GetParameters().Length == 0)
                {
                    var propertyNameConstant = Expression.Constant(propertyInfo.Name, TypeOfString);
                    var getterCall = Expression.Call(instance, propertyGetterInfo);
                    var convertedGetterValueToObject = Expression.Convert(getterCall, TypeOfObject);
                    var propertyTypeConstant = Expression.Constant(propertyInfo.PropertyType, TypeOfType);
                    var addToInfo = Expression.Call(serializationInfo, SerializationInfoAddObjectMethodInfo,
                        propertyNameConstant, convertedGetterValueToObject, propertyTypeConstant);

                    statements.Add(addToInfo);
                }
            }
            return statements;
        }

        protected static IEnumerable<PropertyInfo> GetObjectProperties(Type objectType, HashSet<string> propertiesNames = null)
        {
            return propertiesNames != null
                ? objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => propertiesNames.Contains(property.Name))
                : objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        protected static Type GetUnderlyingTypeIfEnumerable(Type type)
        {
            if (TypeOfIEnumerable.IsAssignableFrom(type))
            {
                var underlyingType = type.IsGenericType
                    ? type.GetGenericArguments()[0]
                    : TypeOfObject;

                return underlyingType;
            }
            return null;
        }

        #region Decorator helper classes

        public abstract class DecoratorBase
        {
            protected readonly object DecoratedInstance;
            protected readonly Type DecoratedInstanceType;
            protected readonly Type DecoratorType;

            protected DecoratorBase(object decoratedInstance, Type decoratedInstanceType)
                : this(decoratedInstance, decoratedInstanceType, null) { }

            protected DecoratorBase(object decoratedInstance, Type decoratedInstanceType, Type decoratorType)
            {
                DecoratedInstance = decoratedInstance;
                DecoratedInstanceType = decoratedInstanceType;
                DecoratorType = decoratorType ?? typeof(DecoratorBase);
            }

            public object GetDecoratedInstance()
            {
                return DecoratedInstance;
            }

            public Type GetDecoratedInstanceType()
            {
                return DecoratedInstanceType;
            }

            public Type GetDecoratorType()
            {
                return DecoratorType;
            }
        }

        protected class EnumerableObjectSerializationDecorator : DecoratorBase, IEnumerable
        {
            private readonly Type _underlyingType;
            private readonly Action<object, SerializationInfo> _fillSerializationInfoAction;

            private List<ObjectSerializationDecorator> _itemsForSerialization;

            public EnumerableObjectSerializationDecorator(object instance, Type decoratedEnumerableType, Type underlyingType, Action<object, SerializationInfo> fillSerializationInfoAction)
                : base(instance, decoratedEnumerableType, EnumerableDecoratorType)
            {
                _underlyingType = underlyingType;
                _fillSerializationInfoAction = fillSerializationInfoAction;

                Initialize();
            }

            private void Initialize()
            {
                var enumerable = DecoratedInstance as IEnumerable;
                if (enumerable == null)
                {
                    throw new InvalidCastException("instance is not of IEnumerable type");
                }

                _itemsForSerialization = new List<ObjectSerializationDecorator>();

                foreach (var item in enumerable)
                {
                    _itemsForSerialization.Add(new ObjectSerializationDecorator(item, _underlyingType, _fillSerializationInfoAction));
                }
            }

            public IEnumerator GetEnumerator()
            {
                return _itemsForSerialization.GetEnumerator();
            }

            private static readonly Type EnumerableDecoratorType = typeof(EnumerableObjectSerializationDecorator);
        }

        protected class ObjectSerializationDecorator : DecoratorBase, ISerializable
        {
            private readonly Action<object, SerializationInfo> _fillSerializationInfoAction;

            public ObjectSerializationDecorator(object instance, Type instanceType, Action<object, SerializationInfo> fillSerializationInfoAction)
                : base(instance, instanceType, SerializationDecoratorType)
            {
                _fillSerializationInfoAction = fillSerializationInfoAction;
            }

            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                _fillSerializationInfoAction(DecoratedInstance, info);
            }

            private static readonly Type SerializationDecoratorType = typeof(ObjectSerializationDecorator);
        }

        #endregion

        #region Static constructor and fields

        static SerializationFilterDecorator()
        {
            TypeOfIEnumerable = typeof(IEnumerable);
            TypeOfString = typeof(string);
            TypeOfObject = typeof(object);
            TypeOfType = typeof(Type);
            TypeofSerializationInfo = typeof(SerializationInfo);

            SerializationInfoAddObjectMethodInfo = TypeofSerializationInfo.GetMethod("AddValue", new[] { TypeOfString, TypeOfObject, TypeOfType });
        }

        private static readonly Type TypeOfString;
        private static readonly Type TypeOfObject;
        private static readonly Type TypeOfType;
        private static readonly Type TypeOfIEnumerable;
        private static readonly Type TypeofSerializationInfo;

        private static readonly MethodInfo SerializationInfoAddObjectMethodInfo;

        #endregion
    }
}