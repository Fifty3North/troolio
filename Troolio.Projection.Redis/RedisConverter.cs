using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Troolio.Projection.Redis
{
    public class RedisConverter
    {
        protected static readonly JsonSerializerSettings _jsonSerializationSettings = new JsonSerializerSettings()
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // No point in serializing/deserializing a string
        protected static readonly Type _stringType = typeof(string);
        // These are value types that can safely be converted from string and as such no point in serializing/deserializing
        // We will not include nullable value types here because we then need to change the Convert.ChangeType code
        protected static readonly Type[] _nonSerializedValueTypes = new Type[] { typeof(short), typeof(int), typeof(long), typeof(bool) };

        public static TProperty FromHashEntry<TProperty>(HashEntry entry)
        {
            return FromRedisValue<TProperty>(entry.Value);
        }

        protected static object FromHashEntry(HashEntry entry, Type propertyType)
        {
            return FromRedisValue(entry.Value, propertyType);
        }

        public static TProperty FromRedisValue<TProperty>(RedisValue value)
        {
            object propertyValue = FromRedisValue(value, typeof(TProperty));
            return (propertyValue != null)
                ? (TProperty)propertyValue
                : default(TProperty);
        }

        protected static object FromRedisValue(RedisValue value, Type propertyType)
        {
            object propertyValue;

            // HasValue returns false when it is RedisValue.EmptyString
            // We have decided that this is ok and if we put an empty string in then it is ok to come back as null
            if (!value.HasValue)
            {
                propertyValue = null;
            }
            else if (propertyType == _stringType)
            {
                propertyValue = value.ToString();
            }
            else if (_nonSerializedValueTypes.Contains(propertyType))
            {
                propertyValue = Convert.ChangeType(value.ToString(), propertyType);
            }
            else
            {
                propertyValue = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(value), propertyType, _jsonSerializationSettings);
            }

            return propertyValue;
        }

        public static HashEntry ToHashEntry<TProperty>(string name, TProperty value)
        {
            return ToHashEntry(name, value, typeof(TProperty));
        }

        protected static HashEntry ToHashEntry(string name, object value, Type propertyType)
        {
            HashEntry entry;
            if (value == null)
            {
                entry = new HashEntry(name, RedisValue.EmptyString);
            }
            else if (propertyType == _stringType)
            {
                entry = new HashEntry(name, value.ToString());
            }
            else if (_nonSerializedValueTypes.Contains(propertyType))
            {
                entry = new HashEntry(name, value.ToString());
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, propertyType, _jsonSerializationSettings));
                entry = new HashEntry(name, bytes);
            }
            return entry;
        }

        public static TEntity FromHashEntries<TEntity>(HashEntry[] hashEntries)
            where TEntity : class, new()
        {
            object entity = FromHashEntries(hashEntries, typeof(TEntity));
            return (TEntity)entity;
        }

        public static object FromHashEntries(HashEntry[] hashEntries, Type type)
        {
            return FromHashEntries(hashEntries, type, type.GetProperties());
        }

        protected static object FromHashEntries(HashEntry[] hashEntries, Type type, PropertyInfo[] properties)
        {
            object entity = Activator.CreateInstance(type);

            if (hashEntries?.Length > 0)
            {
                foreach (PropertyInfo property in properties)
                {
                    HashEntry entry = hashEntries.FirstOrDefault(he => he.Name.ToString().Equals(property.Name));
                    // Note that empty entry is returned if not matched on name
                    if (entry.Name.HasValue)
                    {
                        object value = FromHashEntry(entry, property.PropertyType);
                        if (value != null && property.CanWrite)
                        {
                            property.SetValue(entity, value);
                        }
                    }
                }
            }

            return entity;
        }
    }

    public class RedisConverter<TEntity> : RedisConverter
        where TEntity : class, new()
    {
        private static readonly Type type = typeof(TEntity);
        private static readonly PropertyInfo[] properties = type.GetProperties();

        private class ReflectedProperty
        {
            public readonly PropertyInfo PropertyInfo;

            public readonly Func<TEntity, object> PropertyEvaluator;

            public string PropertyName { get { return PropertyInfo.Name; } }

            public Type PropertyType { get { return PropertyInfo.PropertyType; } }

            public ReflectedProperty(PropertyInfo propertyInfo, Func<TEntity, object> propertyEvaluator)
            {
                PropertyInfo = propertyInfo;
                PropertyEvaluator = propertyEvaluator;
            }
        }

        private static readonly ConcurrentDictionary<string, ReflectedProperty> _propertyReflectedInfo = new ConcurrentDictionary<string, ReflectedProperty>();

        public static HashEntry[] ToHashEntries(TEntity entity)
        {
            List<HashEntry> hashEntries = new List<HashEntry>();

            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(entity);
                if (value != null)
                {
                    HashEntry entry = ToHashEntry(property.Name, value, property.PropertyType);
                    hashEntries.Add(entry);
                }
            }

            return hashEntries.ToArray();
        }

        public static HashEntry[] ToHashEntries(TEntity entity, params Expression<Func<TEntity, object>>[] propertiesToSet)
        {
            List<HashEntry> hashEntries = new List<HashEntry>();

            if (propertiesToSet != null)
            {
                foreach (var propertyToSet in propertiesToSet)
                {
                    HashEntry entry = ToHashEntry(entity, propertyToSet);
                    hashEntries.Add(entry);
                }
            }

            return hashEntries.ToArray();
        }

        public static HashEntry ToHashEntry(TEntity entity, Expression<Func<TEntity, object>> propertyExpression)
        {
            ReflectedProperty rp = GetReflectedProperty(propertyExpression);
            object value = rp.PropertyEvaluator.Invoke(entity);
            return ToHashEntry(rp.PropertyName, value, rp.PropertyType);
        }

        public static TEntity FromHashEntries(HashEntry[] hashEntries)
        {
            object entity = FromHashEntries(hashEntries, type, properties);
            return (TEntity)entity;
        }

        //public static TProperty FromRedisValue<TProperty>(RedisValue value, Expression<Func<TEntity, TProperty>> propertyExpression)
        //{
        //    return FromRedisValue<TProperty>(value);
        //}

        private static ReflectedProperty GetReflectedProperty(Expression<Func<TEntity, object>> propertyExpression)
        {
            MemberExpression membersExpression = ReflectionUtils.GetMemberExpression(propertyExpression);
            string name = membersExpression.Member.Name;

            ReflectedProperty reflectedProperty;
            if (!_propertyReflectedInfo.TryGetValue(name, out reflectedProperty))
            {
                // TODO: There shouldn't be any fields but if there are this will blow up.
                PropertyInfo propertyInfo = (PropertyInfo)membersExpression.Member;
                Func<TEntity, object> propertyEvaluator = propertyExpression.Compile();
                reflectedProperty = new ReflectedProperty(propertyInfo, propertyEvaluator);

                _propertyReflectedInfo.TryAdd(name, reflectedProperty);
            }

            return reflectedProperty;
        }
    }
}
