#!/usr/bin/python

import os
from mininet.net import Mininet
from mininet.node import Controller
from mininet.link import TCLink
from mininet.cli import CLI
from mininet.log import setLogLevel, info


def run_test(B_delay, B_bw):
    info(f'*** Starting test for {B_bw}Mbps-{B_delay}\n')

    net = Mininet(link=TCLink)
    info('*** Adding controller\n')
    net.addController('c0')

    info('*** Adding hosts\n')
    h1 = net.addHost('h1', ip='10.0.0.1/24')
    h2 = net.addHost('h2', ip='10.0.0.2/24')

    info('*** Adding switches\n')
    sa1 = net.addSwitch('sa1')
    sa2 = net.addSwitch('sa2')
    sb1 = net.addSwitch('sb1')
    sb2 = net.addSwitch('sb2')

    info('*** Creating links\n')
    net.addLink(h1, sa1,  delay="0.1ms", bw=100)
    net.addLink(sa1, sa2, delay="20ms",  bw=10)
    net.addLink(sa2, h2,  delay="0.1ms", bw=100)

    net.addLink(h1, sb1,  delay="0.1ms", bw=100)
    net.addLink(sb1, sb2, delay=B_delay, bw=B_bw)
    net.addLink(sb2, h2,  delay="0.1ms", bw=100)

    h1.setIP('10.0.1.1/24', intf='h1-eth1')
    h2.setIP('10.0.1.2/24', intf='h2-eth1')

    info('*** Starting network\n')
    net.start()

    info('*** Running the tests\n')
    h1.cmdPrint(f'ping {h2.IP()} -c 5')

    h2.cmdPrint('iperf -s -i 1 -p 5001 &')
    h1.cmdPrint(f'iperf -c {h2.IP()} -p 5001 -t 10')

    info('*** Stopping network')
    net.stop()


if __name__ == '__main__':
    setLogLevel('info')

    info('*** With MPTCP\n')
    os.system('sysctl -w net.mptcp.mptcp_enabled=1')

    for delay in ["5ms", "40ms", "100ms"]:
        for bw in [5, 20, 50]:
            run_test(delay, bw)

    info('*** Without MPTCP\n')
    os.system('sysctl -w net.mptcp.mptcp_enabled=0')

    for delay in ["5ms", "40ms", "100ms"]:
        for bw in [5, 20, 50]:
            run_test(delay, bw)
