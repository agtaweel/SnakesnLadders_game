using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace NetworkProject
{
    public partial class GameStart : Form
    {
        private static readonly int groupPort = 8000;
        public readonly UdpClient udp = new UdpClient(groupPort);
        private String serverIP;
        public GameStart()
        {
            InitializeComponent();
        }
        private void GameStart_Load(object sender, EventArgs e)
        {
            // Start listening (waiting for a server to broadcast its IP)
            StartListening();
        }
        private void btnStartAsServer_Click(object sender, EventArgs e)
        {
            // Clicked start as server
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            serverIP = GetLocalIPAddress();
            byte[] serverIPBytes = Encoding.ASCII.GetBytes(serverIP);
            // Broadcasting server IP
            udp.Send(serverIPBytes, serverIPBytes.Length, endPoint);
            Console.WriteLine("Broadcasted IP successfully\nServer IP: " + serverIP);
            // Showing game setting screen
            GameSettingScreen gsc = new GameSettingScreen(true, null, udp);
            gsc.Show();
            this.Visible = false;
        }

        private void btnJoinAsClient_Click(object sender, EventArgs e)
        {
            // Clicked join as client
            IPAddress ip = IPAddress.Parse(serverIP);
            // Show game setting screen
            GameSettingScreen gsc = new GameSettingScreen(false, ip, null);
            gsc.Show();
            this.Visible = false;
        }
        private void Receive(IAsyncResult ar)
        {
            try
            {
                IPAddress add;
                // Receive broadcasts from any IP address using port 8000
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, groupPort);
                byte[] bytes = udp.EndReceive(ar, ref clientEndPoint);
                string message = Encoding.ASCII.GetString(bytes);

                // Check the message format
                if (message.StartsWith("Location") && message != "" && message != null)
                    handlePlayerLocationMessage(message);

                else if (message.StartsWith("NextPlayer") && message != "" && message != null)
                    handleNextPlayerMessage(message);

                else if (message.StartsWith("Winner") && message != "" && message != null)
                    handleWinnerMessage(message);

                else if (message != "" && message != null && message == "start")
                    handleStartGameMessage();

                else if (message != "" && message != null && message.Contains(';'))
                    handlePlayerListMessage(message);

                else if (message != "" && message != null && IPAddress.TryParse(message, out add))
                    handleServerIPMessage(message);
                else
                    Console.WriteLine("Unknown message broadcasted by server\nMessage: " + message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
            }
            finally
            {
                StartListening();
            }
        }
        private void StartListening()
        {
            this.udp.BeginReceive(Receive, new object());
        }
        public static string GetLocalIPAddress()
        {
            // Function to get local IP address of the computer (used by both client and server)
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        private void handlePlayerLocationMessage(string message)
        {
            Console.Write("Received player location ");
            String[] messageParts = message.Split('#');
            String playerIP = messageParts[2];
            Console.WriteLine(playerIP);
            String[] locationStr = messageParts[3].Split(',');
            Point newLocation = new Point(Int32.Parse(locationStr[0]), Int32.Parse(locationStr[1]));
            Console.WriteLine("New location: (" + newLocation.X + ", " + newLocation.Y + ")");
            int playerIndex = Int32.Parse(messageParts[1]);
            GamePlayingScreen.PlayersLocation[playerIndex] = newLocation;
            Console.WriteLine("index: " + playerIndex);
        }

        private void handleNextPlayerMessage(String message)
        {
            Console.WriteLine("Received next player index");
            String[] messageParts = message.Split('#');
            int playerIndex = Int32.Parse(messageParts[1]);
            Console.WriteLine("Next player index: " + playerIndex + "\nMy index is " + GamePlayingScreen.myIndex);
            if (playerIndex == GamePlayingScreen.myIndex)
            {
                Console.WriteLine("Next player is me");
                this.Invoke((MethodInvoker)delegate
                {
                    GamePlayingScreen.activeForm.enableRoll();
                });
            }
        }

        private void handleWinnerMessage(String message)
        {
            Console.WriteLine("Received winner message");
            String[] messageParts = message.Split('#');
            String winnerIP = messageParts[1];
            int winnerRank = Int32.Parse(messageParts[2]);
            Console.WriteLine("Parsed winner message\n" + winnerRank + "\t" + winnerIP);
            this.Invoke((MethodInvoker)delegate
            {
                WinningForm form = new WinningForm(winnerRank, winnerIP);
                form.Show();
            });
        }

        private void handlePlayerListMessage(String message)
        {
            Console.WriteLine("Received player list");
            if (GameSettingScreen.activeForm != null)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    GameSettingScreen.activeForm.updatePlayerList(message.Split(';'));
                });
            }
        }

        private void handleStartGameMessage()
        {
            Console.WriteLine("Received start game signal");
            GameSettingScreen.startGame = true;
        }

        private void handleServerIPMessage(String message)
        {
            Console.WriteLine("Server IP Received: " + message);
            serverIP = message;
            this.Invoke((MethodInvoker)delegate
            {
                btnJoinAsClient.Enabled = true;
                btnStartAsServer.Enabled = false;
            });
        }
    }
}
