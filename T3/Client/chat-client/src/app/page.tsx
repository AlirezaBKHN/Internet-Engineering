'use client'
import { useEffect, useRef, useState } from "react";

export default function Home() {
  const [message, setMessage] = useState<string[]>([])
  const [roomId, setRoomId] = useState('')
  const [userName, setUserName] = useState('')
  const socket = useRef<WebSocket | null>(null)
  const [currentRoomId, SetCurrentRoomId] = useState('')

  useEffect(() => {
    socket.current = new WebSocket('ws//localhost:8000')
    socket.current.onmessage = (e) => {
      const {type, message, roomId} = JSON.parse(e.data)
      if (type === 'message') {
        setMessage((prev) => [...prev, message])
      }
      else if(type === 'roomCreate' || type === 'joined'){
        SetCurrentRoomId(roomId)
      }
      else if(type === 'error'){
        alert(message)
      }
    }

    socket.current?.close()
  },[])

  const createRoom = () => { socket.current?.send(JSON.stringify({ type: 'create' })); }; 
  const joinRoom = () => { 
    if (userName && roomId) { 
      socket.current?.send(JSON.stringify({ type: 'join', payload: { roomId, userName: userName } })); 
    } 
    else { 
      alert('Enter a username and room ID first.'); 
    } 
  }; 
  const sendMessage = (text:string) => { 
    if (socket.current?.readyState === WebSocket.OPEN && currentRoomId) { 
      socket.current.send(JSON.stringify({ type: 'message', payload: { roomId: currentRoomId, userName: userName, text } 
      })); 
    }
  }

  return (
    <div className=" w-screen h-screen flex align-middle justify-center content-center">
      <div className="m-auto grid grid-cols-2 grid-rows-2 gap-3  h-2/3 w-2/3">
        <div className="grid grid-cols-1 justify-center bg-opacity-45 bg-white rounded h-full ">

          <h1 className="mx-5 my-2 justify-center align-middle content-center text-center font-sans text-lg">
            Create Room
          </h1>
          <button className="rounded font-bold mx-5 my-2 bg-black bg-opacity-30 hover:bg-opacity-40 text-white ">
            Get Room Id!
          </button>
          <input disabled={true} className="rounded mx-5 mt-2 mb-4 px-2 text-black bg-inherit ">
          </input>
        </div>
        <div className="grid grid-cols-1 rounded bg-white bg-opacity-45 ">
          <h1 className="mx-5 my-2 justify-center align-middle content-center text-center font-sans text-lg">
            Join Room
          </h1>
          <input placeholder="Enter Room Id" className="rounded mx-5 my-2 px-2 text-black bg-inherit placeholder:text-black">
          </input>
          <input placeholder="Enter User Name" className="rounded mx-5 my-2 px-2 text-black bg-inherit placeholder:text-black">
          </input>
          <button className="rounded font-bold mx-5 mt-2 mb-4 bg-black bg-opacity-30 hover:bg-opacity-40 text-white ">
            Join!
          </button>
        </div>
        <div className="w-full h-full col-span-2 rounded bg-white bg-opacity-35">
          <div className="bg-white bg-opacity-50  h-4/6 mx-5 rounded my-4 flex flex-col gap-2">
            <h1 className="px-5 py-2">
              Room Id: ###
            </h1>
            <div className="px-5 pb-2 overflow-y-scroll h-full">

            </div>
          </div>
          <div className="grid grid-cols-8 gap-3 mx-5 h-14">
            <input placeholder="Enter text" className="rounded col-span-7 p-2 bg-white bg-opacity-45 text-black"></input>
            <button className="rounded bg-black bg-opacity-30 hover:bg-opacity-40">Send</button>
          </div>
        </div>
      </div>
    </div>
  );
}
