namespace KBEngine
{
        using System;
        using System.Net.Sockets;
        using System.Net;
        using System.Collections;
        using System.Collections.Generic;
        using System.Text;
        using System.Text.RegularExpressions;
        using System.Threading;
        using System.Runtime.Remoting.Messaging;

        using MessageID = System.UInt16;
        using MessageLength = System.UInt16;

        /*
		包发送模块(与服务端网络部分的名称对应)
		处理网络数据的发送
	*/
        public abstract class PacketSenderBase
        {
                public delegate void AsyncSendMethod();

                protected NetworkInterfaceBase _networkInterface = null;
            
                public PacketSenderBase(NetworkInterfaceBase networkInterface)
                {
                        _networkInterface = networkInterface;
                }

                ~PacketSenderBase()
                {
                }

                public NetworkInterfaceBase networkInterface()
                {
                        return _networkInterface;
                }

                public abstract bool send(MemoryStream stream);


                protected abstract void _asyncSend();

                protected static void _onSent(IAsyncResult ar)
                {
                        AsyncResult result = (AsyncResult)ar;
                        AsyncSendMethod caller = (AsyncSendMethod)result.AsyncDelegate;
                        caller.EndInvoke(ar);
                }
        }
}
