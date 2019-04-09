using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace PepegaCourt.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        [Command("test")]
        public Task PingAsync()
            => ReplyAsync("pong!");
    }
}