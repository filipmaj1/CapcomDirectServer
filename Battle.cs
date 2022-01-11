using System;
using System.Collections.Generic;
using System.Text;

namespace FMaj.CapcomDirectServer
{
    class Battle
    {
        private const byte RANDOM = 0;
        private const byte SIDE1 = 1;
        private const byte SIDE2 = 2;
        private const byte UNSET = 255;

        private readonly Client player1, player2;        
        private byte player1Side = UNSET, player2Side = UNSET;
        public string BattleCode { get; }

        public Battle(Client player1, Client player2, string battleCode)
        {
            this.player1 = player1;
            this.player2 = player2;
            this.BattleCode = battleCode;

            if (player1.capcom.Id.Equals("123456"))
            {
                SetSide(player1, 0);
            } 
            else if (player2.capcom.Id.Equals("123456"))
            {
                SetSide(player2, 0);
            }
        }

        public void SetSide(Client player, byte side)
        {
            // If random; set. If the same choice as opponent has has already, choose other else set.
            if (player.Equals(player1))
            {
                if (side == RANDOM)
                    player1Side = RANDOM;
                else if (side != player2Side)
                    player1Side = side;
                else
                    player1Side = (byte)(side == SIDE1 ? SIDE2 : SIDE1);
            }
            else
            {
                if (side == RANDOM)
                    player2Side = RANDOM;
                else if (side != player1Side)
                    player2Side = side;
                else
                    player2Side = (byte)(side == SIDE1 ? SIDE2 : SIDE1);
            }

            // If both are set to random... randomize
            if (player1Side == RANDOM && player2Side == RANDOM)
            {
                bool player1GetSide1 = new Random().Next(100) < 50;
                player1Side = player1GetSide1 ? (byte)SIDE2 : (byte)SIDE1;
                player2Side = player1GetSide1 ? (byte)SIDE1 : (byte)SIDE2;
            } 
            // If P1 is random, it becomes what P2 isn't
            else if (player1Side == RANDOM && player2Side != UNSET)
            {
                player1Side = player2Side == SIDE1 ? (byte)SIDE2 : (byte)SIDE1;
            }
            // If P2 is random, it becomes what P1 isn't
            else if (player2Side == RANDOM && player1Side != UNSET)
            {
                player2Side = player1Side == SIDE1 ? (byte)SIDE2 : (byte)SIDE1;
            }

            // If sides figured out, send the results
            if (player1Side != UNSET && player2Side != UNSET) 
            {
                player1.VsSideResult(player1Side);
                player2.VsSideResult(player2Side);
            }
        }
    }
}
