using Microsoft.Win32;
using System.Reflection;
using System.Text.Json;

namespace ConfigCreator
{
    internal class Program
    {
        public class Config
        {
            public string RegistryHive { get; set; } = "HKCU";
            public string RegistryBasePath { get; set; }
            public List<string> KeysToCreate { get; set; } = new();
        }

        private static void Main(string[] args)
        {
            string commandAssemblyName = "Configcreator.exe";
            string mode = "Auto";   // default
            string fileName = null;
            bool isSilentInstall = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-help":
                    case "-h":
                        ShowHelp(commandAssemblyName);
                        return;

                    case "-mode":
                    case "-m":

                        i++;
                        if (i >= args.Length)
                        {
                            Console.WriteLine("Error: -mode requires a value.");
                            return;
                        }

                        string modeValue = args[i].ToLower();

                        switch (modeValue)
                        {
                            case "-auto":
                            case "-a":
                                mode = "Auto";

                                if (i + 1 < args.Length)
                                {
                                    string silent = args[i + 1];
                                    if (silent != null && silent == "-s")
                                    {
                                        isSilentInstall = true;
                                    }
                                    i++;
                                }

                                break;

                            case "-file":
                            case "-f":
                                mode = "File";

                                if (i + 1 < args.Length)
                                {
                                    fileName = args[i + 1];

                                    i++;
                                }
                                else
                                {
                                    Console.WriteLine("Error: -file requires a filename.");
                                    return;
                                }

                                if (i + 1 < args.Length)
                                {
                                    string silent = args[i + 1];
                                    if (silent != null && silent == "-s")
                                    {
                                        isSilentInstall = true;
                                        i++;
                                    }
                                }

                                break;

                            case "-manual":
                            case "-m":
                                mode = "Manual";
                                break;

                            default:
                                Console.WriteLine($"Invalid mode: {modeValue}");
                                return;
                        }
                        break;

                        // Only valid AFTER mode=file

                        // default:
                        //  Console.WriteLine($"Unknown argument: {args[i]} at {i}");
                        //  return;
                }
            }

            // -------- VALIDATION --------
            if (mode == "file" && string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("File mode requires  <filename>");
                return;
            }

            // -------- EXECUTION --------
            Console.WriteLine($"Mode : {mode}");
            Console.WriteLine($"File : {fileName}");
            Console.WriteLine($"silent install : {isSilentInstall}");

            // ---------- Execute Based on Mode ----------
            Config config;

            switch (mode)
            {
                case "Auto":
                    Console.WriteLine("Running in AUTO mode...");
                    config = LoadConfiginAutoMode();
                    RunConfig(config, isSilentInstall);
                    break;

                case "File":
                    config = LoadConfig(fileName);
                    RunConfig(config, isSilentInstall);

                    break;

                case "Manual":
                    Console.WriteLine("Running in MANUAL mode...");
                    RunManualMode();
                    // config = GetConfigFromUserInput();
                    break;

                default:
                    throw new Exception("Unexpected mode");
            }
        }

        private static void RunManualMode()
        {
            Console.Write("Would you like to continue with manual entry? (yes/no): ");
            string choice = Console.ReadLine()?.Trim().ToLower();

            if (choice == "no")
            {
                Console.WriteLine("Exiting program...");
                return;
            }

            Config config = new Config();

            Console.Write("Enter registry hive (HKCU or HKLM): ");
            config.RegistryHive = Console.ReadLine()?.Trim().ToUpper() ?? "HKCU";

            Console.Write("Enter registry base path (e.g., Software\\MyManualApp): ");
            config.RegistryBasePath = Console.ReadLine()?.Trim();

            RegistryKey rootKey = GetRootRegistryKey(config.RegistryHive);
            if (rootKey == null)
            {
                Console.WriteLine($"Invalid registry hive: {config.RegistryHive}. Exiting.");
                return;
            }
            CheckOrCreateBasePath(rootKey, config.RegistryBasePath, false);

            while (true)
            {
                Console.Write("\nWould you like to continue and add more keys? (yes/no): ");
                string answer = Console.ReadLine()?.Trim().ToLower();

                if (answer == "no")
                {
                    Console.WriteLine("Exiting program...");
                    break;
                }
                else if (answer == "yes")
                {
                    Console.Write("Enter new key name: ");
                    string keyName = Console.ReadLine();

                    Console.Write("Enter value for the key: ");
                    string value = Console.ReadLine();

                    WriteToRegistry(rootKey, config.RegistryBasePath, keyName, value);
                }
                else
                {
                    Console.WriteLine("Please enter 'yes' or 'no'.");
                }
            }
        }

        private static void ShowHelp(string commandAssemblyName)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine($"  {commandAssemblyName} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h | -help                           Show help");
            Console.WriteLine("  -m | -mode <value>                   Set run mode");
            Console.WriteLine("       -auto | -a                        Automatic mode");
            Console.WriteLine("       -auto | -a -s                       Automatic mode with silent installation, no user input.");
            Console.WriteLine("       -file | -f  <filename>           Load from file");
            Console.WriteLine("       -file | -f  <filename> -s          Load from file with silent installation, no user input.");
            Console.WriteLine("       -manual | -m           Manual entry");

            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine($"  {commandAssemblyName} -m -auto/-a");
            Console.WriteLine($"  {commandAssemblyName} -m -auto/-a -s");
            Console.WriteLine($"  {commandAssemblyName} -m -file/-f cfg.json");
            Console.WriteLine($"  {commandAssemblyName} -m -file/-f cfg.json -s");
            Console.WriteLine($"  {commandAssemblyName} -m manual");
        }

        private static Config LoadConfiginAutoMode()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(exePath, "a1.txt");
            return LoadConfig(filePath);
        }

        private static Config LoadConfig(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Config>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load config: {ex.Message}");
                return new Config();
            }
        }

        private static void RunConfig(Config config, bool isSilentinstall)
        {
            if (string.IsNullOrWhiteSpace(config.RegistryHive))
                config.RegistryHive = "HKCU";

            if (string.IsNullOrWhiteSpace(config.RegistryBasePath))
            {
                Console.WriteLine("Config file is missing 'RegistryBasePath'. Exiting.");
                return;
            }

            Console.WriteLine($"Loaded config. Registry Hive: {config.RegistryHive}, Path: HKEY_{config.RegistryHive}\\{config.RegistryBasePath}");
            RegistryKey rootKey = GetRootRegistryKey(config.RegistryHive);
            if (rootKey == null)
            {
                Console.WriteLine($"Invalid registry hive: {config.RegistryHive}. Exiting.");
                return;
            }

            // Validate and optionally create base path
            if (!CheckOrCreateBasePath(rootKey, config.RegistryBasePath, isSilentinstall))
            {
                Console.WriteLine("Exiting program.");
                return;
            }

            if (config.KeysToCreate?.Count > 0)
            {
                foreach (var keyentity in config.KeysToCreate)
                {
                    var inputkey = keyentity.Split(":");
                    string keyName = inputkey[0].Trim();
                    string keyvalue = inputkey[1].Trim();
                    WriteToRegistry(rootKey, config.RegistryBasePath, keyName, keyvalue);
                }
            }
        }

        private static RegistryKey GetRootRegistryKey(string hive)
        {
            return hive switch
            {
                "HKCU" => Registry.CurrentUser,
                "HKLM" => Registry.LocalMachine,
                _ => null
            };
        }

        private static bool CheckOrCreateBasePath(RegistryKey root, string basePath, bool isSilentInstall)
        {
            RegistryKey existingKey = root.OpenSubKey(basePath, writable: true);

            if (existingKey == null)
            {
                Console.WriteLine($"Registry base path '{root.Name}\\{basePath}' does not exist.");

                if (isSilentInstall)
                {
                    try
                    {
                        root.CreateSubKey(basePath);
                        Console.WriteLine($"Created base path: {root.Name}\\{basePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create registry path: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    Console.Write("Would you like to create it? (yes/no): ");
                    string answer = Console.ReadLine()?.Trim().ToLower();
                    if (answer != "yes")
                        return false;
                    try
                    {
                        root.CreateSubKey(basePath);
                        Console.WriteLine($"Created base path: {root.Name}\\{basePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create registry path: {ex.Message}");
                        return false;
                    }
                }
            }
            else
            {
                existingKey.Dispose();
            }

            return true;
        }

        private static void WriteToRegistry(RegistryKey root, string basePath, string keyName, string value)
        {
            try
            {
                using (RegistryKey baseKey = root.CreateSubKey(basePath))
                {
                    baseKey.SetValue(keyName, value);
                    Console.WriteLine($"'{keyName}' = '{value}' written successfully under {root.Name}\\{basePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to registry: {ex.Message}");
            }
        }
    }
}