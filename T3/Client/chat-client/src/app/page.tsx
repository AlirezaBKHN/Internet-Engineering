'use client'
import { useEffect, useRef, useState } from 'react';

export default function Home() {
  const [messages, setMessages] = useState<string[] | any[]>([]);
  const [roomId, setRoomId] = useState<string>('');
  const [username, setUsername] = useState<string>('');
  const socket = useRef<WebSocket | null>(null);
  const [currentRoomId, setCurrentRoomId] = useState<string>('');
  const [createRoomId, setCreatetRoomId] = useState<string>('');

  useEffect(() => {
    socket.current = new WebSocket('ws://localhost:8000'); //JavaScript
    // socket.current = new WebSocket('ws://localhost:8001'); // GoLang
    socket.current.onopen = () => {
      console.log('WebSocket connection opened');
    };

    socket.current.onmessage = (event) => {
      // alert(JSON.parse(event.data))
      const jsonData = JSON.parse(event.data)
      console.log(jsonData)
      let type: string, message: string, roomId :string
      type = jsonData["type"]
      if ("payload" in jsonData) {
        roomId = jsonData["payload"]["roomId"]
        message = jsonData["payload"]["message"]
      }
      else {
        roomId = jsonData["roomId"]
        message = jsonData["message"]
      }
      if (type === 'message') {
        if (typeof message == 'string') {
          let username = message.split(" ")[0]
          let m = message.substring(message.indexOf(" ")+1)
          setMessages((prev) => [...prev, {user:username,message:m}])
        }
        else{
          setMessages((prev) => [...prev, message]);
        }
        console.log(messages)
      } else if (type === 'roomCreated') {
        setCreatetRoomId(roomId);
        console.log(createRoomId);
      } else if (type === 'joined') {
        console.log(message)
        setCurrentRoomId(roomId);
      } else if (type === 'error') {
        alert(message);
      }
    };

    socket.current.onclose = () => {
      console.log('WebSocket connection closed');
    };

    return () => socket.current?.close();
  }, []);

  const createRoom = () => {
    if (socket.current?.readyState === WebSocket.OPEN) {
      socket.current.send(JSON.stringify({ type: 'create' }));
    } else {
      console.log('WebSocket is not open');
    }
  };

  const joinRoom = () => {
    // leaveRoom()
    if (username && roomId) {
      if (socket.current?.readyState === WebSocket.OPEN) {
        socket.current.send(JSON.stringify({ type: 'join', payload: { roomId, userName: username } }));
      } else {
        console.log('WebSocket is not open');
      }
    } else {
      alert('Enter a username and room ID first.');
    }
  };

  const sendMessage = (text: string) => {
    if (socket.current?.readyState === WebSocket.OPEN && currentRoomId) {
      socket.current.send(JSON.stringify({ type: 'message', payload: { roomId: currentRoomId, userName: username, text } }));
    } else {
      console.log('WebSocket is not open or roomId is not set');
    }
  };
  const leaveRoom = () => {
    if (socket.current?.readyState === WebSocket.OPEN && currentRoomId) {
      socket.current.send(JSON.stringify({ type: 'leave', payload: { roomId: currentRoomId, userName: username } }));

      socket.current.close();
      window.location.reload()

      setMessages([]);
      setCurrentRoomId('');
    }
  }

  return (
    <div className="w-screen h-screen flex align-middle justify-center content-center">
      <div className="m-auto grid grid-cols-2 grid-rows-2 gap-3 h-2/3 w-2/3">
        <div className="grid grid-cols-1 justify-center bg-opacity-45 bg-white rounded h-full">
          <h1 className="mx-5 my-2 justify-center align-middle content-center text-center font-sans text-lg">
            Create Room
          </h1>
          <button onClick={createRoom} className="rounded font-bold mx-5 my-2 bg-black bg-opacity-30 hover:bg-opacity-40 text-white">
            Get Room Id!
          </button>
          <input value={createRoomId} disabled={true} className="rounded mx-5 mt-2 mb-4 px-2 text-black bg-inherit" />
        </div>
        <div className="grid grid-cols-1 rounded bg-white bg-opacity-45">
          <h1 className="mx-5 my-2 justify-center align-middle content-center text-center font-sans text-lg">
            Join Room
          </h1>
          <input placeholder="Enter Room Id" value={roomId} onChange={(e) => setRoomId(e.target.value)} className="rounded mx-5 my-2 px-2 text-black bg-inherit placeholder:text-black" />
          <input placeholder="Enter Username" value={username} onChange={(e) => setUsername(e.target.value)} className="rounded mx-5 my-2 px-2 text-black bg-inherit placeholder:text-black" />
          <button onClick={joinRoom} className="rounded font-bold mx-5 mt-2 mb-4 bg-black bg-opacity-30 hover:bg-opacity-40 text-white">
            Join!
          </button>
        </div>
        <div className="w-full h-full col-span-2 rounded bg-white bg-opacity-35">
          <div className="bg-white bg-opacity-50 h-4/6 mx-5 rounded my-4 flex flex-col gap-2">
            <div className='grid grid-cols-2'>
              <h1 className="px-5 py-2">Room Id: {currentRoomId || '###'}</h1>
              <button onClick={leaveRoom} className='rounded-tr bg-red-600 bg-opacity-60 hover:bg-opacity-50'>
                Leave Room!
              </button>
            </div>
            <div className="px-5 pb-2 overflow-y-scroll h-full">
              {messages.map((msg, index) => (
                <div key={index}>
                  <span className='font-bold text-black'>{msg.user}</span>
                  :
                  <span className='ml-2'>{msg.message}</span>
                </div>
              ))}
            </div>
          </div>
          <div className="grid grid-cols-8 gap-3 mx-5 h-14">
            <input
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  sendMessage((e.target as HTMLInputElement).value);
                  (e.target as HTMLInputElement).value = '';
                }
              }}
              placeholder="Enter text"
              className="rounded col-span-7 p-2 bg-white bg-opacity-45 text-black"
            />
            <button
              onClick={() => {
                const input = document.querySelector('input[placeholder="Enter text"]') as HTMLInputElement;
                if (input) {
                  sendMessage(input.value);
                  input.value = '';
                }
              }}
              className="rounded bg-black bg-opacity-30 hover:bg-opacity-40"
            >
              Send
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
