using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    [PSTypeConverter(typesToConvert: typeof(Hashtable))]
    public class HashtableConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
            => destinationType == typeof(Hashtable);

        public override bool CanConvertTo(object sourceValue, Type destinationType) 
            => sourceValue is Hashtable;

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            if (destinationType != typeof(Hashtable)) throw new InvalidCastException();
            var psobj = PSObject.AsPSObject(sourceValue);
            var output = new Hashtable();
            foreach (var property in psobj.Properties)
            {
                output[property.Name] = property.Value;
            }
            return output;
        }

        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            var hashtable = (sourceValue as PSObject)?.BaseObject as Hashtable ?? (Hashtable)sourceValue;
            var psobj = Construct(hashtable, destinationType);
            if (destinationType != typeof(PSObject))
            {
                return psobj.BaseObject;
            }
            else
            {
                return destinationType;
            }
        }

        /// <exception cref="HashtableMergeTypeException">The type of an object in <paramref name="hashtable"/>
        /// does not match the type of the property of <see cref="data"/>.</exception>
        /// <exception cref="PropertyNotFoundException"><paramref name="otherAsNoteProperties"/> 
        /// is <see langword="false"/> and a key exists in the <paramref name="hashtable"/> that 
        /// does not correspond to a property of <paramref name="data"/>.</exception>
        public static PSObject MergeWith(Hashtable hashtable, PSObject data, bool otherAsNoteProperties = false)
        {
            var actions = new Queue<(Action<PSObject> set, Action<PSObject> rollback)>();
            var errors = new List<Exception>();
            var psproperties = data.Properties;
            foreach (var key in hashtable.Keys)
            {
                string name = key.ToString();
                var property = psproperties.Where(i => i.Name == name).SingleOrDefault();
                if (property is null)
                {
                    if (!otherAsNoteProperties)
                    {
                        errors.Add(new InvalidOperationException($"Cannot set property '{name}' " +
                            $"because it does not exist on the source object."));
                        continue;
                    }
                    else
                    {
                        Action<PSObject> add = i => i.Properties.Add(new PSNoteProperty(name, hashtable[name]));
                        Action<PSObject> remove = i => i.Properties.Remove(name);
                        actions.Enqueue((add, remove));
                        continue;
                    }
                }
                if (!property.IsSettable)
                {
                    errors.Add(new InvalidOperationException($"The property '{name}' is not settable."));
                    continue;
                }
                //if (property.MemberType == PSMemberTypes.Property)
                //{
                //    var newValueType = hashtable[key]?.GetType();
                //    if (newValueType != null && !baseProperty.PropertyType.IsAssignableFrom(newValueType))
                //    {
                //        errors.Add(new HashtableMergeTypeException($"The property '{name}' is not assignable " +
                //            $"from value '{hashtable[key]}' of type [{newValueType.FullName}]."));
                //        continue;
                //    }
                //}
                var baseProperty = data.BaseObject?.GetType().GetProperty(property.Name);
                object newValue;
                if (baseProperty != null && !LanguagePrimitives.TryConvertTo(hashtable[key], baseProperty.PropertyType, out newValue))
                {
                    // baseProperty != null -> do not have to conform to type
                    errors.Add(new HashtableMergeTypeException($"The property '{name}' is not assignable " +
                        $"from value '{hashtable[key]}' of type [{hashtable[key]?.GetType().FullName ?? "null"}]."));
                    continue;
                }
                else
                {
                    newValue = hashtable[key];
                }
                var currentValue = property.Value;
                Action<PSObject> set = i => i.Properties[name].Value = newValue;
                Action<PSObject> rollback = i => i.Properties[name].Value = currentValue;
                actions.Enqueue((set, rollback));
            }
            if (errors.Count > 0)
            {
                if (errors.Count == 1)
                {
                    throw errors[0];
                }
                else
                {
                    throw new AggregateException("One or more exceptions occurred while trying to " +
                        "set properties of the target object.\n" +
                        string.Join("\n", errors.Select(e => e.Message)), errors);
                }
            }
            var rollbacks = new Queue<Action<PSObject>>();
            while (actions.Count > 0)
            {
                var actionSet = actions.Dequeue();
                try
                {
                    actionSet.set.Invoke(data);
                    rollbacks.Enqueue(actionSet.rollback);
                }
                catch (Exception e)
                {
                    // presumably we do not need to invoke the rollback for this SET operation if it failed.
                    while (rollbacks.Count > 0)
                    {
                        rollbacks.Dequeue().Invoke(data);
                    }
                    throw new PSInvalidOperationException("Failed to set all properties of the " +
                        "target object: " + e.Message, e);
                }
            }
            return data;
        }
        [PSCodeMethodDefinition(typeof(Hashtable), Force = true)]
        public static PSObject MergeWith(PSObject hashtable, PSObject data, bool otherAsNoteProperties = false)
            => MergeWith((Hashtable)hashtable.BaseObject, data, otherAsNoteProperties);
        public static PSObject Construct(Hashtable hashtable, Type destinationType)
        {
            Console.WriteLine($".Construct() -> Constructing {destinationType.FullName} from hashtable.");
            var noConstructor = destinationType.GetConstructor(Type.EmptyTypes);
            if (noConstructor != null)
            {
                Console.WriteLine(".Construct() -> Using default constructor with no parameters.");
                return MergeWith(hashtable, PSObject.AsPSObject(noConstructor.Invoke(Array.Empty<object>())), false);
            }

            if (destinationType == typeof(PSObject) || destinationType == typeof(PSCustomObject))
            {
                Console.WriteLine(".Construct() -> Converting to PSCustomObject.");
                return AsPSCustomObject(hashtable);
            }
            if (hashtable.ContainsKey(".ctor"))
            {
                Console.WriteLine(".Construct() -> Using .ctor hashtable key.");
                return ConstructUsingConstructorKey(hashtable, destinationType);
            }
            else
            {
                Console.WriteLine(".Construct() -> Finding named constructor parameters from hashtable keys.");
                return ConstructUsingNamedParameters(hashtable, destinationType);
            }
        }
        private static PSObject AsPSCustomObject(Hashtable hashtable)
        {
            return MergeWith(hashtable, new PSObject(), true);
        }
        private static PSObject ConstructUsingConstructorKey(Hashtable hashtable, Type type)
        {
            PSObject output;
            // .ctor may be hashtable of named constructor parameters
            // or an array of constructor parameters.
            if (hashtable[".ctor"] is IDictionary ctorParams)
            {
                output = ConstructUsingConstructorDictionary(ctorParams, type);
            }
            else if (hashtable[".ctor"] is IEnumerable enumerable && !(hashtable[".ctor"] is string))
            {
                output = ConstructUsingConstructorArray(enumerable, type);
            }
            else
            {
                output = ConstructUsingConstructorArray(new object[] { hashtable[".ctor"] }, type);
            }
            // apply non-constructor members
            hashtable.Remove(".ctor");
            MergeWith(hashtable, output, false);
            return output;
        }
        static IEnumerable<string> GetKeys(IDictionary dictionary)
        {
            foreach (var key in dictionary.Keys) yield return key.ToString();
        }
        private static PSObject ConstructUsingConstructorDictionary(IDictionary namedParameters, Type type)
        {
            Console.WriteLine($".ConstructUsingConstructorDictionary() -> invoking with parameters {string.Join(", ", GetKeys(namedParameters))}");
            var constructors = type.GetConstructors().Where(ctor => ctor.GetParameters().Length == namedParameters.Count);
            Console.WriteLine($".ConstructUsingConstructorDictionary() -> {constructors.Count()} constructors found.");
            var ctorParamNames = new List<string>();
            foreach (var key in namedParameters.Keys)
            {
                ctorParamNames.Add(key.ToString());
            }
            constructors = constructors.Where(ctor =>
            {
                var ctorParams = ctor.GetParameters();
            Console.WriteLine($".ConstructUsingConstructorDictionary() -> Comparing to parameters {string.Join(", ", ctorParams.Select(i => i.Name))}");
                for (int i = 0; i < ctorParamNames.Count; i++)
                {
                    if (!ctorParams[i].Name.Equals(ctorParamNames[i], StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            });
            if (!constructors.Any())
            {
                throw new HashtableMergeException($"No constructor could be found for type [{type.FullName}] " +
                    $"using {namedParameters.Count} arguments with the provided argument names.");
            }
            if (constructors.Count() > 1)
            {
                throw new AmbiguousMatchException($"Multiple constructors were identified with " +
                    $"the named parameters provided.");
            }
            var ctor = constructors.Single();
            var ctorParams = ctor.GetParameters();
            var invokeParameters = new object[ctorParams.Length];
            for (int i = 0; i < ctorParams.Length; i++)
            {
                invokeParameters[i] = namedParameters[ctorParams[i].Name];
            }
            var obj = ctor.Invoke(invokeParameters.ToArray());
            return PSObject.AsPSObject(obj);
        }
        private static PSObject ConstructUsingConstructorArray(IEnumerable constructorParameters, Type type)
        {
            IEnumerable<object> strongParameters;
            if (constructorParameters is IEnumerable<object> strongParams) strongParameters = strongParams;
            else
            {
                var strongParamList = new List<object>();
                foreach (var param in constructorParameters)
                {
                    strongParamList.Add(param);
                }
                strongParameters = strongParamList;
            }
            Console.WriteLine($".ConstructUsingConstructorArray() -> Comparing to parameters of types {string.Join(", ", strongParameters.Select(i => i.GetType().FullName))}");
            var constructors = type.GetConstructors()
                .Select(ctor => (ctor, parameters: ctor.GetParameters()))
                .Where(i => i.parameters.Length == strongParameters.Count());
            foreach (var (ctor, ctorParams) in constructors)
            {
            Console.WriteLine($".ConstructUsingConstructorArray() -> Attempting to invoke with parameters {string.Join(", ", ctorParams.Select(i => "[" + i.ParameterType.FullName + "] " + i.Name))}");
                try
                {
                    var invokeParams = new object[ctorParams.Length];
                    for(int i = 0; i < ctorParams.Length; i++)
                    {
                        if (LanguagePrimitives.TryConvertTo(strongParameters.Skip(i).First(), ctorParams[i].ParameterType, out var ctorParam))
                        {
                            invokeParams[i] = ctorParam;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    return PSObject.AsPSObject(ctor.Invoke(invokeParams));
                }
                catch 
                {
                    continue;
                }
            }
            throw new HashtableMergeException("No constructor succeeded using the constructor parameter array.");
        }
        private static PSObject ConstructUsingNamedParameters(Hashtable hashtable, Type type)
        {
            Console.WriteLine($".ConstructUsingNamedParameters() -> Attempting to invoke with named parameters from hashtable keys.");
            var ctors = type.GetConstructors()
                .Select(ctor => (ctor, parameters: ctor.GetParameters()))
                .OrderByDescending(i=> i.parameters.Length);
            foreach (var (ctor, parameters) in ctors)
            {
                if (parameters.Length > hashtable.Keys.Count) continue;
            Console.WriteLine($".ConstructUsingNamedParameters() -> Testing possible constructor with parameters {string.Join(", ", parameters.Select(i => "[" + i.ParameterType.FullName + "] " + i.Name))}.");
                var invokeParameters = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (!hashtable.ContainsKey(parameters[i].Name)) goto next_ctor;
                    if (LanguagePrimitives.TryConvertTo(hashtable[parameters[i].Name], parameters[i].ParameterType, out var ctorParam))
                    {
                        invokeParameters[i] = ctorParam;
                    }
                    else
                    {
                        goto next_ctor;
                    }
                }
                try
                {
            Console.WriteLine($".ConstructUsingNamedParameters() -> Invoking constructor.");
                    var output = ctor.Invoke(invokeParameters);
                    parameters.Select(i => i.Name).ToList().ForEach(i => hashtable.Remove(i));
                    return MergeWith(hashtable, PSObject.AsPSObject(output), false);
                }
                catch (HashtableMergeException)
                {
                    throw;
                }
                catch
                {
                    continue;
                }
            next_ctor:;
            }
            throw new HashtableMergeException("No constructor succeeded using the hashtable keys as parameters.");
        }
    }

    [Serializable]
    public class HashtableMergeException : Exception
    {
        public HashtableMergeException() { }
        public HashtableMergeException(string message) : base(message) { }
        public HashtableMergeException(string message, Exception inner) : base(message, inner) { }
        protected HashtableMergeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class HashtableMergeTypeException : HashtableMergeException
    {
        public HashtableMergeTypeException() { }
        public HashtableMergeTypeException(string message) : base(message) { }
        public HashtableMergeTypeException(string message, Exception inner) : base(message, inner) { }
        protected HashtableMergeTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
