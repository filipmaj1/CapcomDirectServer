using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FMaj.CapcomDirectServer
{
    class Capcom
    {
        public const string GUEST_USERID = "*******";

        public enum GameCodes
        {
            MarvelVsCapcom2     = 1,
            Powerstone2         = 2,
            SF3ThirdStrike      = 3,
            SFZero3             = 4,
            Spawn               = 5,
            Vampire             = 6,
            NettoDeTennis       = 7,
            CapcomVsSNK_Pro     = 8,
            Jojo                = 9,
            ProjectJustice      = 10,
            SFIIX               = 11,
            TechRomancer        = 12,
            TaisenNetGimmick    = 13,
            HeavyMetal          = 14,
            SuperPuzzleFigher   = 15
        }

        public enum ClientOpcodes
        {
            SendIndividualID    = 0x7101,
            SendGameCode        = 0x7102,
            SendBattleCode      = 0x7103,
            SendUserId          = 0x7106,
            SendUserHandle      = 0x7107,
            SendEmail           = 0x7108,
            SendTelephoneNo     = 0x7109,
            SendBattleData      = 0x710A,
            SendProfile         = 0x710C,
            SendGameParam       = 0x710D,
            SendSubGameParam    = 0x710E,

            GetUserRank         = 0x7202,
            GetUserWinLose      = 0x7203,
            GetUserRanking      = 0x7204,
            GetSegaMessage      = 0x7205,
            GetUserMoney        = 0x7206,
            GetUserTime         = 0x7207,
            GetUserMessage      = 0x720A,
            GetUserRankingB     = 0x720B,

            SendEnterRoomList   = 0x7001,
            SendEnterRoom       = 0x7301,
            SendExitRoom        = 0x7302,
            SendRoomCount       = 0x7004,
            SendChangeGenre     = 0x7005,
            SendRoomInfo        = 0x7401,
            SendChatMessage     = 0x7B01,

            EnterMainMenu       = 0x7C01,
            SendConnectResponse = 0x7E01
        }

        public enum ServerOpcodes
        {
            KeepAlivePing       = 0x5F01,
            BadConnect          = 0x6E01,
            GoodConnect         = 0x6F01,
            GoodConnect2        = 0x6F02,
            Shutdown            = 0x6F04,

            GetIndividualID     = 0x6101,
            GetGameCode         = 0x6102,
            GetBattleCode       = 0x6103,
            GetUserId           = 0x6106,
            GetUserHandle       = 0x6107,
            GetEmail            = 0x6108,
            GetTelephoneNo      = 0x6109,
            GetBattleData       = 0x610A,
            GetProfile          = 0x610C,
            GetGameParam        = 0x610D,
            GetSubGameParam     = 0x610E,

            SetUserRank         = 0x6202,
            SetUserWinLose      = 0x6203,
            SetUserRanking      = 0x6204,
            SetSegaMessage      = 0x6205,
            SetUserMoney        = 0x6206,
            SetUserTime         = 0x6207,
            SetUserMessage      = 0x620A,
            SetUserRankingB     = 0x620B,
            
            EnterRoomList       = 0x6001,
            EnterRoom           = 0x6301,
            ExitRoom            = 0x6302,
            SendRoomCount       = 0x6004,
            ChangeRoomGenre     = 0x6005,
            SendRoomInfo        = 0x6401,
            UpdateRoomAccess    = 0x6402,
            UpdateRoomInfo      = 0x6403,
            UpdateRoomName      = 0x6404,

            ChatroomMatchMakingResult   = 0x6501,
            RankedMatchMakingResult     = 0x6502,
            BeginnerMatchMakingResult   = 0x6503,
            CancelMatchMaking           = 0x6504,
            SearchMatchMakingResult     = 0x6505,
            SendChallenge               = 0x6506,
            Unknown1                    = 0x6507,
            
            SendOpponentUserId          = 0x6601,
            SendOpponentHandle          = 0x6602,
            SendOpponentRank            = 0x6603,
            SendOpponentWinLose         = 0x6604,
            Unknown2                    = 0x6606,

            SendModemDirection          = 0x6701,
            SendBattleCode              = 0x6702,
            SendModemMessage            = 0x6703,
            
            ChatMessage                 = 0x6B01
        }

        public enum SearchResult
        {
            MatchRefused = 0,
            MatchAccepted = 1,
            UserIsBusy = 2,
            UserIsNotOnline1 = 3,
            UserFightingOrNotOnline = 4,
            UserIsNotOnline2 = 5
        }

        public enum MatchMakingResult
        {
            ForceCancel = 0,
            Found = 1,
            EnableCancel = 2
        }
        public enum MatchMakingScope { Any, Ranked, Chatroom }

        private static readonly char[] ID_TABLE =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y', 'Z'
        };

        public static string GenerateNewID()
        {
            long ticks = DateTime.Now.Ticks;
            int hash = ticks.GetHashCode();
            char[] asciiString = new char[6];
            for (int i = 0; i < 4; i++)
                asciiString[i] = ID_TABLE[(((hash >> (8 * i)) & 0xFF)) % ID_TABLE.Length];
            asciiString[4] = ID_TABLE[(ticks & 0xFF) % ID_TABLE.Length];
            asciiString[5] = ID_TABLE[((ticks>>8) & 0xFF) % ID_TABLE.Length];

            return new string(asciiString);
        }

        public static byte[] UpdateRoomInfoBytes(ushort roomNumber, byte access, ushort numUsers)
        {
            byte[] data = new byte[5];
            using (MemoryStream memStream = new MemoryStream(data))
            using (BinaryWriter binWriter = new BinaryWriter(memStream))
            {
                binWriter.Write((UInt16) roomNumber);
                binWriter.Write((Byte)access);
                binWriter.Write((UInt16)numUsers);
            }
            return data;
        }

    }
}
