using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;

namespace TestProject.Helpers
{
    public class SerializationDecoratorManager
    {
        public static DecoratorBase GetDecorator(object instance, Type returnType, HashSet<string> propertiesNames)
        {
            var underlyingType = GetUnderlyingTypeIfEnumerable(returnType);
            if (underlyingType != null)
            {
                var lambdaExpression = BuildExpressionTreeForObject(underlyingType, propertiesNames);
                var compiledLambda = (Action<object, SerializationInfo>) lambdaExpression.Compile();
                return new EnumerableObjectSerializationDecorator(instance, returnType, underlyingType, compiledLambda);
            }
            else
            {
                var lambdaExpression = BuildExpressionTreeForObject(returnType, propertiesNames);
                var compiledLambda = (Action<object, SerializationInfo>)lambdaExpression.Compile();
                return new ObjectSerializationDecorator(instance, returnType, compiledLambda);
            }
        }

        private static LambdaExpression BuildExpressionTreeForObject(Type returnType, HashSet<string> propertiesNames)
        {
            var expressions = new List<Expression>();

            var serializationInfo = Expression.Parameter(TypeofSerializationInfo, "serializationInfo");
            var decoratedObject = Expression.Parameter(TypeOfObject, "decoratedObject");

            var castedInstanceVariable = Expression.Parameter(returnType, "castedObj");
            var castedInstance = Expression.TypeAs(decoratedObject, returnType);
            var castInstanceAssigment = Expression.Assign(castedInstanceVariable, castedInstance);

            expressions.Add(castInstanceAssigment);

            expressions.AddRange(AddObjectsToSerializationInfoStatements(returnType, serializationInfo, castedInstanceVariable, propertiesNames));

            var expressionsBlock = Expression.Block(new[] { castedInstanceVariable }, expressions);

            var lambda = Expression.Lambda(expressionsBlock, decoratedObject, serializationInfo);

            return lambda;
        }

        private static List<Expression> AddObjectsToSerializationInfoStatements(Type objectType, Expression serializationInfo, Expression instance, HashSet<string> propertiesNames)
        {
            var propertyInfos = GetObjectProperties(objectType);
            var statements = new List<Expression>(propertyInfos.Length);

            foreach (var propertyInfo in propertyInfos.Where(pi => propertiesNames.Contains(pi.Name)))
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

        private static PropertyInfo[] GetObjectProperties(Type objectType)
        {
            return objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        private static Type GetUnderlyingTypeIfEnumerable(Type type)
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

        static SerializationDecoratorManager()
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