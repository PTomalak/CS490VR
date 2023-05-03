use std::path::PathBuf;
use std::sync::{Arc, Mutex};

use log::info;

use crate::network::Network;
use crate::scene::Scene;

mod block;
mod scene;
mod network;
mod grid;

fn main() {
    // Default server address
    let default_address = "127.0.0.1:10000".to_string();

    // Initialize logger with environment variables to control display levels
    simple_logger::init_with_env().unwrap();

    // Read arguments (optional port and optional world file to load from)
    let args: Vec<String> = std::env::args().collect();
    let address = args.get(1).unwrap_or(&default_address);
    let world_file = args.get(2).map(|e| Scene::load(&PathBuf::from(e)));

    // Launch server
    let mut server = Network::new(address);
    if let Some(w) = world_file {
        server.world = Arc::new(Mutex::new(w));
        info!("loaded world from file");
    }
    server.run();
}
