const WebSocket = require('ws'); // Ensure you import WebSocket correctly
const { v4: uuidv4 } = require('uuid');

const server = new WebSocket.Server({ port: 8000 });
const rooms = {};

server.on('connection', socket => {
    socket.on('message', message => {
        const { type, payload } = JSON.parse(message);
        if (type === 'create') {
            console.log("Got create request");
            const roomId = uuidv4();
            rooms[roomId] = { users: {}, sockets: [] }; // Ensure users is an object
            socket.send(JSON.stringify({ type: "roomCreated", roomId: roomId }));
        }
        else if (type === 'join') {
            const { roomId, userName } = payload;
            if (roomId && rooms[roomId] && !rooms[roomId].users[userName]) {
                rooms[roomId].users[userName] = socket;
                rooms[roomId].sockets.push(socket);
                socket.roomId = roomId;
                socket.username = userName;
                socket.send(JSON.stringify({ type: "joined", roomId }));
                broadcast(roomId, {user:userName, message: 'has joined the room.'});
            } else {
                socket.send(JSON.stringify({
                    type: "error",
                    message: "Can't join the room."
                }));
            }
        }
        else if (type === "message") {
            const { roomId, userName, text } = payload;
            broadcast(roomId, {user: userName, message: text});
        }
    });

    socket.on('close', () => {
        const { roomId, username } = socket;
        if (roomId && rooms[roomId]) {
            delete rooms[roomId].users[username];
            rooms[roomId].sockets = rooms[roomId].sockets.filter(s => s !== socket);
            if (rooms[roomId].sockets.length === 0) {
                delete rooms[roomId];
            } else {
                broadcast(roomId, `${username} has left the room.`);
            }
        }
    });

    const broadcast = (roomId, message) => {
        rooms[roomId].sockets.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(JSON.stringify({ type: 'message', message }));
            }
        });
    };
});

console.log('WebSocket server is running on ws://localhost:8000');
