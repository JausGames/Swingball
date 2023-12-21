package main

type PlayerGameInfo struct {
	Name        string  `json:"name"`
	ID          string  `json:"id"`
	Elo         float64 `json:"elo"`
	GameVersion string  `json:"gameVersion"`
	// Add other fields as necessary
}
