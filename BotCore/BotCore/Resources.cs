using BotCoreNET.BotVars;
using Discord;
using JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotCoreNET
{
    internal static class Resources
    {
        internal static string BaseDirectory { get; private set; } = null;
        internal static string BotVariablesFilePath { get; private set; } = null;
        internal static string GuildsDirectory { get; private set; } = null;

        internal static void Setup(string baseDirectory = null)
        {
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                if (Directory.Exists(baseDirectory))
                {
                    BaseDirectory = baseDirectory;
                }
                else
                {
                    throw new ArgumentException($"The path provided as basedirectory \"{baseDirectory}\" does not exist!");
                }
            }
            else
            {
                BaseDirectory = $"{Environment.CurrentDirectory}/BotCore/";
            }

            BotVariablesFilePath = BaseDirectory + "BotVars.json";
            GuildsDirectory = BaseDirectory + "Guilds/";
        }

        internal static string GetGuildBotVarSaveFileName(ulong guildId)
        {
            return $"{GuildsDirectory}{guildId}/GuildBotVars.json";
        }

        public static async Task<JSONContainer> LoadJSONFile(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    string filecontent = await File.ReadAllTextAsync(path);
                    if (JSONContainer.TryParse(filecontent, out JSONContainer result, out string jsonerror))
                    {
                        return result;
                    }
                    else
                    {
                        await BotCore.Log(new LogMessage(LogSeverity.Error, "RESOURCES", $"Couldn't load \"{path}\"! JSON parsing error: {jsonerror}"));
                        return null;
                    }
                }
                catch (Exception e)
                {
                    await BotCore.Log(new LogMessage(LogSeverity.Error, "RESOURCES", $"Couldn't load \"{path}\"!", e));
                    return null;
                }
            }
            else
            {
                await BotCore.Log(new LogMessage(LogSeverity.Error, "RESOURCES", $"Couldn't load \"{path}\", does not exist!"));
                return null;
            }
        }

        public static async Task SaveJSONFile(string path, JSONContainer data)
        {
            try
            {
                await File.WriteAllTextAsync(path, data.Build());
            }
            catch (Exception e)
            {
                await BotCore.Log(new LogMessage(LogSeverity.Error, "RESOURCES", $"Couldn't save to \"{path}\"!", e));
            }
        }

        public static async Task LoadGuildFiles()
        {
            Directory.CreateDirectory(GuildsDirectory);
            string[] directories = Directory.GetDirectories(GuildsDirectory);
            foreach (string directory in directories)
            {
                string[] directoryNames = directory.Split('\\', '/');
                if (directoryNames.Length >= 1 && ulong.TryParse(directoryNames[directoryNames.Length - 1], out ulong guildId))
                {
                    GuildBotVarCollection guildCollection = BotVarManager.GetGuildBotVarCollection(guildId);
                    if (!await guildCollection.TryLoadBotVars())
                    {
                        await BotCore.Log(new LogMessage(LogSeverity.Warning, "RESOURCES", $"Could not load BotVars for guild id {guildId}"));
                    }
                }
            }
        }
    }
}
