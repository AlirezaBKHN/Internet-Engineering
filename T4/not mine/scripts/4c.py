#!/usr/bin/python

import os, time
from mininet.net import Mininet
from mininet.node import Controller
from mininet.link import TCLink
from mininet.cli import CLI
from mininet.log import setLogLevel, output

def run_test(sched, cc_alg, delays, duration):
    net = Mininet(link=TCLink)

    output('*** Adding controller\n')
    net.addController('c0')

    output('*** Adding hosts\n')
    h1 = net.addHost('h1', ip='10.0.0.1/24')
    h2 = net.addHost('h2', ip='10.0.0.2/24')
    h3 = net.addHost('h3', ip='10.0.0.3/24')

    output('*** Adding switches\n')
    s1 = net.addSwitch('s1')
    s2 = net.addSwitch('s2')

    output('*** Creating links\n')
    net.addLink(h1, s1, delay="0.1ms", bw=100)
    net.addLink(h2, s1, delay="0.1ms", bw=100)
    net.addLink(h2, s2, delay="0.1ms", bw=100)
    net.addLink(s1, h3, delay=delays[0], bw=12)
    net.addLink(s2, h3, delay=delays[1], bw=24)

    h2.setIP('10.0.1.2/24', intf='h2-eth1')
    h3.setIP('10.0.1.3/24', intf='h3-eth1')

    output('*** Starting network\n')
    net.start()

    output('*** Configuring MPTCP\n')
    os.system('sysctl -w net.mptcp.mptcp_enabled=1')
    os.system(f'sysctl -w net.mptcp.mptcp_scheduler={sched}')
    os.system(f'sysctl -w net.ipv4.tcp_congestion_control={cc_alg}')

    output('*** Starting iperf servers on h3\n')
    seperator = (sched + ' ' + cc + ' ' + str(delays)
        + '-' * (58 - len(sched) - len(cc_alg) - len(str(delays))))

    os.system(f'echo "\n{seperator}\n" >> h3-h1')
    os.system(f'echo "\n{seperator}\n" >> h3-h2')
    h3.cmd('iperf -s -i 1 -p 5001 >> h3-h1 &')
    h3.cmd('iperf -s -i 1 -p 5002 >> h3-h2 &')

    output(f'*** Scheduler={sched}, Congestion Control={cc_alg}, Delays={str(delays)}\n')
    output('*** Testing throughput\n')
    os.system(f'echo "\n{seperator}\n" >> h1')
    os.system(f'echo "\n{seperator}\n" >> h2')
    h1.cmd(f'iperf -c {h3.IP()} -p 5001 -t {duration} >> h1 &')
    h2.cmd(f'iperf -c {h3.IP()} -p 5002 -t {duration} > /dev/null &')
    h2.cmd(f"./measure.sh h2-eth0 {duration} >> h2 &")
    h2.cmd(f"./measure.sh h2-eth1 {duration} >> h2 &")

    time.sleep(duration + 3)

    # output('*** Testing RTT\n')
    # os.system(f'echo "\n{seperator}\n" >> h1-ping')
    # os.system(f'echo "\n{seperator}\n" >> h2-ping')
    # h1.cmdPrint(f'ping {h3.IP()} -c 5 >> h1-ping')
    # h2.cmdPrint(f'ping {h3.IP()} -c 5 >> h2-ping')

    output('*** Stopping network\n')
    net.stop()


if __name__ == '__main__':
    setLogLevel('output')
    schedulers = ['default', 'roundrobin']
    cc_algorithms = ['lia', 'olia', 'balia']
    delay_tuples = [
        ("10ms", "10ms"),
        ("10ms", "20ms"),
        ("10ms", "30ms"),
        ("50ms", "50ms"),
        ("50ms", "100ms"),
        ("50ms", "150ms")
    ]
    TEST_DURATION = 10

    for delays in delay_tuples:
        for sched in schedulers:
            for cc in cc_algorithms:
                run_test(sched, cc, delays, TEST_DURATION)
