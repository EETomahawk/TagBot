using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PlayerIOClient;
using Message = PlayerIOClient.Message;
using Thread = System.Threading.Thread;

namespace TagBot
{
    public partial class Form1 : Form
    {
        #region Generic
        Connection con;
        const string gameID = "everybody-edits-su9rn58o40itdbnw69plyw";
        Dictionary<object, string> players = new Dictionary<object, string>();
        string[] commands = { "bot", "download", "help" };
        char[] prefixes = { '.', '!' };
        #endregion
        #region TagBot
        Dictionary<int, int> teams = new Dictionary<int, int>();
        int lastTeam;
        #endregion

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false; //Bite me
            #region Load details
            if (Properties.Settings.Default.Checkbox)
            {
                tbUsername.Text = Properties.Settings.Default.Username;
                tbPassword.Text = Properties.Settings.Default.Password;
                tbRoomID.Text = Properties.Settings.Default.RoomID;
            }
            cbSave.Checked = Properties.Settings.Default.Checkbox;
            cbTeam.SelectedIndex = Properties.Settings.Default.Team;
            tbCurse.Value = Properties.Settings.Default.Curse;
            #endregion
        }

        private void con_OnMessage(object sender, Message m)
        {
            switch (m.Type)
            {
                #region Init
                case "init":
                    #region Owner-only
                    if (!m.GetBoolean(15))
                    {
                        con.Disconnect();
                        MessageBox.Show("For this bot to work, it must be the owner of the world.\n\nDisconnected.", "TagBot");
                        return;
                    }
                    #endregion
                    con.Send("init2");
                    players.Add(m.GetInt(5), m.GetString(13));
                    lbStatus.Text = "Status: Connected";
                    #region Cookie
                    if (cbSave.Checked)
                    {
                        Properties.Settings.Default.Username = tbUsername.Text;
                        Properties.Settings.Default.Password = tbPassword.Text;
                        Properties.Settings.Default.RoomID = tbRoomID.Text;
                        Properties.Settings.Default.Save();
                    }
                    #endregion
                    break;
                #endregion
                #region Add/Left
                case "add":
                    players.Add(m[0], m.GetString(1));
                    teams.Add(m.GetInt(0), m.GetInt(16));
                    break;
                case "left":
                    players.Remove(m[0]);
                    teams.Remove(m.GetInt(0));
                    break;
                #endregion
                #region Team
                case "team":
                    lastTeam = teams[m.GetInt(0)];
                    teams[m.GetInt(0)] = m.GetInt(1);
                    if (lastTeam == cbTeam.SelectedIndex) con.Send("say", $"/reffect {players[m.GetInt(0)]} curse");
                    break;
                #endregion
                #region Say
                case "say":
                    if (prefixes.Contains(m.GetString(1)[0]))
                    {
                        switch (m.GetString(1).Substring(1).ToLower())
                        {
                            #region bot
                            case "bot":
                                con.Send("say", "This is TagBot, created by Tomahawk.");
                                Thread.Sleep(600);
                                break;
                            #endregion
                            #region download
                            case "download":
                                con.Send("say", "TagBot forum thread: https://forums.everybodyedits.com/viewtopic.php?id=42162");
                                Thread.Sleep(600);
                                break;
                            #endregion
                            #region help
                            case "help":
                                con.Send("say", $"Commands: {string.Join("  ", commands)}");
                                Thread.Sleep(600);
                                break;
                           #endregion
                        }
                    }
                    break;
                    #endregion
            }
        }
        #region Generic
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!con?.Connected ?? true)
            {
                #region Check textboxes
                if (string.IsNullOrEmpty(tbUsername.Text))
                {
                    MessageBox.Show("Please enter a username.", "Connection");
                }
                else if (string.IsNullOrEmpty(tbPassword.Text))
                {
                    MessageBox.Show("Please enter a password.", "Connection");
                }
                else if (string.IsNullOrEmpty(tbRoomID.Text))
                {
                    MessageBox.Show("Please enter a room ID.", "Connection");
                }
                #endregion
                else
                {
                    string simpleID = tbUsername.Text.Contains("@") ? tbUsername.Text : getID(tbUsername.Text);
                    if (simpleID != null)
                    {
                        PlayerIO.QuickConnect.SimpleConnect(gameID, simpleID, tbPassword.Text, null, delegate (Client c)
                        {
                            c.Multiplayer.JoinRoom(tbRoomID.Text, null, delegate (Connection rawr)
                            {
                                con = rawr;
                                con.Send("init");
                                con.OnMessage += con_OnMessage;
                                con.OnDisconnect += con_OnDisconnect;
                            },
                                delegate (PlayerIOError rip)
                                {
                                    MessageBox.Show(rip.Message, "Room connection error");
                                });
                        },
                        delegate (PlayerIOError error)
                        {
                            MessageBox.Show(error.Message, "Game connection error");
                        });
                    }
                    else MessageBox.Show("The username does not exist, or could not be retrieved.\nEnter the correct username, or use an email instead.", "Connection");
                }
            }
            else MessageBox.Show("Already connected.", "Connection");
        }
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (con?.Connected ?? false) con.Disconnect();
            else MessageBox.Show("Not connected.", "Connection");
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will delete all stored login information.\n\nContinue?", "Reset", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Properties.Settings.Default.Username = "";
                Properties.Settings.Default.Password = "";
                Properties.Settings.Default.RoomID = "";
                Properties.Settings.Default.Save();
                MessageBox.Show("Login information deleted.", "Reset");
            }
        }
        private void cbSave_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Checkbox = cbSave.Checked;
            Properties.Settings.Default.Save();
        }
        private string getID(string username)
        {
            try
            {
                return PlayerIO.QuickConnect.SimpleConnect(gameID, "guest", "guest", null).BigDB.Load("usernames", username)["owner"].ToString().Replace("simple", "");
            }
            catch { return null; }
        }
        private void con_OnDisconnect(object sender, string reason)
        {
            lbStatus.Text = "Status: Disconnected";
            players.Clear();
            teams.Clear();
        }
        #endregion
        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (con?.Connected ?? false)
            {
                #region Start
                if (btnStartStop.Text == "Start")
                {
                    #region Save settings
                    Properties.Settings.Default.Team = cbTeam.SelectedIndex;
                    Properties.Settings.Default.Curse = tbCurse.Value;
                    Properties.Settings.Default.Save();
                    #endregion
                    cbTeam.Enabled = false;
                    tbCurse.Enabled = false;
                    btnStartStop.Text = "Stop";
                    timer1.Enabled = true;
                }
                #endregion
                #region Stop
                else
                {
                    timer1.Enabled = false;
                    foreach (KeyValuePair<int, int> player in teams)
                        if (player.Value == cbTeam.SelectedIndex) con.Send("say", $"/reffect {players[player.Key]} curse");
                    cbTeam.Enabled = true;
                    tbCurse.Enabled = true;
                    btnStartStop.Text = "Start";
                }
                #endregion
            }
            else MessageBox.Show("Not connected.", "TagBot");
        }
        private void btnHelp_Click(object sender, EventArgs e)
        {
            string help = " - This bot only works when connected as the world owner.\n\n";
            help += "1. Enter the bot's username and password, and the room ID to connect to, and click \"Connect\".\n\n";
            help += "2. Once connected, choose the colour of the team that will receive the curse, and the duration of the curse.\n\n";
            help += " - The curse on the selected team will constantly be renewed.\n\n";
            help += "--> The bot currently has the following chat commands, which can start with either . or !\n\n.";
            help += string.Join("\n.", commands);
            help += "\n\nIf the players on the cursed team are dying, increase the curse duration by a second or two.";
            MessageBox.Show(help, "Instructions");
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            #region Connected
            if (con?.Connected ?? false)
            {
                foreach (KeyValuePair<int, int> player in teams)
                {
                    if (player.Value == cbTeam.SelectedIndex) con.Send("say", $"/geffect {players[player.Key]} curse {tbCurse.Value}");
                }
            }
            #endregion
            #region Disconnected
            else
            {
                timer1.Enabled = false;
                con_OnDisconnect(this, "Disconnected");
            }
            #endregion
        }
    }
}