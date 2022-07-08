using System.Linq.Expressions;
using System.Reflection;

namespace Troolio.Projection.Redis
{
    public static class ReflectionUtils
    {
        public static string GetPropertyName<T, U>(Expression<Func<T, U>> propertyExpression)
        {
            MemberExpression memberExpression = GetMemberExpression(propertyExpression);
            return memberExpression?.Member.Name;
        }

        public static PropertyInfo GetPropertyInfo<T, U>(Expression<Func<T, U>> propertyExpression)
        {
            PropertyInfo propertyInfo = null;
            MemberExpression memberExpression = GetMemberExpression(propertyExpression);
            if (memberExpression != null)
            {
                propertyInfo = (PropertyInfo)memberExpression.Member;
            }
            return propertyInfo;
        }

        public static MemberExpression GetMemberExpression<T, U>(Expression<Func<T, U>> propertyExpression)
        {
            MemberExpression memberExpression = propertyExpression.Body as MemberExpression ?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression;
            return memberExpression;
        }

        public static bool IsSubclassOfGeneric(this Type type, Type genericBaseType)
        {
            while (type != null && type != typeof(object))
            {
                Type genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

                if (genericType == genericBaseType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        public static bool IsCompilerGenerated(this Type type)
        {
            Attribute attr = Attribute.GetCustomAttribute(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute));
            return attr != null;
        }

        /// <summary>
        /// Gets the value for the public field on the object if it exists and returns it.
        /// Otherwise returns null.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static object GetFieldValue(this Object obj, String fieldName)
        {
            object fieldValue = null;

            if (obj != null && !string.IsNullOrEmpty(fieldName))
            {
                Type objType = obj.GetType();

                FieldInfo field = objType.GetField(fieldName);
                if (field != null)
                {
                    fieldValue = field.GetValue(obj);
                }
            }

            return fieldValue;
        }
    }
}
