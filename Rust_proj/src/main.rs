use crate::network::Network;

mod block;
mod scene;
mod network;

fn main() {
    simple_logger::init_with_env().unwrap();

    let mut server = Network::new("127.0.0.1:10000");
    server.run();
}
