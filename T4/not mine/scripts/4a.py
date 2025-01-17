#!/usr/bin/python

from mininet.net import Mininet
from mininet.node import Controller
from mininet.link import TCLink
from mininet.cli import CLI
from mininet.log import setLogLevel, info


setLogLevel('info')

net = Mininet(link=TCLink)
info('*** Adding controller\n')
net.addController('c0')

info('*** Adding hosts\n')
h1 = net.addHost('h1', ip='10.10.14.1/24')
h2 = net.addHost('h2', ip='10.10.24.2/24')
h3 = net.addHost('h3', ip='10.10.34.3/24')
h4 = net.addHost('h4', ip='10.10.14.4/24')

info('*** Adding switches\n')
s14 = net.addSwitch('s14')
s24 = net.addSwitch('s24')
s34 = net.addSwitch('s34')

info('*** Creating links\n')
net.addLink(h1, s14)
net.addLink(h4, s14)

net.addLink(h2, s24)
net.addLink(h4, s24)

net.addLink(h3, s34)
net.addLink(h4, s34)

h4.setIP('10.10.24.4/24', intf='h4-eth1')
h4.setIP('10.10.34.4/24', intf='h4-eth2')

info('*** Starting network\n')
net.start()

info('*** Setting up routes\n')
h1.cmd('ip route add default via 10.10.14.4')
h2.cmd('ip route add default via 10.10.24.4')
h3.cmd('ip route add default via 10.10.34.4')

h4.cmd('echo 1 > /proc/sys/net/ipv4/ip_forward')

info('*** Running the command line interface\n')
CLI(net)

info('*** Stopping network')
net.stop()
