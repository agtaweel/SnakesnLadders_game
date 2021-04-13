using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkProject
{
    public class Client
    {
        public Socket playerSocket;
        public int Rank = -1;
        public string IP;
    }
}
