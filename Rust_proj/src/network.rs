use std::collections::HashMap;
use std::io::{ErrorKind, Read};
use std::net::{Shutdown, SocketAddr, TcpListener, TcpStream};
use std::path::PathBuf;
use std::sync::{Arc, mpsc, Mutex};
use std::thread;
use std::time::{Duration, Instant, UNIX_EPOCH};

use cgmath::Vector3;
use log::{debug, error, info, warn};
use serde::{Deserialize, Serialize};

use crate::block::{Block, Orient};
use crate::grid::Coord;
use crate::scene::{InstanceID, Scene};

pub type ClientID = String;
pub type Message = (ClientID, Protocol);
pub type MessagePlainSender = mpsc::Sender<Message>;
pub type MessagePlainReceiver = mpsc::Receiver<Message>;
pub type _MessageSender = Arc<Mutex<MessagePlainSender>>;
pub type MessageReceiver = Arc<Mutex<MessagePlainReceiver>>;
pub type World = Arc<Mutex<Scene>>;
pub type Clients = Arc<Mutex<HashMap<ClientID, MessagePlainSender>>>;

pub const SERVER_ID: &str = "";
pub const SERVER_DIRECTORY: &str = "./generated/";
pub const MAX_INCOMING_SIZE: usize = 1 << 16;

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct SaveSettings
{
    path: PathBuf,
    rate: Duration,
}

impl Default for SaveSettings
{
    fn default() -> Self {
        Self {
            path: PathBuf::from(format!("{}{}.world", SERVER_DIRECTORY, UNIX_EPOCH.elapsed().unwrap().as_millis())),
            rate: Duration::new(10, 0),
        }
    }
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Client
{
    name: String,
    position: Vector3<f32>,
    direction: Vector3<f32>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ProtocolPlaceBlock
{
    #[serde(skip_serializing_if = "Option::is_none")]
    id: Option<InstanceID>,
    position: Coord,
    rotation: Orient,
    data: Block,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ProtocolRemoveBlock
{
    id: InstanceID,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ProtocolUpdateBlock
{
    id: InstanceID,
    #[serde(skip_serializing_if = "Option::is_none")]
    position: Option<Coord>,
    #[serde(skip_serializing_if = "Option::is_none")]
    rotation: Option<Orient>,
    #[serde(skip_serializing_if = "Option::is_none")]
    data: Option<Block>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ProtocolResponse
{
    ok: bool,
    message: String,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct ProtocolResponseMetadata
{
    ticks: u32,
    clients: Vec<Client>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
#[serde(tag = "action", content = "data")]
pub enum Protocol
{
    BothNothing,

    BothResponse(ProtocolResponse),

    BothRequestPlaceBlocks(Vec<ProtocolPlaceBlock>),
    BothRequestUpdateBlocks(Vec<ProtocolUpdateBlock>),
    BothRequestRemoveBlocks(Vec<ProtocolRemoveBlock>),

    ClientRequestJoin(Client),
    ClientRequestLeave,

    ServerRequestKick,
    ServerResponseMetadata(ProtocolResponseMetadata),
}

pub struct Network
{
    pub world: World,
    pub saves: SaveSettings,

    clients: Clients,

    listener: TcpListener,

    stream_sender: MessagePlainSender,
    stream_receiver: MessageReceiver,
}

impl Drop for Network
{
    fn drop(&mut self) {
        info!("Server stopping");
    }
}

impl Network
{
    /// Handles combined messages from clients (only one instance of this function)
    /// Communicates with the client handlers via message passing
    fn server_handler(queue: MessageReceiver, world: World, clients: Clients, save_settings: SaveSettings) -> Option<()> {
        let mut w = world.lock().ok()?;
        let mut last_save = Instant::now();

        // Process global message queue (from all clients)
        for message in queue.lock().unwrap().iter() {
            let client_id = message.0;

            // Check if a save needs to be performed
            if last_save.elapsed() >= save_settings.rate {
                w.save(&save_settings.path);
                last_save = Instant::now();
            }

            // Process message
            match message.1 {
                Protocol::BothNothing => {
                    // Do nothing
                    debug!("received nothing from client {}", client_id);
                }
                Protocol::BothResponse(data) => {
                    if !data.ok {
                        warn!("request to client {} failed with \"{}\"", client_id, data.message);
                    }
                }
                Protocol::BothRequestPlaceBlocks(data) => {
                    let mut result = Vec::new();

                    // Process blocks
                    for i in data {
                        if i.id.is_some() {
                            clients.lock().ok()?[&client_id].send((SERVER_ID.to_string(), Protocol::BothResponse(
                                ProtocolResponse {
                                    ok: false,
                                    message: "did not expect instance ID".to_string(),
                                }))).ok()?;
                        } else {
                            if let Some(instanceid) = w.add_block(i.data.clone(), i.position, i.rotation) {
                                result.push(ProtocolPlaceBlock {
                                    id: Some(instanceid),
                                    position: i.position,
                                    rotation: i.rotation,
                                    data: i.data.clone(),
                                });
                            } else {
                                // Send error message to client
                                clients.lock().ok()?[&client_id].send((SERVER_ID.to_string(), Protocol::BothResponse(
                                    ProtocolResponse {
                                        ok: false,
                                        message: "block overlaps existing block".to_string(),
                                    }))).ok()?;
                            }
                        }
                    }

                    // Send block update to all clients
                    let response = (SERVER_ID.to_string(), Protocol::BothRequestPlaceBlocks(result));
                    for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
                        drop(sv_to_cl_sender.send(response.clone()));
                    }
                }
                Protocol::BothRequestUpdateBlocks { .. } => {
                    todo!();
                }
                Protocol::BothRequestRemoveBlocks { .. } => {
                    todo!();
                }
                Protocol::ClientRequestJoin(data) => {
                    info!("client {} has name {}", client_id, data.name);
                }
                Protocol::ClientRequestLeave => {
                    info!("client {} is leaving", client_id);
                }
                _ => {
                    warn!("received server message from client!");
                }
            }
        }

        // Send disconnect message to all clients
        for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
            // Ignore errors as the client is supposed to disconnect anyway
            let _ = sv_to_cl_sender.send((SERVER_ID.to_string(), Protocol::ServerRequestKick));
        }

        return Some(());
    }

    /// Handles network communication to and from a given client (potentially multiple instances of this function)
    /// Communicates with the server handler via message passing
    fn client_handler(inbound: MessagePlainReceiver, outbound: MessagePlainSender, mut stream: TcpStream, addr: SocketAddr) -> Option<()> {
        stream.set_nonblocking(true).ok()?;

        let mut current = 0usize;
        let mut buffer = vec![1u8; MAX_INCOMING_SIZE];

        loop {
            // Get full action received (if applicable, non-blocking)

            match stream.read(&mut buffer[current..]) {
                Ok(num_bytes_read) => {
                    if num_bytes_read == 0 {
                        info!("client {} reached EOF", addr);
                        break;
                    } else {
                        current += num_bytes_read;

                        // Check for buffer limit
                        if current == buffer.len() {
                            error!("client {} reached buffer limit of {} bytes", addr, MAX_INCOMING_SIZE);
                            break;
                        }
                    }
                }
                Err(e) => {
                    match e.kind() {
                        ErrorKind::WouldBlock => {
                            // Do nothing
                        }
                        _ => {
                            error!("client {} encountered network error", addr);
                            break;
                        }
                    }
                }
            }

            let message = buffer
                .iter()
                .position(|e| *e == 0)
                .map(|e| {
                    serde_json::from_slice::<Protocol>(&buffer[..e])
                });

            // Handle (some) communication errors
            // let message = serde_json::from_reader::<_, Protocol>(&stream);
            /*
            if let Err(e) = &message {
                if e.is_eof() {
                    debug!("client {} reached end of file", addr);
                    break;
                }
                if e.is_syntax() || e.is_data() {
                    debug!("client {} sent bad data", addr);
                    break;
                }

                if !e.to_string().starts_with("Resource") {
                    dbg!(e.to_string());
                }
            }
             */

            // Check if any server -> client action need to be sent (non-blocking after existing messages received)
            for sv_to_cl_message in inbound.try_iter() {
                // Send messages
                serde_json::to_writer(&stream, &sv_to_cl_message.1).ok()?;
            }

            // Parse full action
            if let Some(m) = message {
                if let Ok(cl_to_sv_message) = m {
                    // Reset buffer
                    buffer.fill(1);
                    current = 0;

                    // Add to message queue
                    outbound.send((addr.to_string(), cl_to_sv_message)).ok()?;
                } else {
                    error!("client {} sent bad data", addr);
                    break;
                }
            }
        }

        // End connection
        stream.shutdown(Shutdown::Both).ok()?;

        Some(())
    }

    pub fn new(host: &str) -> Self {
        info!("server starting at {}", host);

        let (stream_send, stream_receive) = mpsc::channel();
        Self {
            world: Default::default(),
            listener: TcpListener::bind(host).expect("failed to start TCP listener"),
            clients: Default::default(),
            stream_sender: stream_send,
            stream_receiver: Arc::new(Mutex::new(stream_receive)),
            saves: Default::default(),
        }
    }

    pub fn run(&mut self) {
        // Launch server thread
        let dup_stream = self.stream_receiver.clone();
        let dup_world = self.world.clone();
        let dup_clients = self.clients.clone();
        let dup_saves = self.saves.clone();
        thread::spawn(move || {
            if Self::server_handler(dup_stream, dup_world, dup_clients, dup_saves).is_none() {
                error!("server terminated with an error");
            } else {
                info!("server terminated");
            }
        });

        // Main client connection-handling loop
        loop {
            self.accept_client();
        }
    }

    pub fn accept_client(&mut self) {
        let (stream, addr) = self.listener.accept().expect("failed to accept new client");

        info!("client connected from {}", addr);

        let (sv_to_cl_sender, sv_to_cl_receiver) = mpsc::channel();

        self.clients.lock().unwrap().insert(addr.to_string(), sv_to_cl_sender);

        let dup_cl_to_sv_sender = self.stream_sender.clone();
        let dup_connections = self.clients.clone();
        thread::spawn(move || {
            info!("launched client handler for {}", &addr);

            if Self::client_handler(sv_to_cl_receiver, dup_cl_to_sv_sender, stream, addr).is_none() {
                // Drop client on disconnect
                dup_connections.lock().unwrap().remove(&addr.to_string());
                warn!("client {} disconnected with error", addr);
            } else {
                info!("client {} disconnected", addr);
            }
        });
    }
}