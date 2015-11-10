using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Gianos.Utils.SettableUtils
{
    /// <summary>
    /// A Contract Resolver implementation, that will not
    /// serialize undefined Settable properties.
    /// </summary>
    public class SettableContractResolver : DefaultContractResolver
    {
        public new static readonly SettableContractResolver Instance = new SettableContractResolver();

        private static readonly MethodInfo ShouldSerializeSettableBuilderMethodInfo = typeof(SettableContractResolver)
            .GetMethod("ShouldSerializeSettableBuilder", BindingFlags.Static | BindingFlags.Public);

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            var settableTypeParameter = SettableJsonConverter.ResolveSettableTypeParameter(property.PropertyType);
            
            if (settableTypeParameter != null)
            {
                property.ShouldSerialize = MakePredicateForSettableType(settableTypeParameter, property);
            }

            return property;
        }

        /// <summary>
        /// For each base type, a ShouldSerialize handler is
        /// generated. Using reflection at each serialization would be
        /// slow, so we use MethodInfo.MakeGenericMethod once and then
        /// return the cached Predicate.
        /// </summary>
        /// <param name="baseType">
        /// The type wrapped in a Settable&lt;&gt; property.
        /// </param>
        /// <param name="property"></param>
        /// <returns></returns>
        public Predicate<object> MakePredicateForSettableType(Type baseType, JsonProperty property)
        {
            var typedMethod = ShouldSerializeSettableBuilderMethodInfo.MakeGenericMethod(baseType);
 
            return (Predicate<object>)typedMethod.Invoke(null, new object[] { property });
        }

        /// <summary>
        /// Strongly typed Predicate factory, to be invoked only once for
        /// each type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Predicate<object> ShouldSerializeSettableBuilder<T>(JsonProperty property) where T : struct
        {
            return o =>
            {
                var v = property.ValueProvider.GetValue(o);
                return ((Settable<T>) v).IsSet;
            };
        }
    }
}