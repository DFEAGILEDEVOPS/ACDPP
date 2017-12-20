using Director.NetCore.Models;
using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Director.NetCore
{
    class Program
    {
        private static Dictionary<string, string> Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static List<string> Scripts = new List<string>();
        private static string ProjectId;
        private string ProjectKey;
        private const string HelpMessage = "Usage:\n\nDirector.exe [-s|--script] <scriptfilepath> [-p|--project] <projectId> [-k|--key] <securitykey>\n" 
            + "e.g., director \"c:\\myscript.json\" 360A4F23-7495-46EA-849E-6328A77546C6 ADD90C25-CD35-4E88-AC93-2248A1D9A0F3\n\n";

        enum ExitCode : int
        {
            Success = 0,
            BadArguments=1,
            BadScript = 2,
            BadProject = 3,
            BadSecurityKey = 4,
            UnknownError = 10
        }

        static void Main(string[] args)
        {
            try
            {
                #region Get the parameters
                //Show help if no arguments
                if (args == null || args.Length != 3) ShowHelp();

                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (string.IsNullOrWhiteSpace(arg)) continue;
                    var name = arg.BeforeFirstAny(@"-: /\").TrimI(Text.PunctuationChars + " ");
                    var value = arg.AfterLastAny(@"-: /\").TrimI(Text.PunctuationChars + " ");
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
                    {
                        if (name.EqualsI("s", "script"))
                            Parameters["script"] = value;
                        else if (name.EqualsI("p", "project", "projectid"))
                            Parameters["project"] = value;
                        else if (name.EqualsI("k", "key", "securitykey"))
                            Parameters["key"] = value;
                        continue;
                    }
                    arg = arg.TrimI(Text.PunctuationChars + " ");
                    if (string.IsNullOrWhiteSpace(arg)) continue;

                    if (i == 0 && arg.EndsWithI(".json", ".xml"))
                    {
                        Parameters["script"] = arg;
                        continue;
                    }
                    if (i == 1)
                    {
                        Parameters["project"] = arg;
                        continue;
                    }
                    if (i == 2)
                    {
                        Parameters["key"] = arg;
                        continue;
                    }
                }
                if (Parameters.Count != 3) ShowHelp();
                #endregion

                #region Check the parameters
                //Make sure the file exists
                if (!File.Exists(Parameters["Script"]))
                {
                    Console.WriteLine($"Cannot find script file '{Parameters["Script"]}'");
                    Environment.Exit((int)ExitCode.BadScript);
                }

                //Make sure we can read the json contents
                string json = null;
                try
                {
                    json = File.ReadAllText(Parameters["Script"]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading script file '{Parameters["Script"]}': {ex.Message}");
                    Environment.Exit((int)ExitCode.BadScript);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine($"Script file '{Parameters["Script"]}' cannot be empty");
                    Environment.Exit((int)ExitCode.BadScript);
                }

                //Make sure we can deserialize the json
                Project project = null;
                try
                {
                    var ext = Path.GetExtension(Parameters["Script"];
                    if (ext.EqualsI(".json"))
                        project = JsonConvert.DeserializeObject<Project>(json);
                    else
                        throw new NotImplementedException($"Deserialization of file type '{ext}' has not yet been implemented");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing script file '{Parameters["Script"]}': {ex.Message}");
                    Environment.Exit((int)ExitCode.BadScript);
                }

                //Make sure the project is valid
                try
                {
                    project.Validate();
                }
                catch (AggregateException aex)
                {
                    Console.WriteLine($"Script '{Parameters["Script"]}' has the following validation errors:");
                    foreach (var ex in aex.InnerExceptions)
                        Console.WriteLine(ex.Message);

                    Environment.Exit((int)ExitCode.BadScript);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Script '{Parameters["Script"]}' has the following validation error:\n{ex.Message}");
                    Environment.Exit((int)ExitCode.BadScript);
                }

                //TODO Check the credentials
                Environment.Exit((int)ExitCode.BadProject);
                Environment.Exit((int)ExitCode.BadSecurityKey);

                #endregion


                //Execute the project
                project.Execute();

                //Exit with success
                Console.WriteLine($"Script '{Parameters["Script"]}' completed successfully");
                Environment.Exit((int)ExitCode.Success);
            }
            catch (AggregateException aex)
            {
                Console.WriteLine($"Script '{Parameters["Script"]}' failed with following errors:");
                foreach (var ex in aex.InnerExceptions)
                    Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script '{Parameters["Script"]}' failed with following error:\n{ex.Message}");
            }

            Environment.Exit((int)ExitCode.UnknownError);
        }

        static void ShowHelp()
        {
            Console.WriteLine(HelpMessage);
            Environment.Exit((int)ExitCode.BadArguments);
        }
    }
}
