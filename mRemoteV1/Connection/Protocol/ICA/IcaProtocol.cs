﻿using mRemoteNG.App;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Messages;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.UI.Forms;
using System;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace mRemoteNG.Connection.Protocol.ICA
{
    public class IcaProtocol : ProtocolBase
    {
        private IICAClient _icaClient;
        private ConnectionInfo _info;
        private readonly FrmMain _frmMain = FrmMain.Default;

        #region Public Methods

        public IcaProtocol()
        {
            try
            {
                _icaClient = ICAClientFactory.CreateClientInstance();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    Language.strIcaControlFailed + Environment.NewLine + ex.Message,
                                                    true);
            }
        }

        public override bool Initialize()
        {
            base.Initialize();

            try
            {
                _info = InterfaceControl.Info;
                Control = _icaClient.CreateControl();

                while (!_icaClient.Created)
                {
                    Thread.Sleep(10);
                    Application.DoEvents();
                }

                _icaClient.Address = _info.Hostname;
                SetCredentials();
                SetResolution();
                SetColors();
                SetSecurity();

                _icaClient.Initialize();

                _icaClient.PersistentCacheEnabled = _info.CacheBitmaps;
                _icaClient.Title = _info.Name;
                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    Language.strIcaSetPropsFailed + Environment.NewLine + ex.Message,
                                                    true);
                return false;
            }
        }

        public override bool Connect()
        {
            SetEventHandlers();

            try
            {
                _icaClient.Connect();
                base.Connect();
                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    Language.strIcaConnectionFailed + Environment.NewLine + ex.Message);
                return false;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void SetCredentials()
        {
            try
            {
                if (Force.HasFlag(ConnectionInfo.Force.NoCredentials))
                {
                    return;
                }

                var user = _info?.Username ?? "";
                var pass = _info?.Password ?? "";
                var dom = _info?.Domain ?? "";

                if (string.IsNullOrEmpty(user))
                {
                    if (Settings.Default.EmptyCredentials == "windows")
                    {
                        _icaClient.Username = Environment.UserName;
                    }
                    else if (Settings.Default.EmptyCredentials == "custom")
                    {
                        _icaClient.Username = Settings.Default.DefaultUsername;
                    }
                }
                else
                {
                    _icaClient.Username = user;
                }

                if (string.IsNullOrEmpty(pass))
                {
                    if (Settings.Default.EmptyCredentials == "custom")
                    {
                        if (Settings.Default.DefaultPassword != "")
                        {
                            var cryptographyProvider = new LegacyRijndaelCryptographyProvider();
                            _icaClient.SetProp("ClearPassword",
                                               cryptographyProvider.Decrypt(Settings.Default.DefaultPassword,
                                                                            Runtime.EncryptionKey));
                        }
                    }
                }
                else
                {
                    _icaClient.SetProp("ClearPassword", pass);
                }

                if (string.IsNullOrEmpty(dom))
                {
                    if (Settings.Default.EmptyCredentials == "windows")
                    {
                        _icaClient.Domain = Environment.UserDomainName;
                    }
                    else if (Settings.Default.EmptyCredentials == "custom")
                    {
                        _icaClient.Domain = Settings.Default.DefaultDomain;
                    }
                }
                else
                {
                    _icaClient.Domain = dom;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    Language.strIcaSetCredentialsFailed + Environment.NewLine +
                                                    ex.Message, true);
            }
        }

        private void SetResolution()
        {
            try
            {
                if (Force.HasFlag(ConnectionInfo.Force.Fullscreen))
                {
                    _icaClient.SetWindowSize(Screen.FromControl(_frmMain).Bounds.Width, Screen.FromControl(_frmMain).Bounds.Height);
                    _icaClient.FullScreenWindow();

                    return;
                }

                if (InterfaceControl.Info.Resolution == RDPResolutions.FitToWindow)
                {
                    _icaClient.SetWindowSize(InterfaceControl.Size.Width, InterfaceControl.Size.Height);
                }
                else if (InterfaceControl.Info.Resolution == RDPResolutions.SmartSize)
                {
                    _icaClient.SetWindowSize(InterfaceControl.Size.Width, InterfaceControl.Size.Height);
                }
                else if (InterfaceControl.Info.Resolution == RDPResolutions.Fullscreen)
                {
                    _icaClient.SetWindowSize(Screen.FromControl(_frmMain).Bounds.Width, Screen.FromControl(_frmMain).Bounds.Height);
                    _icaClient.FullScreenWindow();
                }
                else
                {
                    var resolution = _info.Resolution.GetResolutionRectangle();
                    _icaClient.SetWindowSize(resolution.Width, resolution.Height);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    Language.strIcaSetResolutionFailed + Environment.NewLine +
                                                    ex.Message, true);
            }
        }

        private void SetColors()
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (_info.Colors)
            {
                case RDPColors.Colors256:
                    _icaClient.SetProp("DesiredColor", "2");
                    break;

                case RDPColors.Colors15Bit:
                    _icaClient.SetProp("DesiredColor", "4");
                    break;

                case RDPColors.Colors16Bit:
                    _icaClient.SetProp("DesiredColor", "4");
                    break;

                default:
                    _icaClient.SetProp("DesiredColor", "8");
                    break;
            }
        }

        private void SetSecurity()
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (_info.ICAEncryptionStrength)
            {
                case EncryptionStrength.Encr128BitLogonOnly:
                    _icaClient.Encrypt = true;
                    _icaClient.EncryptionLevelSession = "EncRC5-0";
                    break;

                case EncryptionStrength.Encr40Bit:
                    _icaClient.Encrypt = true;
                    _icaClient.EncryptionLevelSession = "EncRC5-40";
                    break;

                case EncryptionStrength.Encr56Bit:
                    _icaClient.Encrypt = true;
                    _icaClient.EncryptionLevelSession = "EncRC5-56";
                    break;

                case EncryptionStrength.Encr128Bit:
                    _icaClient.Encrypt = true;
                    _icaClient.EncryptionLevelSession = "EncRC5-128";
                    break;
            }
        }

        private void SetEventHandlers()
        {
            try
            {
                _icaClient.OnConnecting += ICAEvent_OnConnecting;
                _icaClient.OnConnect += ICAEvent_OnConnected;
                _icaClient.OnConnectFailed += ICAEvent_OnConnectFailed;
                _icaClient.OnDisconnect += ICAEvent_OnDisconnect;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    Language.strIcaSetEventHandlersFailed + Environment.NewLine +
                                                    ex.Message, true);
            }
        }

        #endregion Private Methods

        #region Private Events & Handlers

        private void ICAEvent_OnConnecting(object sender, EventArgs e)
        {
            Event_Connecting(this);
        }

        private void ICAEvent_OnConnected(object sender, EventArgs e)
        {
            Event_Connected(this);
        }

        private void ICAEvent_OnConnectFailed(object sender, EventArgs e)
        {
            Event_ErrorOccured(this, e.ToString(), null);
        }

        private void ICAEvent_OnDisconnect(object sender, EventArgs e)
        {
            Event_Disconnected(this, e.ToString(), null);

            if (Settings.Default.ReconnectOnDisconnect)
            {
                ReconnectGroup = new ReconnectGroup();
                //this.Load += ReconnectGroup_Load;
                ReconnectGroup.Left = (int)(((double)Control.Width / 2) - ((double)ReconnectGroup.Width / 2));
                ReconnectGroup.Top = (int)(((double)Control.Height / 2) - ((double)ReconnectGroup.Height / 2));
                ReconnectGroup.Parent = Control;
                ReconnectGroup.Show();
                tmrReconnect.Enabled = true;
            }
            else
            {
                Close();
            }
        }

        #endregion Private Events & Handlers

        #region Reconnect Stuff

        public void tmrReconnect_Elapsed(object sender, ElapsedEventArgs e)
        {
            var srvReady = PortScanner.IsPortOpen(_info.Hostname, Convert.ToString(_info.Port));

            ReconnectGroup.ServerReady = srvReady;

            if (!ReconnectGroup.ReconnectWhenReady || !srvReady) return;
            tmrReconnect.Enabled = false;
            ReconnectGroup.DisposeReconnectGroup();
            _icaClient.Connect();
        }

        #endregion Reconnect Stuff

        #region Enums

        public enum Defaults
        {
            Port = 1494,
            EncryptionStrength = 0
        }

        public enum EncryptionStrength
        {
            [LocalizedAttributes.LocalizedDescription(nameof(Language.strEncBasic))]
            EncrBasic = 1,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.strEnc128BitLogonOnly))]
            Encr128BitLogonOnly = 127,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.strEnc40Bit))]
            Encr40Bit = 40,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.strEnc56Bit))]
            Encr56Bit = 56,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.strEnc128Bit))]
            Encr128Bit = 128
        }

        #endregion Enums
    }
}