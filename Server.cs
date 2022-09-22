using FMaj.CapcomDirectServer.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FMaj.CapcomDirectServer
{
    class Server
    {
        // Defs
        public const int BUFFER_SIZE = 0xFFFF;
        public const int BACKLOG = 100;
        public const int PING_INTERVAL = 30;

        // Connection Handling
        private Socket serverSocket;
        private List<Client> connList = new List<Client>();
        private Thread pingThread;
        private bool pingThreadAlive = true;

        // Server Data
        private string segaMessage = "Capcom KDDI Server Emulator";
        private string userMessage = "This is an initial release of the server!";
        private List<Room> chatRooms;
        private MatchMaker matchMaker;

        private List<IPAddress> bannedIPs = new List<IPAddress>();

        public Server()
        {
            chatRooms = Database.LoadRooms();
            Program.Log.Info($"Loaded {chatRooms.Count} chatrooms from the database.");
            Program.Log.Info($"Sega Message: {segaMessage}");

            matchMaker = new MatchMaker(this);

            Client fakeClient = new Client(this, null);
            fakeClient.capcom.Id = "123456";
            fakeClient.capcom.Email = "testuser@gmail.com";
            fakeClient.capcom.TelephoneNumber = "8598675309";
            fakeClient.capcom.Handle = "TestUser";
            fakeClient.gameCode = 6;
            fakeClient.currentGenre = 0x00;
            fakeClient.gameData = new GameData(3, 1, 8, 3, 1, 1000, 10);
            //connList.Add(fakeClient);
            //fakeClient.JoinRoom(2);
            //fakeClient.SetState(new MatchMakingState(this, fakeClient, Capcom.MatchMakingScope.Chatroom, GetRoom(6, 2)));
            //fakeClient.SetState(new MatchMakingState(this, fakeClient, Capcom.MatchMakingScope.Any));
        }

        public void Shutdown()
        {
            pingThreadAlive = false;
            lock (serverSocket)
            {
                foreach (Client client in connList)
                {
                    client.Disconnect();
                }
            }
            serverSocket.Close();
            connList.Clear();
        }

        private void StartPingProc()
        {
            pingThread = new Thread(new ThreadStart(PingProc));
            pingThread.Start();
        }

        private void PingProc()
        {
            Thread.Sleep(PING_INTERVAL * 1000);
            while (pingThreadAlive)
            {
                Program.Log.Trace("Sending ping");
                //Program.Log.Debug("There are {0} connections in the list.", connList.Count);
                lock (connList)
                {
                    List<Client> toRemove = new List<Client>();
                    foreach (Client conn in connList)
                    {
                        try
                        {
                            conn.SendMessage(Capcom.ServerOpcodes.KeepAlivePing);
                        }
                        catch (SocketException)
                        {
                            toRemove.Add(conn);
                        }
                    }
                    connList = connList.Except(toRemove).ToList();
                }
                Thread.Sleep(PING_INTERVAL * 1000);

                matchMaker.Update();
            }
        }

        #region Socket Handling
        public bool StartServer(int port)
        {
            IPEndPoint serverEndPoint = new System.Net.IPEndPoint(IPAddress.Parse("0.0.0.0"), port);

            try
            {
                serverSocket = new System.Net.Sockets.Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Could not Create socket, check to make sure not duplicating port", e);
            }
            try
            {
                serverSocket.Bind(serverEndPoint);
                serverSocket.Listen(BACKLOG);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error occured while binding socket, check inner exception", e);
            }
            try
            {
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error occured starting listeners, check inner exception", e);
            }

            Console.ForegroundColor = ConsoleColor.White;
            Program.Log.Info("Server has started @ {0}:{1}", (serverSocket.LocalEndPoint as IPEndPoint).Address, (serverSocket.LocalEndPoint as IPEndPoint).Port);
            Console.ForegroundColor = ConsoleColor.Gray;

            StartPingProc();

            return true;
        }

        public void SendAll(byte[] bytes)
        {
            foreach(Client conn in connList)
            {
                conn.SendBytes(bytes);
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            Client conn = null;
            
            try
            {
                System.Net.Sockets.Socket s = (System.Net.Sockets.Socket)result.AsyncState;
                conn = new Client(this, s.EndAccept(result));

                Program.Log.Debug("AcceptCallback coming from {0}:{1}", (conn.socket.RemoteEndPoint as IPEndPoint).Address, (conn.socket.RemoteEndPoint as IPEndPoint).Port);

                if(bannedIPs.Contains((conn.socket.RemoteEndPoint as IPEndPoint).Address))
                {
                    conn.Disconnect(false);
                    conn = null;
                    return;
                }

                lock (connList)
                {
                    connList.Add(conn);
                }
                //Queue recieving of data from the connection
                conn.socket.BeginReceive(conn.buffer, 0, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
                //Queue the accept of the next incomming connection
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
                Program.Log.Info("{0} has connected.", conn);

                conn.SetState(new LoginInitialState(this, conn));
            }
            catch (SocketException)
            {
                if (conn.socket != null)
                {
                    conn.socket.Close();
                    lock (connList)
                    {
                        connList.Remove(conn);
                    }
                }
                //serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
            }
            catch (ObjectDisposedException)
            {
                serverSocket = null;
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            Client conn = (Client)result.AsyncState;

            try
            {
                int bytesRead = conn.socket.EndReceive(result);
                conn.bufferSize += bytesRead;

                int scanner = 0;
                
                while (conn.bufferSize - scanner >= 3)
                {
                    // Read header
                    byte opcodeHI = conn.buffer[scanner];
                    byte size = (byte) (conn.buffer[scanner + 1] - 3);
                    byte opcodeLO = conn.buffer[scanner + 2];
                    scanner += 3;

                    // Read data
                    if (conn.bufferSize - scanner < size)
                        break;

                    byte[] data = new byte[size];
                    Array.Copy(conn.buffer, scanner, data, 0, size);
                    scanner += size;

                    ushort opcode = (ushort) (opcodeHI << 8 | opcodeLO);

                    Program.Log.Debug(String.Format("Receiving ({0:X}, {1:X}):\n", opcode, size) + Server.ByteArrayToHex(data));

                    conn.ProcessPacket(opcode, data);
                }

                // Move anything unparsed to the beginning
                conn.bufferSize -= scanner;
                if (conn.bufferSize != 0)
                    Array.Copy(conn.buffer, scanner, conn.buffer, 0, conn.bufferSize);

                conn.socket.BeginReceive(conn.buffer, 0, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);                
            }
            catch (SocketException)
            {
                if (conn.socket != null)
                {
                    Program.Log.Info("{0} has disconnected.", conn.GetAddress());

                    lock (connList)
                    {
                        connList.Remove(conn);
                    }
                }
            }
            catch(ObjectDisposedException)
            {
                conn.endLoginTimeout();
                Program.Log.Info("{0} has disconnected.", conn.capcom.Id);
            }
        }

        #endregion

        public static string ByteArrayToHex(byte[] bytes, int offset = 0, int bytesPerLine = 16)
        {
            if (bytes == null)
            {
                return string.Empty;
            }

            var hexChars = "0123456789ABCDEF".ToCharArray();

            var offsetBlock = 8 + 3;
            var byteBlock = offsetBlock + bytesPerLine * 3 + (bytesPerLine - 1) / 8 + 2;
            var lineLength = byteBlock + bytesPerLine + Environment.NewLine.Length;

            var line = (new string(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            var numLines = (bytes.Length + bytesPerLine - 1) / bytesPerLine;

            var sb = new StringBuilder(numLines * lineLength);

            for (var i = 0; i < bytes.Length; i += bytesPerLine)
            {
                var h = i + offset;

                line[0] = hexChars[(h >> 28) & 0xF];
                line[1] = hexChars[(h >> 24) & 0xF];
                line[2] = hexChars[(h >> 20) & 0xF];
                line[3] = hexChars[(h >> 16) & 0xF];
                line[4] = hexChars[(h >> 12) & 0xF];
                line[5] = hexChars[(h >> 8) & 0xF];
                line[6] = hexChars[(h >> 4) & 0xF];
                line[7] = hexChars[(h >> 0) & 0xF];

                var hexColumn = offsetBlock;
                var charColumn = byteBlock;

                for (var j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0)
                    {
                        hexColumn++;
                    }

                    if (i + j >= bytes.Length)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        var by = bytes[i + j];
                        line[hexColumn] = hexChars[(by >> 4) & 0xF];
                        line[hexColumn + 1] = hexChars[by & 0xF];
                        line[charColumn] = by < 32 ? '.' : (char)by;
                    }

                    hexColumn += 3;
                    charColumn++;
                }

                sb.Append(line);
            }

            return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public string GetSegaMessage()
        {
            return segaMessage;
        }

        public ushort GetRoomCount(byte gameCode, byte genreCode)
        {
            return (ushort) chatRooms.Where(room => room.GameCode == gameCode && room.GenreCode == genreCode).Count();
        }

        public Room GetRoom(byte gameCode, byte genreCode, ushort roomNum)
        {
            return chatRooms.FirstOrDefault(room => room.GameCode == gameCode && room.GenreCode == genreCode && room.RoomNumber == roomNum);
        }

        public Client FindClientByID(string capcomID)
        {
            lock (connList)
            {
                return connList.FirstOrDefault(client => (client.capcom.Id ?? "").Equals(capcomID));
            }
        }

        public Battle NewBattle(Client p1, Client p2)
        {            
            byte[] codeBytes = new byte[7];
            byte[] timeBytes = BitConverter.GetBytes(DateTime.Now.Ticks);
            byte[] p1IdBytes = Encoding.ASCII.GetBytes(p1.capcom.Id);
            byte[] p2IdBytes = Encoding.ASCII.GetBytes(p2.capcom.Id);

            for (int i = 0; i < 6; i++)            
                timeBytes[i] ^= p1IdBytes[i] ^= p2IdBytes[i];
            
            Array.Copy(timeBytes, 0, codeBytes, 0, 6);
            codeBytes[6] = p1.gameCode;
            string code = BitConverter.ToString(codeBytes).Replace("-", "");

            Database.AddBattleCode(p1, p2, code);

            return new Battle(p1, p2, code);
        }

        public MatchMaker GetMatchMaker()
        {
            return matchMaker;
        }

        public void removeFromConnList(Client toRemove)
        {
            if(connList.Contains(toRemove))
            {
                lock (connList)
                {
                    connList.Remove(toRemove);
                }
            }
        }

        public void banIP(IPAddress toBan)
        {
            Program.Log.Debug("Banning {0}...", toBan.ToString());
            bannedIPs.Add(toBan);
            foreach(Client thisConn in connList)
            {
                if((thisConn.socket.RemoteEndPoint as IPEndPoint).Address == toBan) {
                    thisConn.Disconnect(false);
                }
            }
        }
    }
}
