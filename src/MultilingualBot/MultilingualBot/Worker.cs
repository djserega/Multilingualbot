using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultilingualBot
{
    public class Worker : BackgroundService
    {
        private ulong _checkChannel;
        private ulong[] _checkMessageIds;
        private readonly Dictionary<string, string> _dictEmojiRole;

        private IConfigurationRoot _config;

        private readonly ILogger<Worker> _logger;
        private readonly CancellationTokenSource _ctsBot;
        private DiscordClient _discord;
        private CommandsNextExtension _commands;
        private readonly InteractivityConfiguration _interactivity;

        public Worker(ILogger<Worker> logger)
        {
            Console.WriteLine("Добро пожаловать в мультиязычный бот!");
            Console.WriteLine();

            try
            {
                _dictEmojiRole = new Dictionary<string, string>();

                _logger = logger;

                _ctsBot = new CancellationTokenSource();

                if (!InitBuilder())
                    return;

                if (!InitDiscordClient())
                    return;

                //if (!InitCommands())
                //    return;

                if (!GetBaseConfigSettings())
                    return;

                if (!FillDictEmojiRole())
                    return;

                _discord.MessageReactionAdded += Discord_MessageReactionAdded;
                _discord.MessageReactionRemoved += Discord_MessageReactionRemoved;
                //_discord.GuildMemberAdded += Discord_GuildMemberAdded;
                //_discord.GuildMemberRemoved += Discord_GuildMemberRemoved;
                //_discord.GuildMemberUpdated += Discord_GuildMemberUpdated;
                //_discord.GuildBanAdded += Discord_GuildBanAdded;
                //_discord.GuildDeleted += Discord_GuildDeleted;

                //Commands.SetCommand.SetEmojiRoleEvent += SetCommand_SetEmojiRoleEvent;

                _discord.ConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        //private async Task Discord_GuildMemberAdded(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberAddEventArgs e)
        //{

        //}

        //private async Task Discord_GuildDeleted(DiscordClient sender, DSharpPlus.EventArgs.GuildDeleteEventArgs e)
        //{
            
        //}

        //private async Task Discord_GuildBanAdded(DiscordClient sender, DSharpPlus.EventArgs.GuildBanAddEventArgs e)
        //{
            
        //}

        //private async Task Discord_GuildMemberUpdated(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberUpdateEventArgs e)
        //{
            
        //}

        #region Initialize bot

        private bool InitBuilder()
        {
            try
            {
                Console.WriteLine("Инициализация config-файла...");

                _config = new ConfigurationBuilder()
                    .SetBasePath(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName)
                    .AddJsonFile("config.json", false, true)
                    .Build();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Не удалось инициализировать config-файл." +
                    "" + ex.Message);

                return false;
            }
        }

        private bool InitDiscordClient()
        {
            try
            {
                Console.WriteLine("Создание дискорд-клиента...");

                _discord = new DiscordClient(new DiscordConfiguration
                {
                    Token = _config.GetValue<string>("discord:token"),
                    TokenType = TokenType.Bot
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Не удалось инициализировать дискорд-клиент." +
                    "" + ex.Message);

                return false;
            }
        }

        private bool InitCommands()
        {
            try
            {
                Console.WriteLine("Загрузка комманд...");

                _commands = _discord.UseCommandsNext(new CommandsNextConfiguration()
                {
                    StringPrefixes = new[] { _config.GetValue<string>("discord:commandPrefix") }
                });

                Type baseInterfaceCommand = typeof(Commands.ICommands);
                IEnumerable<Type> typesCommand = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => baseInterfaceCommand.IsAssignableFrom(p) && !p.IsInterface);

                Type[] typeList = typesCommand as Type[] ?? typesCommand.ToArray();
                foreach (Type typeCommand in typeList)
                {
                    Console.WriteLine($" -- загружено -> {typeCommand.Name}.");

                    _commands.RegisterCommands(typeCommand);
                }

                Console.WriteLine($" Загружено модулей: {typeList.Length}.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Не удалось инициализировать команды\n" +
                    "" + ex.Message);

                return false;
            }
        }

        private bool GetBaseConfigSettings()
        {
            try
            {
                _checkChannel = _config.GetValue<string>("checkChannel").GetID();


                IEnumerable<KeyValuePair<string, string>> messageIds = _config.GetSection("messageIds").AsEnumerable().Where(el => el.Value != null);
                _checkMessageIds = new ulong[messageIds.Count()];
                int i = 0;
                foreach (KeyValuePair<string, string> keyValueId in messageIds)
                    _checkMessageIds[i++] = ulong.Parse(keyValueId.Value);


                StringBuilder logBuilder = new("Get base config:");
                logBuilder.AppendLine();

                logBuilder.AppendLine($"check channel: {_checkChannel}");
                logBuilder.AppendLine($"check messages: {string.Join(", ", _checkMessageIds)}");

                _logger.LogInformation(logBuilder.ToString());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Не удалось загрузить базовые параметры настроек\n" +
                    "" + ex.Message);

                return false;
            }
        }

        private bool FillDictEmojiRole()
        {
            IEnumerable<KeyValuePair<string, string>> listEmojiRole = _config.GetSection("emojiroles").AsEnumerable().Where(el => el.Value != null);

            StringBuilder logBuilder = new("Fill list emoji+role:");
            logBuilder.AppendLine();

            string emoji = default;
            string role = default;
            foreach (KeyValuePair<string, string> keyEmojiRole in listEmojiRole)
            {
                if (keyEmojiRole.Key.Contains(":emoji"))
                    emoji = keyEmojiRole.Value;

                if (keyEmojiRole.Key.Contains(":role"))
                    role = keyEmojiRole.Value;

                if (emoji != default && role != default)
                {
                    logBuilder.AppendLine($" set pair -> {emoji} : {role}");

                    _dictEmojiRole.Add(emoji, role);

                    emoji = default;
                    role = default;
                }
            }

            _logger.LogInformation(logBuilder.ToString());

            return true;
        }

        #endregion

        private async Task Discord_MessageReactionAdded(DiscordClient sender, DSharpPlus.EventArgs.MessageReactionAddEventArgs e)
        {
            try
            {
                if (e.Channel.Id.Equals(_checkChannel))
                {
                    string emojiName = e.Emoji.Name;

                    if (_dictEmojiRole.ContainsKey(emojiName))
                    {
                        DiscordRole role = _dictEmojiRole[emojiName].GetDiscordRole(e.Guild);

                        _logger.LogInformation($"ID: {e.User.Id}. User: {e.User.Username}#{e.User.Discriminator}. Выдача роли: {role.Name}");

                        await (await e.Guild.GetMemberAsync(e.User.Id)).GrantRoleAsync(role, $"Роль выдана по эмоции: {emojiName}");

                        StringBuilder builderAllRoles = new StringBuilder();
                        foreach (DiscordRole itemRole in (await e.Guild.GetMemberAsync(e.User.Id)).Roles)
                        {
                            builderAllRoles.Append(itemRole.Name);
                            builderAllRoles.Append("; ");
                        }
                        _logger.LogInformation($"ID: {e.User.Id}. User: {e.User.Username}#{e.User.Discriminator}. Текущие роли: {builderAllRoles}");

                        _logger.LogInformation($"ID: {e.User.Id}. User: {e.User.Username}#{e.User.Discriminator}. Роль выдана: {role.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ID: {e.User.Id}. User: {e.User.Username}#{e.User.Discriminator}.\n{ex}");
            }
        }

        private async Task Discord_MessageReactionRemoved(DiscordClient sender, DSharpPlus.EventArgs.MessageReactionRemoveEventArgs e)
        {
            try
            {
                if (e.Channel.Id.Equals(_checkChannel))
                {
                    string emojiName = e.Emoji.Name;

                    if (_dictEmojiRole.ContainsKey(emojiName))
                    {
                        DiscordRole role = _dictEmojiRole[emojiName].GetDiscordRole(e.Guild);

                        _logger.LogInformation($"ID: {e.User.Id}. User: {e.User.Username}#{e.User.Discriminator}. Удаление роли: {role.Name}");

                        await (await e.Guild.GetMemberAsync(e.User.Id)).RevokeRoleAsync(role, $"Роль снята по удаленной эмоции: {emojiName}");

                        _logger.LogInformation($"ID: {e.User.Id}. User: {e.User.Username}#{e.User.Discriminator}. Роль удалена: {role.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ID: {e.User.Id}. User: {e.User.Username}#{e.User.Discriminator}.\n{ex}");
            }
        }

        //private async Task Discord_GuildMemberRemoved(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs e)
        //{
        //    try
        //    {
        //        DiscordChannel channel = await _discord.GetChannelAsync(_checkChannel);

        //        if (channel == default)
        //        {
        //            _logger.LogWarning($"'Привязаный' канал ({_checkChannel}) удален.");
        //        }
        //        else
        //        {
        //            foreach (ulong idMessage in _checkMessageIds)
        //            {
        //                DiscordMessage message = await channel.GetMessageAsync(idMessage);
        //                if (message == default)
        //                {
        //                    _logger.LogWarning($"'Привязаное' сообщение ({idMessage}) удалено.");
        //                }
        //                else
        //                {
        //                    DiscordUser user = await _discord.GetUserAsync(e.Member.Id);

        //                    if (user == default)
        //                    {
        //                        _logger.LogWarning($"Не найден пользователь ({e.Member.Id}).");
        //                    }
        //                    else
        //                    {
        //                        foreach (DiscordReaction keyEmoji in message.Reactions)
        //                            message.DeleteReactionAsync(keyEmoji.Emoji, user, "Leave from server");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex.ToString());
        //    }
        //}

        private bool SetCommand_SetEmojiRoleEvent(string emoji, string role)
        {
            try
            {
                if (_dictEmojiRole.ContainsKey(emoji))
                    return false;
                else
                {
                    _dictEmojiRole.Add(emoji, role);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return false;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10 * 60 * 1000, stoppingToken);
            }
        }
    }
}
