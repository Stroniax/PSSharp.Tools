using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace PSSharp
{
    /// <summary>
    /// Data used for <see cref="LearningCompletionAttribute"/> and <see cref="CompletionLearnerAttribute"/>.
    /// </summary>
    public class LearnedCompletionData
    {
#pragma warning disable CS8618 // Default constructor required for serialization.
        public LearnedCompletionData() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal LearnedCompletionData(
            string command,
            string parameter,
            params string[] completions)
        {
            Command = command;
            Parameter = parameter;
            Completions.AddRange(completions);
        }
        internal static string LearningStoragePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PSSharp", "LearningCompletion.xml");
        internal static List<LearnedCompletionData> GetLearnedCompletions()
        {
            if (!File.Exists(LearningStoragePath))
            {
                return new List<LearnedCompletionData>();
            }
            try
            {
                var serializer = new XmlSerializer(typeof(List<LearnedCompletionData>));
                using var fs = new FileStream(LearningStoragePath, FileMode.Open, FileAccess.Read);
                return (List<LearnedCompletionData>)serializer.Deserialize(fs);
            }
            catch
            {
                File.Delete(LearningStoragePath);
                return new List<LearnedCompletionData>();
            }
        }
        internal static void LearnCompletion(string command, string parameter, string value)
        {
            var currentData = GetLearnedCompletions();
            var foundExisting = false;
            foreach (var item in currentData)
            {
                if (item.Command.Equals(command, StringComparison.OrdinalIgnoreCase)
                    && item.Parameter.Equals(parameter, StringComparison.OrdinalIgnoreCase))
                {
                    foundExisting = true;
                    if (item.Completions.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    else
                    {
                        item.Completions.Add(value);
                    }
                }
            }
            if (!foundExisting)
            {
                currentData.Add(new LearnedCompletionData(command, parameter, value));
            }

            var directory = Path.GetDirectoryName(LearningStoragePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var serializer = new XmlSerializer(typeof(List<LearnedCompletionData>));
            using var fs = new FileStream(LearningStoragePath, FileMode.OpenOrCreate, FileAccess.Write);
            serializer.Serialize(fs, currentData);
        }
        [XmlAttribute]
        public string Command { get; set; }
        [XmlAttribute]
        public string Parameter { get; set; }
        [XmlArrayItem]
        public List<string> Completions { get; private set; } = new List<string>();
    }
}
