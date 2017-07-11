using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkProject
{
    public partial class GameSettingScreen : Form
    {
        public static GameSettingScreen activeForm;
        public static GamePlayingScreen gpc;
        private static int groupPort = 8000;
        Socket currentSocket;
        List<Client> clients = new List<Client>();
        Dictionary<Point, int> snakes = new Dictionary<Point, int>();
        Dictionary<Point, int> ladders = new Dictionary<Point, int>();
        private readonly UdpClient udp = null;
        IPAddress serverIP;
        public static bool startGame = false;
        public static bool isServer;
        char[,] board;
        int numberOfPlayers = -1;

        //Form Constructor
        public GameSettingScreen(bool isServer, IPAddress IP, UdpClient udp)
        {
            activeForm = this;
            InitializeComponent();
            serverIP = IP;
            GameSettingScreen.isServer = isServer;
            if (!isServer)
            {
                //joined as client (initialize the TCP client socket, connect to Servers IP and wait for the server to start the game
                timerStartGame.Start();
                btnStartGame.Enabled = false;
                JoinServer(IP);
                Thread threadReceiveDataFromServer = new Thread(RecieveDataFromServer);
                threadReceiveDataFromServer.Start();
            }
            else
            {
                //joined as server (start the TCP server socket, start UDP server socket to broadcasting the servers IP and finally accept client sockets using the TCP socket
                this.udp = udp;
                InitializeServer();
                tmrBroadCastIP.Start();
                AcceptPlayers();
            }
        }




        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////CLIENT///////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Client Function implementation
        void JoinServer(IPAddress IP)
        {
            currentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEndPoint = new IPEndPoint(IP, groupPort);
            Console.WriteLine("Trying to connect to server " + IP.ToString());
            currentSocket.Connect(serverEndPoint);
            Console.WriteLine("Connected to server");
            //byte[] welcomeMessageBytes = new byte[50];
            //currentSocket.Receive(welcomeMessageBytes);
            //Console.WriteLine(Encoding.ASCII.GetString(welcomeMessageBytes));
        }

        void RecieveDataFromServer()
        {
            try
            {
                byte[] numPlayersBytes = new byte[20];
                Console.WriteLine("Waiting to receive number of players from server");
                currentSocket.Receive(numPlayersBytes);
                String receivedData = (Encoding.ASCII.GetString(numPlayersBytes));
                Console.WriteLine("Received From Server: " + receivedData);
                //this function in different thread as it will halt the application during recieving from server
                if (Int32.TryParse(receivedData, out numberOfPlayers))
                {
                    Console.WriteLine("Received number of players from server " + (numberOfPlayers + 1));
                    GenerateSnakesAndLadders();
                    board = GenerateBoard(snakes, ladders);
                    //gpc = new GamePlayingScreen(board, snakes, ladders, null, numberOfPlayers, currentSocket, false);
                    //if (startGame)
                    //{
                    //    Console.WriteLine("Game will start");
                    //    Thread.Sleep(1000);
                    //    this.Visible = false;
                    //    gpc.Show();
                    //    timerStartGame.Stop();
                    //    Console.WriteLine("Game started");
                    //    return;
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed to receive data from server\n" + ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
            }
        }

        public void updatePlayerList(String[] players)
        {
            if (!isServer)
            {
                try
                {
                    numberOfPlayers = players.Length;
                    listBox1.Items.Clear();
                    foreach (String s in players)
                    {
                        listBox1.Items.Add(s);
                    }
                    Console.WriteLine("Updated player list");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("failed to update player list\n" + ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
                }

            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////SERVER///////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Server Functions implementation
        void InitializeServer()
        {
            try
            {
                //write code to initialize currentSocket to be server socket
                currentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, groupPort);
                currentSocket.Bind(serverEndPoint);
                Console.WriteLine("Initialized server");
                //add the server to clientList
                //set the servers rank = 0;
                numberOfPlayers = 0;
                Client server = new Client();
                server.IP = GameStart.GetLocalIPAddress();
                server.Rank = numberOfPlayers;
                server.playerSocket = currentSocket;
                clients.Add(server);
                listBox1.Items.Add(server.Rank + ". " + server.IP);
                numberOfPlayers++;
                Console.WriteLine("Added the server to player list");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to initialize server\n" + ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
            }
        }

        void BroadCastIP()
        {
            //write code to broadcast your IP
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            String serverIP = GameStart.GetLocalIPAddress();
            byte[] serverIPBytes = Encoding.ASCII.GetBytes(serverIP);
            udp.Send(serverIPBytes, serverIPBytes.Length, endPoint);
            Console.WriteLine("Broadcasted IP\nServer IP: " + serverIP);
            broadcastPlayerList();
            //Hint: this function is called repeatedly using timer every 5seconds
        }
        void AcceptPlayers()
        {
            //write the code of server socket to accept incoming players
            //create an object from class Client and fill in its information
            //assign a rank for this created object (which is the client index in list) and add to list of clients
            //where Client is a class contains all information about client (mentioned in the project document)
            currentSocket.Listen(4);
            for (int i = 0; i < 4; i++)
            {
                Thread acceptClientsThread = new Thread(new ParameterizedThreadStart(acceptClients));
                Console.WriteLine("Starting thread for player #" + 1);
                acceptClientsThread.Start(i);
            }
        }


        private void acceptClients(Object o)
        {
            Client newPlayer = new Client();
            newPlayer.playerSocket = currentSocket.Accept();
            newPlayer.Rank = numberOfPlayers;
            newPlayer.IP = ((IPEndPoint)(newPlayer.playerSocket.RemoteEndPoint)).Address.ToString();
            clients.Add(newPlayer);
            this.Invoke((MethodInvoker)delegate
            {
                listBox1.Items.Add(newPlayer.Rank + ".  " + newPlayer.IP);
            });
            Console.WriteLine("Added new Player\nIP: " + newPlayer.IP + "\nRank: " + newPlayer.Rank);
            byte[] numPlayersBytes = new byte[20];
            numPlayersBytes = Encoding.ASCII.GetBytes(numberOfPlayers.ToString());
            newPlayer.playerSocket.Send(numPlayersBytes);
            numberOfPlayers++;
            //Console.WriteLine("Added new Player: " + newPlayer.IP);
            //newPlayer.playerSocket.Send(Encoding.ASCII.GetBytes("Welcome!\n"));
            //byte[] clientMessageBytes = new byte[20];
            //newPlayer.playerSocket.Receive(clientMessageBytes);
            //Console.WriteLine("Client said hello\n" + Encoding.ASCII.GetString(clientMessageBytes));
        }

        private void broadcastPlayerList()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            byte[] playerListBytes = encodePlayerList();
            udp.Send(playerListBytes, playerListBytes.Length, endPoint);
            Console.WriteLine("Broadcasted Player List");
        }

        private byte[] encodePlayerList()
        {
            try
            {
                String playerList = "";
                byte[] playerListBytes = new byte[1000];
                for (int i = 0; i < clients.Count; i++)
                {
                    Client c = clients[i];
                    if (i == clients.Count - 1)
                        playerList += c.Rank + ". " + c.IP;
                    else
                        playerList += c.Rank + ". " + c.IP + ";";
                }
                playerListBytes = Encoding.ASCII.GetBytes(playerList);
                return playerListBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
                return null;
            }
        }

        private void btnStartGame_Click(object sender, EventArgs e)
        {
            //foreach(Client c in clients)
            //{
            //    byte[] numPlayersBytes = new byte[20];
            //    numPlayersBytes = Encoding.ASCII.GetBytes(numberOfPlayers.ToString());
            //    c.playerSocket.Send(numPlayersBytes);
            //}
            tmrBroadCastIP.Stop();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            String serverIP = GameStart.GetLocalIPAddress();
            byte[] serverIPBytes = Encoding.ASCII.GetBytes("start");
            udp.Send(serverIPBytes, serverIPBytes.Length, endPoint);
            Console.WriteLine("Broadcasted start game");
            GenerateSnakesAndLadders();
            char[,] board = GenerateBoard(snakes, ladders);
            gpc = new GamePlayingScreen(board, snakes, ladders, clients, clients.Count, currentSocket, true, udp);
            this.Visible = false;
            gpc.Show();
        }
        private void tmrBroadCastIP_Tick(object sender, EventArgs e)
        {
            BroadCastIP();
            broadcastPlayerList();
        }
        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////COMMON FUNCTION USED BY BOTH///////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Common functions
        void GenerateSnakesAndLadders()
        {
            //Random r = new Random();
            //for (int i = 0; i < 10; i++)
            //{
            //    try
            //    {
            //        int length = r.Next(1, 4);
            //        int snakeY = r.Next(2, 9);
            //        Point startPoint = new Point(i, snakeY);
            //        if ((i == 0 && (snakeY + length) == 9))
            //        {
            //            i--;
            //            continue;
            //        }
            //        snakes.Add(startPoint, length);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
            //        i--;
            //    }
            //}
            //r = new Random();
            //for (int i = 0; i < 10; i++)
            //{
            //    try
            //    {
            //        int length = r.Next(1, 4);
            //        int ladderY = r.Next(0, 7);
            //        Point startPoint = new Point(i, ladderY);
            //        ladders.Add(startPoint, length);
            //        if (snakes.Keys.Contains(startPoint))
            //        {
            //            i--;
            //            throw new Exception("Overlap!");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
            //        i--;
            //    }
            //}

            //Console.WriteLine("Snakes: \n");
            //for (int i = 0; i < 10; i++)
            //{
            //    Point p = snakes.Keys.ElementAt(i);
            //    Console.WriteLine(i + ": " + snakes.Values.ElementAt(i) +" (" + p.X + ", " + p.Y + ")");
            //}
            //Console.WriteLine("Ladders: \n");
            //for (int i = 0; i < 10; i++)
            //{
            //    Point p = ladders.Keys.ElementAt(i);
            //    Console.WriteLine(i + ": " + ladders.Values.ElementAt(i) + " (" + p.X + ", " + p.Y + ")");
            //}

            snakes.Add(new Point(6, 1), 1);
            snakes.Add(new Point(2, 2), 2);
            snakes.Add(new Point(9, 4), 1);
            snakes.Add(new Point(1, 6), 3);
            snakes.Add(new Point(2, 6), 2);
            snakes.Add(new Point(4, 6), 1);
            snakes.Add(new Point(1, 8), 1);
            snakes.Add(new Point(2, 9), 2);
            snakes.Add(new Point(8, 9), 1);
            snakes.Add(new Point(0, 7), 1);
            ladders.Add(new Point(1, 0), 1);
            ladders.Add(new Point(3, 0), 2);
            ladders.Add(new Point(8, 1), 1);
            ladders.Add(new Point(6, 2), 1);
            ladders.Add(new Point(0, 2), 2);
            ladders.Add(new Point(6, 5), 1);
            ladders.Add(new Point(3, 6), 2);
            ladders.Add(new Point(8, 4), 3);
            ladders.Add(new Point(4, 8), 1);
            ladders.Add(new Point(0, 8), 1);
        }

        char[,] GenerateBoard(Dictionary<Point, int> snakes, Dictionary<Point, int> ladders)
        {
            char[,] board = new char[10, 10];
            foreach (var snake in snakes)
            {
                board[snake.Key.Y, snake.Key.X] = 'S';
            }
            foreach (var ladder in ladders)
            {
                board[ladder.Key.Y, ladder.Key.X] = 'L';
            }
            return board;
        }
        private void GameSettingScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }
        
        private void timerStartGame_Tick(object sender, EventArgs e)
        {
            try
            {
                if (startGame)
                {
                    gpc = new GamePlayingScreen(board, snakes, ladders, null, numberOfPlayers, currentSocket, false, null);
                    Console.WriteLine("Game will start");
                    Thread.Sleep(1000);
                    this.Visible = false;
                    gpc.Show();
                    timerStartGame.Stop();
                    Console.WriteLine("Game started");
                    return;
                }
            }
            catch(Exception ex)
            { 
                Console.WriteLine("Game not started\n" + ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
            }
        }
    }
}