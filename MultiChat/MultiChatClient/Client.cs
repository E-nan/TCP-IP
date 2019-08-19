using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Drawing;

namespace MultiChatClient {
    public partial class Client : Form {
        delegate void AppendTextDelegate(Control ctrl, string s);
        Socket mainSock;
        Paint_Game pg;
        public string recvDt = "";
        public string pictureDt = "";
        public string crenick = "";
        public int sendkind;
        bool ServerConnected = false;
        bool Formset = false;

        public Client()
        {
            InitializeComponent();
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            AppendTextDelegate Appender = new AppendTextDelegate(AppendText);
        }
        // 실행
        public void OnFormLoaded(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress defaultHostAddress = null;
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    defaultHostAddress = addr;
                    break;
                }
            }
            if (defaultHostAddress == null)
                defaultHostAddress = IPAddress.Loopback;
            txtServerIP.Text = defaultHostAddress.ToString();
        }
        // 중복 체크
        private void Overlap_Check(object sender, EventArgs e)
        {
            if (!ServerConnected)
            {
                int port = 15000;
                try
                {
                    mainSock.Connect(txtServerIP.Text, port);
                    ServerConnected = true;
                    AsyncObject obj = new AsyncObject(50000);
                    obj.WorkingSocket = mainSock;
                    mainSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
                }
                catch (Exception ex)
                {
                    MsgBoxHelper.Error("연결에 실패했습니다!\n오류 내용: {0}", MessageBoxButtons.OK, ex.Message);
                    return;
                }
            }
            byte[] bDts = Encoding.UTF8.GetBytes("7" + '\x01' + mainSock.LocalEndPoint.ToString() + '\x01' + txtNick.Text + '\x01');
            mainSock.Send(bDts);
        }
        // 닉네임 변경
        private void txtNick_TextChanged(object sender, EventArgs e)
        {
            button2.Enabled = true;
        }
        // 연결 버튼
        public void OnConnectToServer(object sender, EventArgs e)
        {
            if (!button2.Enabled)
            {
                if (!mainSock.IsBound)
                    return;
                byte[] bDts = Encoding.UTF8.GetBytes("8" + '\x01' + "1" + '\x01' + mainSock.LocalEndPoint.ToString() + '\x01' + txtNick.Text + '\x01');
                mainSock.Send(bDts);
                panel2.Visible = false;
                panel1.Visible = true;
                txtAddress.Text = txtServerIP.Text;
                Nick_label.Text = txtNick.Text;
                this.Size = new Size(755, 565);
            }
            else
            {
                MessageBox.Show("중복확인을 해주세요!");
            }
        }
        // 보내기 버튼
        public void OnSendData(object sender, EventArgs e)
        {
            if (!mainSock.IsBound)
                return;
            string tts = txtTTS.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                txtTTS.Focus();
                return;
            }
            byte[] bDts = Encoding.UTF8.GetBytes("1" + '\x01' + Nick_label.Text + '\x01' + tts);
            mainSock.Send(bDts);
            AppendText(txtHistory, string.Format("[보냄]{0}: {1}", Nick_label.Text, tts));
            txtTTS.Clear();
        }
        // 보내기 버튼 실행키
        private void QuickSend(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnSend.PerformClick();
        }
        // 그림 맞추기방 버튼
        private void OpenPaintGame(object sender, EventArgs e)
        {

            if (ClientList.CheckedItems.Count == 0)
            {
                MsgBoxHelper.Error("사용자를 선택해주세요.");
                return;
            }
            for (int i = ClientList.CheckedItems.Count - 1; i >= 0; i--)
            {
                if (Nick_label.Text.Equals(ClientList.CheckedItems[i].ToString()))
                {
                    MsgBoxHelper.Error("본인을 제외한 사용자를 선택해주세요.");
                    return;
                }
            }
            pg = new Paint_Game(this, Nick_label.Text);
            pg.Show();
        }
        // 종료 시 연결 종료
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (mainSock.Connected)
            {
                byte[] closeInfo = Encoding.UTF8.GetBytes("9" + '\x01' + Nick_label.Text + '\x01');
                mainSock.Send(closeInfo);
                return;
            }
        }
        // 다른 창에서 메세지 보내는 함수
        public void TalkSend()
        {
            if (sendkind == 1)
            {
                byte[] bDts = Encoding.UTF8.GetBytes("2" + '\x01' + crenick + '\x01' + Nick_label.Text + '\x01' + recvDt);
                recvDt = "";
                mainSock.Send(bDts);
            }
            else if (sendkind == 2)
            {
                Byte[] bDts = Encoding.UTF8.GetBytes("3" + '\x01' + crenick + '\x01' + pictureDt + '\x01' + recvDt);
                pictureDt = "";
                recvDt = "";
                mainSock.Send(bDts);
            }
            else if (sendkind == 3)
            {
                byte[] bDts = Encoding.UTF8.GetBytes("4" + '\x01' + crenick + '\x01' + Nick_label.Text + '\x01' + recvDt);
                recvDt = "";
                mainSock.Send(bDts);
            }
            else if (sendkind == 4)
            {
                byte[] bDts = Encoding.UTF8.GetBytes("5" + '\x01' + crenick + '\x01' + Nick_label.Text + '\x01' + recvDt);
                recvDt = "";
                mainSock.Send(bDts);
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
            this.Invoke(new MethodInvoker(delegate ()
            {
                if (arrDts[0] == "1") // 전체 채팅
                    AppendText(txtHistory, string.Format("[받음]{0}: {1}", arrDts[1], arrDts[2]));
                else if (arrDts[0] == "2") // 그림방 채팅
                {
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + crenick + " )")
                        {
                            pg.recvChat = text;
                            return;
                        }
                    }  
                    pg = new Paint_Game(this, Nick_label.Text);
                    recvDt = text;
                    pg.Show();
                }
                else if (arrDts[0] == "3") // 그림 받기
                {
                    int cnt = int.Parse(arrDts[3]);
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + crenick + " )")
                        {
                            Byte[] Img = Convert.FromBase64String(arrDts[2]);
                            TypeConverter tc = TypeDescriptor.GetConverter(typeof(Bitmap));
                            Bitmap Imgbit = (Bitmap)tc.ConvertFrom(Img);
                            pg.recvPicture = Imgbit;
                            return;
                        }
                    }
                    pg = new Paint_Game(this, Nick_label.Text);
                    pg.Show();
                }
                else if (arrDts[0] == "4")// ******************************************************************************************배열 1씩 더하ㅡㄴ중
                {
                    int cnt = int.Parse(arrDts[4]);
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + crenick + " )")
                        {
                            if(arrDts[5] == "1" || arrDts[5] == "2")
                            {
                                pg.recvState = text;
                                return;
                            }
                            else
                            {
                                pg.recvStart = text;
                                return;
                            }
                        }
                    }
                    pg = new Paint_Game(this, Nick_label.Text);
                    recvDt = text;
                    pg.Show();
                }
                else if (arrDts[0] == "5")
                {
                    int cnt = int.Parse(arrDts[5]);
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + crenick + " )")
                        {
                            pg.recvCollect = text;
                            return;
                        }
                    }
                    pg = new Paint_Game(this, mainSock.LocalEndPoint.ToString());
                    recvDt = text;
                    pg.Show();
                }
                else if (arrDts[0] == "6")
                {
                    int cnt = int.Parse(arrDts[5]);
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + crenick + " )")
                        {
                            pg.recvEnd = text;
                            return;
                        }
                    }
                    pg = new Paint_Game(this, Nick_label.Text);
                    recvDt = text;
                    pg.Show();
                }
                else if (arrDts[0] == "7")
                {
                    if(arrDts[1] == "1")
                    {
                        MessageBox.Show("사용 가능한 닉네임입니다!");
                        button2.Enabled = false;
                    }
                    else
                        MessageBox.Show("이미 사용중인 닉네임입니다!");
                }
                else if (arrDts[0] == "8") // 참여
                {
                    ClientList.Items.Clear();
                    for (int i = 2; i < arrDts.Length - 1; i++)
                        ClientList.Items.Add(arrDts[i]);
                    AppendText(txtHistory, string.Format("{0}", arrDts[1]));
                }
                else if (arrDts[0] == "9") // 퇴장
                {
                    ClientList.Items.Clear();
                    for (int i = 2; i < arrDts.Length - 1; i++)
                        ClientList.Items.Add(arrDts[i]);
                    AppendText(txtHistory, string.Format("{0}", arrDts[1]));
                }
                else if (arrDts[0] == "0") // 서버 종료
                {
                    mainSock.Disconnect(true);
                    ClientList.Items.Clear();
                    AppendText(txtHistory, string.Format("서버와의 연결이 종료되었습니다."));
                    ServerConnected = false;
                    obj.ClearBuffer();
                    mainSock.Close();
                    mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                    return;
                }
            }));
            obj.ClearBuffer();
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 50000, 0, DataReceived, obj);
        }
    }
}