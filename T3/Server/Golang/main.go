package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"sync"

	"github.com/google/uuid"
	"github.com/gorilla/websocket"
)

type Message struct {
	Type    string          `json:"type"`
	Payload json.RawMessage `json:"payload"`
}

type JoinPayload struct {
	RoomId   string `json:"roomId"`
	UserName string `json:"userName"`
}

type MessagePayload struct {
	RoomId   string `json:"roomId"`
	UserName string `json:"userName"`
	Text     string `json:"text"`
}

type Room struct {
	Users   map[string]*websocket.Conn
	Sockets []*websocket.Conn
	Mutex   sync.Mutex
}

var rooms = make(map[string]*Room)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		return true
	},
}

func handleConnection(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Println("Upgrade error:", err)
		return
	}
	defer conn.Close()

	var username, roomId string

	for {
		var msg Message
		err := conn.ReadJSON(&msg)
		if err != nil {
			log.Println("Read error:", err)
			break
		}

		switch msg.Type {
		case "create":
			log.Println("Got create request")
			roomId = uuid.New().String()
			rooms[roomId] = &Room{
				Users:   make(map[string]*websocket.Conn),
				Sockets: []*websocket.Conn{conn},
			}
			conn.WriteJSON(Message{Type: "roomCreated", Payload: json.RawMessage(fmt.Sprintf(`{"roomId":"%s"}`, roomId))})
			log.Printf("Room %s created", roomId)

		case "join":
			var joinPayload JoinPayload
			err := json.Unmarshal(msg.Payload, &joinPayload)
			if err != nil {
				log.Println("Error parsing join payload:", err)
				break
			}
			roomId = joinPayload.RoomId
			username = joinPayload.UserName
			if room, ok := rooms[roomId]; ok && room.Users[username] == nil {
				room.Mutex.Lock()
				room.Users[username] = conn
				room.Sockets = append(room.Sockets, conn)
				room.Mutex.Unlock()
				conn.WriteJSON(Message{
					Type:    "joined",
					Payload: json.RawMessage(fmt.Sprintf(`{"roomId":"%s"}`, roomId)),
				})
				broadcast(roomId, fmt.Sprintf("%s has joined the room.", username))
			} else {
				conn.WriteJSON(Message{Type: "error", Payload: json.RawMessage(`{"message":"Can't join the room."}`)})
			}

		case "message":
			var messagePayload MessagePayload
			err := json.Unmarshal(msg.Payload, &messagePayload)
			if err != nil {
				log.Println("Error parsing message payload:", err)
				break
			}
			broadcast(messagePayload.RoomId, fmt.Sprintf("%s: %s", messagePayload.UserName, messagePayload.Text))

		case "leave":
			var leavePayload JoinPayload
			err := json.Unmarshal(msg.Payload, &leavePayload)
			if err != nil {
				log.Println("Error parsing leave payload:", err)
				break
			}
			roomId = leavePayload.RoomId
			username = leavePayload.UserName
			if room, ok := rooms[roomId]; ok {
				room.Mutex.Lock()
				delete(room.Users, username)
				var updatedSockets []*websocket.Conn
				for _, socket := range room.Sockets {
					if socket != conn {
						updatedSockets = append(updatedSockets, socket)
					}
				}
				room.Sockets = updatedSockets
				room.Mutex.Unlock()
				broadcast(roomId, fmt.Sprintf("%s has left the room.", username))
			}

		default:
			log.Println("Unknown message type:", msg.Type)
		}
	}

	if roomId != "" {
		if room, ok := rooms[roomId]; ok {
			room.Mutex.Lock()
			delete(room.Users, username)
			var updatedSockets []*websocket.Conn
			for _, socket := range room.Sockets {
				if socket != conn {
					updatedSockets = append(updatedSockets, socket)
				}
			}
			room.Sockets = updatedSockets
			room.Mutex.Unlock()

			if len(room.Sockets) == 0 {
				delete(rooms, roomId)
			} else {
				broadcast(roomId, fmt.Sprintf("%s has left the room.", username))
			}
		}
	}
}

func broadcast(roomId, message string) {
	if room, ok := rooms[roomId]; ok {
		room.Mutex.Lock()
		for _, conn := range room.Sockets {
			if err := conn.WriteJSON(Message{Type: "message", Payload: json.RawMessage(fmt.Sprintf(`{"message":"%s"}`, message))}); err != nil {
				log.Println("Error sending message:", err)
			}
		}
		room.Mutex.Unlock()
	}
}

func main() {
	http.HandleFunc("/", handleConnection)

	serverAddr := "localhost:8001"
	fmt.Println("WebSocket server is running on ws://" + serverAddr)
	err := http.ListenAndServe(serverAddr, nil)
	if err != nil {
		log.Fatal("ListenAndServe error:", err)
	}
}
