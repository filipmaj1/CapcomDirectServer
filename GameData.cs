using System;
using System.Collections.Generic;
using System.Text;

namespace FMaj.CapcomDirectServer
{
    class GameData
    {
        public byte Rank { get; }
        public ushort Ranking { get; }
        public ushort Wins { get; }
        public ushort Losses { get; }
        public ushort Draws { get; }
        public uint SpentMoney { get; }
        public uint PlayTime { get; }

        public GameData(byte rank, ushort ranking, ushort wins, ushort losses, ushort draws, uint money, uint playtime)
        {
            this.Rank = rank;
            this.Ranking = ranking;
            this.Wins = wins;
            this.Losses = losses;
            this.Draws = draws;
            this.SpentMoney = money;
            this.PlayTime = playtime;
        }
    }
}
