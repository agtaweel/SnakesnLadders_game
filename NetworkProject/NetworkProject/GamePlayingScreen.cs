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
    public partial class GamePlayingScreen : Form
    {
        public static GamePlayingScreen activeForm;
        private static readonly int groupPort = 8000;
        UdpClient serverUdpClient;
        char[,] gameBoard;
        Dictionary<Point, int> Snakes;
        Dictionary<Point, int> Ladders;
        private static List<Client> Clients;
        public static List<Point> PlayersLocation;
        public static int myIndex;
        Socket currentPlayer;
        bool isServer;
        Bitmap Board;
        int numberOfPlayers;
        int currentPlayerIndex = 0;
        public GamePlayingScreen(char[,] board, Dictionary<Point, int> snakes, Dictionary<Point, int> ladders, List<Client> clients, int numberOfPlayers, Socket me, bool Server, UdpClient udp)
        {
            activeForm = this;
            InitializeComponent();
            Clients = clients;
            gameBoard = board;
            Snakes = snakes;
            Ladders = ladders;
            currentPlayer = me;
            this.numberOfPlayers = numberOfPlayers;

            PlayersLocation = new List<Point>();
            for (int i = 0; i < numberOfPlayers; i++)
            {
                PlayersLocation.Add(new Point(0, 0));
            }

            GeneratePlayerList(numberOfPlayers);
            isServer = Server;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            DrawBoard();

            if (isServer)
            {
                this.serverUdpClient = udp;
                btnRollTheDice.Enabled = true;
                myIndex = 0;
                for (int i = 1; i < clients.Count; i++)
                {
                    Thread t = new Thread(new ParameterizedThreadStart(RecieveFromClients));
                    t.Start(clients[i]);
                }
                for (int i = 1; i < clients.Count; i++)
                {
                    clients[i].playerSocket.Send(Encoding.ASCII.GetBytes(clients[i].Rank.ToString()));
                }
            }
            else
            {
                Console.WriteLine("Sending join game notification to server");
                currentPlayer.Send(Encoding.ASCII.GetBytes("Client with IP " + GameStart.GetLocalIPAddress() + " joined the game"));
                Console.WriteLine("Sent join game notification to server");
                byte[] myIndexBytes = new byte[20];
                currentPlayer.Receive(myIndexBytes);
                Console.WriteLine("My index is " + Encoding.ASCII.GetString(myIndexBytes));
                myIndex = Int32.Parse(Encoding.ASCII.GetString(myIndexBytes));
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////DRAWING FUNCTIONS/////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////YOU DON'T NEED TO WRITE ANY CODE HERE///////////////////////////////////////////////////////////////////////////////////
        void GeneratePlayerList(int numberOfPlayers)
        {
            //maximum number of players is 8
            numberOfPlayers = numberOfPlayers > 8 ? 8 : numberOfPlayers;

            for (int i = 0; i < numberOfPlayers; i++)
            {
                Label label = new Label();
                label.AutoSize = true;
                label.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label.Location = new System.Drawing.Point(85, 65 + i * 50);
                label.Name = "label2";
                label.Size = new System.Drawing.Size(76, 19);
                label.TabIndex = 0;
                label.Text = "Player " + (i + 1);

                this.groupBox1.Controls.Add(label);

                PictureBox pictureBox = new PictureBox();
                pictureBox.Location = new System.Drawing.Point(30, 55 + i * 50);
                pictureBox.Name = "pictureBox2";
                pictureBox.Size = new System.Drawing.Size(48, 40);
                pictureBox.TabIndex = 0;
                pictureBox.TabStop = false;
                GeneratePlayerColor(i + 1);
                Image bmp = new Bitmap(pictureBox.Width, pictureBox.Height);
                Graphics g = Graphics.FromImage(bmp);
                g.FillEllipse(new SolidBrush(PlayerColors[i]), 0, 0, 48, 40);
                g.Flush();
                pictureBox.BackgroundImage = bmp;
                this.groupBox1.Controls.Add(pictureBox);

            }
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
        }
        List<Color> PlayerColors = new List<Color>();
        void GeneratePlayerColor(int index)
        {
            PlayerColors.Add(Color.FromArgb(index * 200 % 255, index * 300 % 255, index * 400 % 255));
        }
        void DrawBoard()
        {
            Bitmap bmp = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            Graphics g = Graphics.FromImage(bmp);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (i % 2 == 0)
                    {
                        if (j % 2 == 0)
                            g.FillRectangle(Brushes.White, new Rectangle(j * 50, i * 50, 50, 50));
                        else
                            g.FillRectangle(Brushes.Gray, new Rectangle(j * 50, i * 50, 50, 50));
                    }
                    else
                    {
                        if (j % 2 == 0)
                            g.FillRectangle(Brushes.Gray, new Rectangle(j * 50, i * 50, 50, 50));
                        else
                            g.FillRectangle(Brushes.White, new Rectangle(j * 50, i * 50, 50, 50));
                    }
                }
            }

            for (int i = 0; i < 11; i++)
            {
                g.DrawLine(Pens.Black, new Point(0, i * 50), new Point(500, i * 50));
                g.DrawLine(Pens.Black, new Point(i * 50, 0), new Point(i * 50, 500));
            }

            g.FillRectangle(Brushes.LightPink, new Rectangle(0, 0, 50, 50));
            g.FillRectangle(Brushes.LightPink, new Rectangle(0, 450, 50, 50));

            Bitmap snakeImg = new Bitmap("snake.png");
            foreach (var snake in Snakes)
            {
                g.DrawImage(snakeImg, snake.Key.X * 50, (9 - snake.Key.Y) * 50, 50, (snake.Value + 1) * 50);
            }
            Bitmap ladderImg = new Bitmap("ladder.png");
            foreach (var ladder in Ladders)
            {
                g.DrawImage(ladderImg, ladder.Key.X * 50, (9 - ladder.Key.Y - ladder.Value) * 50 + 25, 50, ladder.Value * 50 + 10);
            }

            g.DrawString("START", SystemFonts.DefaultFont, Brushes.Red, new PointF(5, 470));
            g.DrawString("END", SystemFonts.DefaultFont, Brushes.Red, new PointF(10, 20));
            Board = bmp;
            pictureBox1.BackgroundImage = bmp;
        }
        private void GamePlayingScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        void DrawAllPlayers()
        {
            Bitmap bmp = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            Graphics g = Graphics.FromImage(bmp);
            for (int i = 0; i < PlayersLocation.Count; i++)
            {
                g.FillEllipse(new SolidBrush(PlayerColors[i]), new Rectangle(PlayersLocation[i].X * 50, (9 - PlayersLocation[i].Y) * 50, 50 - i, 50 - i));
            }
            pictureBox1.Image = bmp;
        }

        private void GamePlayingScreen_Paint(object sender, PaintEventArgs e)
        {
            DrawAllPlayers();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////YOUR CODE HERE///////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void btnRollTheDice_Click(object sender, EventArgs e)
        {
            btnRollTheDice.Enabled = false;
            Random ran = new Random();
            int move = ran.Next(1, 7);
            textBox1.Text = move.ToString();
            Point oldLocation = PlayersLocation[myIndex];
            Point newLocation = new Point(oldLocation.X, oldLocation.Y);

            if (newLocation.X - move < 0 && newLocation.Y >= 9)
            {
                Console.WriteLine("No move");
            }
            else
            {
                while (move != 0)
                {
                    if (newLocation.Y % 2 == 0)
                    {
                        if (newLocation.X < 9)
                        {
                            newLocation.X++;
                            move--;
                        }
                        else
                        {
                            newLocation.Y++;
                            move--;
                        }
                    }
                    else if (newLocation.Y % 2 == 1)
                    {
                        if (newLocation.X > 0)
                        {
                            newLocation.X--;
                            move--;
                        }
                        else
                        {
                            newLocation.Y++;
                            move--;
                        }
                    }
                }

                if (gameBoard[newLocation.Y, newLocation.X] == 'L')
                {
                    newLocation.Y += Ladders[newLocation];
                }
                else if (gameBoard[newLocation.Y, newLocation.X] == 'S')
                {
                    newLocation.Y -= Snakes[newLocation];
                }

                PlayersLocation[myIndex] = newLocation;
                //Thread.Sleep(3000);
                DrawAllPlayers();


                if (newLocation.X == 0 && newLocation.Y >= 9)
                {
                    newLocation.X = 0;
                    newLocation.Y = 9;
                    Console.WriteLine("Player # " + myIndex + "Is the winner\nNumber : " + myIndex);
                    if (isServer)
                        BroadCastTheWinnerIs(myIndex);
                    else
                        SendTheWinnerIsMeToServer();
                    return;
                }
            }
            if (currentPlayerIndex < numberOfPlayers - 1)
                currentPlayerIndex++;
            else currentPlayerIndex = 0;

            if (isServer)
            {
                //call BroadCastLocation(0) as the server index is always 0 in the client list
                BroadCastLocation(0);

                //call BroadCastWhoseTurn(0) to see which player will play after server
                //BroadCastWhoseTurn(0);
                BroadCastWhoseTurn(currentPlayerIndex);
            }
            else
            {
                //if final location is the winning location then call the function SendTheWinnerIsMeToServer()

                //else send the final location to server by calling SendLocationToServer()
                SendLocationToServer();
            }
        }



        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////CLIENT///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void SendLocationToServer()
        {
            //use the currentPlayer socket to send to server "PlayersLocation[myIndex]"
            //message should look like this:
            //IP#PlayersLocation[myIndex]#
            currentPlayer.Send(Encoding.ASCII.GetBytes(GameStart.GetLocalIPAddress() + "#" + PlayersLocation[myIndex].X + "," + PlayersLocation[myIndex].Y + "#"));
            Console.WriteLine("Sent new location to server");
        }
        void SendTheWinnerIsMeToServer()
        {
            //use the currentPlayer socket to send to server the winner message
            //message should look like this:
            //IP#
            currentPlayer.Send(Encoding.ASCII.GetBytes(GameStart.GetLocalIPAddress() + "#"));
            Console.WriteLine("Sent the winner is me to server");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////SERVER///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecieveFromClients(Object client)
        {
            try
            {
                Client c = (Client)client;
                //recieve message and parse it

                //if Winning Message
                //call BroadCastTheWinnerIs(playerNumber)
                //go to WinningForm

                //IP#PlayersLocation[myIndex]
                byte[] clientMsgBytes = new byte[100];
                Console.WriteLine("Waiting to receive from client #" + c.Rank);
                c.playerSocket.Receive(clientMsgBytes);
                String playerMsg = Encoding.ASCII.GetString(clientMsgBytes);
                if (playerMsg.StartsWith("Client with IP "))
                {
                    Console.WriteLine("Total number of players " + Clients.Count + "\t" + numberOfPlayers);
                    Console.WriteLine("Received from client #" + c.Rank + "\n" + playerMsg);
                }
                else
                {

                    String[] parts = playerMsg.Split('#');
                    if (parts.Length == 2)
                    {
                        //winner message
                        int winnerIndex = -1;
                        String winnerIP = parts[0];
                        foreach (Client nClient in Clients)
                        {
                            if (nClient.IP == winnerIP)
                            {
                                winnerIndex = nClient.Rank;
                            }
                        }
                        if (winnerIndex != -1)
                        {
                            Console.WriteLine("Winner number is " + winnerIndex);
                            BroadCastTheWinnerIs(winnerIndex);
                        }
                        else
                        {
                            Console.WriteLine("Wrong winner IP");
                        }
                    }
                    //if LocationMessage
                    //call BraodCastLocation(player number)
                    //call BroadCastWhoseTurn(player number)
                    else if (parts.Length == 3)
                    {
                        //player location message
                        int playerIndex = -1;
                        String playerIP = parts[0];
                        foreach (Client nClient in Clients)
                        {
                            if (nClient.IP == playerIP)
                            {
                                playerIndex = nClient.Rank;
                            }
                        }
                        if (playerIndex != -1)
                        {
                            String[] location = parts[1].Split(',');
                            PlayersLocation[playerIndex] = new Point(Int32.Parse(location[0]), Int32.Parse(location[1]));
                            BroadCastLocation(playerIndex);
                        }
                        int nextPlayer;
                        if (playerIndex == (numberOfPlayers - 1))
                            nextPlayer = 0;
                        else
                            nextPlayer = playerIndex + 1;
                        BroadCastWhoseTurn(nextPlayer);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
            }
            finally
            {
                RecieveFromClients(client);
            }
        }

        void BroadCastLocation(int playerNumber)
        {
            // here send the mssage to all clients, containing the location of PlayersLocation[playerNumber] and attach its IP and playerNumber
            Console.WriteLine("Broadcasting location");
            String playerIP = "";
            for(int i = 0; i < numberOfPlayers; i++)
            {
                Client c = Clients[i];
                if(c.Rank == playerNumber)
                {
                    playerIP = c.IP;
                }
            }
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            String nextPlayerIndex = "Location#" + playerNumber + "#" + playerIP + "#" + PlayersLocation[playerNumber].X + "," + PlayersLocation[playerNumber].Y;
            byte[] nextPlayerIndexBytes = Encoding.ASCII.GetBytes(nextPlayerIndex);
            serverUdpClient.Send(nextPlayerIndexBytes, nextPlayerIndexBytes.Length, endPoint);
            Console.WriteLine("Broadcast next player done");
        }

        void BroadCastWhoseTurn(int playerNumber)
        {
            Console.WriteLine("Current player is is " + playerNumber);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            String nextPlayerIndex = "NextPlayer#" + playerNumber.ToString();
            byte[] nextPlayerIndexBytes = Encoding.ASCII.GetBytes(nextPlayerIndex);
            serverUdpClient.Send(nextPlayerIndexBytes, nextPlayerIndexBytes.Length, endPoint);
            Console.WriteLine("Broadcast next player done");
        }
        void BroadCastTheWinnerIs(int playerNumber)
        {
            Console.WriteLine("The winner is " + playerNumber);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            String winnerMessage = "Winner#" + Clients[playerNumber].IP + "#" + Clients[playerNumber].Rank;
            byte[] winnerMessageBytes = Encoding.ASCII.GetBytes(winnerMessage);
            serverUdpClient.Send(winnerMessageBytes, winnerMessageBytes.Length, endPoint);
            Console.WriteLine("Broadcast winner done");
        }

        public void enableRoll()
        {
            btnRollTheDice.Enabled = true;
        }
    }
}
