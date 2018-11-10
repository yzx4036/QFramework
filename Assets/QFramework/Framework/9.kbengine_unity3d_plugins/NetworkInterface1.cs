namespace KBEngine
{
        using UnityEngine;
        using System;
        using System.Net.Sockets;
        using System.Net;
        using System.Collections;
        using System.Collections.Generic;
        using System.Text;
        using System.Text.RegularExpressions;
        using System.Threading;
        using MessageID = System.UInt16;
        using MessageLength = System.UInt16;
        using System.Threading.Tasks;

        //using System.Runtime.Remoting.Messaging;

        /// <summary>
        /// 网络模块
        /// 处理连接、收发数据
        /// </summary>
        public abstract class NetworkInterfaceBase
        {

                public const int TCP_PACKET_MAX = 1460;
                public const int UDP_PACKET_MAX = 1472;
                public const string UDP_HELLO = "62a559f3fa7748bc22f8e0766019d498";
                public const string UDP_HELLO_ACK = "1432ad7c829170a76dd31982c3501eca";
                public delegate void ConnectCallback(string ip, int port, bool success, object userData);
                public delegate void OnConnectedDelegate();
                public delegate void OnDisconnectedDelegate(string exception);
                //public delegate void OnPacketDelegate(ByteArray pack);
                public delegate void TestHandler();
                public TestHandler OnTestHandle;
                public OnConnectedDelegate onConnected;
                public OnDisconnectedDelegate onDisconnected;
                //public OnPacketDelegate onPacket;
                protected Socket _clientSocket = null;
                private SocketAsyncEventArgs _clientSocketEventArg = null;
                protected SocketState _state;
                protected float _timeStamp;
                const int CONNECTION_TIME_OUT = 5000;//Socket连接超时时间

                protected PacketReceiverBase _packetReceiver = null;
                protected PacketSenderBase _packetSender = null;
                protected EncryptionFilter _filter = null;
                private string _reason; 

                public class ConnectState
                {
                        // for connect
                        public string connectIP = "";
                        public int connectPort = 0;
                        public ConnectCallback connectCB = null;
                        public object userData = null;

                        public Socket socket = null;
                        public SocketAsyncEventArgs socketAEA = null;
                        public NetworkInterfaceBase networkInterface = null;
                        public SocketState socketState = SocketState.None;
                        public string error = "";
                }
                public enum SocketState
                {
                        None,
                        Disconnect,
                        Connecting,
                        Success,
                        Connected
                }
                public NetworkInterfaceBase()
                {
                        reset();
                }

                ~NetworkInterfaceBase()
                {
                        Dbg.DEBUG_MSG("NetworkInterfaceBase::~NetworkInterfaceBase(), destructed!!!");
                        reset();
                }

                public virtual Socket sock()
                {
                        return _clientSocket;
                }

                public void Test()
                {
                        if (this.OnTestHandle != null)
                                OnTestHandle();
                }

                public virtual void reset()
                {
                        if (valid())
                        {
                                Dbg.DEBUG_MSG(string.Format("NetworkInterfaceBase::reset(), close socket from '{0}'", _clientSocket.RemoteEndPoint.ToString()));
                                if (_clientSocketEventArg != null)
                                        _clientSocketEventArg.Completed -= _clientSocketEventArg_Completed;
                                _clientSocket.Dispose();
                        }
                        this._state = SocketState.None;

                        _clientSocket = null;
                        if (_packetReceiver != null) _packetReceiver.Release();
                        _packetReceiver = null;
                        _packetSender = null;
                        _filter = null;

                }

                //被动关闭，可能在其他线程
                bool _inDispose;
                protected void Dispose(string reason = null)
                {
                        if (_inDispose || _clientSocket == null)
                                return;

                        _inDispose = true;
                        if (reason != null)
                                _reason = reason;

                        if (this._clientSocket != null)
                        {
                                if (this._clientSocket.Connected)
                                {
                                        try
                                        {
                                                this._clientSocket.Shutdown(SocketShutdown.Both);
                                                this._clientSocketEventArg.Completed -= _clientSocketEventArg_Completed;
                                                this._clientSocket.Dispose();
                                                this._clientSocketEventArg = null;
                                        }
                                        catch (Exception ex)
                                        {
                                                Debug.LogError(ex.Message);
                                        }
                                }
                                this._clientSocket = null;
                        }

                        this._state = SocketState.Disconnect;
                        _inDispose = false;
                }
                public virtual void close(string result = null)
                {
                        this.Dispose(result);
                }

                protected abstract PacketReceiverBase createPacketReceiver();
                protected abstract PacketSenderBase createPacketSender();
                protected abstract Socket createSocket();  //子类决定创建tcp 还是udp
                protected abstract void onAsyncConnect(ConnectState state);


                public virtual bool valid()
                {
                        return ((_clientSocket != null) && (_clientSocket.Connected == true));
                }


                /// <summary>
                /// 在非主线程执行：连接服务器  已修改支持新版 
                /// </summary>
                private void _asyncConnect(ConnectState state)
                {
                        Dbg.DEBUG_MSG(string.Format("NetWorkInterface::_asyncConnect(), will connect to '{0}:{1}' ...", state.connectIP, state.connectPort));
                        onAsyncConnect(state);
                }
               
                //不用变
                public void connectTo(string ip, int port, ConnectCallback callback, object userData)
                {
                        if (valid())
                                throw new InvalidOperationException("Have already connected!");
                        IPEndPoint ipEndpoint = null;

                        if ((new Regex(@"((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))")).IsMatch(ip))
                        {
                                IPAddress ipAddress = IPAddress.Parse(ip);
                                ipEndpoint = new IPEndPoint(ipAddress, port);
                        }

                        AddressFamily addressFamily = AddressFamily.InterNetwork;
                        //if (Socket.OSSupportsIPv6 && this.IsHaveIpV6Address(hostAddresses, ref outIPs))
                        //{
                        //    addressFamily = AddressFamily.InterNetworkV6;
                        //}
                        _clientSocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

                        _clientSocket.NoDelay = true;

                        ConnectState state = new ConnectState();

                        _clientSocketEventArg = new SocketAsyncEventArgs();
                        _clientSocketEventArg.Completed += _clientSocketEventArg_Completed;
                        _clientSocketEventArg.RemoteEndPoint = ipEndpoint;
                        _clientSocketEventArg.UserToken = state;

                        state.connectIP = ip;
                        state.connectPort = port;
                        state.connectCB = callback;
                        state.userData = userData;
                        state.socket = _clientSocket;
                        state.socketAEA = _clientSocketEventArg;
                        state.networkInterface = this;

                        Dbg.DEBUG_MSG("ready to connect " + ip + ":" + port + " ...");

                        this._asyncConnect(state);
                }

                private void _clientSocketEventArg_Completed(object sender, SocketAsyncEventArgs e)
                {
                        if (e.SocketError != SocketError.Success)
                                return;

                        var socket = sender as Socket;
                        var stateToken = e.UserToken as ConnectState;

                        if (socket.Connected)
                        {
                                this._state = stateToken.socketState = SocketState.Success;
                                try
                                {
                                        Dbg.DEBUG_MSG("?>>>_clientSocketEventArg_Completed" + stateToken.connectPort);
                                        _packetReceiver = createPacketReceiver();
                                        _packetReceiver.startRecv();
                                }
                                catch (Exception exc)
                                {
                                        Dbg.ERROR_MSG("_clientSocketEventArg_Completed.." + exc.Message);
                                        this.Dispose(exc.Message);

                                }
                                if (stateToken.connectCB != null)
                                        stateToken.connectCB(stateToken.connectIP, stateToken.connectPort, true, stateToken.userData);
                                _clientSocketEventArg.Completed -= _clientSocketEventArg_Completed;

                        }
                }

                private bool IsHaveIpV6Address(IPAddress[] IPs, ref IPAddress[] outIPs)
                {
                        int length = 0;
                        for (int index = 0; index < IPs.Length; ++index)
                        {
                                if (AddressFamily.InterNetworkV6.Equals((object)IPs[index].AddressFamily))
                                        ++length;
                        }
                        if (length <= 0)
                                return false;
                        outIPs = new IPAddress[length];
                        int num = 0;
                        for (int index = 0; index < IPs.Length; ++index)
                        {
                                if (AddressFamily.InterNetworkV6.Equals((object)IPs[index].AddressFamily))
                                        outIPs[num++] = IPs[index];
                        }
                        return true;
                }


                public virtual bool send(MemoryStream stream)
                {
                        if (!valid())
                        {
                                throw new ArgumentException("invalid socket!");
                        }

                        if (_packetSender == null)
                                _packetSender = createPacketSender();

                        if (_filter != null)
                                return _filter.send(_packetSender, stream);

                        return _packetSender.send(stream);
                }

                public virtual void process()
                {
                        switch (this._state)
                        {
                                case SocketState.Connecting:
                                        float ct = Time.realtimeSinceStartup;
                                        float dt = (ct - this._timeStamp) * 1000.0f;
                                        if (dt >= CONNECTION_TIME_OUT)
                                        {
                                                _reason = "Socket connection timeout";
                                                Dbg.ERROR_MSG(_reason);
                                                this._state = SocketState.Disconnect;
                                        }
                                        break;

                                case SocketState.Success:
                                        this._state = SocketState.Connected;
                                        if (onConnected != null)
                                                onConnected();
                                        break;

                                case SocketState.Disconnect:
                                        this._state = SocketState.None;
                                        //Timers.inst.Remove(this.process);
                                        if (onDisconnected != null)
                                                onDisconnected(_reason);
                                        break;
                        }

                        if (!valid())
                                return;
                        if (_packetReceiver != null)
                                _packetReceiver.process();
                        //LuaManager.Instance.CallFunction1("KBEngineLua", "sendTickProcess");
                        //LuaManager.Instance.CallFunction("KBEngineLua", "sendTickProcess");
                }

                public EncryptionFilter fileter()
                {
                        return _filter;
                }

                public void setFilter(EncryptionFilter filter)
                {
                        _filter = filter;
                }
        }
}

