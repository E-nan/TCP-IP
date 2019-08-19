using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.ComponentModel;

namespace MultiChatServer {
    public partial class Server : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        Socket mainSock;
        IPAddress thisAddress;
        List<Socket> connectedClients = new List<Socket>();
        string[] problem = File.ReadAllLines(@"C:\Users\zv961\Desktop\Paint_Game.txt", Encoding.Default);
        string[] anslist;
        string ans;

        public Server()
        {
            InitializeComponent();
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            AppendTextDelegate Appender = new AppendTextDelegate(AppendText);
        }
        // 실행
        private void OnFormLoaded(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork) {
                    thisAddress = addr;
                    break;
                }    
            }
            if (thisAddress == null) thisAddress = IPAddress.Loopback;
            txtAddress.Text = thisAddress.ToString();
        }
        // 시작 버튼
        private void BeginStartServer(object sender, EventArgs e)
        {
            int port = 15000;
            if (mainSock.IsBound)
            {
                MsgBoxHelper.Warn("서버가 실행중입니다.");
                return;
            }
            IPEndPoint serverEP = new IPEndPoint(thisAddress, port);
            mainSock.Bind(serverEP);
            mainSock.Listen(10);
            mainSock.BeginAccept(AcceptCallback, null);
            AppendText(txtHistory, string.Format("서버가 실행되었습니다."));
        }
        // 클라 연결 함수
        void AcceptCallback(IAsyncResult ar)
        {
            Socket client = mainSock.EndAccept(ar);
            mainSock.BeginAccept(AcceptCallback, null);
            AsyncObject obj = new AsyncObject(50000);
            obj.WorkingSocket = client;
            connectedClients.Add(client);
            client.BeginReceive(obj.Buffer, 0, 50000, 0, DataReceived, obj);
        }
        // 보내기 버튼
        private void OnSendData(object sender, EventArgs e)
        {
            if (!mainSock.IsBound)
            {
                MsgBoxHelper.Warn("서버가 실행되고 있지 않습니다!");
                return;
            }
            string tts = txtTTS.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                txtTTS.Focus();
                return;
            }
            byte[] bDts = Encoding.UTF8.GetBytes("1" + '\x01' + "[서버]" + '\x01' + tts);
            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                try { socket.Send(bDts); } catch
                {
                    try { socket.Dispose(); } catch { }
                    connectedClients.RemoveAt(i);
                }
            }
            AppendText(txtHistory, string.Format("[보냄][서버]: {0}", tts));
            txtTTS.Clear();
        }
        // 보내기 버튼 실행키
        private void QuickSend(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnSend.PerformClick();
        }
        // 서버 종료시 클라와 연결 끊기
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (mainSock.IsBound)
            {
                byte[] bDts = Encoding.UTF8.GetBytes("0" + '\x01');
                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    Socket socket = connectedClients[i];
                    try { socket.Send(bDts); }
                    catch
                    {
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }
            }
        }
        // 텍스트 붙이는 함수
        void AppendText(Control ctrl, string s)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                ctrl.Text += Environment.NewLine + s;
                txtHistory.SelectionStart = txtHistory.Text.Length;
                txtHistory.ScrollToCaret();
            }));
        }
        // 데이터 수신 함수
        void DataReceived(IAsyncResult ar)
        {
            AsyncObject obj = (AsyncObject)ar.AsyncState;
            string text = Encoding.UTF8.GetString(obj.Buffer);
            string[] arrDts = text.Split('\x01');
            Random rand = new Random();
            this.Invoke(new MethodInvoker(delegate 
            { 
                if (arrDts[0] == "1") // 전체톡
                {
                    AppendText(txtHistory, string.Format("[받음]{0}: {1}", arrDts[1], arrDts[2]));
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                    {
                        Socket socket = connectedClients[i];
                        if (socket != obj.WorkingSocket)
                        {
                            try { socket.Send(obj.Buffer); }
                            catch
                            {
                                try { socket.Dispose(); } catch { }
                                connectedClients.RemoveAt(i);
                            }
                        }
                    }
                }
                else if (arrDts[0] == "2") // 게임톡
                {
                    int cnt = int.Parse(arrDts[5]);
                    int x = 0;
                    string sendDt = "";
                    if (arrDts[4] == ans)
                    {
                        if (anslist[10] != "")
                        {
                            arrDts[0] = "6";
                            arrDts[4] = ans;
                            foreach (string str in arrDts)
                            {
                                sendDt += arrDts[x] + '\x01';
                                x++;
                                if (x == 6)
                                {
                                    sendDt += "!" + '\x01';
                                }
                            }
                            byte[] sendByt = Encoding.UTF8.GetBytes(sendDt);
                            for (int j = cnt + 5; j >= 6; j--)
                            {
                                string nip = "";
                                for (int k = 0; k < ClientList.Items.Count; k++)
                                {
                                    if (arrDts[j].Equals(ClientList.Items[k].SubItems[1].Text))
                                    {
                                        nip = ClientList.Items[k].SubItems[0].Text;
                                        break;
                                    }
                                }
                                if (nip != "")
                                {
                                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                                    {
                                        if (nip.Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                        {
                                            Socket socket = connectedClients[i];
                                            try { socket.Send(sendByt); }
                                            catch
                                            {
                                                try { socket.Dispose(); } catch { }
                                                connectedClients.RemoveAt(i);
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            ans = "";
                        }
                        else
                        {
                            int ranpbm = 0;
                            int endi = 1;
                            while (endi != 0)
                            {
                                endi = 0;
                                ranpbm = rand.Next(0, problem.Length);
                                for (int j = 1; j < anslist.Length; j++)
                                {
                                    if (anslist[j] == "") break;
                                    if (problem[ranpbm] == anslist[j]) endi++;
                                }
                            }
                            arrDts[0] = "5";
                            arrDts[3] = ans;
                            arrDts[4] = problem[ranpbm];
                            for (int i = 0; i < anslist.Length; i++)
                            {
                                if (anslist[i] == "")
                                {
                                    anslist[i] = arrDts[4];
                                    break;
                                }
                            }
                            ans = arrDts[4];
                            foreach (string str in arrDts)
                            {
                                sendDt += arrDts[x] + '\x01';
                                x++;
                            }
                            byte[] sendBt = Encoding.UTF8.GetBytes(sendDt);
                            for (int j = cnt + 5; j >= 6; j--)
                            {
                                string nip = "";
                                for (int k = 0; k < ClientList.Items.Count; k++)
                                {
                                    if (arrDts[j].Equals(ClientList.Items[k].SubItems[1].Text))
                                    {
                                        nip = ClientList.Items[k].SubItems[0].Text;
                                        break;
                                    }
                                }
                                if (nip != "")
                                {
                                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                                    {
                                        if (nip.Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                        {
                                            Socket socket = connectedClients[i];
                                            try { socket.Send(sendBt); }
                                            catch
                                            {
                                                try { socket.Dispose(); } catch { }
                                                connectedClients.RemoveAt(i);
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = cnt + 5; j >= 6; j--)
                        {
                            string nip = "";
                            for (int k = 0; k < ClientList.Items.Count; k++)
                            {
                                if (arrDts[j].Equals(ClientList.Items[k].SubItems[1].Text))
                                {
                                    nip = ClientList.Items[k].SubItems[0].Text;
                                    break;
                                }
                            }
                            if (nip != "")
                            {
                                for (int i = connectedClients.Count - 1; i >= 0; i--)
                                {
                                    if (nip.Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                    {
                                        Socket socket = connectedClients[i];
                                        if(socket != obj.WorkingSocket)
                                        try { socket.Send(obj.Buffer); }
                                        catch
                                        {
                                            try { socket.Dispose(); } catch { }
                                            connectedClients.RemoveAt(i);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (arrDts[0] == "3") // 그림전송
                {
                    int cnt = int.Parse(arrDts[3]);
                    for (int j = cnt + 3; j >= 4; j--)
                    {
                        string nip = "";
                        for (int k = 0; k < ClientList.Items.Count; k++)
                        {
                            if (arrDts[j].Equals(ClientList.Items[k].SubItems[1].Text))
                            {
                                nip = ClientList.Items[k].SubItems[0].Text;
                                break;
                            }
                        }
                        if (nip != "")
                        {
                            for (int i = connectedClients.Count - 1; i >= 0; i--)
                            {
                                if (nip.Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                {
                                    Socket socket = connectedClients[i];
                                    if (socket != obj.WorkingSocket)
                                        try { socket.Send(obj.Buffer); }
                                        catch
                                        {
                                            try { socket.Dispose(); } catch { }
                                            connectedClients.RemoveAt(i);
                                        }
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (arrDts[0] == "4")
                {
                    int cnt = int.Parse(arrDts[4]);
                    if (arrDts[5] == "3")
                    {
                        anslist = new string[] { "", "", "", "", "", "", "", "", "", "", "" };
                        anslist[0] =  arrDts[1];
                        int ran = rand.Next(6, cnt + 6);
                        int ranpbm = rand.Next(0, problem.Length);
                        while (arrDts[ran].Equals(arrDts[3]))
                        {
                            ran = rand.Next(6, cnt + 6);
                        }
                        arrDts[3] = arrDts[ran];
                        arrDts[5] = problem[ranpbm];
                        anslist[1] = arrDts[5];
                        ans = arrDts[5];
                        string sendDt = "";
                        int x = 0;
                        foreach (string str in arrDts)
                        {
                            sendDt += arrDts[x] + '\x01';
                            x ++;
                        }
                        byte[] sendBt = Encoding.UTF8.GetBytes(sendDt);
                        for (int j = cnt + 5; j >= 6; j--)
                        {
                            string nip = "";
                            for (int k = 0; k < ClientList.Items.Count; k++)
                            {
                                if (arrDts[j].Equals(ClientList.Items[k].SubItems[1].Text))
                                {
                                    nip = ClientList.Items[k].SubItems[0].Text;
                                    break;
                                }
                            }
                            if (nip != "")
                            {
                                for (int i = connectedClients.Count - 1; i >= 0; i--)
                                {
                                    if (nip.Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                    {
                                        Socket socket = connectedClients[i];
                                        try { socket.Send(sendBt); }
                                        catch
                                        {
                                            try { socket.Dispose(); } catch { }
                                            connectedClients.RemoveAt(i);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = cnt + 5; j >= 6; j--)
                        {
                            string nip = "";
                            for (int k = 0; k < ClientList.Items.Count; k++)
                            {
                                if (arrDts[j].Equals(ClientList.Items[k].SubItems[1].Text))
                                {
                                    nip = ClientList.Items[k].SubItems[0].Text;
                                    break;
                                }
                            }
                            if (nip != "")
                            {
                                for (int i = connectedClients.Count - 1; i >= 0; i--)
                                {
                                    if (nip.Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                    {
                                        Socket socket = connectedClients[i];
                                        if (socket != obj.WorkingSocket)
                                        {
                                            try { socket.Send(obj.Buffer); }
                                            catch
                                            {
                                                try { socket.Dispose(); } catch { }
                                                connectedClients.RemoveAt(i);
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }//***************************************************************************************************************************************
                else if (arrDts[0] == "5")
                {
                    int cnt = int.Parse(arrDts[5]);
                    string sendDt = "";
                    int x = 0;
                    if (anslist[10] != "")
                    {
                        arrDts[0] = "6";
                        arrDts[3] = ans;
                        foreach (string str in arrDts)
                        {
                            sendDt += arrDts[x] + '\x01';
                            x++;
                        }
                        byte[] sendByt = Encoding.UTF8.GetBytes(sendDt);
                        for (int j = cnt + 6; j >= 7; j--)
                        {
                            string nip = "";
                            for (int k = 0; k < ClientList.Items.Count; k++)
                            {
                                if (arrDts[j].Equals(ClientList.Items[k].SubItems[1].Text))
                                {
                                    nip = ClientList.Items[k].SubItems[0].Text;
                                    break;
                                }
                            }
                            if (nip != "")
                            {
                                for (int i = connectedClients.Count - 1; i >= 0; i--)
                                {
                                    if (nip.Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                    {
                                        Socket socket = connectedClients[i];
                                        try { socket.Send(sendByt); }
                                        catch
                                        {
                                            try { socket.Dispose(); } catch { }
                                            connectedClients.RemoveAt(i);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        ans = "";
                        return;
                    }
                    int ran = rand.Next(7, cnt + 7);
                    int ranpbm = rand.Next(0, problem.Length);
                    while (arrDts[ran].Equals(arrDts[3]))
                    {
                        ran = rand.Next(7, cnt + 7);
                    }
                    int endi = 1;
                    while (endi != 0)
                    {
                        endi = 0;
                        ranpbm = rand.Next(0, problem.Length);
                        for (int j = 1; j < anslist.Length; j++)
                        {
                            if (anslist[j] == "") break;
                            if (problem[ranpbm] == anslist[j]) endi++;
                        }
                    }
                    arrDts[2] = arrDts[ran];
                    arrDts[3] = ans;
                    arrDts[4] = problem[ranpbm];
                    for(int i = 0; i < anslist.Length; i ++)
                    {
                        if (anslist[i] == "")
                        {
                            anslist[i] = arrDts[4];
                            break;
                        }
                    }
                    ans = arrDts[4];
                    foreach (string str in arrDts)
                    {
                        sendDt += arrDts[x] + '\x01';
                        x++;
                    }
                    byte[] sendBt = Encoding.UTF8.GetBytes(sendDt);
                    for (int j = cnt + 6; j >= 7; j--)
                    {
                        string nip = "";
                        for (int k = 0; k < ClientList.Items.Count; k++)
                        {
                            if (arrDts[j].Equals(ClientList.Items[k].SubItems[1].Text))
                            {
                                nip = ClientList.Items[k].SubItems[0].Text;
                                break;
                            }
                        }
                        if (nip != "")
                        {
                            for (int i = connectedClients.Count - 1; i >= 0; i--)
                            {
                                if (nip.Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                {
                                    Socket socket = connectedClients[i];
                                    try { socket.Send(sendBt); }
                                    catch
                                    {
                                        try { socket.Dispose(); } catch { }
                                        connectedClients.RemoveAt(i);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (arrDts[0] == "7") // 닉네임 중복검사
                {
                    bool chknick = true;
                    string mssg;
                    for (int i = 0; i < ClientList.Items.Count; i++)
                    {
                        if (arrDts[2].Equals(ClientList.Items[i].SubItems[1].Text))
                        {
                            chknick = false;
                            break;
                        }
                    }
                    if (chknick)
                        mssg = "1";
                    else
                        mssg = "0";
                    byte[] overlapsd = Encoding.UTF8.GetBytes("7" + '\x01' + mssg + '\x01');
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                    {
                        if (arrDts[1].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                        {
                            Socket socket = connectedClients[i];
                            try { socket.Send(overlapsd); }
                            catch
                            {
                                try { socket.Dispose(); } catch { }
                                connectedClients.RemoveAt(i);
                            }
                            break;
                        }
                    }
                }
                else if (arrDts[0] == "8")
                {
                    ListViewItem lstitm = new ListViewItem();
                    lstitm.Text = arrDts[2];
                    lstitm.SubItems.Add(arrDts[3]);
                    ClientList.Items.Add(lstitm);
                    string msg = "[ " + arrDts[3] + " ]님이 입장하였습니다.";
                    string nicklist = "8" + '\x01' + msg + '\x01';
                    for (int i = 0; i < ClientList.Items.Count; i++)
                        nicklist += ClientList.Items[i].SubItems[1].Text + '\x01';
                    byte[] lbip = Encoding.UTF8.GetBytes(nicklist);
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                    {
                        Socket socket = connectedClients[i];
                        try { socket.Send(lbip); }
                        catch
                        {
                            try { socket.Dispose(); } catch { }
                            connectedClients.RemoveAt(i);
                        }
                    }
                    AppendText(txtHistory, string.Format("클라이언트 (@ {0})가 연결되었습니다.", arrDts[3]));
                }
                else if (arrDts[0] == "9") // 클라이언트 리스트 갱신
                {
                    for (int i = ClientList.Items.Count - 1; i >= 0; i--)
                    {
                        if (ClientList.Items[i].SubItems[1].Text.Equals(arrDts[1]))
                        {
                            ClientList.Items.Remove(ClientList.Items[i]);
                            connectedClients.Remove(connectedClients[i]);
                            break;
                        }
                    }
                    string msg = "[ " + arrDts[1] + " ]님이 퇴장하였습니다.";
                    string iptext = "9" + '\x01' + msg + '\x01';
                    for (int i = 0; i < ClientList.Items.Count; i++)
                        iptext += ClientList.Items[i].SubItems[1].Text + '\x01';
                    byte[] lbip = Encoding.UTF8.GetBytes(iptext);
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                    {
                        Socket socket = connectedClients[i];
                        try { socket.Send(lbip); }
                        catch
                        {
                            try { socket.Dispose(); } catch { }
                            connectedClients.RemoveAt(i);
                        }
                    }
                    obj.WorkingSocket.Close();
                    return;
                }
            }));
            obj.ClearBuffer();
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 50000, 0, DataReceived, obj);
        }
    }
}