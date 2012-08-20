import pydot
graph = pydot.Dot(graph_type='digraph')
# node_a = pydot.Node("Node A", style="filled", fillcolor='red')
# node_b = pydot.Node("Node A", style="filled", fillcolor='red')
# graph.add_edge(pydot.Edge(node_a,node_b, label="X",labelfontcolor="#009933", fontsize="10.0", color="blue"))
# graph.write_png("example2.png")

def addNode(nodes,key):
    if key not in nodes:
        node = pydot.Node(key)
        nodes[key]=node

nodes ={}
f = open("NFA.txt")
lines= f.readlines()
for line in lines:
    edgeParts = line.split(',')
    for part in edgeParts:
        element= part.split(':')
        if element[0]=="Start":
            startNodeKey = element[1]
            addNode(nodes,startNodeKey)
        elif element[0]=="End":
            endNodeKey = element[1]
            addNode(nodes,endNodeKey)
        else:
            label=element[1].strip('\n')
            if label=="EPSILON":
                label='Ɛ'
            graph.add_edge(pydot.Edge(nodes[startNodeKey],nodes[endNodeKey],label=label))
f.close()

graph.write_png("NFA.png")