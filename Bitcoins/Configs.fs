module Configs

open Akka
open Akka.FSharp

let seedAkkaConfig hostName port = Configuration.parse("""
akka {  
actor {
provider = "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
serializers {
event = "Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion"
}
serialization-bindings {
"System.Object" = event
}
deployment {
/mineGenerator = {
router = round-robin-pool
arg = cpu 
nr-of-instances = 35
cluster {
enabled = on
max-nr-of-instances-per-node = 7
allow-local-routees = on
use-role = child 


}
}
/routerCreatorClust = {
router = broadcast-pool
arg = cpu
nr-of-instances = 15
cluster {
enabled = on
max-nr-of-instances-per-node = 6
allow-local-routees = on
use-role = child

}
}
}
}
remote {
log-remote-lifecycle-events = off
helios.tcp {
hostname = """ + hostName + """
port = """ + port + """      
}
}
cluster {
min-nr-of-members = 1
roles = [parent, child]
role {
parent.min-nr-of-members = 1
child.min-nr-of-members = 1
}
seed-nodes = ["akka.tcp://clusters-miners@""" + hostName + """:""" + port + """"]

auto-down-unreachable-after = 10 s
failure-detector {      
threshHold = 6.0
maxSampl = 500
heartbeatInv = 2 s
exptResp = 1 s
minStdDev = 75 ms
accpt_Pause = 10 s
}
}
log-dead-letters = 0
log-dead-letters-during-shutdown = off
}
""")

let clientAkkaConfig hostName port seedHostName = Configuration.parse("""
akka {  
actor {
provider = "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
serializers {
event = "Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion"
}
serialization-bindings {
"System.Object" = event
}
}
remote {
log-remote-lifecycle-events = off
helios.tcp {
hostname = """ + hostName + """
port = 0      
}
}
cluster {
roles = ["child"]  # custom node roles
seed-nodes = ["akka.tcp://clusters-miners@""" + seedHostName + """:""" + port + """"]
# when node cannot be reached within 10 sec, mark is as down
auto-down-unreachable-after = 10 s
failure-detector {  
threshHold = 6.0
maxSampl = 500
heartbeatInv = 2 s
exptResp = 1 s
minStdDev = 75 ms
accpt_Pause = 10 s

}
}
log-dead-letters = 0
log-dead-letters-during-shutdown = off
}
""")

let loneNodeConfig = Configuration.parse("""
akka {
    actor {
        deployment {
            /mineGenerator {
                router = round-robin-pool
                nr-of-instances = 15
            }
            /routerCreatorClust{
                router = broadcast-pool
                nr-of-instances = 10
            }
        }
    }
}
""")