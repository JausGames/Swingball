using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
{
	"Items": [
	{
		"name": "game-server-xppts",
		"kind": "",
		"nspace": "gameserver",
		"status": "Ready",
		"ip": "20.216.183.9",
		"port": 7396,
		"player_count": 0,
		"player_max": 0,
		"uid": "38227f22-8fbc-4e3c-8bee-679394425092"
	}
	]
}
*/

[System.Serializable]
public class GameServer
{
	public string name;     //`json:"name"`
	public string kind;     //`json:"kind"`
	public string nspace;   //`json:"namespace"`
	public string status;   //`json:"status"`
	public string ip;       //`json:"ip"`
	public int port;        //`json:"port"`
	public int player_count;//`json:"player_count"`
	public int max_player;  //`json:"player_max"`
	public string uid;      //`json:"uid"`

}