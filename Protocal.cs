using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;



public class MsgBase
{
    public const int DataSize = sizeof(Int32);  //发送数据长度预设 4字节
    public const int NameSize = sizeof(Int16);  //发送数据长度预设 2字节


    public string protoName;

    public SendType sendType = SendType.None;

    public EndPoint SendClient; //发送客户端
    //public ClientInfo[] Receiveclient; // TODO:特定广播 

    /// <summary>
    /// 编码
    /// </summary>
    /// <param name="msgBase"></param>
    /// <returns></returns>
    public static byte[] Encode(MsgBase msgBase)
    {
        string s = JsonConvert.SerializeObject(msgBase);
        return Encoding.UTF8.GetBytes(s);
    }

    /// <summary>
    /// 解码
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static T Decode<T>(byte[] bytes, int offset, int count) where T : MsgBase
    {
        string s = Encoding.UTF8.GetString(bytes, offset, count);
        MsgBase msgBase = (MsgBase)JsonConvert.DeserializeObject(s, Type.GetType("123"));
        return (T)msgBase;
    }


    public static MsgBase Decode(Type type, byte[] bytes, int offset, int count)
    {
        string s = Encoding.UTF8.GetString(bytes, offset, count);
        MsgBase msgBase = (MsgBase)JsonConvert.DeserializeObject(s, type);
        return msgBase;
    }




    ///协议名的解码编码 使用Int16表示长度 (4 + 字符串)
    public static byte[] EncodeName(MsgBase msgBase)
    {
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(msgBase.protoName);
        //Int32 len = (Int32)nameBytes.Length;
        Int16 len = (Int16)nameBytes.Length;
        //申请bytes 数值
        byte[] bytes = new byte[NameSize + len];

        //组装2字节信息长度   2的8次方等于256 低位存放对应低位 高位存放对应高位 小段
        bytes[0] = (byte)(len % 256);
        bytes[1] = (byte)(len % 16777216 % 65536 / 256);
        bytes[2] = (byte)(len % 16777216 / 65536);
        bytes[3] = (byte)(len / 16777216);
        ////组装替代方法
        //byte[] length = BitConverter.GetBytes(len);

        //组装名字
        Array.Copy(nameBytes, 0, bytes, NameSize, len);
        return bytes;
    }


    /// <summary>
    /// 解码协议名
    /// </summary>
    /// <param name="bs"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static string DecodeName(byte[] bs, int offset, out int count)
    {
        count = 0;
        //最小长度应该大于2
        if (offset + NameSize > bs.Length)
        {
            return "解析失败";
        }

        //读取长度
        //Int32 len = (Int16)(bs[offset + 1] << 8 | bs[offset]);
        ////读取长度替代方法
        Int32 len = BitConverter.ToInt16(bs, offset);

        if (len <= 0)
        {
            return "解析失败";
        }
        if (offset + NameSize + len > bs.Length)
        {
            return "解析失败";
        }
        //解析
        count = NameSize + len;
        string name = System.Text.Encoding.UTF8.GetString(bs, offset + NameSize, len);
        return name;
    }



}

public static class ProtocalTool
{

    public const int DataSize = sizeof(Int32);  //发送数据长度预设 4字节
    public const int NameSize = sizeof(Int16);  //发送数据长度预设 2字节

    /// <summary>
    /// 数据封包
    /// </summary>
    /// <param name="msgBase"></param>
    public static byte[] PackageMessage(this MsgBase msgBase)
    {
        //数据编码
        byte[] nameBytes = MsgBase.EncodeName(msgBase);
        byte[] bodyBytes = MsgBase.Encode(msgBase);

        int len = nameBytes.Length + bodyBytes.Length;
        byte[] SendBytes = new byte[DataSize + len];

        //组装长度
        SendBytes[0] = (byte)(len % 256);
        SendBytes[1] = (byte)(len % 16777216 % 65536 / 256);
        SendBytes[2] = (byte)(len % 16777216 / 65536);
        SendBytes[3] = (byte)(len / 16777216);
        //SendBytes = BitConverter.GetBytes(len); //大小端

        //组装名字
        Array.Copy(nameBytes, 0, SendBytes, DataSize, nameBytes.Length);
        //组装消息体 
        Array.Copy(bodyBytes, 0, SendBytes, DataSize + nameBytes.Length, bodyBytes.Length);

        return SendBytes;
    }


    /// <summary>
    /// 数据拆包
    /// </summary>
    /// <param name="dataBytes"></param>
    /// <returns></returns>
    public static MsgBase UnPackMessage(byte[] dataBytes)
    {
        Int16 bodyLength = BitConverter.ToInt16(dataBytes, 0);

        int nameCount = 0;
        string ProtoName = MsgBase.DecodeName(dataBytes, DataSize, out nameCount);

        //Console.WriteLine(ProtoName);
        //Console.WriteLine(nameCount);

        if (ProtoName == "")
        {
            Console.WriteLine($"消息解析失败");
            return null;
        }

        //解析协议体
        MsgBase msgBase = MsgBase.Decode(Type.GetType(ProtoName), dataBytes, nameCount + DataSize, bodyLength - nameCount);

        return msgBase;
    }

}

[Serializable]
public class MsgMove : MsgBase
{
    public MsgMove()
    {
        protoName = "MsgMove";
        sendType = SendType.Other;
    }

    public int x = 0;
    public int y = 0;
    public int z = 0;

}

[Serializable]
public class MsgHeat : MsgBase
{
    public MsgHeat()
    {
        protoName = "MsgHeat";
        sendType = SendType.None;
    }
}


public struct SyncState
{
    public float Pox_x;
    public float Pox_y;
    public float Pox_z;

    public float Rot_x;
    public float Rot_y;
    public float Rot_Z;

}

public enum SendType
{
    None,      //不发送
    Broadcast, //广播
    Self,      //自己
    Other,     //除了自己之外
    Specific,  //特播

}