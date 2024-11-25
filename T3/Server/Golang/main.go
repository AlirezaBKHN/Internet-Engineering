package main

import (
    "encoding/json"
    "log"
    "net/http"
    "github.com/gorilla/websocket"
    "github.com/google/uuid"
)

var rooms = make(map[string]*Room)

type Room struct {
    Users   map[string]*websocket.Conn
    Sockets []*websocket.Conn
}

var upgrader = websocket.Upgrader{
    ReadBufferSize:  1024,
    WriteBufferSize: 1024,
    CheckOrigin: func(r *http.Request) bool {
        return true
    },
}

func main() {
    http.HandleFunc("/ws", handleConnections)
    log.Println("WebSocket server is running on ws://localhost:8001")
    log.Fatal(http.ListenAndServe(":8001", nil))
}

func handleConnections(w http.ResponseWriter, r *http.Request) {
    ws, err := upgrader.Upgrade(w, r, nil)
    if err != nil {
        log.Fatal(err)
    }
    defer ws.Close()

    for {
        _, message, err := ws.ReadMessage()
        if err != nil {
            log.Printf("error: %v", err)
            break
        }

        var msg map[string]interface{}
        if err := json.Unmarshal(message, &msg); err != nil {
            log.Printf("error: %v", err)
            continue
        }

        switch msg["type"] {
        case "create":
            handleCreateRoom(ws)
        case "join":
            handleJoinRoom(ws, msg["payload"].(map[string]interface{}))
        case "message":
            handleMessage(ws, msg["payload"].(map[string]interface{}))
        }
    }
}

func handleCreateRoom(ws *websocket.Conn) {
    roomId := uuid.New().String()
    rooms[roomId] = &Room{Users: make(map[string]*websocket.Conn), Sockets: []*websocket.Conn{ws}}
    response := map[string]interface{}{"type": "roomCreated", "roomId": roomId}
    ws.WriteJSON(response)
}

func handleJoinRoom(ws *websocket.Conn, payload map[string]interface{}) {
    roomId := payload["roomId"].(string)
    userName := payload["userName"].(string)

    if room, exists := rooms[roomId]; exists && room.Users[userName] == nil {
        room.Users[userName] = ws
        room.Sockets = append(room.Sockets, ws)
        ws.WriteJSON(map[string]interface{}{"type": "joined", "roomId": roomId})
        broadcast(roomId, map[string]interface{}{"user": userName, "message": "has joined the room."})
    } else {
        ws.WriteJSON(map[string]interface{}{"type": "error", "message": "Can't join the room."})
    }
}

func handleMessage(ws *websocket.Conn, payload map[string]interface{}) {
    roomId := payload["roomId"].(string)
    userName := payload["userName"].(string)
    text := payload["text"].(string)
    broadcast(roomId, map[string]interface{}{"user": userName, "message": text})
}

func broadcast(roomId string, message map[string]interface{}) {
    room, exists := rooms[roomId]
    if !exists {
        return
    }

    for _, client := range room.Sockets {
        if err := client.WriteJSON(map[string]interface{}{"type": "message", "message": message}); err != nil {
            log.Printf("error: %v", err)
        }
    }
}
