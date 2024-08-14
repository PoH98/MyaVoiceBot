using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace MyaVoiceBot.Services
{
    internal class SongService
    {
        private readonly Queue<string> songIds = new Queue<string>();
        private IAudioClient audio;
        private bool IsPlaying = false;
        internal EventHandler<EventArgs> DonePlaying;
        private readonly IVoiceChannel voiceChannel;
        private readonly IDiscordClient client;
        public SongService(IDiscordClient client, IVoiceChannel voiceChannel)
        {
            this.voiceChannel = voiceChannel;
            this.client = client;
        }
        private async Task Play(string songId)
        {
            var youtube = new YoutubeClient();
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync("https://youtube.com/watch?v=" + songId);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            using(var stream = await youtube.Videos.Streams.GetAsync(streamInfo))
            {
                using (var discord = audio.CreatePCMStream(AudioApplication.Mixed))
                {
                    while (stream.CanRead)
                    {
                        discord.WriteByte((byte)stream.ReadByte());
                    }
                    await discord.FlushAsync();
                    try { await stream.CopyToAsync(discord); }
                    finally { }
                }
            }
        }

        private async Task PlayAll()
        {
            audio = await voiceChannel.ConnectAsync(true);
            while (songIds.TryDequeue(out var songId))
            {
                await Play(songId);
            }
            IsPlaying = false;
            DonePlaying?.Invoke(audio, null);
        }

        internal void AddLink(string youtubeLink)
        {
            var regex = new Regex("(?:youtu\\.be\\/|youtube\\.com(?:\\/embed\\/|\\/v\\/|\\/watch\\?v=|\\/user\\/\\S+|\\/ytscreeningroom\\?v=|\\/sandalsResorts#\\w\\/\\w\\/.*\\/))([^\\/&]{10,12})");
            var matched = regex.Match(youtubeLink);
            songIds.Enqueue(matched.Groups[1].Value);
        }

        internal void StartPlay()
        {
            if (IsPlaying)
            {
                return;
            }
            IsPlaying = true;
            _ = PlayAll();
        }

    }
}
