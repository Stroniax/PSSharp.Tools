using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace PSSharp.Commands
{
    [Cmdlet("New","FormattedString")]
    public class NewFormattedStringCommand : Cmdlet
    {
        const char Escape = (char)0x1b;
        const string CancelEscape = "0m[";
        private static string ApplyEscapeSequence(string input, params byte[] escapeCodes)
        {
            return string.Join("", escapeCodes.Select(i => Escape + "[" + i + "m")) + input + Escape + CancelEscape;
        }
        public class ForegroundColors
        {
            public const byte Black = 30;
            public const byte DarkRed = 31;
            public const byte DarkGreen = 32;
            public const byte White = 33;
            public const byte DarkBlue = 34;
            // public const byte Invisible = 35;
            public const byte DarkCyan = 36;
            public const byte Gray = 37;

            public const byte DarkGray = 90;
            public const byte Red = 91;
            public const byte Green = 92;
            public const byte Yellow = 93;
            public const byte Blue = 94;
            public const byte Magenta = 95;
            public const byte Cyan = 96;
        }
        public class BackgroundColors
        {
            public const byte Black = 40;
            public const byte DarkRed = 41;
            public const byte DarkGreen = 42;
            public const byte White = 43;
            public const byte DarkBlue = 44;
            // public const byte Invisible = 45;
            public const byte DarkCyan = 46;
            public const byte Gray = 47;
            
            public const byte DarkGray = 100;
            public const byte Red = 101;
            public const byte Green = 102;
            public const byte Yellow = 103;
            public const byte Blue = 104;
            public const byte Magenta = 105;
            public const byte Cyan = 106;
            public const byte DarkWhite = 107;
        }

        [Parameter(ValueFromPipeline = true)]
        public PSObject[]? InputObject { get; set; }
        [Parameter]
        public bool NoEnumerate { get; set; }
        [Parameter]
        [ConstantValueCompletion(typeof(ForegroundColors))]
        [ConstantValueTransformation(typeof(ForegroundColors))]
        public byte? ForegroundColor { get; set; }
        [Parameter]
        [ConstantValueCompletion(typeof(BackgroundColors))]
        [ConstantValueTransformation(typeof(BackgroundColors))]
        public byte? BackgroundColor { get; set; }
        [Parameter]
        [ConstantValueCompletion(typeof(Features))]
        [ConstantValueTransformation(typeof(Features))]
        public byte? Feature { get; set; }
        public enum Features
        {
            Highlight = 7,
            Underline = 4,
        }
        protected override void ProcessRecord()
        {
            WriteObject(Escape + "[" + ForegroundColor ?? 0 + "m");
            WriteObject(Escape + "[" + BackgroundColor ?? 0 + "m");
            WriteObject(Escape + "[" + Feature ?? 0 + "m");
            WriteObject(InputObject, !NoEnumerate);
            WriteObject(Escape + CancelEscape);
            base.ProcessRecord();
        }
    }
}
