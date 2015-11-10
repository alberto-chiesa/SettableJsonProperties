using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Gianos.Utils.SettableUtils
{
    /// <summary>
    /// Class implementing Json serialization and deserialization
    /// of Settable types.
    /// </summary>
    public class SettableJsonConverter : JsonConverter
    {
        /// <summary>
        /// Simple interface implemented by
        /// strongly typed converters.
        /// </summary>
        public interface ITypedConverter
        {
            /// <summary>
            /// Converts a boxed value into a Settable
            /// instance and returns it.
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            object Deserialize(object value);

            /// <summary>
            /// Extracts a value 
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            object ExtractValue(object value);
        }

        /// <summary>
        /// Class implementing a strongly typed json converter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class TypedConverter<T> : ITypedConverter
            where T : struct
        {
            /// <summary>
            /// Converts a boxed value into a Settable
            /// instance and returns it.
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public object Deserialize(object value)
            {
                if (value == null)
                    return (Settable<T>)null;

                try
                {
                    var settedValue = (T)Convert.ChangeType(value, typeof(T));
                    return (Settable<T>) settedValue;
                }
                catch (Exception)
                {
                    return (Settable<T>)null;
                }
            }

            /// <summary>
            /// Extracts a value 
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public object ExtractValue(object value)
            {
                return (T?) ((Settable<T>) value);
            }
        }

        private static readonly Dictionary<Type, ITypedConverter> Converters = new Dictionary<Type, ITypedConverter>();

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var converter = GetConverter(value.GetType());

            serializer.Serialize(writer, converter.ExtractValue(value));
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var converter = GetConverter(objectType);

            return converter.Deserialize(reader.Value);
        }

        private ITypedConverter GetConverter(Type objectType)
        {
            if (!Converters.ContainsKey(objectType))
            {
                var valueType = ResolveSettableTypeParameter(objectType);

                Converters[objectType] = Activator.CreateInstance(typeof(TypedConverter<>).MakeGenericType(valueType)) as ITypedConverter;
            }

            return Converters[objectType];
        }

        public static Type ResolveSettableTypeParameter(Type settableType)
        {
            var toCheck = settableType;
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (typeof(Settable<>) == cur)
                {
                    return toCheck.GetGenericArguments().Single();
                }
                toCheck = toCheck.BaseType;
            }

            return null;
            // throw new InvalidOperationException("The provided type " + settableType.FullName + " does not inherit from Settable<T>");
        }
        
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return Converters.ContainsKey(objectType) || ResolveSettableTypeParameter(objectType) != null;
        }
    }
}