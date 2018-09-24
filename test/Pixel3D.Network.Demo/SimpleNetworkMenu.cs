using System;
using System.Net;
using Common.GlobalInput;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Pixel3D.Network.Common;
using Pixel3D.Network.Rollback;
using Pixel3D.P2P;

namespace Pixel3D.Network.Demo
{
    public class SimpleNetworkMenu
    {
        SimpleConsole console;
        Func<BadNetworkSimulation, P2PNetwork> createNetwork;
        Func<RollbackDriver> _createRollbackDriverAndGame;

        public string commandLineHost;
        public byte[] localPlayerData;
        

        P2PNetwork network = null;
        RollbackDriver rollbackDriver = null;

        bool useSideChannelAuth;
        bool tokenIssued;
        int sideChannelToken = 0; // <- Right now, the "side channel" for testing is "open in the debugger and sync this value by hand..."


        public SimpleNetworkMenu(string commandLineHost, SimpleConsole console, Func<BadNetworkSimulation, P2PNetwork> createNetwork, Func<RollbackDriver> createRollbackDriverAndGame)
        {
            this.commandLineHost = commandLineHost;
            this.console = console;
            this.createNetwork = createNetwork;
            this._createRollbackDriverAndGame = createRollbackDriverAndGame;
        }


        int badNetworkSimPreset = 0;


        private RollbackDriver CreateRollbackDriverAndGame()
        {
            rollbackDriver = _createRollbackDriverAndGame();
            return rollbackDriver;
        }


        private void ExternalEndpointDiscovered(IPEndPoint endpoint)
        {
            network.Log("Discovered external endpoint: " + endpoint.ToString());
        }


        public void Update()
        {
            if(network == null)
            {
                //
                // Before Network Startup
                //

                if(Input.KeyWentDown(Keys.F3)) // Simulate bad network conditions
                {
                    badNetworkSimPreset = BadNetworkSimulation.NextPreset(badNetworkSimPreset);
                    console.WriteLine(BadNetworkSimulation.GetPreset(badNetworkSimPreset).ToString());
                }

                if(Input.KeyWentDown(Keys.F1)) // Start Network
                {
                    network = createNetwork(BadNetworkSimulation.GetPreset(badNetworkSimPreset));
                    network.OnExternalEndpointDiscovered += ExternalEndpointDiscovered;
                    network.StartDiscovery();
                }
            }
            else
            {
                //
                // After Network Started
                //

                if(Input.Control && Input.KeyWentDown(Keys.S))
                    network.RequestExternalEndpoint();

                if(rollbackDriver == null)
                {
                    //
                    // Before Game Start/Connect
                    //

                    if(Input.KeyWentDown(Keys.F4))
                        useSideChannelAuth = !useSideChannelAuth;

                    bool internet = false;
                    if(Input.KeyWentDown(Keys.F1) || (internet = Input.KeyWentDown(Keys.F2))) // Start Internet or LAN game
                    {
                        network.StopDiscovery();

                        console.WriteLine(internet ? "Starting Internet Game..." : "Starting LAN Game...", Color.DarkRed);

                        network.StartGame(CreateRollbackDriverAndGame(), "Player", localPlayerData, "Default Game", internet, useSideChannelAuth, 0);

                        if(!network.UsingKnownPort)
                            console.WriteLine("To connect to this game on LAN, connect to: " + network.LocalPeerInfo.InternalEndPoint, Color.Red);
                    }

                    // Hard coded for this test...
                    const ulong sideChannelId = 1;

                    // Endpoint to connect to for game client:
                    IPEndPoint clientConnectTo = null;

                    if(commandLineHost != null && Input.KeyWentDown(Keys.F3))
                    {
                        try { clientConnectTo = network.ParseAndResolveEndpoint(commandLineHost); }
                        catch { clientConnectTo = null; }

                        if(clientConnectTo == null)
                        {
                            console.WriteLine("Failed to parse or resolve endpoint: \"" + commandLineHost + "\"");
                            commandLineHost = null;
                        }
                    }

                    if(network.Discovery != null)
                    {
                        for(int i = 0; i < network.Discovery.Items.Count; i++) // Join Discovered Game
                        {
                            if(Input.KeyWentDown(Keys.D0 + i) && network.Discovery.Items[i].CanJoin)
                            {
                                DiscoveredGame discoveredGame = network.Discovery.Items[i];
                                clientConnectTo = discoveredGame.EndPoint;
                                console.WriteLine("Selected discovererd game: " + discoveredGame.GameInfo.Name, Color.DarkRed);
                                break;
                            }
                        }
                    }

                    if(clientConnectTo != null)
                    {
                        network.StopDiscovery();
                        console.WriteLine("Connecting to " + clientConnectTo, Color.DarkRed);
                        network.ConnectToGame(CreateRollbackDriverAndGame(), "Player", localPlayerData,
                                useSideChannelAuth ? null : clientConnectTo, sideChannelId, sideChannelToken);
                        if(useSideChannelAuth)
                            network.SideChannelTryVerifyAndConnect(clientConnectTo); // <- Use the verify path if we're using side-channel auth
                    }
                }
                else
                {
                    //
                    // After game start:
                    //

                    if(useSideChannelAuth && network.IsServer)
                    {
                        if(Input.KeyWentDown(Keys.F8))
                        {
                            if(!tokenIssued)
                            {
                                tokenIssued = true;
                                sideChannelToken = network.SideChannelAdd(1);
                                console.WriteLine("Token issued: " + sideChannelToken);
                            }
                            else
                            {
                                tokenIssued = false;
                                network.SideChannelRemove(1); // Kick any user connected to the network with this ID
                                console.WriteLine("Token revoked");
                            }
                        }
                    }
                }
            }
        }


        public void UpdateRollbackDriverHotkeys()
        {
            if(rollbackDriver != null)
            {
                if(Input.Shift && Input.KeyWentDown(Keys.F10))
                    rollbackDriver.debugDisableInputBroadcast = true;


                for(int i = 0; i < MultiInputState.Count; i++)
                {
                    if(Input.Alt && Input.KeyWentDown(Keys.D1 + i))
                    {
                        rollbackDriver.LocalInputSourceIndex = i;
                    }
                }
                
                if(Input.KeyWentDown(Keys.OemPlus))
                    rollbackDriver.LocalFrameDelay++;
                if(Input.KeyWentDown(Keys.OemMinus))
                    rollbackDriver.LocalFrameDelay--;
            }
        }



        public void Draw(DisplayText dt)
        {
            dt.Begin(new Vector2(50, 50));

            if(network == null)
            {
                // Before Network Startup
                dt.WriteLine("[F1] = Begin Network");
                dt.WriteLine();
                dt.WriteLine("[F3] = Cycle bad network simulation");
                dt.WriteLine();
                dt.WriteLine();
                dt.WriteLine();
                dt.WriteLine("After starting:", Color.DarkGray);
                dt.WriteLine("---------------", Color.DarkGray);
                dt.WriteLine("[F12] = Debug output", Color.DarkGray);
                dt.WriteLine("[-/+] = Adjust local input delay", Color.DarkGray);
            }
            else
            {
                // After Network Started
                if(rollbackDriver == null)
                {
                    // Before Game Start/Connect
                    if(!network.UsingKnownPort)
                        dt.WriteLine("WARNING: Using non-standard port: " + network.PortNumber, Color.Red);
                    dt.WriteLine("[F1] = Start LAN Game");
                    dt.WriteLine("[F2] = Start Internet Game");
                    if(commandLineHost != null)
                        dt.WriteLine("[F3] = Connect to \"" + commandLineHost + "\"");
                    else
                        dt.WriteLine("[F3] = (specify host on command line)", Color.LightGray);
                    dt.WriteLine("[F4] = Side channel Auth (" + (useSideChannelAuth ? "on" : "off") + ")");

                    dt.WriteLine();
                    if(network.Discovery != null)
                    {
                        if(network.Discovery.Items.Count == 0)
                            dt.WriteLine("Searching for LAN games...");
                        for(int i = 0; i < network.Discovery.Items.Count && i < 10; i++)
                        {
                            string status = network.Discovery.Items[i].StatusString;
                            dt.WriteLine("[" + ((char)('0' + i)) + "] = " + network.Discovery.Items[i].GameInfo.Name + " - " + network.Discovery.Items[i].EndPoint
                                    +  (status == "" ? "" : " - " + status));
                        }
                    }
                }
                else
                {
                    if(useSideChannelAuth && network.IsServer)
                    {
                        dt.WriteLine("[F8] = Toggle Auth Token");
                        dt.WriteLine();
                    }

                    if(!network.IsApplicationConnected)
                    {
                        if(network.LocalisedDisconnectReason == null)
                            dt.WriteLine("Connecting...");
                        else
                        {
                            dt.WriteLine("Disconnected!", Color.DarkRed);
                            dt.WriteLine(network.LocalisedDisconnectReason, Color.DarkRed);
                        }
                    }
                }
            }

            dt.End();
        }

    }
}
