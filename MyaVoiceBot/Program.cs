using Discord;
using Discord.WebSocket;
using LiteDB;
using MyaVoiceBot.Model;
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
            _client.UserVoiceStateUpdated += Socket_UserVoiceStateUpdated;
            _client.ButtonExecuted += _client_ButtonExecuted;
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

        private static async Task _client_ButtonExecuted(SocketMessageComponent arg)
        {
            var channel = (IVoiceChannel)await arg.GetChannelAsync();
            var guild = channel.Guild;
            var self = await guild.GetUserAsync(_client.CurrentUser.Id);
            var userId = arg.User.Id;
            using (var db = new LiteDatabase("Filename=save\\" + channel.Id + ".db;connection=shared"))
            {
                var members = db.GetCollection<Member>("Members");
                var user = members.FindOne(x => x.Id == userId);
                if (user == null)
                {
                    return;
                }
                switch (arg.Data.CustomId.Trim())
                {
                    case "priv":
                        if (user.Role != Role.Owner)
                        {
                            await arg.RespondAsync("你個PK，冇權限set咩set，想死？", ephemeral: true);
                            return;
                        }
                        foreach(var role in guild.Roles)
                        {
                            _ = channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(connect: PermValue.Deny));
                        }
                        await arg.RespondAsync("已設置為私人頻道");
                        break;
                    case "own":
                        user.Role = Role.Owner;
                        members.Update(user);
                        await arg.RespondAsync(arg.User.Mention + "已設置為此頻道主人");
                        break;
                    case "ban":
                        var select = new SelectMenuBuilder();
                        foreach(var member in members.FindAll())
                        {
                            if(member.Role == Role.Owner)
                            {
                                continue;
                            }
                            select.AddOption(new SelectMenuOptionBuilder().WithValue("confirm-ban-" + member.Id.ToString()).WithLabel(member.Name));
                        }
                        if(select.Options.Count < 1)
                        {
                            await arg.RespondAsync("你想ban你自己？？", ephemeral: true);
                        }
                        else
                        {
                            await arg.RespondAsync("請選擇想ban的用戶：", ephemeral: true, components: new ComponentBuilder().WithSelectMenu(select).Build());
                        }
                        break;
                    default:
                        
                        break;
                }
            }
           
        }

        private static async Task Socket_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            if(arg3.VoiceChannel != null && arg3.VoiceChannel.Id == _configuration.VoiceCreationChannel)
            {
                var name = arg1.GlobalName + "的語音頻道";
                var newChannel = await arg3.VoiceChannel.Guild.CreateVoiceChannelAsync(name, opt =>
                {
                    opt.CategoryId = _configuration.VoiceCategory;
                });
                _ = newChannel.SetStatusAsync("講緊米亞壞話(?)");
                _ = newChannel.SendMessageAsync("頻道設置", components: new ComponentBuilder().WithButton("私人頻道", "priv", ButtonStyle.Success).WithButton("Ban", "ban", ButtonStyle.Danger).Build());
                await arg3.VoiceChannel.Guild.MoveAsync(arg3.VoiceChannel.GetUser(arg1.Id), newChannel);
                using(var db = new LiteDatabase("Filename=save\\" + newChannel.Id + ".db;connection=shared"))
                {
                    var members = db.GetCollection<Member>("Members");
                    members.Insert(new Member
                    {
                        Id = arg1.Id,
                        Role = Role.Owner,
                        Name = arg1.GlobalName
                    });
                }
                await newChannel.AddPermissionOverwriteAsync(arg1, OverwritePermissions.AllowAll(newChannel));
            }
            else if(arg3.VoiceChannel != null && File.Exists("save\\" + arg3.VoiceChannel.Id + ".db"))
            {
                using(var db = new LiteDatabase("Filename=save\\" + arg3.VoiceChannel.Id + ".db;connection=shared"))
                {
                    var members = db.GetCollection<Member>("Members");
                    var user = members.FindOne(x => x.Id == arg1.Id);
                    if(user == null)
                    {
                        members.Insert(new Member
                        {
                            Id = arg1.Id,
                            Role = Role.Member,
                            Name = arg1.GlobalName
                        });
                    }
                    else if(user.Banned)
                    {
                        await arg3.VoiceChannel.Guild.MoveAsync(arg3.VoiceChannel.GetUser(arg1.Id), null);
                    }
                }
            }
            else if(arg2.VoiceChannel != null && File.Exists("save\\" + arg2.VoiceChannel.Id + ".db"))
            {
                if (arg2.VoiceChannel.ConnectedUsers.Where(x => !x.IsBot).Count() == 0)
                {
                    //remove
                    await arg2.VoiceChannel.DeleteAsync();
                    File.Delete("save\\" + arg2.VoiceChannel.Id + ".db");
                }
                else
                {
                    using (var db = new LiteDatabase("Filename=save\\" + arg2.VoiceChannel.Id + ".db;connection=shared"))
                    {
                        var members = db.GetCollection<Member>("Members");
                        var user = members.FindOne(x => x.Id == arg1.Id);
                        if (user == null)
                        {
                            return;
                        }
                        if(user.Role == Role.Owner)
                        {
                            await arg2.VoiceChannel.SendMessageAsync("頻道主人已經離開，請點擊以下按鈕獲取頻道主人權限！", components: new ComponentBuilder().WithButton("獲取權限", "own").Build());
                        }
                    }
                }
            }
        }
    }
}
