using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyaVoiceBot.Services
{
    internal class ModelService
    {
        private readonly IDiscordClient _client;
        private SongService _songService;
        private ulong channelId;
        internal ModelService(IDiscordClient client)
        {
            _client = client;
        }

        internal async Task ModalSubmitted(SocketModal arg)
        {
            var channel = (IVoiceChannel)await arg.GetChannelAsync();
            if (_songService == null)
            {
                channelId = arg.ChannelId.Value;
                _songService = new SongService(_client, channel);
                _songService.DonePlaying += async (sender, e) =>
                {
                    var channel = (IVoiceChannel)await _client.GetChannelAsync(channelId);
                    await channel.DisconnectAsync();
                    _songService = null;
                };
            }
            var links = arg.Data.Components.First();
            foreach (var link in links.Value.Split("\n"))
            {
                _songService.AddLink(link);
            }
            await arg.RespondAsync("已經添加成功！");
            _songService.StartPlay();
        }
    }
}
