﻿using Door_of_Soul.Client.TestEnvironment;
using Door_of_Soul.Client.TestEnvironment.SimpleOperations;
using Door_of_Soul.Communication.Client;
using Door_of_Soul.Core;
using Door_of_Soul.Core.Client;
using ExitGames.Logging;
using ExitGames.Logging.Log4Net;
using log4net.Config;
using Photon.SocketServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Door_of_Soul.Client.Test.PhotonServer
{
    class PhotonServerClientTestEnvironment : ClientTestEnvironment
    {
        private static Random randomGenerator = new Random();
        private static List<PhotonServerTestPeer> peers = new List<PhotonServerTestPeer>();
        public static IEnumerable<PhotonServerTestPeer> AllPeers { get { return peers.ToArray(); } }
        public static PhotonServerTestPeer RandomPeer
        {
            get
            {
                return peers[randomGenerator.Next(peers.Count)];
            }
        }

        public override bool SetupCommunication(out string errorMessage)
        {
            CommunicationService.Initialize(new PhotonServerClientCommunicationService());
            for (int i = 0; i < TestEnvironmentConfiguration.Instance.TotalLoginServerConnectionCount; i++)
            {
                PhotonServerTestPeer peer = new PhotonServerTestPeer(ApplicationBase.Instance);
                peers.Add(peer);
                if(!peer.ConnectTcp(
                    remoteEndPoint: new IPEndPoint(IPAddress.Parse(TestEnvironmentConfiguration.Instance.LoginServerAddresses[i]), TestEnvironmentConfiguration.Instance.LoginServerPorts[i]),
                    applicationName: TestEnvironmentConfiguration.Instance.LoginServerApplicationNames[i]))
                {
                    errorMessage = $"Connect {TestEnvironmentConfiguration.Instance.LoginServerApplicationNames[i]} Failed";
                    return false;
                }
            }
            errorMessage = "";
            return true;
        }

        public override bool SetupConfiguration(out string errorMessage)
        {
            TestEnvironmentConfiguration testEnvironmentConfiguration;
            if (GenericConfigurationLoader<TestEnvironmentConfiguration>.Load(Path.Combine(ApplicationBase.Instance.ApplicationPath, "config", "ServerEnvironment.config"), out testEnvironmentConfiguration))
            {
                TestEnvironmentConfiguration.Initialize(testEnvironmentConfiguration);
                errorMessage = $"TestEnvironmentConfiguration IsTurnOn:{TestEnvironmentConfiguration.Instance.IsTurnOn}";
                return TestEnvironmentConfiguration.Instance.IsTurnOn;
            }
            else
            {
                errorMessage = "TestEnvironmentConfiguration Load Failed";
                return false;
            }
        }

        public override bool SetupLog(out string errorMessage)
        {
            log4net.GlobalContext.Properties["Photon:ApplicationLogPath"] = Path.Combine(ApplicationBase.Instance.ApplicationPath, "log");
            FileInfo file = new FileInfo(Path.Combine(ApplicationBase.Instance.BinaryPath, "log4net.config"));
            if (file.Exists)
            {
                LogManager.SetLoggerFactory(Log4NetLoggerFactory.Instance);
                XmlConfigurator.ConfigureAndWatch(file);
            }
            else
            {
                errorMessage = "Setup Log Failed";
                return false;
            }

            errorMessage = "";
            return true;
        }

        public override void TearDown()
        {
            foreach(var peer in peers)
            {
                peer.Disconnect();
            }
            peers.Clear();
        }

        public override bool SetupEnvironment(out string errorMessage)
        {
            VirtualSystem.Initialize(new ClientSystem());
            ScenariosPool.Initialize(new RegisterScenariosPool());
            errorMessage = "";
            return true;
        }
    }
}
