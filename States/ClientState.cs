using System;
using System.Collections.Generic;
using System.Text;

namespace FMaj.CapcomDirectServer.States
{
    abstract class ClientState
    {
        protected Server server;
        protected Client client;

        public ClientState(Server server, Client client)
        {
            this.server = server;
            this.client = client;
        }

        public abstract void OnEnterState();
        public abstract void OnExitState();
        public abstract void DoPacket(ushort opcode, byte[] data);
    }
}
