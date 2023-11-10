package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"

	"agones.dev/agones/pkg/client/clientset/versioned"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
)

type GameServer struct {
	Name      string `json:"name"`
	Kind      string `json:"kind"`
	Namespace string `json:"nspace"`
	Status    string `json:"status"`
	Ip        string `json:"ip"`
	Port      int32  `json:"port"`
	Players   int64  `json:"player_count"`
	MaxPlayer int64  `json:"player_max"`
	UID       string `json:"uid"`
}

func CRUD_Server_Index(agonesClient *versioned.Clientset, w http.ResponseWriter) {
	listGS, err := agonesClient.AgonesV1().GameServers("gameserver").List(context.TODO(), metav1.ListOptions{})
	if err != nil {
		panic(err.Error())
	}

	fmt.Printf("There are %d GS in the cluster\n", len(listGS.Items))

	jsonBytes := "{\"Items\":["

	for i := 0; i < len(listGS.Items); i++ {
		podname := listGS.Items[i].GetName()
		fmt.Printf(" - %s %s", podname, "\n")

		var name = listGS.Items[i].GetName()
		gs := GetGameServerInfo(agonesClient, name)

		json, err := json.Marshal(gs)
		if err != nil {
			panic(err)
		}
		if i > 0 {
			jsonBytes += ", "
		}

		jsonString := string(json)
		jsonBytes += jsonString

	}
	jsonBytes += "]}"
	fmt.Fprintf(w, jsonBytes)
}
func CRUD_Server_MMFind(agonesClient *versioned.Clientset, w http.ResponseWriter) {

	listGS, err := agonesClient.AgonesV1().GameServers("gameserver").List(context.TODO(), metav1.ListOptions{})
	if err != nil {
		panic(err.Error())
	}

	fmt.Printf("There are %d GS in the cluster\n", len(listGS.Items))

	var list []GameServer

	for i := 0; i < len(listGS.Items); i++ {

		var name = listGS.Items[i].GetName()
		gs := GetGameServerInfo(agonesClient, name)
		list = append(list, gs)
	}

	serverAlloc := filterGameServersByStatus(list, "Allocated")
	server := FindSuitableServer(serverAlloc, agonesClient)

	if server.UID == "" {
		serverReady := filterGameServersByStatus(list, "Ready")
		server = FindSuitableServer(serverReady, agonesClient)
	}

	json, err := json.Marshal(server)
	if err != nil {
		panic(err)
	}
	if server.UID == "" {
		fmt.Println("No suitable server was found or 'server' is uninitialized")
	} else {
		jsonString := string(json)
		fmt.Fprintf(w, jsonString)
	}
}

func FindSuitableServer(list []GameServer, agonesClient *versioned.Clientset) GameServer {
	var server GameServer
	for i := 0; i < len(list); i++ {

		var name = list[i].Name
		gs := GetGameServerInfo(agonesClient, name)
		if gs.MaxPlayer > gs.Players {
			server = gs
		}
	}
	return server
}

func filterGameServersByStatus(gameServers []GameServer, status string) []GameServer {
	var filtered []GameServer
	for _, gs := range gameServers {
		if gs.Status == status {
			filtered = append(filtered, gs)
		}
	}
	return filtered
}

func GetGameServerInfo(agonesClient *versioned.Clientset, name string) GameServer {
	getGS, err := agonesClient.AgonesV1().GameServers("gameserver").Get(context.TODO(), name, metav1.GetOptions{})

	if err != nil {
		panic(err.Error())
	}

	gs := GameServer{
		Name:      getGS.GetName(),
		Kind:      getGS.Kind,
		Namespace: getGS.Namespace,
		Status:    string(getGS.Status.State),
		Ip:        getGS.Status.Address,
		Port:      getGS.Status.Ports[0].Port,
		Players:   getGS.Status.Players.Count,
		MaxPlayer: getGS.Status.Players.Capacity,
		UID:       string(getGS.UID),
	}
	return gs
}
