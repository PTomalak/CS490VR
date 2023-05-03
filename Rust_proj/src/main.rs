use std::path::PathBuf;
use std::str::FromStr;
use std::sync::{Arc, Mutex};

use log::info;

use crate::network::Network;
use crate::scene::Scene;

mod block;
mod scene;
mod network;
mod grid;

fn main() {
    // Initialize logger with environment variables to control display levels
    simple_logger::init_with_env().unwrap();

    // Read arguments (optional port and optional world file to load from)
    let args: Vec<String> = std::env::args().collect();
    let port = args.get(1)
        .map(|e| u16::from_str(e).expect("invalid port number"))
        .unwrap_or(10000);
    let world_file = args.get(2)
        .map(|e| Scene::load(&PathBuf::from(e)));

    // Launch server
    let mut server = Network::new(&format!("192.168.118.230:{}", port));
    if let Some(w) = world_file {
        server.world = Arc::new(Mutex::new(w));
        info!("loaded world from file");
    }
    server.run();
}
