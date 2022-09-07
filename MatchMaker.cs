using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer
{
    class MatchMaker
    {
        private readonly Server server;
        private readonly List<MatchMakingEntry> currentlyMatchMaking = new List<MatchMakingEntry>();

        class MatchMakingEntry
        {
            public readonly Client client;
            public readonly MatchMakingScope scope;
            public readonly byte gameCode;
            public readonly byte genreCode;
            public readonly Room room;
            public readonly long joinTime;

            public MatchMakingEntry(Client client, MatchMakingScope scope, byte gameCode, Room room, long joinTime)
            {
                this.client = client;
                this.scope = scope;
                this.gameCode = gameCode;
                this.genreCode = client.currentGenre;
                this.room = room;
                this.joinTime = joinTime;
            }
        }

        public MatchMaker(Server server)
        {
            this.server = server;
        }

        public void AddClient(Client client, MatchMakingScope scope, Room room = null)
        { 
            lock (currentlyMatchMaking)
            {
                bool alreadyExists = currentlyMatchMaking.Where(entry => entry.client.Equals(client)).Count() != 0;
                if (alreadyExists)
                    return;

                currentlyMatchMaking.Add(new MatchMakingEntry(client, scope, client.gameCode, room, DateTime.Now.Ticks));
                currentlyMatchMaking.Sort(delegate (MatchMakingEntry e1, MatchMakingEntry e2)
                {
                    return e1.joinTime.CompareTo(e2.joinTime);
                });
            }
        }

        public void RemoveClient(Client client)
        {
            lock (currentlyMatchMaking)
            {
                MatchMakingEntry toRemove = currentlyMatchMaking.FirstOrDefault(entry => entry.client.Equals(client));
                if (toRemove != null)
                    currentlyMatchMaking.Remove(toRemove);
            }
        }

        public void ClearAll()
        {
            PacketWriter writer = new PacketWriter();
            lock (currentlyMatchMaking)
            {
                foreach (MatchMakingEntry entry in currentlyMatchMaking)                
                    entry.client.SendMessage(ServerOpcodes.BeginnerMatchMakingResult, writer.WriteByte((byte)MatchMakingResult.ForceCancel).Finish());                

                currentlyMatchMaking.Clear();
            }
        }

        public void Update()
        {
            lock (currentlyMatchMaking) {
                for (int i = 0; i < currentlyMatchMaking.Count; i++) 
                {
                    MatchMakingEntry entry = currentlyMatchMaking[i];
                    MatchMakingEntry otherEntry;

                    if (entry.scope == MatchMakingScope.Any)
                    {
                        otherEntry = currentlyMatchMaking.FirstOrDefault(otherEntry => !entry.Equals(otherEntry) && (otherEntry.scope == MatchMakingScope.Any ||
                            (otherEntry.scope == MatchMakingScope.Ranked && Math.Abs(entry.client.gameData.Rank - otherEntry.client.gameData.Rank) <= 3)) && entry.genreCode == otherEntry.genreCode);                        
                    }
                    else if (entry.scope == MatchMakingScope.Ranked)
                    {
                        otherEntry = currentlyMatchMaking.FirstOrDefault(otherEntry => !entry.Equals(otherEntry) && (otherEntry.scope == MatchMakingScope.Ranked && Math.Abs(entry.client.gameData.Rank - otherEntry.client.gameData.Rank) <= 3) && entry.genreCode == otherEntry.genreCode);
                    }
                    else
                    {
                        otherEntry = currentlyMatchMaking.FirstOrDefault(otherEntry => !entry.Equals(otherEntry) && entry.room.Equals(otherEntry.room));
                    }

                    if (otherEntry != null)
                    {
                        currentlyMatchMaking.Remove(entry);
                        currentlyMatchMaking.Remove(otherEntry);

                        Battle battle = server.NewBattle(entry.client, otherEntry.client);
                        entry.client.FoundMatch(otherEntry.client, battle);
                        otherEntry.client.FoundMatch(entry.client, battle);

                        i--;
                    }
                }
            }
        }
    }
}
