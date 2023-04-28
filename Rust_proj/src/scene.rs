use std::collections::{HashMap, HashSet};
use std::path::Path;
use std::process::Command;

use cgmath::Array;
use petgraph::{Graph, Undirected};
use petgraph::dot::Dot;
use petgraph::graph::NodeIndex;
use serde::{Deserialize, Serialize};

use crate::block::{Block, circuit_voxel, is_circuit_voxel, Orient, PowerState, VoxelID};
use crate::grid::{Coord, Grid};

pub type InstanceID = u32;

pub type NodeIDType = u32;
pub type NodeID = NodeIndex<NodeIDType>;

pub const OFF: PowerState = false;
pub const ON: PowerState = true;

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct Scene
{
    blocks: HashMap<InstanceID, (Coord, Orient, Block)>,
    circuit: Graph<(InstanceID, VoxelID, Coord), PowerState, Undirected, NodeIDType>,
    space: Grid<(InstanceID, VoxelID, Option<NodeID>)>,
    ticks: u32,
}

impl Scene
{
    /// Load a scene from a file
    pub fn load(path: &Path) -> Scene {
        serde_json::from_str(&std::fs::read_to_string(path)
            .expect("failed to read world file"))
            .expect("failed to deserialize world data")
    }

    /// Save this scene to a file
    pub fn save(&self, path: &Path) {
        std::fs::write(path, serde_json::to_string(&self)
            .expect("failed to serialize world data"))
            .expect("failed to save world file")
    }

    /// Get the global location of a voxel
    ///
    /// NOTE: Does not handle non-forward orientations
    fn get_voxel_location(&self, id: InstanceID, voxel_id: VoxelID) -> Coord {
        self.blocks[&id].0 + self.blocks[&id].2.get_structure()[&voxel_id]
    }

    /// Get the circuit nodes associated with the provided block
    fn get_circuit_nodes(&self, id: InstanceID) -> HashMap<VoxelID, NodeID> {
        self.blocks[&id]
            .2
            .get_circuit_voxels()
            .iter()
            .map(|(voxel_id, coord)|
                (voxel_id.clone(), self.space.get(*coord).unwrap().2.unwrap()))
            .collect()
    }

    /// Save the circuit to a `.dot` file
    ///
    /// Requires the `dot` program to be in the system `PATH`
    #[allow(dead_code)]
    pub fn save_debug_circuit(&self, path: &Path) {
        let d = Dot::with_config(&self.circuit, &[]);
        std::fs::write(&path, format!("{:?}", d)).unwrap();
        Command::new("dot")
            .args(["-Tps", path.to_str().unwrap(), "-o", &format!("{}.pdf", path.to_str().unwrap())])
            .spawn()
            .unwrap();
    }

    /// Performs one simulation tick of the circuit
    ///
    /// Returns all the blocks that changed state
    pub fn simulate_tick(&mut self) -> Vec<InstanceID> {
        self.ticks += 1;

        /*
         Simulation Algorithm (High-level Overview)

         1. Identify all non-wire circuit blocks and contiguous wire networks
         2. Determine if, using the pre-tick state as input, each block's new state (record changes)
         3. Store lookup for each active block
         4. For each contiguous wire network, consider all associated sources (using the lookup in step 3) to determine the wire state
         5. Update the wires as needed (record changes)

         */

        // Get wire circuit blocks

        let wire_circuit_blocks = self.blocks
            .iter()
            .filter_map(|(id, (_, _, e))| matches!(e, Block::Wire(_)).then_some((*id, e.clone())))
            .collect::<HashMap<InstanceID, Block>>();

        // Compute non-wire circuit block states

        let non_wire_circuit_blocks = self.blocks
            .iter()
            .filter_map(|(id, (_, _, e))| (e.is_circuit_block() && !matches!(e, Block::Wire(_))).then_some((*id, e.clone())))
            .collect::<HashMap<InstanceID, Block>>();

        let non_wire_circuit_blocks_updated = non_wire_circuit_blocks
            .iter()
            .map(|(id, block)| {
                let mut result = block.clone();
                let node_ids = self.get_circuit_nodes(*id);

                match &mut result {
                    Block::Toggle(_) => {
                        // Toggle is an external independent source therefore its state can only be modified by the user
                    }
                    Block::Pixel(data) => {
                        // Get named node
                        let input_node = node_ids[&circuit_voxel("pixel")];

                        // Get input node state

                        let input_state = self.circuit.edges(input_node).any(|e| *e.weight() == ON);

                        // Compute output state

                        data.powered = input_state;
                    }
                    Block::ANDGate(data) => {
                        // Get named nodes
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.circuit.edges(input_a_node).any(|e| *e.weight() == ON);
                        let input_b_state = self.circuit.edges(input_b_node).any(|e| *e.weight() == ON);

                        // Compute output state

                        data.powered = input_a_state && input_b_state;
                    }
                    Block::ORGate(data) => {
                        // Get named nodes
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.circuit.edges(input_a_node).any(|e| *e.weight() == ON);
                        let input_b_state = self.circuit.edges(input_b_node).any(|e| *e.weight() == ON);

                        // Compute output state

                        data.powered = input_a_state || input_b_state;
                    }
                    Block::NOTGate(data) => {
                        // Get named nodes
                        let (input_node, _output_node) =
                            (node_ids[&circuit_voxel("in")], node_ids[&circuit_voxel("out")]);

                        // Get input node state

                        let input_state = self.circuit.edges(input_node).any(|e| *e.weight() == ON);

                        // Compute output state

                        data.powered = !input_state;
                    }
                    Block::Clock(data) => {
                        // Clock is an external independent source and therefore has no circuit inputs,
                        // but it does have a circuit output value based on the input state

                        // Compute clock state

                        data.powered = if data.start_tick > self.ticks {
                            false
                        } else {
                            (self.ticks - data.start_tick) % data.rate == 0
                        };
                    }
                    Block::Pulse(data) => {
                        // Pulse is an external independent source and therefore has no circuit inputs,
                        // but it does have a circuit output value based on the input state

                        // Compute pulse state

                        data.powered = data.start_tick + data.pulse_ticks >= self.ticks;
                    }
                    _ => panic!("not possible, encountered non-circuit or wire block")
                }

                (*id, result)
            })
            .collect::<HashMap<InstanceID, Block>>();

        let non_wire_circuit_blocks_delta = non_wire_circuit_blocks_updated
            .iter()
            .filter(|(id, block)| non_wire_circuit_blocks[id] != *block.clone())
            .map(|e| (*e.0, e.1.clone()))
            .collect::<HashMap<InstanceID, Block>>();

        // Compute contiguous wire networks

        let mut unvisited = wire_circuit_blocks
            .keys()
            .map(|e| self.get_circuit_nodes(*e)[&circuit_voxel("wire")])
            .collect::<HashSet<NodeID>>();
        let mut contiguous_wire_networks = Vec::<HashSet<(InstanceID, NodeID)>>::new();

        // Internal BFS graph search helper
        fn bfs_wires(
            root: NodeID,
            blocks: &HashMap<InstanceID, (Coord, Orient, Block)>,
            circuit: &Graph<(InstanceID, VoxelID, Coord), PowerState, Undirected, NodeIDType>,
            visited: &mut HashSet<NodeID>)
        {
            let neighbor_wires = circuit
                .neighbors(root)
                .filter(|e| !visited.contains(e))
                .filter(|e| matches!(blocks[&circuit.node_weight(*e).unwrap().0].2, Block::Wire(_)))
                .collect::<HashSet<NodeID>>();

            *visited = visited.union(&neighbor_wires).copied().collect();

            for neighbor in neighbor_wires {
                bfs_wires(neighbor, blocks, circuit, visited)
            }
        }

        // Wire network search loop
        loop {
            if let Some(node_id) = unvisited.iter().next().copied() {
                let mut current_set = HashSet::new();
                bfs_wires(node_id, &self.blocks, &self.circuit, &mut current_set);

                contiguous_wire_networks.push(current_set
                    .iter()
                    .map(|e| (self.circuit.node_weight(*e).unwrap().0, *e))
                    .collect());

                unvisited = unvisited.difference(&current_set).copied().collect();
            } else {
                break;
            }
        }

        // Compute new wire states

        // Determine wire network state using the provided wire network and block states
        // Stored as an external lambda for better readability
        let compute_wire_network_state =
            |wire_network: &HashSet<(InstanceID, NodeID)>, blocks: &HashMap<InstanceID, (Coord, Orient, Block)>| -> PowerState {
                for (_, node_id) in wire_network {
                    for neighbor_node_id in self.circuit.neighbors(*node_id) {
                        let (neighbor_id, _, _) = self.circuit.node_weight(neighbor_node_id).unwrap();
                        let neighbor_block = &blocks[neighbor_id].2;

                        if neighbor_block.get_power().unwrap() {
                            return ON;
                        }

                        /*
                        match neighbor_block {
                            Block::Wire(_) => {
                                // Signals are propagated by wire networks, not individual wires, so no actions are needed here
                            }
                            Block::Toggle(data) => {
                                // Check if this toggle powers the network
                                if data.powered {
                                    return ON;
                                }
                            }
                            Block::Pixel(_) => {
                                // Not a source block
                            }
                            Block::ANDGate(data) => {
                                // Check if this gate powers the network
                                if data.powered {
                                    return ON;
                                }
                            }
                            Block::ORGate(data) => {
                                // Check if this gate powers the network
                                if data.powered {
                                    return ON;
                                }
                            }
                            Block::NOTGate(data) => {
                                // Check if this gate powers the network
                                if data.powered {
                                    return ON;
                                }
                            }
                            Block::Clock(data) => {
                                // Check if this clock powers the network
                                if data.powered {
                                    return ON;
                                }
                            }
                            Block::Pulse(data) => {
                                // Check if this pulse powers the network
                                if data.powered {
                                    return ON;
                                }
                            }
                            _ => panic!("not possible, encountered non-circuit or wire block")
                        }
                         */
                    }
                }

                OFF
            };

        // Determine wire networks' state prior to this tick's updates
        // This can be done by referencing `self.*` since `self` has not been modified yet
        let contiguous_wire_networks_original_state = contiguous_wire_networks
            .iter()
            .map(|e| e.iter().any(|(id, _)| {
                if let Block::Wire(v) = &self.blocks[id].2 {
                    v.powered
                } else {
                    panic!("not possible, encountered non-wire block");
                }
            }))
            .collect::<Vec<PowerState>>();

        // Determine wire networks' state (prior to this tick's non-wire circuit block updates)
        let contiguous_wire_networks_updated_state = contiguous_wire_networks
            .iter()
            .map(|e| compute_wire_network_state(e, &self.blocks))
            .collect::<Vec<PowerState>>();

        // Compute delta for all wire blocks of the scene together
        let all_wires_delta_state = contiguous_wire_networks_updated_state
            .iter()
            .enumerate()
            .filter(|(i, e)| contiguous_wire_networks_original_state[*i] != **e)
            .flat_map(|(i, e)| contiguous_wire_networks[i].iter().map(|v| (v.0, *e)))
            .collect::<Vec<(InstanceID, PowerState)>>();

        // Compute delta for all wire blocks of the scene together
        let all_wires_delta = all_wires_delta_state
            .iter()
            .map(|e| e.0)
            .collect::<Vec<InstanceID>>();

        // Update scene state

        // Non-wire circuit blocks
        for (id, block) in &non_wire_circuit_blocks_delta {
            self.blocks.get_mut(id).unwrap().2 = block.clone();
        }

        // Wire circuit blocks
        for (id, power) in &all_wires_delta_state {
            if let Block::Wire(w) = &mut self.blocks.get_mut(id).unwrap().2 {
                w.powered = *power;
            } else {
                panic!("not possible, encountered non-wire node");
            }
        }

        // Circuit edges
        for i in self.circuit.edge_indices() {
            let (node_a, node_b) = self.circuit.edge_endpoints(i).unwrap();
            let (id_a, id_b) = (self.circuit.node_weight(node_a).unwrap().0, self.circuit.node_weight(node_b).unwrap().0);
            let (block_a, block_b) = (&self.blocks[&id_a].2, &self.blocks[&id_b].2);

            if block_a.get_power().unwrap_or(OFF) || block_b.get_power().unwrap_or(OFF) {
                self.circuit.update_edge(node_a, node_b, ON);
            } else {
                self.circuit.update_edge(node_a, node_b, OFF);
            }
        }

        // Combine delta IDs and return

        return non_wire_circuit_blocks_delta.keys()
            .copied()
            .chain(all_wires_delta.iter().copied())
            .collect();
    }

    /// Adds the given block at the given location and updates the internal circuit
    /// Returns the block's ID or `None` if a block overlaps an existing block
    pub fn add_block(&mut self, block: Block, location: Coord, orientation: Orient) -> Option<InstanceID> {
        let id = self.blocks.len() as u32;

        assert!(self.blocks.insert(id, (location, orientation, block.clone())).is_none());

        // Check if block overlaps existing block

        for (_, voxel_relative_location) in block.get_structure() {
            if self.space.contains(location + voxel_relative_location) {
                return None;
            }
        }

        // Add block connections and voxels

        // Filter out non-circuit voxels and add to circuit
        let terminal_node_ids = block.get_structure()
            .iter()
            .filter(|e| is_circuit_voxel(&e.0))
            .map(|(tid, gc)| (tid.clone(), self.circuit.add_node((id, tid.clone(), *gc))))
            .collect::<HashMap<VoxelID, NodeID>>();

        // Add all voxels (including non-terminals) to grid

        for (voxel_id, voxel_relative_location) in block.get_structure() {
            self.space.set(location + voxel_relative_location,
                           (id, voxel_id.clone(), terminal_node_ids.get(&voxel_id).copied()));
        }

        // Iterate over the circuit voxels (terminals) that make up this block
        for (terminal_voxel_id, terminal_node_id) in terminal_node_ids {
            let terminal_parent_id = self.circuit.node_weight(terminal_node_id).unwrap().0;

            let terminal_voxel_global_location = self.get_voxel_location(terminal_parent_id, terminal_voxel_id);

            // Iterate over adjacent voxels
            for (_, (neighbor_parent_id, _, neighbor_node_id))
            in self.space.get_adjacent(terminal_voxel_global_location) {
                // Add connection to circuit if not part of the same block
                if *neighbor_parent_id != terminal_parent_id {
                    self.circuit.update_edge(terminal_node_id, neighbor_node_id.unwrap(), OFF);
                }
            }
        }

        Some(id)
    }

    /// Removes the block with the given ID
    pub fn _remove_block(&mut self, _id: InstanceID) -> Option<()> {
        todo!();
    }

    /// Add a wire that follows the given path
    /// Returns the wire block IDs if the path is valid, `None` if not
    #[allow(dead_code)]
    pub fn add_wire(&mut self, path: Vec<Coord>) -> Option<Vec<InstanceID>> {
        /// Check if two integer coordinates are adjacent (Manhattan distance of 1)
        fn is_adjacent(a: Coord, b: Coord) -> bool {
            (a - b).map(|e| e.abs()).sum() == 1
        }

        let mut last = path.first().copied()?;

        // Verify path is continuous
        for step in path.iter().skip(1).copied() {
            if !is_adjacent(last, step) {
                return None;
            }
            last = step;
        }

        Some(path.iter()
            .copied()
            .map(|e| self.add_block(Block::Wire(Default::default()), e, Orient::default()).unwrap())
            .collect())
    }
}

#[test]
pub fn wire_test()
{
    let mut scene = Scene::default();

    assert!(scene.add_wire((0..10).map(|e| Coord::new(0, 0, e)).collect()).is_some());
    assert!(scene.add_wire((0..10).map(|e| Coord::new(-5 + e, 1, 0)).collect()).is_some());

    scene.save_debug_circuit(&Path::new("./result.dot"));
}