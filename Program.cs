using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class MainClass
{
    public static string Server_IP = "192.168.10.103";
    public static int Server_Port = 10011;


    public static string IP = "192.168.10.103";
    static List<Socket> clientsockets = new List<Socket>();

    public static void Main(string[] args)
    {
        for (int i = 20000; i < 20001; i++)
        {
            Thread thread = new Thread(SendMsg);
            thread.IsBackground = true;
            thread.Start(i);

            Thread H = new Thread(SengHeat);
            H.IsBackground = true;
            H.Start(i + 200);

            Thread Rece = new Thread(Receive);
            Rece.IsBackground = true;
            Rece.Start();


            Thread Handle = new Thread(HandleEvent);
            Handle.IsBackground = true;
            Handle.Start();

        }
        Console.ReadLine();
    }

    private static void Receive()
    {
        while (true)
        {
            //用来保存发送发IP
            EndPoint point = new IPEndPoint(IPAddress.Any, 0);
            byte[] buff = new byte[1024 * 1024];

            //foreach (var item in clientsockets)
            //{
            //    int length = item.ReceiveFrom(buff, ref point);
            //    byte[] newbyte = new byte[length];
            //    Array.Copy(buff, 0, newbyte, 0, length);


            //    string msg = Encoding.UTF8.GetString(buff, 0, length);
            //    Console.WriteLine("接收到来自:{ " + point.ToString() + " }的消息：[" + msg + "]");

            //    ReceiveQueue.Enqueue(new ReceEvent(newbyte, point));
            //}

            if (clientsockets.Count > 0)
            {
                int length = clientsockets[0].ReceiveFrom(buff, ref point);
                byte[] newbyte = new byte[length];
                Array.Copy(buff, 0, newbyte, 0, length);


                string msg = Encoding.UTF8.GetString(buff, 0, length);
                Console.WriteLine("接收到来自:{ " + point.ToString() + " }的消息：[" + msg + "]");

                ReceiveQueue.Enqueue(new ReceEvent(newbyte, point));
            }

            Thread.Sleep(0);
        }
    }


    static Queue<ReceEvent> ReceiveQueue = new Queue<ReceEvent>();


    private static void HandleEvent()
    {
        while (true)
        {
            if (ReceiveQueue.Count >= 1)
            {
                EndPoint endPoint;
                MsgBase msgBase = Judge(out endPoint);
                switch (msgBase)
                {
                    case MsgHeat:

                        break;
                    case MsgMove:
                        Console.Write("位置移动X:" + ((MsgMove)msgBase).x + "!!!");
                        break;
                    default:
                        break;
                }

            }
            Thread.Sleep(0);
        }
    }

    public static MsgBase Judge(out EndPoint endPoint)
    {
        if (ReceiveQueue.Count > 0)
        {
            ReceEvent temp = ReceiveQueue.Dequeue();
            //拆包体
            MsgBase msgBase = ProtocalTool.UnPackMessage(temp.buff);
            endPoint = temp.SendClient;

            if (msgBase is MsgHeat) return msgBase as MsgHeat;
            else if (msgBase is MsgMove) return msgBase as MsgMove;  
        }
        endPoint = null;
        return null;
    }



    private static void SengHeat(object? port)
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        clientsockets.Add(socket);

        IPAddress ip = IPAddress.Parse(IP);
        IPEndPoint endPoint = new IPEndPoint(ip, (int)port);
        socket.Bind(endPoint);


        IPEndPoint Serverip = new IPEndPoint(IPAddress.Parse(Server_IP), Server_Port);
        //Console.WriteLine(Serverip);

        int i = 0;
        while (true)
        {
            Console.WriteLine($"发送次数{i++}");
            MsgHeat msgMove = new MsgHeat();
            socket.SendTo(msgMove.PackageMessage(), Serverip);
            Thread.Sleep(1000);
        }
    }


    private static void SendMsg(object? port)
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress ip = IPAddress.Parse(IP);
        IPEndPoint endPoint = new IPEndPoint(ip, (int)port);
        socket.Bind(endPoint);


        IPEndPoint Serverip = new IPEndPoint(IPAddress.Parse(Server_IP), Server_Port);
        Console.WriteLine(Serverip);

        int i = 0;
        while (true)
        {
            Console.WriteLine($"发送次数{i++}");

            MsgMove msgMove = new MsgMove();
            msgMove.x = 10 + i;
            msgMove.y = 20 + i;
            msgMove.z = 30 + i;

            socket.SendTo(msgMove.PackageMessage(), Serverip);

            Thread.Sleep(2000);
        }
    }

}

public class SendInfo
{
    public MsgBase SendMsg;
    public EndPoint SendClient;

    public SendInfo(MsgBase sendMsg, EndPoint sendClient)
    {
        SendMsg = sendMsg;
        SendClient = sendClient;
    }
}

public class ReceEvent
{
    public byte[] buff;
    public EndPoint SendClient;

    public ReceEvent(byte[] buff, EndPoint sendClient)
    {
        this.buff = buff;
        SendClient = sendClient;
    }
}


