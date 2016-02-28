using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EloBuddy;

namespace DevALot
{
    public class GameObjectDiagnosis : IDisposable
    {
        public const string IndentString = "    ";
        public const int MaxRecursiveDepth = 4;
        public static int SingleDiagnosisCount { get; set; }

        private static readonly List<Type> ObjectsToDeeplyAnalyze = new List<Type>
        {
            typeof (GameObject),
            typeof (BuffInstance),
            typeof (InventorySlot),
            typeof (Spellbook),
            typeof (SpellDataInst),
            typeof (Experience),
            typeof (EventArgs)
        };

        // aka known bugs
        public static readonly List<string> PropertiesToIgnore = new List<string>
        {
            "IsVisible", // General
            "OverrideCollisionHeight", // Obj_BarracksDampener
            "IsBot", // Obj_SpawnPoint
            "IsRanged", // Obj_Barracks
            "ResourceName"
        };

        public static readonly List<string> PreferredProperties = new List<string>
        {
            "ChampionName",
            "BaseSkinName",
            "Name",
            "NetworkId"
        };

        public GameObject Handle { get; set; }
        public StreamWriter Writer { get; set; }
        public string FileLocation { get; set; }
        public bool DisposeWriter { get; set; }

        private int CurrentIndent { get; set; }
        private readonly List<int> _analyzedObejcts = new List<int>();
        public int CurrentRecursiveDepth { get; set; }
        public string CurrentProperty { get; set; }
        public readonly Dictionary<double, string> ComputeTimes = new Dictionary<double, string>();

        public GameObjectDiagnosis(GameObject obj, StreamWriter writer = null, string fileLocation = null)
        {
            Handle = obj;
            FileLocation = writer == null ? Path.Combine(Environment.CurrentDirectory, string.Format("single_diagnosis_{0}.txt", SingleDiagnosisCount++)) : null;
            Writer = writer ?? File.CreateText(fileLocation ?? FileLocation);
            DisposeWriter = writer == null;

            if (Handle == null)
            {
                WriteLine("Handle is null!");
                Flush();
                throw new ArgumentNullException("obj");
            }

            // Create header
            WriteLine("------------------------------------------------------------------------------------------------");
            WriteLine("Analyzing GameObject of System.Type: {0}", Handle.GetType().Name);
            Write("GameObjectType: ");
            Flush();
            WriteLine(Handle.Type);
            WriteLine("Beginning recursive diagnosis of the properties...");
            WriteLine();
        }

        public void Analyze(object toAnalyze = null, bool analyzeHandle = true, bool recursiveCheck = true)
        {
            if (toAnalyze != null && !ObjectsToDeeplyAnalyze.Any(o => o.IsInstanceOfType(toAnalyze)))
            {
                // Do not analyze this object
                return;
            }
            if (toAnalyze == null && !analyzeHandle)
            {
                // Don't analyze null
                return;
            }

            var stopwatch = new Stopwatch();

            var gameObjectAnalyze = (toAnalyze ?? Handle) as GameObject;
            if (gameObjectAnalyze != null)
            {
                _analyzedObejcts.Add(gameObjectAnalyze.NetworkId);
            }

            WriteLine("Properties of {0}:", (toAnalyze ?? Handle).GetType().Name);
            WriteLine("------------------------");

            var properties = (toAnalyze ?? Handle).GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead).ToList();
            var preferredProperties = properties.Where(o => PreferredProperties.Contains(o.Name)).ToList();

            foreach (var propertyInfo in preferredProperties.Concat(properties.Where(o => !preferredProperties.Contains(o))))
            {
                Write(" - " + propertyInfo.Name + ": ");
                Flush();
                CurrentProperty = propertyInfo.Name;

                if (PropertiesToIgnore.Contains(propertyInfo.Name))
                {
                    WriteLine("<disabled>");
                }
                else
                {
                    stopwatch.Start();
                    var value = propertyInfo.GetValue((toAnalyze ?? Handle), null);
                    stopwatch.Stop();
                    ComputeTimes[stopwatch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond] = (toAnalyze ?? Handle).GetType().Name + "." + propertyInfo.Name;

                    // Collections
                    if ((propertyInfo.PropertyType.IsArray || typeof (IEnumerable).IsAssignableFrom(propertyInfo.PropertyType)) && !typeof (string).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        var contentType = propertyInfo.PropertyType.GetElementType() ?? propertyInfo.PropertyType.GetGenericArguments().FirstOrDefault();
                        if (contentType != null)
                        {
                            WriteLine("Collection<{0}>    ({1}ms)", contentType.Name, stopwatch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond);

                            if (ObjectsToDeeplyAnalyze.Any(o => o.IsAssignableFrom(contentType)))
                            {
                                var array = ((IEnumerable) value).Cast<object>().ToArray();
                                if (array.Length > 0)
                                {
                                    Flush();
                                    CurrentIndent++;
                                    Write(IndentString);
                                    stopwatch.Reset();
                                    for (var i = 0; i < array.Length - 1; i++)
                                    {
                                        stopwatch.Start();
                                        Analyze(array[i], false);
                                        stopwatch.Stop();
                                        WriteLine();
                                    }
                                    stopwatch.Start();
                                    Analyze(array[array.Length - 1], false);
                                    stopwatch.Stop();
                                    WriteLine();
                                    Write("Total analyze time: {0}ms", stopwatch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond);
                                    CurrentIndent--;
                                    WriteLine();
                                }
                            }
                        }
                        else
                        {
                            WriteLine("{0}    ({1}ms)", value, stopwatch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond);
                        }
                    }
                    else
                    {
                        WriteLine("{0}    ({1}ms)", value, stopwatch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond);

                        if (ObjectsToDeeplyAnalyze.Any(o => o.IsAssignableFrom(propertyInfo.PropertyType)))
                        {
                            var gameObject = value as GameObject;
                            if (gameObject == null || !_analyzedObejcts.Contains(gameObject.NetworkId))
                            {
                                if (recursiveCheck)
                                {
                                    if (CurrentRecursiveDepth < MaxRecursiveDepth && value != null)
                                    {
                                        CurrentRecursiveDepth++;
                                        CurrentIndent++;
                                        Write(IndentString);
                                        stopwatch.Reset();
                                        stopwatch.Start();
                                        Analyze(value, false);
                                        stopwatch.Stop();
                                        WriteLine();
                                        Write("Total analyze time: {0}ms", stopwatch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond);
                                        CurrentIndent--;
                                        WriteLine();
                                        CurrentRecursiveDepth--;
                                    }
                                }
                            }
                        }
                    }
                }

                Flush();
            }
            WriteLine("------------------------");
            Write("End of properties of {0}!", (toAnalyze ?? Handle).GetType().Name);
        }

        private void Write(object obj)
        {
            Write(obj != null ? obj.ToString() : "Null");
        }

        private void Write(string format, params object[] args)
        {
            Writer.Write(format, args);
        }

        private void WriteLine()
        {
            Writer.WriteLine();
            AddIndentString();
        }

        private void WriteLine(object obj)
        {
            WriteLine(obj != null ? obj.ToString() : "Null");
        }

        private void WriteLine(string format, params object[] args)
        {
            try
            {
                if (args == null)
                {
                    Write(format, "Null");
                }
                else
                {
                    Write(format, args);
                }
            }
            catch (FormatException e)
            {
                Console.WriteLine("Wrong property value format!");
                Console.WriteLine("Original handle: {0}", Handle != null ? Handle.Type.ToString() : "Null");
                Console.WriteLine("Current Property: {0}", CurrentProperty);
            }
            WriteLine();
        }

        private void Flush()
        {
            Writer.Flush();
        }

        private void AddIndentString()
        {
            Enumerable.Range(0, CurrentIndent).ForEach(o => Writer.Write(IndentString));
        }

        public void Dispose()
        {
            if (Writer != null)
            {
                // Create footer
                WriteLine();
                WriteLine();
                WriteLine("Recursive diagnosis of {0} complete!", Handle != null ? Handle.GetType().Name : "Null");
                WriteLine("------------------------------------------------------------------------------------------------");
                Flush();
            }

            if (DisposeWriter)
            {
                if (Writer != null)
                {
                    Writer.Dispose();
                }
                if (FileLocation != null && File.Exists(FileLocation))
                {
                    File.Delete(FileLocation);
                }
            }
            Handle = null;
            Writer = null;
            FileLocation = null;
        }
    }
}
