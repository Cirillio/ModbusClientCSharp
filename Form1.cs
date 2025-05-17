using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NModbus;

namespace ModbusClient
{
    public partial class Form1 : Form
    {
        private Button  sendButton;
        private Label connectionStatus;
        private TcpClient client;
        private IModbusMaster master;
        private Thread listenThread;
        private TableLayoutPanel rootPanel;
        private Panel header;
        private Label headerLabel;
        private TableLayoutPanel mainPanel;
        private TableLayoutPanel leftPanel;
        private Label connLabel;
      
        private TextBox portTextBox;
        private TableLayoutPanel tableLayoutPanel1;
        private Button okButton;
        private TextBox ipTextBox;
        private Button connectButton;
        private TableLayoutPanel tableLayoutPanel2;
        private Label connectionStatusLabel;
        private ListBox errorListBox;
        private Button buttonClearLogs;
        private TableLayoutPanel tableLayoutPanel3;
        private TableLayoutPanel inputPanel;
        private TextBox messageBox;
        private Panel panel1;
        private ListBox chatBox;
        private Button clearButton;
        private volatile bool isListening = false;


        public Form1()
        {
            InitializeComponent();
            InitHandlers();
        }

        private void Connect()
        {
            try
            {

                client = new TcpClient(ipTextBox.Text, int.Parse(portTextBox.Text));
                master = new ModbusFactory().CreateMaster(client);
                connectionStatus.Text = "Connected";
                connectionStatus.BackColor = Color.Green;
                connectionStatus.ForeColor = Color.White;
                isListening = true;
                listenThread = new Thread(new ThreadStart(() => ListenLoop(master)));
                listenThread.IsBackground = true;
                listenThread.Start();

            }
            catch (Exception ex)
            {
                connectionStatus.Text = "Error";
                connectionStatus.BackColor = Color.Red;
                connectionStatus.ForeColor = Color.White;
                errorListBox.Items.Add(ex.Message);
            }
        }

        private void InitHandlers() {
            connectButton.Click += new EventHandler((s, e) => ToggleConnection());
            buttonClearLogs.Click += (s, e) => errorListBox.Items.Clear();
            messageBox.KeyDown += new KeyEventHandler(MessageBox_KeyDown);
            okButton.Click += new EventHandler((s, e) => Connect());
            clearButton.Click += new EventHandler((s, e) => { ipTextBox.Text = ""; portTextBox.Text = ""; });
            sendButton.Click += new EventHandler((s, e) => SendMessage());

        }


        private void ToggleConnection()
        {
            if (client != null && client.Connected)
            {
                isListening = false; // ⬅ остановка потока
                Thread.Sleep(200);   // ⬅ дайте потоку завершиться
                client.Close();
                connectionStatus.Text = "Disconnected";
                connectionStatus.ForeColor = Color.Black;
                connectionStatus.BackColor = Color.Silver;
            }
            else
            {
                Connect();
            }
        }

        private void SendMessage()
        {
            try
            {
                if (master == null) return;
                string msg = messageBox.Text;
                ushort[] data = Encode(msg);
                master.WriteMultipleRegisters(1, 0, data);
                master.WriteSingleRegister(1, 100, 1);
                chatBox.Items.Add("[Client " + DateTime.Now.ToLongTimeString() + "]: " + msg);
                messageBox.Clear();
                Thread.Sleep(500);
                master.WriteSingleRegister(1, 100, 0);
            }
            catch (Exception ex)
            {
                errorListBox.Items.Add(ex.Message);
            }
        }

        private void ListenLoop(IModbusMaster master)
        {
            while (isListening)
            {
                try
                {
                    ushort[] flag = master.ReadHoldingRegisters(1, 300, 1);
                    if (flag[0] == 1)
                    {
                        ushort[] buf = master.ReadHoldingRegisters(1, 200, 50);
                        string msg = Decode(buf);
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                chatBox.Items.Add("[Server " + DateTime.Now.ToLongTimeString() + "]: " + msg);
                            });
                            master.WriteMultipleRegisters(1, 200, new ushort[50]);
                            master.WriteSingleRegister(1, 300, 0);
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // просто выходим молча, если объект уже уничтожен
                    break;
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        errorListBox.Items.Add(ex.Message);
                    });
                }

                Thread.Sleep(100);
            }

        }

        private static ushort[] Encode(string message)
        {
            var bytes = Encoding.ASCII.GetBytes(message);
            int len = (bytes.Length + 1) / 2;
            var result = new ushort[len];
            for (int i = 0; i < bytes.Length; i++)
            {
                int index = i / 2;
                if (i % 2 == 0)
                    result[index] = bytes[i];
                else
                    result[index] |= (ushort)(bytes[i] << 8);
            }
            return result;
        }

        private static string Decode(ushort[] data)
        {
            byte[] bytes = new byte[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                bytes[i * 2] = (byte)(data[i] & 0xFF);
                bytes[i * 2 + 1] = (byte)(data[i] >> 8);
            }
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        }

        private void MessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // отменить звуковой "бип"
                SendMessage();
            }
        }

        private void InitializeComponent()
        {
            this.rootPanel = new System.Windows.Forms.TableLayoutPanel();
            this.header = new System.Windows.Forms.Panel();
            this.headerLabel = new System.Windows.Forms.Label();
            this.mainPanel = new System.Windows.Forms.TableLayoutPanel();
            this.leftPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ipTextBox = new System.Windows.Forms.TextBox();
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.connLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.clearButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.connectButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.connectionStatus = new System.Windows.Forms.Label();
            this.connectionStatusLabel = new System.Windows.Forms.Label();
            this.errorListBox = new System.Windows.Forms.ListBox();
            this.buttonClearLogs = new System.Windows.Forms.Button();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.inputPanel = new System.Windows.Forms.TableLayoutPanel();
            this.sendButton = new System.Windows.Forms.Button();
            this.messageBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.chatBox = new System.Windows.Forms.ListBox();
            this.rootPanel.SuspendLayout();
            this.header.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.inputPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootPanel
            // 
            this.rootPanel.ColumnCount = 1;
            this.rootPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootPanel.Controls.Add(this.header, 0, 0);
            this.rootPanel.Controls.Add(this.mainPanel, 0, 1);
            this.rootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootPanel.Location = new System.Drawing.Point(0, 0);
            this.rootPanel.Name = "rootPanel";
            this.rootPanel.RowCount = 2;
            this.rootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.rootPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootPanel.Size = new System.Drawing.Size(944, 501);
            this.rootPanel.TabIndex = 0;
            // 
            // header
            // 
            this.header.BackColor = System.Drawing.SystemColors.ControlLight;
            this.header.Controls.Add(this.headerLabel);
            this.header.Dock = System.Windows.Forms.DockStyle.Fill;
            this.header.Location = new System.Drawing.Point(3, 3);
            this.header.Name = "header";
            this.header.Size = new System.Drawing.Size(938, 54);
            this.header.TabIndex = 0;
            // 
            // headerLabel
            // 
            this.headerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.headerLabel.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.headerLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.headerLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.headerLabel.Font = new System.Drawing.Font("Bahnschrift Condensed", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.headerLabel.ForeColor = System.Drawing.SystemColors.InfoText;
            this.headerLabel.Location = new System.Drawing.Point(0, 0);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Padding = new System.Windows.Forms.Padding(10);
            this.headerLabel.Size = new System.Drawing.Size(938, 54);
            this.headerLabel.TabIndex = 0;
            this.headerLabel.Text = "Modbus Client";
            this.headerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // mainPanel
            // 
            this.mainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.mainPanel.BackColor = System.Drawing.SystemColors.ControlLight;
            this.mainPanel.ColumnCount = 2;
            this.mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainPanel.Controls.Add(this.leftPanel, 0, 0);
            this.mainPanel.Controls.Add(this.tableLayoutPanel3, 1, 0);
            this.mainPanel.Location = new System.Drawing.Point(3, 63);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.RowCount = 1;
            this.mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainPanel.Size = new System.Drawing.Size(938, 435);
            this.mainPanel.TabIndex = 1;
            // 
            // leftPanel
            // 
            this.leftPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.leftPanel.AutoSize = true;
            this.leftPanel.BackColor = System.Drawing.SystemColors.ControlLight;
            this.leftPanel.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.InsetDouble;
            this.leftPanel.ColumnCount = 1;
            this.leftPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftPanel.Controls.Add(this.ipTextBox, 0, 1);
            this.leftPanel.Controls.Add(this.portTextBox, 0, 2);
            this.leftPanel.Controls.Add(this.connLabel, 0, 0);
            this.leftPanel.Controls.Add(this.tableLayoutPanel1, 0, 3);
            this.leftPanel.Controls.Add(this.connectButton, 0, 4);
            this.leftPanel.Controls.Add(this.tableLayoutPanel2, 0, 5);
            this.leftPanel.Controls.Add(this.errorListBox, 0, 6);
            this.leftPanel.Controls.Add(this.buttonClearLogs, 0, 7);
            this.leftPanel.Location = new System.Drawing.Point(3, 3);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Padding = new System.Windows.Forms.Padding(10);
            this.leftPanel.RowCount = 8;
            this.leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftPanel.Size = new System.Drawing.Size(181, 374);
            this.leftPanel.TabIndex = 0;
            // 
            // ipTextBox
            // 
            this.ipTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ipTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ipTextBox.Font = new System.Drawing.Font("Bahnschrift Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.ipTextBox.Location = new System.Drawing.Point(16, 48);
            this.ipTextBox.MaxLength = 16;
            this.ipTextBox.Name = "ipTextBox";
            this.ipTextBox.Size = new System.Drawing.Size(149, 27);
            this.ipTextBox.TabIndex = 4;
            this.ipTextBox.Text = "127.0.0.1";
            this.ipTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // portTextBox
            // 
            this.portTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.portTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.portTextBox.Font = new System.Drawing.Font("Bahnschrift Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.portTextBox.Location = new System.Drawing.Point(16, 84);
            this.portTextBox.MaxLength = 5;
            this.portTextBox.Name = "portTextBox";
            this.portTextBox.Size = new System.Drawing.Size(149, 27);
            this.portTextBox.TabIndex = 2;
            this.portTextBox.Text = "502";
            this.portTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // connLabel
            // 
            this.connLabel.AutoSize = true;
            this.connLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.connLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.connLabel.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.connLabel.Font = new System.Drawing.Font("Bahnschrift Condensed", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.connLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.connLabel.Location = new System.Drawing.Point(16, 13);
            this.connLabel.Name = "connLabel";
            this.connLabel.Size = new System.Drawing.Size(149, 29);
            this.connLabel.TabIndex = 0;
            this.connLabel.Text = "Connection";
            this.connLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.clearButton, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.okButton, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(16, 120);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(149, 40);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // clearButton
            // 
            this.clearButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.clearButton.AutoSize = true;
            this.clearButton.FlatAppearance.BorderSize = 0;
            this.clearButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.clearButton.Font = new System.Drawing.Font("Bahnschrift Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.clearButton.Location = new System.Drawing.Point(91, 5);
            this.clearButton.MaximumSize = new System.Drawing.Size(50, 0);
            this.clearButton.MinimumSize = new System.Drawing.Size(40, 0);
            this.clearButton.Name = "clearButton";
            this.clearButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.clearButton.Size = new System.Drawing.Size(40, 29);
            this.clearButton.TabIndex = 1;
            this.clearButton.Text = "X";
            this.clearButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.okButton.AutoSize = true;
            this.okButton.FlatAppearance.BorderSize = 0;
            this.okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.okButton.Font = new System.Drawing.Font("Bahnschrift Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.okButton.Location = new System.Drawing.Point(17, 5);
            this.okButton.MaximumSize = new System.Drawing.Size(50, 0);
            this.okButton.MinimumSize = new System.Drawing.Size(40, 0);
            this.okButton.Name = "okButton";
            this.okButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.okButton.Size = new System.Drawing.Size(40, 29);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // connectButton
            // 
            this.connectButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.connectButton.AutoSize = true;
            this.connectButton.Font = new System.Drawing.Font("Bahnschrift Condensed", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.connectButton.Location = new System.Drawing.Point(51, 169);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(79, 40);
            this.connectButton.TabIndex = 5;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel2.Controls.Add(this.connectionStatus, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.connectionStatusLabel, 0, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(23, 225);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(10);
            this.tableLayoutPanel2.MinimumSize = new System.Drawing.Size(0, 20);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(135, 20);
            this.tableLayoutPanel2.TabIndex = 6;
            // 
            // connectionStatus
            // 
            this.connectionStatus.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.connectionStatus.AutoSize = true;
            this.connectionStatus.BackColor = System.Drawing.Color.Silver;
            this.connectionStatus.Font = new System.Drawing.Font("Bahnschrift SemiLight SemiConde", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.connectionStatus.Location = new System.Drawing.Point(55, 1);
            this.connectionStatus.Margin = new System.Windows.Forms.Padding(0);
            this.connectionStatus.Name = "connectionStatus";
            this.connectionStatus.Size = new System.Drawing.Size(79, 17);
            this.connectionStatus.TabIndex = 1;
            this.connectionStatus.Text = "disconnected";
            this.connectionStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // connectionStatusLabel
            // 
            this.connectionStatusLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.connectionStatusLabel.AutoSize = true;
            this.connectionStatusLabel.BackColor = System.Drawing.Color.Transparent;
            this.connectionStatusLabel.Font = new System.Drawing.Font("Bahnschrift Condensed", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.connectionStatusLabel.Location = new System.Drawing.Point(3, 0);
            this.connectionStatusLabel.Margin = new System.Windows.Forms.Padding(0);
            this.connectionStatusLabel.Name = "connectionStatusLabel";
            this.connectionStatusLabel.Size = new System.Drawing.Size(47, 19);
            this.connectionStatusLabel.TabIndex = 0;
            this.connectionStatusLabel.Text = "Status:";
            this.connectionStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // errorListBox
            // 
            this.errorListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.errorListBox.Font = new System.Drawing.Font("Bahnschrift Light Condensed", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.errorListBox.FormattingEnabled = true;
            this.errorListBox.HorizontalScrollbar = true;
            this.errorListBox.IntegralHeight = false;
            this.errorListBox.Location = new System.Drawing.Point(16, 261);
            this.errorListBox.MaximumSize = new System.Drawing.Size(0, 180);
            this.errorListBox.MinimumSize = new System.Drawing.Size(0, 60);
            this.errorListBox.Name = "errorListBox";
            this.errorListBox.Size = new System.Drawing.Size(149, 60);
            this.errorListBox.TabIndex = 7;
            // 
            // buttonClearLogs
            // 
            this.buttonClearLogs.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.buttonClearLogs.AutoSize = true;
            this.buttonClearLogs.BackColor = System.Drawing.Color.Transparent;
            this.buttonClearLogs.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonClearLogs.FlatAppearance.BorderColor = System.Drawing.SystemColors.ButtonHighlight;
            this.buttonClearLogs.Font = new System.Drawing.Font("Bahnschrift", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonClearLogs.Location = new System.Drawing.Point(60, 331);
            this.buttonClearLogs.Margin = new System.Windows.Forms.Padding(4);
            this.buttonClearLogs.MaximumSize = new System.Drawing.Size(60, 32);
            this.buttonClearLogs.MinimumSize = new System.Drawing.Size(60, 0);
            this.buttonClearLogs.Name = "buttonClearLogs";
            this.buttonClearLogs.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.buttonClearLogs.Size = new System.Drawing.Size(60, 26);
            this.buttonClearLogs.TabIndex = 8;
            this.buttonClearLogs.Text = "Clear";
            this.buttonClearLogs.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.inputPanel, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(190, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(745, 429);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // inputPanel
            // 
            this.inputPanel.BackColor = System.Drawing.SystemColors.ControlLight;
            this.inputPanel.ColumnCount = 2;
            this.inputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.inputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.inputPanel.Controls.Add(this.sendButton, 1, 0);
            this.inputPanel.Controls.Add(this.messageBox, 0, 0);
            this.inputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inputPanel.Location = new System.Drawing.Point(3, 362);
            this.inputPanel.Name = "inputPanel";
            this.inputPanel.RowCount = 1;
            this.inputPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.inputPanel.Size = new System.Drawing.Size(739, 64);
            this.inputPanel.TabIndex = 0;
            // 
            // sendButton
            // 
            this.sendButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.sendButton.Font = new System.Drawing.Font("Bahnschrift", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.sendButton.Location = new System.Drawing.Point(649, 7);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(80, 50);
            this.sendButton.TabIndex = 0;
            this.sendButton.Text = "Send";
            this.sendButton.UseVisualStyleBackColor = true;
            // 
            // messageBox
            // 
            this.messageBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.messageBox.Font = new System.Drawing.Font("Bahnschrift", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.messageBox.Location = new System.Drawing.Point(10, 3);
            this.messageBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.messageBox.MaxLength = 240;
            this.messageBox.MinimumSize = new System.Drawing.Size(0, 60);
            this.messageBox.Multiline = true;
            this.messageBox.Name = "messageBox";
            this.messageBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.messageBox.Size = new System.Drawing.Size(619, 60);
            this.messageBox.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Window;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.chatBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(739, 353);
            this.panel1.TabIndex = 1;
            // 
            // chatBox
            // 
            this.chatBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.chatBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chatBox.Font = new System.Drawing.Font("Bahnschrift Light", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chatBox.FormattingEnabled = true;
            this.chatBox.HorizontalScrollbar = true;
            this.chatBox.ItemHeight = 19;
            this.chatBox.Location = new System.Drawing.Point(10, 10);
            this.chatBox.Margin = new System.Windows.Forms.Padding(10);
            this.chatBox.Name = "chatBox";
            this.chatBox.Size = new System.Drawing.Size(715, 329);
            this.chatBox.TabIndex = 0;
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(944, 501);
            this.Controls.Add(this.rootPanel);
            this.MinimumSize = new System.Drawing.Size(960, 540);
            this.Name = "Form1";
            this.Text = "Modbus client";
            this.rootPanel.ResumeLayout(false);
            this.header.ResumeLayout(false);
            this.mainPanel.ResumeLayout(false);
            this.mainPanel.PerformLayout();
            this.leftPanel.ResumeLayout(false);
            this.leftPanel.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.inputPanel.ResumeLayout(false);
            this.inputPanel.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

    }
}
