/////// -----------ye
/////// ----------- 把异步方式BegineInvoke改为async、awaite 
//namespace KBEngine
//{
//        using System;
//        using System.Net.Sockets;
//        using System.Net;
//        using System.Collections;
//        using System.Collections.Generic;
//        using System.Text;
//        using System.Text.RegularExpressions;
//        using System.Threading;
//        //using System.Runtime.Remoting.Messaging;

//        using MessageID = System.UInt16;
//        using MessageLength = System.UInt16;
//        using System.Threading.Tasks;

//        /*
//                    包发送模块(与服务端网络部分的名称对应)
//                    处理网络数据的发送
//            */
//        public class PacketSender
//        {
//                public delegate void AsyncSendMethod();

//                private byte[] _buffer;

//                int _wpos = 0;              // 写入的数据位置
//                int _spos = 0;              // 发送完毕的数据位置
//                int _sending = 0;

//                private NetworkInterface _networkInterface = null;

//                public PacketSender(NetworkInterface networkInterface)
//                {
//                        _init(networkInterface);
//                }

//                ~PacketSender()
//                {
//                        this.Release();
//                        Dbg.DEBUG_MSG("PacketSender::~PacketSender(), destroyed!");
//                }

//                void _init(NetworkInterface networkInterface)
//                {
//                        _networkInterface = networkInterface;

//                        _buffer = new byte[NetworkInterface.TCP_PACKET_MAX];
//                        //_asyncSendMethod = new AsyncSendMethod(this._asyncSend);
//                        //_asyncCallback = new AsyncCallback(_onSent);

//                        _wpos = 0;
//                        _spos = 0;
//                        _sending = 0;
//                }

//                public NetworkInterface networkInterface()
//                {
//                        return _networkInterface;
//                }

//                public bool send(MemoryStream stream)
//                {
//                        int dataLength = (int)stream.length();
//                        if (dataLength <= 0)
//                                return true;

//                        if (0 == Interlocked.Add(ref _sending, 0))
//                        {
//                                if (_wpos == _spos)
//                                {
//                                        _wpos = 0;
//                                        _spos = 0;
//                                }
//                        }

//                        int t_spos = Interlocked.Add(ref _spos, 0);
//                        int space = 0;
//                        int tt_wpos = _wpos % _buffer.Length;
//                        int tt_spos = t_spos % _buffer.Length;

//                        if (tt_wpos >= tt_spos)
//                                space = _buffer.Length - tt_wpos + tt_spos - 1;
//                        else
//                                space = tt_spos - tt_wpos - 1;

//                        if (dataLength > space)
//                        {
//                                Dbg.ERROR_MSG("PacketSender::send(): no space, Please adjust 'SEND_BUFFER_MAX'! data(" + dataLength
//                                    + ") > space(" + space + "), wpos=" + _wpos + ", spos=" + t_spos);

//                                return false;
//                        }

//                        int expect_total = tt_wpos + dataLength;
//                        if (expect_total <= _buffer.Length)
//                        {
//                                Array.Copy(stream.data(), stream.rpos, _buffer, tt_wpos, dataLength);
//                        }
//                        else
//                        {
//                                int remain = _buffer.Length - tt_wpos;
//                                Array.Copy(stream.data, stream.rpos, _buffer, tt_wpos, remain);
//                                Array.Copy(stream.data, stream.rpos + remain, _buffer, 0, expect_total - _buffer.Length);
//                        }

//                        Interlocked.Add(ref _wpos, dataLength);

//                        if (Interlocked.CompareExchange(ref _sending, 1, 0) == 0)
//                        {
//                                _startSend();
//                        }

//                        return true;
//                }

//                void _startSend()
//                {
//                        // 由于socket用的是非阻塞式，因此在这里不能直接使用socket.send()方法
//                        // 必须放到另一个线程中去做
//                        //_asyncSendMethod.BeginInvoke(_asyncCallback, null);
//                        //_asyncSendMethod.
//                        this._asyncSend();
//                        //DoSendAsync();   //执行可等待异步发送
//                }
//                /// <summary>
//                /// 创建异步发送任务并执行
//                /// </summary>
//                /// <returns></returns>
//                private Task DoSendTask()
//                {
//                        return Task.Run(() => { this._asyncSend(); });
//                }

//                /// <summary>
//                /// 可等待异步发送
//                /// </summary>
//                private async void DoSendAsync()
//                {
//                        await DoSendTask();
//                }
//                private SocketAsyncEventArgs sendSaea = null;
//                void _asyncSend()
//                {
//                        if (_networkInterface == null || !_networkInterface.valid())
//                        {
//                                Dbg.WARNING_MSG("PacketSender::_asyncSend(): network interface invalid!");
//                                return;
//                        }

//                        var socket = _networkInterface.sock();

//                        int sendSize = this.GetSendSize();

//                        int bytesSent = 0;
//                        try
//                        {
//                                sendSaea = new SocketAsyncEventArgs();
//                                sendSaea.SetBuffer(_buffer, _spos % _buffer.Length, sendSize);       //设置消息的缓冲区大小
//                                sendSaea.Completed += SendSaea_Completed;         //绑定回调事件
//                                sendSaea.RemoteEndPoint = socket.RemoteEndPoint;
//                                sendSaea.UserToken = bytesSent;
//                                socket.SendAsync(sendSaea);

//                                //bytesSent = socket.Send(_buffer, _spos % _buffer.Length, sendSize, 0);
//                        }
//                        catch (SocketException se)
//                        {
//                                Dbg.ERROR_MSG(string.Format("PacketSender::_asyncSend(): send data error, disconnect from '{0}'! error = '{1}'", socket.RemoteEndPoint, se));
//                                Event.fireIn("_closeNetwork", new object[] { _networkInterface });
//                                return;
//                        }

//                        int spos = Interlocked.Add(ref _spos, bytesSent);

//                        // 所有数据发送完毕了
//                        if (spos == Interlocked.Add(ref _wpos, 0))
//                        {
//                                Interlocked.Exchange(ref _sending, 0);
//                                return;
//                        }

//                }
//                private int GetSendSize()
//                {
//                        int sendSize = Interlocked.Add(ref _wpos, 0) - _spos;
//                        int t_spos = _spos % _buffer.Length;
//                        if (t_spos == 0)
//                                t_spos = sendSize;

//                        if (sendSize > _buffer.Length - t_spos)
//                                sendSize = _buffer.Length - t_spos;
//                        return sendSize;
//                }
//                private void SendSaea_Completed(object sender, SocketAsyncEventArgs e)
//                {
//                        if (e.SocketError != SocketError.Success) return;
//                        var socket = sender as Socket;

//                        int bytesSent = e.BytesTransferred;
//                        int spos = Interlocked.Add(ref _spos, bytesSent);
//                        // 所有数据发送完毕了
//                        if (spos != Interlocked.Add(ref _wpos, 0))
//                        {
//                                int sendSize = this.GetSendSize();
//                                e.SetBuffer(_spos % _buffer.Length, sendSize);
//                                socket.SendAsync(e);
//                        }
//                        else
//                        {
//                                Interlocked.Exchange(ref _sending, 0);
//                                return;
//                        }
//                }
//                private void Release()
//                {
//                        sendSaea.Completed -= SendSaea_Completed;
//                        sendSaea.Dispose();
//                        sendSaea = null;
//                }
//                //private static void _onSent(IAsyncResult ar)
//                //{
//                //	AsyncResult result = (AsyncResult)ar;
//                //	AsyncSendMethod caller = (AsyncSendMethod)result.AsyncDelegate;
//                //	caller.EndInvoke(ar);
//                //}
//        }
//}
