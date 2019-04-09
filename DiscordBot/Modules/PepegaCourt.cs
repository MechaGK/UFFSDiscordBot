using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;

namespace DiscordBot.Modules
{
    public class PepegaCourt : ModuleBase<SocketCommandContext>
    {
        class Vote
        {
            public IUserMessage Message { get; }

            public IUser User { get; }

            public int VoteCount { get; }

            public bool InProgress { get; }

            public Vote(IUserMessage message, IUser user, int voteCount = 0, bool inProgress = true)
            {
                this.User = user;
                this.Message = message;
                this.VoteCount = voteCount;
                this.InProgress = inProgress;
            }

            public Vote Copy(IUserMessage messageId = null, IUser user = null, int? voteCount = null,
                bool? inProgress = null)
            {
                return new Vote(
                    message: messageId ?? this.Message,
                    user: user ?? this.User,
                    voteCount: voteCount ?? this.VoteCount,
                    inProgress: inProgress ?? this.InProgress
                );
            }
        }

        private Vote _vote = new Vote(message: null, user: null, voteCount: 0, inProgress: false);
        private IEmote _pepega;
        private bool _reactionAddedAttached;
        private int _votesNeeded = 5;
        private int _pepegaTime = 30;

        [Command("pepega")]
        public async Task PingAsync(IUser user = null)
        {
            if (!_reactionAddedAttached)
            {
                Context.Client.ReactionAdded += ReactionAdded;
                Context.Client.ReactionRemoved += ReactionRemoved;
                this._reactionAddedAttached = true;
            }

            if (_pepega is null)
            {
                this._pepega = Emote.Parse("<:pepega:558348236730007592>");
            }

            bool voteInProgress;
            lock (_vote)
            {
                voteInProgress = _vote.InProgress;

                if (!voteInProgress)
                {
                    user = user ?? Context.User;
                    _vote = _vote.Copy(messageId: null, user: user, voteCount: 0, inProgress: true);
                }
            }

            if (voteInProgress)
            {
                await ReplyAsync($"{Context.User} der er en nominering i gang {_pepega}");
                return;
            }


            var message = await ReplyAsync($"{user.Mention} er blevet nomineret til at være {_pepega}");

            lock (_vote)
            {
                _vote = _vote.Copy(messageId: message);
            }

            await message.AddReactionAsync(_pepega);

            var task = new Task(async () => await TimeoutTest(message, user));
            task.Start();
        }

        private async Task TimeoutTest(IUserMessage message, IUser user)
        {
            await Task.Delay(TimeSpan.FromMinutes(2));

            var noVerdict = false;
            lock (_vote)
            {
                if (_vote.InProgress && _vote.Message.Id == message.Id)
                {
                    noVerdict = true;
                    _vote = _vote.Copy(inProgress: false);
                }
            }

            if (noVerdict)
            {
                await message.AddReactionAsync(new Emoji("⏳"));
                await ReplyAsync($"Tiden er gået! Det ser ikke ud til at {user.Mention} er en {_pepega}");
            }
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            lock (_vote)
            {
                if (_vote.Message.Id != message.Id || !_vote.InProgress || !reaction.Emote.Equals(_pepega))
                {
                    return;
                }

                _vote = _vote.Copy(voteCount: _vote.VoteCount - 1);
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Vote succeededVote = null;
            lock (_vote)
            {
                if (_vote.Message.Id != message.Id || !_vote.InProgress || !reaction.Emote.Equals(_pepega))
                {
                    return;
                }

                if (_vote.VoteCount + 1 >= _votesNeeded)
                {
                    succeededVote = _vote.Copy(voteCount: _vote.VoteCount + 1);
                    _vote = _vote.Copy(inProgress: false, voteCount: _vote.VoteCount + 1);
                }
                else
                {
                    _vote = _vote.Copy(voteCount: _vote.VoteCount + 1);
                }
            }

            if (succeededVote != null)
            {
                await succeededVote.Message.AddReactionAsync(new Emoji("✅"));
                await ReplyAsync($"Stemmerne er talt! {_vote.User.Mention} er en {_pepega}");
                
                
                var user = succeededVote.User;
                var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Pepega");
                await ((IGuildUser) user).AddRoleAsync(role);
                
                var task = new Task(async () => await DelayedRemovePepegaTitle(user));
                task.Start();
            }
        }

        private async Task DelayedRemovePepegaTitle(IUser user)
        {
            await Task.Delay(TimeSpan.FromMinutes(_pepegaTime));
            
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Pepega");
            await ((IGuildUser) user).RemoveRoleAsync(role);
        }
    }
}