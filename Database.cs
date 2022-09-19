using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace FMaj.CapcomDirectServer
{
    class Database
    {
        public enum BattleResult { WIN, LOSE, DRAW }

        const string HOST = "127.0.0.1";
        const string PORT = "3306";
        const string DB_NAME = "capcomkddi";
        const string USERNAME = "root";
        const string PASSWORD = "";

        public static UserProfile GetCapcomAccount(string capcomID, ref CapcomInfo capcom)
        {
            MySqlCommand cmd;

            using (MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM accounts WHERE id = @capcomID";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@capcomID", capcomID);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        capcom.Handle = Reader.GetString("handle");
                        capcom.TelephoneNumber = Reader.GetString("telephoneNumber");
                        capcom.Email = Reader.GetString("email");

                        string name = Reader.GetString("name");
                        string address = Reader.GetString("address");
                        ushort age = Reader.GetUInt16("age");
                        string profession = Reader.GetString("profession");
                        bool directMail = Reader.GetBoolean("isDirectMail");

                        return new UserProfile(directMail, name, address, age, profession);
                    }
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return null;
        }

        public static List<Room> LoadRooms()
        {
            MySqlCommand cmd;
            List<Room> rooms = new List<Room>();

            using (MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM rooms";
                    cmd = new MySqlCommand(query, conn);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        byte gameCode = Reader.GetByte("gamecode");
                        byte genreCode = Reader.GetByte("genrecode");
                        ushort number = Reader.GetUInt16("number");
                        string name = Reader.GetString("name");
                        ushort maxUsers = Reader.GetUInt16("maxUsers");
                        rooms.Add(new Room(gameCode, genreCode, number, name, maxUsers));
                    }
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return rooms;
        }

        public static string CreateCapcomAccount(string individualID, CapcomInfo info, UserProfile profile)
        {
            string query;
            MySqlCommand cmd;

            // Generate a CAPCOM ID            
            string id = Capcom.GenerateNewID();

            using MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();

                query = @"
                    INSERT INTO accounts (capcomId, individualId, handle, email, telephone, directMail, name, address, age, profession) 
                    VALUES (@capcomId, @individualId, @handle, @email, @telephone, @directMail, @name, @address, @age, @profession)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@capcomId", id);
                cmd.Parameters.AddWithValue("@individualId", individualID);
                cmd.Parameters.AddWithValue("@handle", info.Handle);
                cmd.Parameters.AddWithValue("@email", info.Email);
                cmd.Parameters.AddWithValue("@telephone", info.TelephoneNumber);

                cmd.Parameters.AddWithValue("@directMail", profile.DirectMail);
                cmd.Parameters.AddWithValue("@name", profile.RealName);
                cmd.Parameters.AddWithValue("@address", profile.Address);
                cmd.Parameters.AddWithValue("@age", profile.Age);
                cmd.Parameters.AddWithValue("@profession", profile.Profession);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }

            return id;
        }

        public static void UpdateCapcomAccount(CapcomInfo info, UserProfile profile)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();

                query = @"
                    UPDATE accounts 
                    SET 
                    email=@email, 
                    telephone=@telephone, 
                    directMail=@directMail, 
                    name=@name, 
                    address=@address, 
                    age=@age, 
                    profession=@profession
                    WHERE capcomId = @capcomID
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@capcomID", info.Id);

                cmd.Parameters.AddWithValue("@email", info.Email);
                cmd.Parameters.AddWithValue("@telephone", info.TelephoneNumber);

                cmd.Parameters.AddWithValue("@directMail", profile.DirectMail);
                cmd.Parameters.AddWithValue("@name", profile.RealName);
                cmd.Parameters.AddWithValue("@address", profile.Address);
                cmd.Parameters.AddWithValue("@age", profile.Age);
                cmd.Parameters.AddWithValue("@profession", profile.Profession);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }
        }

        public static GameData CreateGameData(string capcomID, byte gameCode)
        {
            string query;
            MySqlCommand cmd;
            GameData data = null;

            using MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();

                query = @"
                    INSERT INTO gamedata (capcomId, gamecode) 
                    VALUES (@capcomId, @gameCode)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@capcomId", capcomID);
                cmd.Parameters.AddWithValue("@gameCode", gameCode);
                cmd.ExecuteNonQuery();

                data = new GameData(0, 0, 0, 0, 0, 0, 0);
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }

            return data;
        }

        public static GameData GetGameData(string capcomID, byte gameCode)
        {
            MySqlCommand cmd;
            GameData gameData = null;

            using (MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM gamedata WHERE capcomId = @capcomID AND gamecode = @gameCode";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@capcomID", capcomID);
                    cmd.Parameters.AddWithValue("@gameCode", gameCode);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        uint spentMoney = Reader.GetUInt32("moneyUsed");
                        ushort wins = Reader.GetUInt16("wins");
                        ushort losses = Reader.GetUInt16("losses");
                        ushort draws = Reader.GetUInt16("draws");
                        byte rank = Reader.GetByte("rank");
                        ushort ranking = Reader.GetUInt16("ranking");
                        uint playtime = Reader.GetUInt32("playtime");

                        gameData = new GameData(rank, ranking, wins, losses, draws, spentMoney, playtime);
                    }
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return gameData;
        }

        public static void UpdateBattleResult(string capcomID, byte gameCode, BattleResult result, int matchTime)
        {
            MySqlCommand cmd;
            string resultColumn = null;

            switch (result)
            {
                case BattleResult.WIN:
                    resultColumn = "wins";
                    break;
                case BattleResult.LOSE:
                    resultColumn = "losses";
                    break;
                case BattleResult.DRAW:
                    resultColumn = "draws";
                    break;
            }

            using (MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = @$"
                    UPDATE accounts 
                    SET 
                    {resultColumn} = {resultColumn} + 1,
                    playtime = playtime + @matchtime,
                    WHERE id = @capcomID AND gameCode = @gameCode
                    ";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@capcomID", capcomID);
                    cmd.Parameters.AddWithValue("@gameCode", gameCode);
                    cmd.Parameters.AddWithValue("@matchtime", matchTime);
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }
        }

        public static bool AddBattleCode(Client player1, Client player2, string code)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();

                query = @"
                    INSERT INTO battles (battlecode, player1Id, player2Id) 
                    VALUES (@battlecode, @player1, @player2)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@battlecode", code);
                cmd.Parameters.AddWithValue("@player1", player1.capcom.Id);
                cmd.Parameters.AddWithValue("@player2", player2.capcom.Id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }

            return false;
        }

        public static Tuple<string, string> GetBattle(string code)
        {
            MySqlCommand cmd;

            using (MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM battles WHERE battlecode = @battlecode";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@battlecode", code);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        string p1Id = Reader.GetString("player1Id");
                        string p2Id = Reader.GetString("player2Id");
                        return Tuple.Create(p1Id, p2Id);
                    }
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return null;
        }

        public static void storeUserIP(string capcomId, Client client)
        {
            MySqlCommand cmd;

            using (MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = @"
                                INSERT INTO dialplanservice (capcomId, phonenumber, currentIP) 
                                VALUES (@capcomIDs, (SELECT telephone FROM accounts WHERE capcomId=@capcomIDs), @currentIPs)
                                ON DUPLICATE KEY UPDATE currentIP=@currentIPs;";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@capcomIDs", capcomId);
                    cmd.Parameters.AddWithValue("@currentIPs", client.GetAddress().Split(':')[0]);
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }
        }

        public static UInt16 getTotalUsersForRanking(byte gameCode)
        {
            MySqlCommand cmd;

            using (MySqlConnection conn = new MySqlConnection(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = @"SELECT COUNT(*) FROM gamedata WHERE gamecode=@gameCode;";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@gameCode", gameCode);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        string count = Reader.GetString(0);
                        return Convert.ToUInt16(count);
                    }
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return 0;
        }
    }
}
