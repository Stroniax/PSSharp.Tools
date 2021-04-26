using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PSSharp.Futuristic
{
    /// <summary>
    /// Defines methods used for validating an argument passed to a parameter, variable, or property.
    /// If this is implemented by a type that also implements <see cref="IArgumentTransformation"/>,
    /// transformation will be invoked before validation.
    /// <para>
    /// Note that the System.ComponentModel.DataAnnotations.ValidationAttribute and derived classes 
    /// may also be used for argument validation without inheriting this method. If this interface 
    /// is implemented by a class derived from the ValidationAttribute, both validation methods will
    /// be invoked, beginning with <see cref="ValidateArgument(object, EngineIntrinsics)"/>.
    /// </para>
    /// </summary>
    internal interface IArgumentValidation
    {
        /// <summary>
        /// <see langword="true"/> indicates that each parameter, if enumerable, should be provided separately
        /// to <see cref="ValidateArgument"/>. <see langword="false"/> indicates
        /// that the entire value provided to the parameter should be passed to <see cref="ValidateArgument"/>
        /// as a single object.
        /// </summary>
        bool ProcessEnumeratedArgumentsSeparately { get; }
        /// <summary>
        /// Performs validation on an argument. This method should throw an exception
        /// when an input value is invalid for the given parameter.
        /// </summary>
        /// <param name="argument">The argument provided to the instance being monitored.</param>
        /// <param name="engineIntrinsics"><inheritdoc cref="EngineIntrinsics" path="/summary"/></param>
        void ValidateArgument(object argument, EngineIntrinsics engineIntrinsics);
    }
    /// <summary>
    /// Defines methods used for transforming an argument from one type to another.
    /// If this is implemented by a type that also implements <see cref="IArgumentValidation"/>,
    /// this contract's implementation will be invoked first on any arguments before
    /// they are validated.
    /// </summary>
    internal interface IArgumentTransformation
    {
        /// <inheritdoc cref="IArgumentValidation.ProcessEnumeratedArgumentsSeparately"/>
        bool ProcessEnumeratedArgumentsSeparately { get; }
        /// <summary>
        /// Provides transformation on an argument. This method should return a value
        /// that can be assigned to the parameter - such as an instance of the type
        /// defined by <paramref name="parameterType"/>.
        /// </summary>
        /// <param name="argument">The argument provided to the instance being monitored.</param>
        /// <param name="parameterType">The type required by the parameter.</param>
        /// <param name="engineIntrinsics"><inheritdoc cref="EngineIntrinsics" path="/summary"/></param>
        /// <returns></returns>
        object TransformArgument(object argument, Type parameterType, EngineIntrinsics engineIntrinsics);
    }
    /// <summary>
    /// Defines methods used for offering argument completion, such as when a user types in
    /// a partial parameter value and presses the "Tab", "Ctrl + ," or other key binding
    /// to complete a parameter argument.
    /// </summary>
    internal interface IArgumentCompletion
    {
        /// <summary>
        /// Provides completion for an argument.
        /// </summary>
        /// <param name="wordToComplete">The partial defined when completion was requested.</param>
        /// <param name="commandName">The name of the command for which completion was requested.</param>
        /// <param name="parameterName">The name of the parameter for which completion was requested.</param>
        /// <param name="engineIntrinsics"><inheritdoc cref="EngineIntrinsics" path="/summary"/></param>
        /// <param name="commandAst"><inheritdoc cref="CommandAst" path="/summary"/></param>
        /// <param name="fakeBoundParameters">Current values bound to the command.</param>
        /// <returns></returns>
        IEnumerable<CompletionResult> CompleteArgument(
            string wordToComplete,
            string commandName,
            string parameterName,
            EngineIntrinsics engineIntrinsics,
            CommandAst commandAst,
            IDictionary<string, object?> fakeBoundParameters);
    }
    internal abstract class ArgumentTransformationBase : Attribute, IArgumentTransformation
    {
        public virtual bool ProcessEnumeratedArgumentsSeparately => true;

        public abstract object TransformArgument(object argument, Type parameterType, EngineIntrinsics engineIntrinsics);
        public T ConvertTo<T>(object valueToConvert)
            => LanguagePrimitives.ConvertTo<T>(valueToConvert);
        public object ConvertTo(object valueToConvert, Type resultType)
            => LanguagePrimitives.ConvertTo(valueToConvert, resultType);
        public object ConvertTo(object valueToConvert, Type resultType, IFormatProvider formatProvider)
            => LanguagePrimitives.ConvertTo(valueToConvert, resultType, formatProvider);
        public bool TryConvertTo<T>(object valueToConvert, out T result)
            => LanguagePrimitives.TryConvertTo<T>(valueToConvert, out result);
        public bool TryConvertTo<T>(object valueToConvert, IFormatProvider formatProvider, out T result)
            => LanguagePrimitives.TryConvertTo<T>(valueToConvert, formatProvider, out result);
        public bool TryConvertTo(object valueToConvert, Type resultType, out object result)
            => LanguagePrimitives.TryConvertTo(valueToConvert, resultType, out result);
        public bool TryConvertTo(object valueToConvert, Type resultType, IFormatProvider formatProvider, out object result)
            => LanguagePrimitives.TryConvertTo(valueToConvert, resultType, formatProvider, out result);
        public IEnumerable GetEnumerable(object obj)
            => LanguagePrimitives.GetEnumerable(obj);
        public IEnumerator GetEnumerator(object obj)
            => LanguagePrimitives.GetEnumerator(obj);
    }
    internal class ScriptTransformationAttribute : ArgumentTransformationBase
    {
        public override bool ProcessEnumeratedArgumentsSeparately => false;
        public ScriptTransformationAttribute(ScriptBlock scriptBlock)
        {
            ScriptBlock = scriptBlock;
        }
        public ScriptTransformationAttribute(string scriptText)
        {
            ScriptBlock = ScriptBlock.Create(scriptText);
        }
        public ScriptBlock ScriptBlock { get; }
        public override object TransformArgument(object argument, Type parameterType, EngineIntrinsics engineIntrinsics)
        {
            var input = new List<object>() { argument };
            var results = engineIntrinsics.SessionState.InvokeCommand
                .InvokeScript(true, ScriptBlock, input, argument, engineIntrinsics);

            if (TryConvertTo(results, parameterType, out var convertedResults))
            {
                return convertedResults;
            }
            else
            {
                return results;
            }
        }
    }
    internal class IPv4Attribute : Attribute, IArgumentTransformation, IArgumentValidation, IArgumentCompletion
    {
        public bool ProcessEnumeratedArgumentsSeparately => true;

        public IEnumerable<CompletionResult> CompleteArgument(string wordToComplete, string commandName, string parameterName, EngineIntrinsics engineIntrinsics, CommandAst commandAst, IDictionary<string, object?> fakeBoundParameters)
        {
            throw new NotImplementedException();
        }

        public object TransformArgument(object argument, Type parameterType, EngineIntrinsics engineIntrinsics)
        {
            IPAddress ip;
            if (argument is IPAddress)
            {
                ip = (IPAddress)argument;
                try
                {
                    return ToType(ip.MapToIPv4(), parameterType);
                }
                catch
                {
                    return ToType(argument, parameterType);
                }
            }
            else if (LanguagePrimitives.TryConvertTo<long>(argument, out var ipLong))
            {
                var octets = new byte[4];
                octets[0] = (byte)Math.Truncate((double)ipLong / 16777216);
                octets[1] = (byte)Math.Truncate((double)ipLong % 16777216 / 65536);
                octets[2] = (byte)Math.Truncate((double)ipLong % 65536 / 256);
                octets[3] = (byte)Math.Truncate((double)ipLong % 256);
                return ToType(new IPAddress(octets), parameterType);
            }
            else if (IPAddress.TryParse(argument.ToString(), out ip))
            {
                try
                {
                    return ToType(ip.MapToIPv4(), parameterType);
                }
                catch
                {
                    return ToType(ip, parameterType);
                }
            }
            else if (LanguagePrimitives.TryConvertTo(argument, out ip))
            {
                return ToType(ip, parameterType);
            }
            else
            {
                return argument;
            }
        }
        /// <summary>
        /// May convert an instance to <paramref name="resultType"/>.
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="resultType"></param>
        /// <returns></returns>
        private object ToType(object argument, Type resultType)
        {
            if (LanguagePrimitives.TryConvertTo(argument, resultType, out var result)) {
                return result;
            }
            else
            {
                return argument;
            }
        }

        public void ValidateArgument(object argument, EngineIntrinsics engineIntrinsics)
        {
            IPAddress ip;
            if (argument is IPAddress isIp)
            {
                ip = isIp;
            }
            else if (IPAddress.TryParse(argument.ToString(), out var parsedIp))
            {
                ip = parsedIp;
            }
            else
            {
                throw new ValidationMetadataException($"The value {argument} is not an IP address.");
            }
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                throw new ValidationMetadataException($"The value {ip} is not an IPv4 address (the address family is {ip.AddressFamily}).");
            }
        }
    }

}
