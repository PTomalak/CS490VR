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
use crate::network::Protocol::BothRequestPlaceBlocks;
use crate::scene::{InstanceID, Scene};

pub type ClientID = String;
pub type Message = (ClientID, Protocol);
pub type MessagePlainSender = mpsc::Sender<Message>;
pub type MessagePlainReceiver = mpsc::Receiver<Message>;
pub type _MessageSender = Arc<Mutex<MessagePlainSender>>;
pub type MessageReceiver = Arc<Mutex<MessagePlainReceiver>>;
pub type World = Arc<Mutex<Scene>>;
pub type Clients = Arc<Mutex<HashMap<ClientID, (Option<Client>, MessagePlainSender)>>>;

pub const SERVER_ID: &str = "";
pub const SERVER_DIRECTORY: &str = "./generated/";
pub const MAX_INCOMING_SIZE: usize = 1 << 16;

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct WorldSettings
{
    save_path: PathBuf,
    autosave_duration: Duration,
    tick_duration: Duration,
}

impl Default for WorldSettings
{
    fn default() -> Self {
        Self {
            save_path: PathBuf::from(format!("{}{}.world", SERVER_DIRECTORY, UNIX_EPOCH.elapsed().unwrap().as_millis())),
            autosave_duration: Duration::from_secs(10),
            tick_duration: Duration::from_millis(100),
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
    #[serde(skip_serializing_if = "Option::is_none", default)]
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
    #[serde(skip_serializing_if = "Option::is_none", default)]
    position: Option<Coord>,
    #[serde(skip_serializing_if = "Option::is_none", default)]
    rotation: Option<Orient>,
    #[serde(skip_serializing_if = "Option::is_none", default)]
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

#[ignore]
#[test]
fn protocol_serialize_test()
{
    #[allow(unused_imports)]
    use crate::block::{Block::Clock, VoxelClock};

    let p = BothRequestPlaceBlocks(vec![ProtocolPlaceBlock {
        id: Some(5),
        position: Vector3::unit_y(),
        rotation: Orient::FORWARD,
        data: Clock(VoxelClock::default()),
    }]);

    println!("{}", serde_json::to_string_pretty(&p).unwrap());
}

pub struct Network
{
    pub world: World,
    pub saves: WorldSettings,

    clients: Clients,

    listener: TcpListener,

    stream_sender: MessagePlainSender,
    stream_receiver: MessageReceiver,
}

impl Network
{
    /// Handles combined messages from clients (only one instance of this function)
    ///
    /// Communicates with the client handlers via message passing
    fn server_handler(queue: MessageReceiver, world: World, clients: Clients, settings: WorldSettings) -> Option<()> {
        let mut w = world.lock().ok()?;
        let mut last_save = Instant::now();
        let mut last_tick = Instant::now();

        let (end_tx, end_rx) = mpsc::channel();
        ctrlc::set_handler(move || end_tx.send(())
            .expect("failed to send signal on channel"))
            .expect("failed to set Ctrl+C handler");

        info!("running main server loop");

        // Server run loop
        loop {
            // Check if server needs to quit
            let should_quit = end_rx.try_recv().is_ok();

            // Check if a save needs to be performed
            if last_save.elapsed() >= settings.autosave_duration || should_quit {
                info!("saving world...");
                w.save(&settings.save_path);
                info!("successfully saved world to \"{}\" (next save in {}s)", settings.save_path.to_str().unwrap(), settings.autosave_duration.as_secs_f32());
                last_save = Instant::now();
            }

            // Exit loop if signal received
            if should_quit {
                info!("received termination signal, exiting...");

                // Send disconnect message to all clients
                for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
                    // Ignore errors as the client is supposed to disconnect anyway
                    let _ = sv_to_cl_sender.1.send((SERVER_ID.to_string(), Protocol::ServerRequestKick));
                }

                // Wait for clients to leave (hardcoded wait)
                thread::sleep(Duration::from_secs(1));

                // Remove clients from storage
                clients.lock().ok()?.clear();

                info!("dropped all clients");

                break;
            }

            // Check if a tick needs to be simulated (and associated tasks)
            if last_tick.elapsed() >= settings.tick_duration {
                let _now = Instant::now();
                let updates = w.simulate_tick();
                last_tick = Instant::now();

                // info!("simulated tick in ~{}ms (versus {}ms maximum)", now.elapsed().as_millis(), settings.tick_duration.as_millis());

                // Send client data to all clients
                let response = (SERVER_ID.to_string(), Protocol::ServerResponseMetadata(ProtocolResponseMetadata {
                    ticks: w.get_ticks(),
                    clients: clients
                        .lock()
                        .ok()?
                        .iter()
                        .filter_map(|(_, (c, _))| c.clone())
                        .collect(),
                }));
                for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
                    drop(sv_to_cl_sender.1.send(response.clone()));
                }

                // Send block updates to all clients
                let deltas = updates.iter().map(|e| ProtocolUpdateBlock {
                    id: *e,
                    position: None,
                    rotation: None,
                    data: Some(w.get_block(*e).unwrap().2),
                }).collect();
                let response = (SERVER_ID.to_string(), Protocol::BothRequestUpdateBlocks(deltas));
                for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
                    drop(sv_to_cl_sender.1.send(response.clone()));
                }
            }

            // Process global message queue (from all clients)
            if let Some(message) = queue.lock().ok()?.try_recv().ok() { // for message in queue.lock().unwrap().iter() {
                let client_id = message.0;

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
                        for i in &data {
                            if let Some(instanceid) = w.add_block(i.data.clone(), i.position, i.rotation) {
                                result.push(ProtocolPlaceBlock {
                                    id: Some(instanceid),
                                    position: i.position,
                                    rotation: i.rotation,
                                    data: i.data.clone(),
                                });
                            } else {
                                // Send error message to client
                                clients.lock().ok()?[&client_id].1.send((SERVER_ID.to_string(), Protocol::BothResponse(
                                    ProtocolResponse {
                                        ok: false,
                                        message: "block overlaps existing block".to_string(),
                                    }))).ok()?;
                                break;
                            }
                        }

                        // Check for success
                        if result.len() != data.len() {
                            continue;
                        }

                        // Send success message to client
                        clients.lock().ok()?[&client_id].1.send((SERVER_ID.to_string(), Protocol::BothResponse(
                            ProtocolResponse {
                                ok: true,
                                message: "".to_string(),
                            }))).ok()?;

                        // Send data to all clients
                        let response = (SERVER_ID.to_string(), Protocol::BothRequestPlaceBlocks(result));
                        for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
                            drop(sv_to_cl_sender.1.send(response.clone()));
                        }
                    }
                    Protocol::BothRequestUpdateBlocks(data) => {
                        let mut result = Vec::new();

                        // Process blocks
                        for i in &data {
                            let update_result = if i.position.is_some() || i.rotation.is_some() {
                                if let Some((coord, orient, block)) = w.get_block(i.id) {
                                    w.replace_block(i.id,
                                                    i.data.clone().unwrap_or(block),
                                                    i.position.unwrap_or(coord),
                                                    i.rotation.unwrap_or(orient))
                                } else {
                                    None
                                }
                            } else if let Some(d) = i.data.clone() {
                                w.update_block(i.id, d)
                                    .map(|_| ())
                            } else {
                                None
                            };

                            if update_result.is_none() {
                                // Send error message to client
                                clients.lock().ok()?[&client_id].1.send((SERVER_ID.to_string(), Protocol::BothResponse(
                                    ProtocolResponse {
                                        ok: false,
                                        message: "block overlaps existing block".to_string(),
                                    }))).ok()?;
                                break;
                            }

                            result.push(i.clone());
                        }

                        // Check for success
                        if result.len() != data.len() {
                            continue;
                        }

                        // Send success message to client
                        clients.lock().ok()?[&client_id].1.send((SERVER_ID.to_string(), Protocol::BothResponse(
                            ProtocolResponse {
                                ok: true,
                                message: "".to_string(),
                            }))).ok()?;

                        // Send data to all clients
                        let response = (SERVER_ID.to_string(), Protocol::BothRequestUpdateBlocks(result));
                        for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
                            drop(sv_to_cl_sender.1.send(response.clone()));
                        }
                    }
                    Protocol::BothRequestRemoveBlocks(data) => {
                        let mut result = Vec::new();

                        // Process blocks
                        for i in &data {
                            if let None = w.remove_block(i.id) {
                                // Send error message to client
                                clients.lock().ok()?[&client_id].1.send((SERVER_ID.to_string(), Protocol::BothResponse(
                                    ProtocolResponse {
                                        ok: false,
                                        message: "failed to remove block (block might not exist)".to_string(),
                                    }))).ok()?;
                                break;
                            }

                            result.push(i.clone());
                        }

                        // Check for success
                        if result.len() != data.len() {
                            continue;
                        }

                        // Send success message to client
                        clients.lock().ok()?[&client_id].1.send((SERVER_ID.to_string(), Protocol::BothResponse(
                            ProtocolResponse {
                                ok: true,
                                message: "".to_string(),
                            }))).ok()?;

                        // Send data to all clients
                        let response = (SERVER_ID.to_string(), Protocol::BothRequestRemoveBlocks(result));
                        for (_, sv_to_cl_sender) in clients.lock().ok()?.iter() {
                            drop(sv_to_cl_sender.1.send(response.clone()));
                        }
                    }
                    Protocol::ClientRequestJoin(data) => {
                        if clients.lock().ok()?.get_mut(&client_id).unwrap().0.is_none() {
                            info!("client {} has name {}", client_id, data.name);
                        }

                        clients.lock().ok()?.get_mut(&client_id).unwrap().0 = Some(data.clone());

                        // Send world state as of this tick

                        let world_data = BothRequestPlaceBlocks(w
                            .get_world_blocks()
                            .into_iter()
                            .map(|(id, (position, rotation, data))| ProtocolPlaceBlock {
                                id: Some(id),
                                position,
                                rotation,
                                data,
                            })
                            .collect());

                        clients.lock().ok()?[&client_id].1.send((SERVER_ID.to_string(), world_data)).ok()?;
                    }
                    Protocol::ClientRequestLeave => {
                        info!("client {} is leaving", client_id);

                        // Remove client from storage
                        clients.lock().ok()?.remove(&client_id);
                    }
                    _ => {
                        warn!("received server message from client!");
                    }
                }
            }
        }

        return Some(());
    }

    /// Handles network communication to and from a given client (potentially multiple instances of this function)
    ///
    /// Communicates with the server handler via message passing
    fn client_handler(inbound: MessagePlainReceiver, outbound: MessagePlainSender, mut stream: TcpStream, addr: SocketAddr) -> Option<()> {
        stream.set_nonblocking(true).ok()?;

        const DELIM: u8 = 0;
        const FILL: u8 = 1;

        let mut current = 0usize;
        let mut buffer = vec![FILL; MAX_INCOMING_SIZE];
        let mut all_messages = Vec::new();

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

            /*
            let message = buffer
                .iter()
                .position(|e| *e == DELIM)
                .map(|e| {
                    serde_json::from_slice::<Protocol>(&buffer[..e])
                });
             */

            // Update message buffers

            let messages = buffer
                .as_slice()
                .split(|b| *b == DELIM)
                .map(|e| {
                    let v = e.to_vec();
                    (v.len(), serde_json::from_slice::<Protocol>(&v))
                })
                .filter(|(_, p)| p.is_ok())
                .map(|(i, e)| (i, e.unwrap()))
                .collect::<Vec<(usize, Protocol)>>();

            let mut new_messages = messages.iter().map(|e| e.1.clone()).collect::<Vec<Protocol>>();
            let valid_length = messages.iter().fold(0, |acc, (sz, _)| acc + *sz);

            all_messages.append(&mut new_messages);

            let mut tmp_buffer = buffer[current - valid_length..current].iter().copied().collect::<Vec<u8>>();
            tmp_buffer.extend(vec![FILL; MAX_INCOMING_SIZE - current].iter());
            buffer = tmp_buffer.clone();

            current -= valid_length;

            // Check if any server -> client action need to be sent (non-blocking after existing messages received)
            for sv_to_cl_message in inbound.try_iter() {
                // Send messages
                serde_json::to_writer(&stream, &sv_to_cl_message.1).ok()?;
            }

            // Parse full actions
            for m in &all_messages {
                // Add to message queue
                outbound.send((addr.to_string(), m.clone())).ok()?;
            }
        }

        // End connection
        outbound.send((addr.to_string(), Protocol::ClientRequestLeave)).ok()?;
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
        let server_handle = thread::spawn(move || {
            if Self::server_handler(dup_stream, dup_world, dup_clients, dup_saves).is_none() {
                error!("server terminated with an error");
            } else {
                info!("server terminated");
            }
        });

        // Main client connection-handling loop
        self.listener.set_nonblocking(true).unwrap();
        loop {
            let _ = self.accept_client();
            if server_handle.is_finished() {
                break;
            }
        }
    }

    pub fn accept_client(&mut self) -> Option<()> {
        let (stream, addr) = self.listener.accept().ok()?;

        info!("client connected from {}", addr);

        let (sv_to_cl_sender, sv_to_cl_receiver) = mpsc::channel();

        self.clients.lock().unwrap().insert(addr.to_string(), (None, sv_to_cl_sender));

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

        Some(())
    }
}