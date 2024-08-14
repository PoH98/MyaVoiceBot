using Discord;
using Discord.WebSocket;
using LiteDB;
using MyaVoiceBot.Model;
using MyaVoiceBot.Services;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

namespace MyaVoiceBot
{
    internal class Program
    {
        private static Configuration _configuration;
        private static DiscordSocketClient _client;
        static async Task Main(string[] args)
        {
            Process[] instances = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location));
            int procId = Process.GetCurrentProcess().Id;
            if (instances.Length > 1)
            {
                foreach (Process i in instances)
                {
                    if (i.Id != procId)
                    {
                        i.Kill();
                    }
                }
            }
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildVoiceStates | GatewayIntents.GuildMessages | GatewayIntents.Guilds
            });
            _configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("appsettings.json"));

            VoiceService voiceService = new VoiceService(_configuration);
            ButtonService buttonService = new ButtonService(_client);
            ModelService modelService = new ModelService(_client);
            _client.UserVoiceStateUpdated += voiceService.UserVoiceStateUpdated;
            _client.ButtonExecuted += buttonService.ButtonExecuted;
            _client.ModalSubmitted += modelService.ModalSubmitted;
            _client.SelectMenuExecuted += _client_SelectMenuExecuted;
            _client.Log += _client_Log;
            if (!Directory.Exists("save"))
            {
                Directory.CreateDirectory("save");
            }
            await _client.LoginAsync(TokenType.Bot, _configuration.Token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private static async Task _client_SelectMenuExecuted(SocketMessageComponent arg)
        {
            var channel = (IVoiceChannel)await arg.GetChannelAsync();
            if (arg.Data.Value.StartsWith("confirm-ban-"))
            {
                var id = ulong.Parse(arg.Data.Value.Replace("confirm-ban-", ""));
                var user = await channel.GetUserAsync(id);
                await channel.AddPermissionOverwriteAsync(user, OverwritePermissions.DenyAll(channel));
                await channel.Guild.MoveAsync(user, null);
            }
        }

        private static Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());
            return Task.CompletedTask;
        }
        
    }
}
