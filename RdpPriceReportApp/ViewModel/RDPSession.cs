﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RdpRealTimePricing.Events;
using Refinitiv.DataPlatform;
using Refinitiv.DataPlatform.Core;

namespace RdpPriceReportApp.ViewModel
{
    public class RdpSession
    {

        #region TREPCredential
        public  string TrepUsername = "";
        public string TrepAppid = "256";
        public string TrepPosition = "localhost/net";
        public string WebSocketHost = "";
        #endregion

        #region RDPUserCredential
        public string RdpUser = "";
        public string RdpPassword = "";
        public string RdpAppKey = "";
        //214d5f8636494afb9142b6e05188cbc5dd03c18f
        #endregion

        private ISession _session;
        internal ISession ServiceSession => _session ?? throw new Exception("Please call InitWebSocketConnectionAsync to initialize RdpSession ");
        //IStream stream;
        public Session.State SessionState { get; internal set; }
        public bool IsLoggedIn { get; set; }

        public RdpSession()
        {
            this.IsLoggedIn = false;
            this._session = null;
            SessionState = Session.State.Closed;
        }

        public void CloseSession()
        {
            OnStateChangedEvents = null;
            OnSessionEvents = null;
            _session.Close();
            SessionState = Session.State.Closed;
            
            _session = null;
        }
        public Task InitWebSocketConnectionAsync(bool useRdp)
        {
            return Task.Run(() =>
            {
                Log.Level = NLog.LogLevel.Off;

                if (!useRdp)
                {
                    _session = CoreFactory.CreateSession(new DeployedPlatformSession.Params()
                        .Host(WebSocketHost)
                        .WithDacsUserName(TrepUsername)
                        .WithDacsApplicationID(TrepAppid)
                        .WithDacsPosition(TrepPosition)
                        .OnState(ProcessOnState)
                        .OnEvent(ProcessOnEvent));
                }
                else
                {
                    System.Console.WriteLine("Start RDP PlatformSession");
                    _session = CoreFactory.CreateSession(new PlatformSession.Params()
                        .WithOAuthGrantType(new GrantPassword().UserName(RdpUser)
                            .Password(RdpPassword))
                        .AppKey(RdpAppKey)
                        .WithTakeSignonControl(true)
                        .OnState(ProcessOnState)
                        .OnEvent(ProcessOnEvent));
                }
                _session.OpenAsync().ConfigureAwait(false);

            });
        }
        private void ProcessOnEvent(ISession session, Session.EventCode code, JObject message)
        {
            RaiseSessionEvent(code, message);
        }
        private void ProcessOnState(ISession session, Session.State state, string message)
        {
            SessionState = state;
            RaiseStateChanged(state, message);
        }
        public event EventHandler<OnStateChangedEventArgs> OnStateChangedEvents;
        public event EventHandler<OnSessionEventArgs> OnSessionEvents;

        protected void RaiseStateChanged(Session.State state, string message)
        {
            var sessionState = new OnStateChangedEventArgs() { State = state, Message = message };
            OnStateChanged(sessionState);
        }
        protected virtual void OnStateChanged(OnStateChangedEventArgs e)
        {
            var handler = OnStateChangedEvents;
            handler?.Invoke(this, e);
        }
        protected void RaiseSessionEvent(Session.EventCode code, JObject message)
        {
            var connectionState = new OnSessionEventArgs() { EventCode = code, Message = message };
            OnSessionEvent(connectionState);
        }
        protected virtual void OnSessionEvent(OnSessionEventArgs e)
        {
            var handler = OnSessionEvents;
            handler?.Invoke(this, e);
        }
        
       
    }
}