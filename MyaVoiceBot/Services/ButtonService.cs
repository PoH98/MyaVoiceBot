using Discord.WebSocket;
using Discord;
using LiteDB;
using MyaVoiceBot.Model;

namespace MyaVoiceBot.Services
{
    internal class ButtonService
    {
        private readonly DiscordSocketClient _client;
        internal ButtonService(DiscordSocketClient client) 
        {
            _client = client;
        }
        internal async Task ButtonExecuted(SocketMessageComponent arg)
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
                        foreach (var role in guild.Roles)
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
                        foreach (var member in members.FindAll())
                        {
                            if (member.Role == Role.Owner)
                            {
                                continue;
                            }
                            select.AddOption(new SelectMenuOptionBuilder().WithValue("confirm-ban-" + member.Id.ToString()).WithLabel(member.Name));
                        }
                        if (select.Options.Count < 1)
                        {
                            await arg.RespondAsync("你想ban你自己？？", ephemeral: true);
                        }
                        else
                        {
                            await arg.RespondAsync("請選擇想ban的用戶：", ephemeral: true, components: new ComponentBuilder().WithSelectMenu(select).Build());
                        }
                        break;
                    case "music":
                        if (self.VoiceChannel != null && self.VoiceChannel.Id != channel.Id)
                        {
                            //had been in another voice channel
                            await arg.RespondAsync("我已經係其他VC("+self.VoiceChannel.Mention+")內播緊歌，無法同時間播放！");
                            return;
                        }
                        var mb = new ModalBuilder()
                        .WithTitle("請貼上你要播放的Youtube鏈接 (一行一個link)")
                        .WithCustomId("music_bot")
                        .AddTextInput("Youtube Link", "yt_link", style: TextInputStyle.Paragraph, required: true);
                        await arg.RespondWithModalAsync(mb.Build());
                        break;
                    default:

                        break;
                }
            }

        }
    }
}
