#!/usr/bin/python

import os
from mininet.net import Mininet
from mininet.link import TCLink
from mininet.log import setLogLevel

setLogLevel('info')
mptcp = input("mptcp? ")
delay = input("delay=")
bw = int(input("bandwidth="))
os.system(f'sysctl -w net.mptcp.mptcp_enabled={mptcp}')
net = Mininet(link=TCLink)
net.addController('c0')
h1 = net.addHost('h1', ip='10.0.0.1/24')
h2 = net.addHost('h2', ip='10.0.0.2/24')
sa1 = net.addSwitch('sa1')
sa2 = net.addSwitch('sa2')
sb1 = net.addSwitch('sb1')
sb2 = net.addSwitch('sb2')
net.addLink(h1, sa1,  delay="0.1ms", bw=100)
net.addLink(sa1, sa2, delay="20ms",  bw=10)
net.addLink(sa2, h2,  delay="0.1ms", bw=100)
net.addLink(h1, sb1,  delay="0.1ms", bw=100)
net.addLink(sb1, sb2, delay=delay, bw=bw)
net.addLink(sb2, h2,  delay="0.1ms", bw=100)
h1.setIP('10.0.1.1/24', intf='h1-eth1')
h2.setIP('10.0.1.2/24', intf='h2-eth1')
net.start()
h1.cmdPrint(f'ping {h2.IP()} -c 5')
h2.cmdPrint('iperf -s -i 1 -p 5001 &')
h1.cmdPrint(f'iperf -c {h2.IP()} -p 5001 -t 10')
net.stop()


    
    

