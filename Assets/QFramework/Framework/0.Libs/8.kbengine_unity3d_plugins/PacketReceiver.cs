namespace KBEngine
{
    using System;
    using System.Net.Sockets;
    using System.Threading;
    //using System.Runtime.Remoting.Messaging;

    using System.Threading.Tasks;

    /*
		包接收模块(与服务端网络部分的名称对应)
		处理网络数据的接收
	*/
    public class PacketReceiver
    {
        private NetworkInterface _networkInterface = null;

        private byte[] _buffer;

        // socket向缓冲区写的起始位置
        int _wpos = 0;

        // 主线程读取数据的起始位置
        int _rpos = 0;

        public PacketReceiver(NetworkInterface networkInterface)
        {
            Dbg.DEBUG_MSG("PacketReceiver::init");

            _init(ref networkInterface);
        }

        ~PacketReceiver()
        {
            Dbg.DEBUG_MSG("PacketReceiver::~PacketReceiver(), destroyed!");
        }

        void _init(ref NetworkInterface networkInterface)
        {
            _networkInterface = networkInterface;
            cts = new CancellationTokenSource();
            _buffer = new byte[NetworkInterface.TCP_PACKET_MAX];
            //_networkInterface.OnTestHandle = () => 
            //{
            //    var x= _buffer;
            //    var w = _wpos;
            //};

        }

        public NetworkInterface networkInterface()
        {
            return _networkInterface;
        }

        public void process()
        {
            int t_wpos = Interlocked.Add(ref _wpos, 0);
            //UnityEngine.Debug.Log(">>>>>wpos:"+_wpos+" >t_wpos:" +t_wpos +" >_rpos:"+_rpos +" >result=" + (UInt32)(t_wpos - _rpos));
            if (_rpos < t_wpos)
            {
                LuaManager.Instance.CallFunction("KBEngineLua.MessageReader", "process", new object[] { _buffer, (UInt32)_rpos, (UInt32)(Math.Abs(t_wpos - _rpos)) });
                Interlocked.Exchange(ref _rpos, t_wpos);
            }
            else if (t_wpos < _rpos)
            {
                LuaManager.Instance.CallFunction("KBEngineLua.MessageReader", "process", new object[] { _buffer, (UInt32)_rpos, (UInt32)(Math.Abs(_buffer.Length - _rpos)) });
                LuaManager.Instance.CallFunction("KBEngineLua.MessageReader", "process", new object[] { _buffer, (UInt32)0, (UInt32)t_wpos });
                Interlocked.Exchange(ref _rpos, t_wpos);
            }
            else
            {
                // 没有可读数据
            }
        }

        int _free()
        {
            int t_rpos = Interlocked.Add(ref _rpos, 0);

            if (_wpos == _buffer.Length)
            {
                if (t_rpos == 0)
                {
                    return 0;
                }

                Interlocked.Exchange(ref _wpos, 0);
            }

            if (t_rpos <= _wpos)
            {
                return _buffer.Length - _wpos;
            }

            return t_rpos - _wpos - 1;
        }

        public void startRecv()
        {
            this._asyncReceive();
        }

        /// <summary>
        /// 创建异步等待接受空间
        /// </summary>
        /// <returns></returns>
        private async Task<int> DoWaitSpaceTask(CancellationToken token)
        {
            // 必须有空间可写，否则我们等待直到有空间为止
            int first = 0;
            int space = _free();

            while (space <= 0)
            {
                if (token.IsCancellationRequested)
                { break; }
                else UnityEngine.Debug.Log(_networkInterface.valid().ToString());
                if (first > 0)
                {
                    if (first > 100)
                    {

                    }
                    if (first > 1000)
                    {
                        Dbg.ERROR_MSG("PacketReceiver::_asyncReceive(): no space!");
                        Event.fireIn("_closeNetwork", new object[] { _networkInterface });
                        break;
                    }

                    Dbg.WARNING_MSG("PacketReceiver::_asyncReceive(): waiting for space, Please adjust 'RECV_BUFFER_MAX'! retries=" + first);
                }

                first += 1;
                space = _free();
                await Task.Delay(500);

            }

            return space;
        }

        private SocketAsyncEventArgs receiveSaea = null;
        private async void _asyncReceive()
        {
            if (_networkInterface == null || !_networkInterface.valid())
            {
                Dbg.WARNING_MSG("PacketReceiver::_asyncReceive(): network interface invalid!");
                return;
            }

            var socket = _networkInterface.sock();
            int space = await Task.Factory.StartNew(() => { return DoWaitSpaceTask(cts.Token); }, cts.Token).Result;
            UnityEngine.Debug.Log(">>space:" + space);

            //Dbg.WARNING_MSG(".........");
            try
            {
                receiveSaea = new SocketAsyncEventArgs();

                receiveSaea.SetBuffer(_buffer, _wpos, space);       //设置消息的缓冲区大小
                receiveSaea.Completed += ReceiveSaea_Completed;         //绑定回调事件
                receiveSaea.RemoteEndPoint = socket.RemoteEndPoint;
                receiveSaea.UserToken = socket;
                socket.ReceiveAsync(receiveSaea);
                //UnityEngine.Debug.Log("receive asyncFlag..:" + asyncFlag);
            }
            catch (SocketException se)
            {
                Dbg.ERROR_MSG(string.Format("PacketReceiver::_asyncReceive(): receive error, disconnect from '{0}'! error = '{1}'", socket.RemoteEndPoint, se));
                Event.fireIn("_closeNetwork", new object[] { _networkInterface });
                return;
            }
        }



        private async void ReceiveSaea_Completed(object sender, SocketAsyncEventArgs e)
        {
            UnityEngine.Debug.Log(">>>>>>>DDDDDDDDDDDDDD");
            if (e.SocketError == SocketError.OperationAborted) return;

            Socket client = e.UserToken as Socket;
            int revCount = e.BytesTransferred;
            //Dbg.INFO_MSG("revCount:" + revCount);
            if (e.SocketError == SocketError.Success)
            {
                //UnityEngine.Debug.Log("count>>>>>>>>>>>>>>>>>>>>>>>>" + revCount);
                //_receiveBuffer.Append(e.Buffer, 0, revCount);
                //this.Unpack();
                //UnityEngine.Debug.Log(">>space:" + space);



                if (revCount > 0)
                {
                    Interlocked.Add(ref _wpos, revCount);
                    //UnityEngine.Debug.Log(">>>>r>>_wpos：" + _wpos + " revCount:" + revCount);
                }
                int space = await Task.Factory.StartNew(() => { return DoWaitSpaceTask(cts.Token); }, cts.Token).Result;

                UnityEngine.Debug.Log(">>space:" + space);
                try
                {
                    //UnityEngine.Debug.Log(">>>>qqqr>>>>>>>>>>>>>>_wpos：" + _wpos + " space:" + space);
                    e.SetBuffer(_buffer, _wpos, space);
                    client.ReceiveAsync(e);
                    //UnityEngine.Debug.Log("receive completed asyncFlag..:" + asyncFlag);

                }
                catch (Exception exc)
                {
                    //Event.fireIn("_closeNetwork", new object[] { _networkInterface });

                    Dbg.ERROR_MSG("2>>>" + exc.Message);
                    _networkInterface.close(exc.Message);
                }
                //if (revCount > 0)
                //{
                //    Interlocked.Add(ref _wpos, revCount);
                //    UnityEngine.Debug.Log(">>>>r>>_wpos：" + _wpos + " revCount:" + revCount);
                //}
                //else
                //{
                //    _networkInterface.close();
                //}
                //UnityEngine.Debug.Log("rtt>>>>>>>>>>>>>>");

            }
            else
            {
                Dbg.WARNING_MSG(string.Format("PacketReceiver::_asyncReceive(): receive 0 bytes, disconnect from '{0}'!", client.RemoteEndPoint));
                Event.fireIn("_closeNetwork", new object[] { _networkInterface });
                return;

            }
        }
        CancellationTokenSource cts;
        public void Release()
        {
            Dbg.ERROR_MSG("packreceiver release");
            receiveSaea.Completed -= ReceiveSaea_Completed;
            receiveSaea.Dispose();
            cts.Cancel();
            cts = null;
            receiveSaea = null;
            //this._networkInterface = null;

        }
    }
}
