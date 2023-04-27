use std::collections::HashMap;
use std::net::{SocketAddr, TcpListener, TcpStream};
use std::sync::{Arc, mpsc, Mutex};
use std::thread;

use cgmath::Vector3;
use log::{error, info, warn};
use serde::{Deserialize, Serialize};

use crate::block::{Block, BlockRotation, GridCoord};
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

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Client
{
    name: String,
    position: Vector3<f32>,
    direction: Vector3<f32>,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
#[serde(tag = "action", content = "data")]
pub enum Protocol
{
    BothNothing,

    BothResponse {
        ok: bool,
        message: String,
    },

    BothRequestPlaceBlock {
        #[serde(skip_serializing_if = "Option::is_none")]
        id: Option<InstanceID>,
        position: Vector3<f32>,
        rotation: BlockRotation,
        data: Block,
    },
    BothRequestUpdateBlock {
        id: InstanceID,
        #[serde(skip_serializing_if = "Option::is_none")]
        position: Option<Vector3<f32>>,
        #[serde(skip_serializing_if = "Option::is_none")]
        rotation: Option<BlockRotation>,
        #[serde(skip_serializing_if = "Option::is_none")]
        data: Option<Block>,
    },
    BothRequestRemoveBlock {
        ids: Vec<InstanceID>,
    },

    ClientRequestJoin(Client),
    ClientRequestLeave,

    ServerRequestKick,
    ServerResponseMetadata {
        ticks: u32,
        clients: Vec<Client>,
    },
}

pub struct Network
{
    world: World,
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
    fn server_handler(queue: MessageReceiver, world: World, clients: Clients) -> Option<()> {
        let mut w = world.lock().ok()?;
        let c = clients.lock().ok()?;

        for message in queue.lock().unwrap().iter() {
            let client_id = message.0;

            match message.1 {
                Protocol::BothNothing => {
                    // Do nothing
                }
                Protocol::BothResponse { .. } => {
                    todo!();
                }
                Protocol::BothRequestPlaceBlock { id, position, data, .. } => {
                    if id.is_some() {
                        c[&client_id].send((SERVER_ID.to_string(), Protocol::BothResponse {
                            ok: false,
                            message: "did not expect instance ID".to_string(),
                        })).ok()?;
                    } else {
                        if let Some(instanceid) = w.add_block(data.clone(), GridCoord::from(position)) {
                            // Send block update to all clients
                            for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
                                drop(sv_to_cl_sender.send((SERVER_ID.to_string(), Protocol::BothRequestPlaceBlock {
                                    id: Some(instanceid),
                                    position,
                                    rotation: Default::default(),
                                    data: data.clone(),
                                })));
                            }
                        } else {
                            // Send error message to client
                            c[&client_id].send((SERVER_ID.to_string(), Protocol::BothResponse {
                                ok: false,
                                message: "block overlaps existing block".to_string(),
                            })).ok()?;
                        }
                    }
                }
                Protocol::BothRequestUpdateBlock { .. } => {
                    todo!();
                }
                Protocol::BothRequestRemoveBlock { .. } => {
                    todo!();
                }
                Protocol::ClientRequestJoin(_) => {
                    todo!();
                }
                Protocol::ClientRequestLeave => {
                    todo!();
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
    fn client_handler(inbound: MessagePlainReceiver, outbound: MessagePlainSender, stream: TcpStream, addr: SocketAddr) -> Option<()> {
        stream.set_nonblocking(true).ok()?;

        loop {
            // Get full action received (if applicable, non-blocking)
            let message = serde_json::from_reader::<_, Protocol>(&stream);

            // Check if any server -> client action need to be sent (non-blocking after existing messages received)
            for sv_to_cl_message in inbound.try_iter() {
                // Send messages
                serde_json::to_writer(&stream, &sv_to_cl_message.1).ok()?;
            }

            // Parse full action
            if let Ok(cl_to_sv_message) = message {
                // Add to message queue
                outbound.send((addr.to_string(), cl_to_sv_message)).ok()?;
            }
        }
    }

    pub fn new(host: &str) -> Self {
        info!("Server starting at {}", host);

        let (stream_send, stream_receive) = mpsc::channel();
        Self {
            world: Default::default(),
            listener: TcpListener::bind(host).expect("failed to start TCP listener"),
            clients: Default::default(),
            stream_sender: stream_send,
            stream_receiver: Arc::new(Mutex::new(stream_receive)),
        }
    }

    pub fn run(&mut self) {
        // Launch server thread
        let dup_stream = self.stream_receiver.clone();
        let dup_world = self.world.clone();
        let dup_clients = self.clients.clone();
        thread::spawn(move || {
            if Self::server_handler(dup_stream, dup_world, dup_clients).is_none() {
                error!("server terminated with an error");
                return;
            }

            info!("server terminated");
        });

        // Main client connection-handling loop
        loop {
            self.accept_client();
        }
    }

    pub fn accept_client(&mut self) {
        let (stream, addr) = self.listener.accept().expect("failed to accept new client");

        info!("Client connected from {}", addr);

        let (sv_to_cl_sender, sv_to_cl_receiver) = mpsc::channel();

        self.clients.lock().unwrap().insert(addr.to_string(), sv_to_cl_sender);

        let dup_cl_to_sv_sender = self.stream_sender.clone();
        let dup_connections = self.clients.clone();
        thread::spawn(move || {
            if Self::client_handler(sv_to_cl_receiver, dup_cl_to_sv_sender, stream, addr).is_none() {
                // Drop client on disconnect
                dup_connections.lock().unwrap().remove(&addr.to_string());

                warn!("client disconnected with error");
                return;
            }

            info!("client disconnected");
        });
    }
}