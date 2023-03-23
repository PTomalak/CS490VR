use std::io::{Read, Write};
use std::net::{TcpListener, TcpStream};
use std::thread;

use log::{error, info};

mod voxel;
mod scene;

// Networking code courtesy of ChatGPT

fn handle_client(mut stream: TcpStream) {
    // Create a buffer to store the received data
    let mut buf = [0; 1024];
    loop {
        // Read data from the stream
        match stream.read(&mut buf) {
            Ok(n) => {
                if n == 0 {
                    // Connection closed
                    return;
                }
                // Echo the received data back to the client
                match stream.write_all(&buf[0..n]) {
                    Ok(_) => {}
                    Err(_) => {
                        // Error occurred while writing
                        return;
                    }
                }
            }
            Err(_) => {
                // Error occurred while reading
                return;
            }
        }
    }
}

fn main() {
    simple_logger::init_with_env().unwrap();

    // Bind to a local address and start listening for incoming connections
    let listener = TcpListener::bind("127.0.0.1:8080").unwrap();
    info!("Server listening on port 8080");

    // Handle each incoming connection in a separate thread
    for stream in listener.incoming() {
        match stream {
            Ok(stream) => {
                info!("New client connected: {:?}", stream.peer_addr().unwrap());
                thread::spawn(|| handle_client(stream));
            }
            Err(e) => {
                error!("Error accepting client connection: {}", e);
            }
        }
    }
}
