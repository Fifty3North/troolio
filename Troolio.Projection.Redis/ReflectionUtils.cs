using System.Linq.Expressions;

namespace Troolio.Projection.Redis
{
    internal class ReflectionUtils
    {
        internal static string GetPropertyName<T, U>(Expression<Func<T, U>> propertyExpression)
        {
            MemberExpression memberExpression = GetMemberExpression(propertyExpression);
            return memberExpression?.Member.Name;
        }

        internal static MemberExpression GetMemberExpression<T, U>(Expression<Func<T, U>> propertyExpression)
        {
            MemberExpression memberExpression = propertyExpression.Body as MemberExpression ?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression;
            return memberExpression;
        }
    }
}
