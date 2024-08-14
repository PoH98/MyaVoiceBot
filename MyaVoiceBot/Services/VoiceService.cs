using Discord.WebSocket;
using Discord;
using LiteDB;
using MyaVoiceBot.Model;

namespace MyaVoiceBot.Services
{
    internal class VoiceService
    {
        private readonly Configuration _configuration;
        internal VoiceService(Configuration configuration)
        {
            _configuration = configuration;
        }
        internal async Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            if (arg3.VoiceChannel != null && arg3.VoiceChannel.Id == _configuration.VoiceCreationChannel)
            {
                var name = arg1.GlobalName + "的語音頻道";
                var newChannel = await arg3.VoiceChannel.Guild.CreateVoiceChannelAsync(name, opt =>
                {
                    opt.CategoryId = _configuration.VoiceCategory;
                });
                _ = newChannel.SetStatusAsync("講緊米亞壞話(?)");
                _ = newChannel.SendMessageAsync("頻道設置", components: new ComponentBuilder()
                    .WithButton("私人頻道", "priv", ButtonStyle.Success)
                    .WithButton("Ban", "ban", ButtonStyle.Danger)
                    .WithButton("播歌", "music", ButtonStyle.Primary)
                    .Build());
                await arg3.VoiceChannel.Guild.MoveAsync(arg3.VoiceChannel.GetUser(arg1.Id), newChannel);
                using (var db = new LiteDatabase("Filename=save\\" + newChannel.Id + ".db;connection=shared"))
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
            else if (arg3.VoiceChannel != null && File.Exists("save\\" + arg3.VoiceChannel.Id + ".db"))
            {
                using (var db = new LiteDatabase("Filename=save\\" + arg3.VoiceChannel.Id + ".db;connection=shared"))
                {
                    var members = db.GetCollection<Member>("Members");
                    var user = members.FindOne(x => x.Id == arg1.Id);
                    if (user == null)
                    {
                        members.Insert(new Member
                        {
                            Id = arg1.Id,
                            Role = Role.Member,
                            Name = arg1.GlobalName
                        });
                    }
                    else if (user.Banned)
                    {
                        await arg3.VoiceChannel.Guild.MoveAsync(arg3.VoiceChannel.GetUser(arg1.Id), null);
                    }
                }
            }
            else if (arg2.VoiceChannel != null && File.Exists("save\\" + arg2.VoiceChannel.Id + ".db"))
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
                        if (user.Role == Role.Owner)
                        {
                            await arg2.VoiceChannel.SendMessageAsync("頻道主人已經離開，請點擊以下按鈕獲取頻道主人權限！", components: new ComponentBuilder().WithButton("獲取權限", "own").Build());
                        }
                    }
                }
            }
        }
    }
}
