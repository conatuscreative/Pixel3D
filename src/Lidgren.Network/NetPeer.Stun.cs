using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Net.Sockets;

namespace Lidgren.Network
{
    // Implements a (fairly unsophsticated version of a) STUN client as defined by RFC 5389
    // https://tools.ietf.org/html/rfc5389
    
    public partial class NetPeer
    {
        private const int stunDefaultPort = 3478; // <- TODO: Should really be querying DNS SRV as per RFC 5389



        bool stunConfigLocked = false;

        List<string> stunHostnames;
        List<int> stunPorts;

        public void ConfigureStun(string[] configLines)
        {
            if(stunConfigLocked)
                throw new InvalidOperationException("Cannot reconfigure STUN after first use.");

            stunHostnames = new List<string>();
            stunPorts = new List<int>();

            for(int i = 0; i < configLines.Length; i++)
            {
                string line = configLines[i];
                if(string.IsNullOrWhiteSpace(line))
                    continue;
                line = line.Trim();
                if(line.Length == 0 || line[0] == '#') // Comments
                    continue;

                // Parse out bracketed addresses (required for IPv6)
                if(line[0] ==  '[')
                {
                    int endBrace = line.IndexOf(']');
                    if(endBrace != -1)
                    {
                        stunHostnames.Add(line.Substring(0, endBrace-1));

                        if(line.Length > endBrace+2)
                        {
                            int port;
                            if(int.TryParse(line.Substring(endBrace+1), out port) && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
                            {
                                stunPorts.Add(port);
                                continue;
                            }
                        }

                        stunPorts.Add(stunDefaultPort);
                        continue;
                    }
                }

                // Otherwise just split and hope for the best:
                {
                    string[] split = line.Split(':');
                    if(split.Length <= 0 || split.Length > 2)
                        continue; // Silent rejection (oh well...)

                    stunHostnames.Add(split[0]);

                    int port;
                    if(split.Length == 2 && int.TryParse(split[1], out port))
                        stunPorts.Add(port);
                    else
                        stunPorts.Add(stunDefaultPort);
                }
            }

            Debug.Assert(stunPorts.Count == stunHostnames.Count);
        }




        // Boolean used for cross-thread communication with interlocked (which may not be strictly necessary, but oh well, better to be safe)
        private int stunRequestedCount;

        /// <summary>Return the external endpoint if known. Otherwise return null -- continue polling to (maybe) get a result.</summary>
        public void RequestExternalEndpoint()
        {
            if(stunHostnames == null) // Not yet configured
            {
                // Kinda hacky, if not configured, just configure with this list of public STUN servers:
                ConfigureStun(new string[] {
                        "stun.l.google.com:19302",
                        "stun.stunprotocol.org:3478",
                        "stun1.l.google.com:19302",
                        "stun.services.mozilla.com",
                        "stun2.l.google.com:19302",                                                                                                                              
                        "stun3.l.google.com:19302",
                        "stun4.l.google.com:19302",
                    });
            }

            stunConfigLocked = true;
            Interlocked.Increment(ref stunRequestedCount);
        }



        //
        //
        // ^^^^^ Main thread ^^^^^
        //
        // vvvvv Network Thread vvvvv
        //
        //



        struct StunInfo
        {
            public IAsyncResult pendingDnsRequest;
            public IPEndPoint endpoint;
            public uint transactionID0, transactionID1, transactionID2;

            public int remainingTransmits;
            public double nextRetransmitDelay;
            public double nextTransmitTime;

            public bool dead;
        }

        StunInfo[] pendingStun;

        // Number of pendingStun items that have been started
        int stunStartedCount = 0;



        // NOTE: We'd really love to push the expiry time forward every time we send data across the NAT,
        //       but unfortunately we don't have a reliable way of detecting who is outside our NAT, and
        //       I (-AR) don't really want to implement one right now (have to ask the remotes to echo our
        //       external endpoint back at us, and then only refresh if it matches the STUN result).
        double stunExpireTime = double.NegativeInfinity;

        bool stunInProgress;
        IPEndPoint stunResult;
        


        private NetRandom _stunRNG;
        private NetRandom StunRNG { get { return _stunRNG != null ? _stunRNG : (_stunRNG = new NetRandom()); } }



        // Network Thread
        private void UpdateStun()
        {
            bool didPoll = stunRequestedCount > 0;

            double now = NetTime.Now;
            if(now > stunExpireTime)
            {
                stunResult = null;
                stunInProgress = false;

                if(didPoll)
                {
                    if(stunHostnames.Count == 0)
                        return;

                    // Restart STUN
                    stunInProgress = true;
                    stunExpireTime = now + Configuration.AssumedNATMappingTime; // <- When do we give up querying?

                    if(pendingStun == null)
                        pendingStun = new StunInfo[stunHostnames.Count];
                    else
                        Array.Clear(pendingStun, 0, pendingStun.Length);

                    var rng = StunRNG;

                    for(int i = 0; i < pendingStun.Length; i++)
                    {
                        // NOTE: Retransmit values are specified in RFC 5389
                        pendingStun[i].remainingTransmits = 7;
                        pendingStun[i].nextRetransmitDelay = 0.5;
                        pendingStun[i].nextTransmitTime = double.NegativeInfinity;

                        pendingStun[i].transactionID0 = rng.NextUInt();
                        pendingStun[i].transactionID1 = rng.NextUInt();
                        pendingStun[i].transactionID2 = rng.NextUInt();
                    }

                    stunStartedCount = 1; // <- Cause the first request to start
                }
            }

            if(stunInProgress)
            {
                int liveStunCount = 0;

                for(int i = 0; i < stunStartedCount; i++)
                {
                    if(pendingStun[i].dead)
                        goto finish;
                    
                    // STEP 1: Start a DNS query
                    if(pendingStun[i].endpoint == null && pendingStun[i].pendingDnsRequest == null)
                    {
                        try
                        {
                            pendingStun[i].pendingDnsRequest = Dns.BeginGetHostAddresses(stunHostnames[i], null, null);
                        }
                        catch
                        {
                            LogDebug("STUN BeginGetHostAddresses failed for \"" + stunHostnames[i] + "\"");
                            pendingStun[i].dead = true;
                            goto finish;
                        }
                    }

                    // STEP 2: Finish DNS query
                    bool firstTransmit = false;
                    if(pendingStun[i].pendingDnsRequest != null && pendingStun[i].pendingDnsRequest.IsCompleted)
                    {
                        try
                        {
                            var addresses = Dns.EndGetHostAddresses(pendingStun[i].pendingDnsRequest);
                            IPAddress bestAddress = null;
                            foreach(var address in addresses)
                            {
                                if(address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
                                    continue;

                                // TODO: How to preference IPv4 vs IPv6 (for now, always prefer IPv4)
                                if(bestAddress == null || bestAddress.AddressFamily == AddressFamily.InterNetworkV6 && address.AddressFamily == AddressFamily.InterNetwork)
                                    bestAddress = address;
                            }

                            if(bestAddress == null)
                            {
                                LogDebug("STUN No address result for \"" + stunHostnames[i] + "\"");
                                pendingStun[i].dead = true;
                                goto finish;
                            }

                            pendingStun[i].endpoint = new IPEndPoint(bestAddress, stunPorts[i]);
                        }
                        catch
                        {
                            LogDebug("STUN EndGetHostAddresses failed for \"" + stunHostnames[i] + "\"");
                            pendingStun[i].dead = true;
                            goto finish;
                        }
                        finally
                        {
                            Debug.Assert(pendingStun[i].endpoint != null || pendingStun[i].dead);
                            pendingStun[i].pendingDnsRequest = null;
                        }

                        Debug.Assert(pendingStun[i].endpoint != null);
                        firstTransmit = true;
                    }

                    // STEP 3: Generate and transmit STUN requests
                    if(pendingStun[i].endpoint != null)
                    {
                        if(now >= pendingStun[i].nextTransmitTime)
                        {
                            // Use retransmit as a trigger to fail-over to the next STUN
                            if(!firstTransmit && i == stunStartedCount-1 && stunStartedCount < pendingStun.Length)
                                stunStartedCount++;

                            if(pendingStun[i].remainingTransmits > 0)
                            {
                                pendingStun[i].remainingTransmits--;
                                pendingStun[i].nextTransmitTime = now + pendingStun[i].nextRetransmitDelay;
                                pendingStun[i].nextRetransmitDelay *= 2.0;

                                LogVerbose("STUN Sending packet to \"" + stunHostnames[i] + "\" at " + pendingStun[i].endpoint + " (remain="
                                        + pendingStun[i].remainingTransmits + "; delay=" + pendingStun[i].nextRetransmitDelay + ")");

                                if(!SendStunPacket(pendingStun[i].endpoint, pendingStun[i].transactionID0, pendingStun[i].transactionID1, pendingStun[i].transactionID2))
                                {
                                    LogDebug("STUN refused by ICMP remote \"" + stunHostnames[i] + "\"");
                                    pendingStun[i].dead = true;
                                    goto finish;
                                }
                            }
                        }
                    }

                    liveStunCount++;

                finish:
                    // If all current STUNs have failed, start a fresh one:
                    if(liveStunCount == 0 && i == stunStartedCount-1 && stunStartedCount < pendingStun.Length)
                        stunStartedCount++;
                }


               
                if(liveStunCount == 0 && stunStartedCount == pendingStun.Length)
                {
                    // We failed to get a response (NOTE: expiry time gets set when we _start_)
                    stunInProgress = false;
                }
            }

            if(!stunInProgress)
            {
                PostStunResults();
            }
        }



        // Called on the network thread
        private void PostStunResults()
        {
            Debug.Assert(!stunInProgress); // Caller checks

            while(stunRequestedCount > 0)
            {
                Interlocked.Decrement(ref stunRequestedCount);

                NetIncomingMessage message = CreateIncomingMessage(NetIncomingMessageType.EndpointRequestResult, 0);
                message.m_senderEndPoint = stunResult;
                ReleaseMessage(message);
            }
        }



        const uint stunMagicCookie = 0x2112A442;


        // Called on the network thread
        /// <returns>False if the remote host reset the connection.</returns>
        private bool SendStunPacket(IPEndPoint target, uint transactionID0, uint transactionID1, uint transactionID2)
        {
            // STUN Header (from RFC 5389)
            // 
            //   0                   1                   2                   3   
            //   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 
            //  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //  |0 0|     STUN Message Type     |         Message Length        |
            //  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //  |                   Magic Cookie  =  0x2112A442                 |
            //  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //  |                                                               |
            //  |                     Transaction ID (96 bits)                  |
            //  |                                                               |
            //  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //
            
            // Just manually construct it (NOTE: send fields in network byte order)
            Debug.Assert(BitConverter.IsLittleEndian);
            if(m_sendBuffer.Length < 20)
            {
                LogWarning("Send buffer too short to send STUN packet");
                return false; // <- just fail everyone.
            }
            m_sendBuffer[ 0] = 0; // Type MSB
            m_sendBuffer[ 1] = 1; // Type LSB ("Binding, Request")
            m_sendBuffer[ 2] = 0; // Payload Length MSB
            m_sendBuffer[ 3] = 0; // Payload Length LSB
            m_sendBuffer[ 4] = unchecked((byte)(stunMagicCookie >> 24));
            m_sendBuffer[ 5] = unchecked((byte)(stunMagicCookie >> 16));
            m_sendBuffer[ 6] = unchecked((byte)(stunMagicCookie >> 8));
            m_sendBuffer[ 7] = unchecked((byte)(stunMagicCookie));
            m_sendBuffer[ 8] = (byte)(transactionID0 >> 24);
            m_sendBuffer[ 9] = (byte)(transactionID0 >> 16);
            m_sendBuffer[10] = (byte)(transactionID0 >> 8);
            m_sendBuffer[11] = (byte)(transactionID0);
            m_sendBuffer[12] = (byte)(transactionID1 >> 24);
            m_sendBuffer[13] = (byte)(transactionID1 >> 16);
            m_sendBuffer[14] = (byte)(transactionID1 >> 8);
            m_sendBuffer[15] = (byte)(transactionID1);
            m_sendBuffer[16] = (byte)(transactionID2 >> 24);
            m_sendBuffer[17] = (byte)(transactionID2 >> 16);
            m_sendBuffer[18] = (byte)(transactionID2 >> 8);
            m_sendBuffer[19] = (byte)(transactionID2);

            bool connectionReset;
            SendPacket(20, target, 1, out connectionReset);

            return !connectionReset;
        }


        // Called on the network thread
        /// <summary>Determine if a packet is a STUN packet; if it is, handles the packet and returns true.</summary>
        private bool MaybeHandleStunPacket(byte[] buffer, int index, int count)
        {
            // STUN Header (from RFC 5389)
            // 
            //   0                   1                   2                   3   
            //   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 
            //  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //  |0 0|     STUN Message Type     |         Message Length        |
            //  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //  |                   Magic Cookie  =  0x2112A442                 |
            //  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //  |                                                               |
            //  |                     Transaction ID (96 bits)                  |
            //  |                                                               |
            //  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //

            // Read STUN header:
            if(count < 20)
                return false;
            ushort receivedMessageType = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, index+0));
            int receivedMessageLength = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, index+2));
            uint receivedMagicCookie = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, index+4));
            uint transactionID0 = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, index+8));
            uint transactionID1 = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, index+12));
            uint transactionID2 = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, index+16));

            // Validate STUN header:
            if((receivedMessageType & 0xC000) != 0)
                return false;
            if((receivedMessageLength & 0x3) != 0) // <- size must be a multiple of 4
                return false;
            if(receivedMessageLength + 20 > count) // <- valid message length
                return false;
            if(receivedMagicCookie != stunMagicCookie)
                return false;

            //
            // IMPORTANT: From this point onwards, we have a valid STUN message. Don't pass back to Lidgren (ie: we must return true)
            //

            if(pendingStun == null)
                return true;
            if(!stunInProgress)
                return true;

            int i;
            for(i = 0; i < pendingStun.Length; i++)
			{
                if(!pendingStun[i].dead
                    && pendingStun[i].transactionID0 == transactionID0
                    && pendingStun[i].transactionID1 == transactionID1
                    && pendingStun[i].transactionID2 == transactionID2)
                {
                    goto knownTransaction;
                }
			}
            LogVerbose("STUN packet ignored because we don't recognise the transaction ID");
            return true;
        knownTransaction:

            // We no longer care about any further messages regarding this transaction
            // If it is a success, we'll deal with it. If it's an error, or anything else at all, stop harassing the server.
            // If the server is well-behaved, we'll never receieve another response for this transaction anyway.
            // (unless we re-transmit, in which case we'll probably get an identical response.)
            pendingStun[i].dead = true;

            if(receivedMessageType == 0x101) // "Binding, Success"
            {
                int attributeStart = index + 20;

                IPEndPoint endpoint = null;
                while(true)
                {
                    if(attributeStart + 4 > count)
                        break; // <- Not enough data for attribute header

                    int attributeType = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, attributeStart+0));
                    int attributeLength = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, attributeStart+2));
                    int attributeLengthPadded = ((attributeLength + 3) & ~3);

                    int attributeData = attributeStart + 4;
                    if(attributeData + attributeLength > count)
                        break; // <- Not enough data for this attribute

                    if(attributeType == 0x0020) // XOR-MAPPED-ADDRESS (NOTE: Ignore regular MAPPED-ADDRESS, because it is potentially wrong, hold out for XOR)
                    {
                        if(attributeLength < 4)
                            goto nextAttribute;
                        int family = buffer[attributeData+1];
                        uint xPort = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, attributeData+2));
                        uint port = xPort ^ (stunMagicCookie >> 16);

                        if(port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                            goto nextAttribute;

                        if(family == 1) // IPv4
                        {
                            if(attributeLength < 8)
                                goto nextAttribute;
                            uint xAddress = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, attributeData+4));
                            uint address = xAddress ^ stunMagicCookie;

                            endpoint = new IPEndPoint((long)(UInt32)IPAddress.HostToNetworkOrder((Int32)address), (int)port); // <- NOTE: Fancy casting!
                            break;
                        }
                        else if(family == 2) // IPv6
                        {
                            if(attributeLength < 20)
                                goto nextAttribute;

                            // NOTE: we could save a tiny bit of processing by leaving transactionID and address in network-byte-order,
                            //       but working with host-byte-order makes the code clearer.

                            uint xAddress0 = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, attributeData+4));
                            uint xAddress1 = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, attributeData+8));
                            uint xAddress2 = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, attributeData+12));
                            uint xAddress3 = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, attributeData+16));

                            uint address0 = xAddress0 ^ stunMagicCookie;
                            uint address1 = xAddress1 ^ transactionID0;
                            uint address2 = xAddress2 ^ transactionID1;
                            uint address3 = xAddress3 ^ transactionID2;

                            // Convert to network byte order by hand:
                            // (NOTE: DWORDs are already in that order)
                            Debug.Assert(BitConverter.IsLittleEndian);
                            byte[] ipv6Address = new byte[16];
                            ipv6Address[ 0] = (byte)(address0 >> 24);
                            ipv6Address[ 1] = (byte)(address0 >> 16);
                            ipv6Address[ 2] = (byte)(address0 >> 8);
                            ipv6Address[ 3] = (byte)(address0);
                            ipv6Address[ 4] = (byte)(address1 >> 24);
                            ipv6Address[ 5] = (byte)(address1 >> 16);
                            ipv6Address[ 6] = (byte)(address1 >> 8);
                            ipv6Address[ 7] = (byte)(address1);
                            ipv6Address[ 8] = (byte)(address2 >> 24);
                            ipv6Address[ 9] = (byte)(address2 >> 16);
                            ipv6Address[10] = (byte)(address2 >> 8);
                            ipv6Address[11] = (byte)(address2);
                            ipv6Address[12] = (byte)(address3 >> 24);
                            ipv6Address[13] = (byte)(address3 >> 16);
                            ipv6Address[14] = (byte)(address3 >> 8);
                            ipv6Address[15] = (byte)(address3);

                            endpoint = new IPEndPoint(new IPAddress(ipv6Address), (int)port);
                            break;
                        }
                    }

                nextAttribute:
                    attributeStart = attributeData + attributeLengthPadded;
                }


                if(endpoint != null)
                {
                    stunInProgress = false;
                    stunExpireTime = NetTime.Now + Configuration.AssumedNATMappingTime;
                    stunResult = endpoint;

                    PostStunResults();

                    LogDebug("STUN Received valid endpoint from \"" + stunHostnames[i] + "\": " + stunResult.ToString());
                }
                else
                {
                    LogDebug("STUN No endpoint receieved from \"" + stunHostnames[i] + "\"");
                }
            }
            else
            {
                LogDebug("STUN Unwanted response from \"" + stunHostnames[i] + "\" (" + receivedMessageType.ToString("X4") + ")");
            }


            return true;
        }

    }

}
