use std::collections::{HashMap, HashSet};
use std::path::Path;
use std::process::Command;
use std::str::FromStr;

use cgmath::Array;
use petgraph::dot::Dot;
use petgraph::graph::NodeIndex;
use petgraph::stable_graph::StableGraph;
use petgraph::Undirected;
use serde::{Deserialize, Serialize};

#[allow(unused_imports)]
use crate::block::{Block, circuit_voxel, is_circuit_voxel, Orient, PowerState, VoxelClock, VoxelID, VoxelPowered};
use crate::grid::{Coord, Grid, GridData};

pub type InstanceID = u32;

pub type NodeIDType = u32;
pub type NodeID = NodeIndex<NodeIDType>;

pub const OFF: PowerState = false;
pub const ON: PowerState = true;

/// Scene data storage type for serialization/deserialization
#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct SceneData
{
    blocks: HashMap<String, (Coord, Orient, Block)>,
    circuit: StableGraph<(InstanceID, VoxelID, Coord), PowerState, Undirected, NodeIDType>,
    space: GridData<(InstanceID, VoxelID, Option<NodeID>)>,
    ticks: u32,
}

impl From<Scene> for SceneData
{
    fn from(value: Scene) -> Self {
        Self {
            blocks: value.blocks
                .into_iter()
                .map(|(i, e)| (i.to_string(), e))
                .collect(),
            circuit: value.circuit,
            space: GridData::from(value.space),
            ticks: value.ticks,
        }
    }
}

impl From<SceneData> for Scene
{
    fn from(value: SceneData) -> Self {
        Self {
            blocks: value.blocks
                .into_iter()
                .map(|(i, e)| (InstanceID::from_str(&i).unwrap(), e))
                .collect(),
            circuit: value.circuit,
            space: Grid::from(value.space),
            ticks: value.ticks,
        }
    }
}

#[ignore]
#[test]
fn scene_serialize_test()
{
    #[allow(unused_imports)]
    use cgmath::Vector3;

    let mut s = Scene::default();
    s.add_block(Block::ANDGate(VoxelPowered::default()), Vector3::unit_y(), Default::default()).unwrap();

    let g = SceneData::from(s);
    println!("{}", serde_json::to_string_pretty(&g).unwrap());
}

#[derive(Clone, Debug, Default)]
pub struct Scene
{
    blocks: HashMap<InstanceID, (Coord, Orient, Block)>,
    circuit: StableGraph<(InstanceID, VoxelID, Coord), PowerState, Undirected, NodeIDType>,
    space: Grid<(InstanceID, VoxelID, Option<NodeID>)>,
    ticks: u32,
}

impl Scene
{
    /// Load a scene from a file
    pub fn load(path: &Path) -> Scene {
        let s: SceneData = serde_json::from_str(&std::fs::read_to_string(path)
            .expect("failed to read world file"))
            .expect("failed to deserialize world data");
        Scene::from(s)
    }

    /// Save this scene to a file
    pub fn save(&self, path: &Path) {
        std::fs::write(path, serde_json::to_string(&SceneData::from(self.clone()))
            .expect("failed to serialize world data"))
            .expect("failed to save world file")
    }

    /// Get the global location of a voxel
    ///
    /// NOTE: Does not handle non-forward orientations
    fn get_voxel_location(&self, id: InstanceID, voxel_id: VoxelID) -> Coord {
        self.blocks[&id].2.get_global_structure(self.blocks[&id].0, self.blocks[&id].1)[&voxel_id]
    }

    /// Get world tick count
    pub fn get_ticks(&self) -> u32 {
        self.ticks
    }

    /// Get the voxels coordinates (global) associated with the provided block
    fn get_voxel_locations(&self, id: InstanceID) -> Vec<Coord> {
        self.blocks[&id].2
            .get_structure()
            .iter()
            .map(|(voxel_id, _)| self.get_voxel_location(id, voxel_id.clone()))
            .collect()
    }

    /// Get the circuit nodes associated with the provided block
    fn get_circuit_nodes(&self, id: InstanceID) -> HashMap<VoxelID, NodeID> {
        self.blocks[&id]
            .2
            .get_global_circuit_voxels(self.blocks[&id].0, self.blocks[&id].1)
            .iter()
            .map(|(voxel_id, coord)|
                (voxel_id.clone(), self.space.get(*coord).expect(&format!("voxel {} does not exist", voxel_id)).2.expect("voxel does not have a circuit node")))
            .collect()
    }

    /// Determine if any of the given node's edges are independently powered
    fn get_edge_independent_power(&self, node_id: NodeID) -> PowerState {
        self.circuit
            .neighbors(node_id)
            .any(|e| {
                let (id, voxel_id, _) = self.circuit.node_weight(e).unwrap();
                self.blocks[id].2.get_circuit_voxel_power()[voxel_id].unwrap_or(OFF)
            })
    }

    /// Save the circuit to a `.dot` file
    ///
    /// Requires the `dot` program to be in the system `PATH`
    #[allow(dead_code)]
    pub fn save_debug_circuit(&self, path: &Path) {
        let d = Dot::with_config(&self.circuit, &[]);
        std::fs::write(&path, format!("{:?}", d)).unwrap();
        Command::new("dot")
            .args(["-Tpng", path.to_str().unwrap(), "-o", &format!("{}.png", path.to_str().unwrap())])
            .spawn()
            .unwrap();
    }

    /// Retrieve a copy of all blocks and corresponding states
    pub fn get_world_blocks(&self) -> HashMap<InstanceID, (Coord, Orient, Block)> {
        self.blocks.clone()
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

                        let input_state = self.get_edge_independent_power(input_node);

                        // Compute output state

                        data.powered = input_state;
                    }
                    Block::ANDGate(data) => {
                        // Get named nodes
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.get_edge_independent_power(input_a_node);
                        let input_b_state = self.get_edge_independent_power(input_b_node);

                        // Compute output state

                        data.powered = input_a_state && input_b_state;
                    }
                    Block::ORGate(data) => {
                        // Get named nodes
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.get_edge_independent_power(input_a_node);
                        let input_b_state = self.get_edge_independent_power(input_b_node);

                        // Compute output state

                        data.powered = input_a_state || input_b_state;
                    }
                    Block::XORGate(data) => {
                        // Get named nodGrides
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.get_edge_independent_power(input_a_node);
                        let input_b_state = self.get_edge_independent_power(input_b_node);

                        // Compute output state

                        data.powered = input_a_state != input_b_state;
                    }
                    Block::NANDGate(data) => {
                        // Get named nodes
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.get_edge_independent_power(input_a_node);
                        let input_b_state = self.get_edge_independent_power(input_b_node);

                        // Compute output state

                        data.powered = !(input_a_state && input_b_state);
                    }
                    Block::NORGate(data) => {
                        // Get named nodes
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.get_edge_independent_power(input_a_node);
                        let input_b_state = self.get_edge_independent_power(input_b_node);

                        // Compute output state

                        data.powered = !(input_a_state || input_b_state);
                    }
                    Block::XNORGate(data) => {
                        // Get named nodes
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.get_edge_independent_power(input_a_node);
                        let input_b_state = self.get_edge_independent_power(input_b_node);

                        // Compute output state

                        data.powered = input_a_state == input_b_state;
                    }
                    Block::NOTGate(data) => {
                        // Get named nodes
                        let (input_node, _output_node) =
                            (node_ids[&circuit_voxel("in")], node_ids[&circuit_voxel("out")]);

                        // Get input node state

                        let input_state = self.get_edge_independent_power(input_node);

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
                    Block::Diode(data) => {
                        // Get named nodes
                        let (input_node, _output_node) =
                            (node_ids[&circuit_voxel("in")], node_ids[&circuit_voxel("out")]);

                        // Get input node state

                        let input_state = self.get_edge_independent_power(input_node);

                        // Compute output state

                        data.powered = input_state;
                    }
                    Block::ToggleLatch(data) => {
                        // Get named nodes
                        let (input_node, _output_node) =
                            (node_ids[&circuit_voxel("in")], node_ids[&circuit_voxel("out")]);

                        // Get input node state

                        let input_state = self.get_edge_independent_power(input_node);

                        // Compute output state

                        data.stored = if input_state && !data.powered {
                            !data.stored
                        } else {
                            data.stored
                        };
                        data.powered = input_state;
                    }
                    Block::PulseLatch(data) => {
                        // Get named nodes
                        let (input_node, _output_node) =
                            (node_ids[&circuit_voxel("in")], node_ids[&circuit_voxel("out")]);

                        // Get input node state

                        let input_state = self.get_edge_independent_power(input_node);

                        // Compute output state

                        data.pulse_battery = data.pulse_battery.checked_sub(1).unwrap_or(0) +
                            if input_state {
                                data.pulse_ticks
                            } else {
                                0
                            };
                        data.powered = data.pulse_battery > 0;
                    }
                    Block::MemoryLatch(data) => {
                        // Get named nodes
                        let (input_a_node, input_b_node, _output_node) =
                            (node_ids[&circuit_voxel("in_a")], node_ids[&circuit_voxel("in_b")], node_ids[&circuit_voxel("out")]);

                        // Get input node states

                        let input_a_state = self.get_edge_independent_power(input_a_node);
                        let input_b_state = self.get_edge_independent_power(input_b_node);

                        // Compute output state

                        data.stored = if input_a_state == input_b_state {
                            data.stored
                        } else {
                            if input_a_state {
                                ON
                            } else {
                                OFF
                            }
                        };
                        data.powered = data.stored;
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
            circuit: &StableGraph<(InstanceID, VoxelID, Coord), PowerState, Undirected, NodeIDType>,
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
                current_set.insert(node_id);
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

        // Determine wire networks' state (after this tick's non-wire circuit block updates)
        let contiguous_wire_networks_updated_state = contiguous_wire_networks
            .iter()
            .map(|wire_network| {
                for (_, node_id) in wire_network {
                    // Check if this node's non-wire neighbors are independently powered (i.e. powered from outside this wire network)
                    // Same as `self.get_edge_independent_power()` but uses the updated circuit block state
                    if self.circuit
                        .neighbors(*node_id)
                        .filter(|e| non_wire_circuit_blocks_updated.contains_key(&self.circuit.node_weight(*e).unwrap().0))
                        .any(|e| {
                            let (id, voxel_id, _) = self.circuit.node_weight(e).unwrap();
                            non_wire_circuit_blocks_updated[id].get_circuit_voxel_power()[voxel_id].unwrap_or(OFF)
                        }) {
                        return ON;
                    }
                }
                OFF
            })
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
        let edge_indices = self.circuit.edge_indices().collect::<Vec<_>>();
        for i in edge_indices {
            let (node_a, node_b) = self.circuit.edge_endpoints(i).unwrap();
            let (id_a, id_b) = (self.circuit.node_weight(node_a).unwrap().0, self.circuit.node_weight(node_b).unwrap().0);
            let (block_a, block_b) = (&self.blocks[&id_a].2, &self.blocks[&id_b].2);

            if block_a.get_circuit_power().unwrap_or(OFF) || block_b.get_circuit_power().unwrap_or(OFF) {
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
    ///
    /// Returns the block's ID or `None` if a block overlaps an existing block
    pub fn add_block(&mut self, block: Block, location: Coord, orientation: Orient) -> Option<InstanceID> {
        let id = self.blocks.len() as u32;
        self.add_block_with_id(id, block, location, orientation)
    }

    /// Add block with custom ID (internal use only)
    fn add_block_with_id(&mut self, id: InstanceID, block: Block, location: Coord, orientation: Orient) -> Option<InstanceID> {
        // Check if block overlaps existing block

        for (_, voxel_location) in block.get_global_structure(location, orientation) {
            if self.space.contains(voxel_location) {
                return None;
            }
        }

        // Add block

        assert!(self.blocks.insert(id, (location, orientation, block.clone())).is_none());

        // Add block connections and voxels

        // Filter out non-circuit voxels and add to circuit
        let terminal_node_ids = block.get_structure()
            .iter()
            .filter(|e| is_circuit_voxel(&e.0))
            .map(|(tid, gc)| (tid.clone(), self.circuit.add_node((id, tid.clone(), *gc))))
            .collect::<HashMap<VoxelID, NodeID>>();

        // Add all voxels (including non-terminals) to grid

        for (voxel_id, voxel_location) in block.get_global_structure(location, orientation) {
            self.space.set(voxel_location,
                           (id, voxel_id.clone(), terminal_node_ids.get(&voxel_id).copied()));
        }

        // Iterate over the circuit voxels (terminals) that make up this block
        for (terminal_voxel_id, terminal_node_id) in terminal_node_ids {
            let terminal_parent_id = self.circuit.node_weight(terminal_node_id).unwrap().0;

            let terminal_voxel_global_location = self.get_voxel_location(terminal_parent_id, terminal_voxel_id);

            // Iterate over adjacent voxels
            for (_, (neighbor_parent_id, neighbor_voxel_id, neighbor_node_id))
            in self.space.get_adjacent(terminal_voxel_global_location) {
                // Skip non-circuit voxels
                if !is_circuit_voxel(neighbor_voxel_id) {
                    continue;
                }

                // Add connection to circuit if not part of the same block
                if *neighbor_parent_id != terminal_parent_id {
                    self.circuit.update_edge(terminal_node_id, neighbor_node_id.unwrap(), OFF);
                }
            }
        }

        Some(id)
    }

    /// Convenience function used when a block needs to be moved and change state
    pub fn replace_block(&mut self, id: InstanceID, block: Block, location: Coord, orientation: Orient) -> Option<()> {
        self.remove_block(id)?;
        self.add_block_with_id(id, block, location, orientation).map(|_| ())
    }

    /// Get block data
    pub fn get_block(&self, id: InstanceID) -> Option<(Coord, Orient, Block)> {
        self.blocks.get(&id).cloned()
    }

    /// Updates the block state with the given ID
    ///
    /// Returns the old block state
    pub fn update_block(&mut self, id: InstanceID, block: Block) -> Option<Block> {
        self.blocks
            .get_mut(&id)
            .map(|(_, _, b)| {
                let current = b.clone();
                *b = block;
                current
            })
    }

    /// Removes the block with the given ID from all internal data structures
    pub fn remove_block(&mut self, id: InstanceID) -> Option<(Coord, Orient, Block)> {
        if !self.blocks.contains_key(&id) {
            return None;
        }

        for (_, node_id) in self.get_circuit_nodes(id) {
            self.circuit.remove_node(node_id).unwrap();
        }

        for location in self.get_voxel_locations(id) {
            self.space.remove(location).unwrap();
        }

        self.blocks.remove(&id)
    }

    /// Add a wire that follows the given path
    ///
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
pub fn scene_solo_test()
{
    #[allow(unused_imports)]
    use cgmath::Vector3;

    let mut scene = Scene::default();

    scene.add_block(Block::Wire(VoxelPowered::default()), Vector3::unit_y(), Default::default()).unwrap();

    let deltas = scene.simulate_tick();

    dbg!(deltas);

    println!("successfully added wire");
}

#[ignore]
#[test]
pub fn scene_gate_test()
{
    let mut scene = Scene::default();

    let l = 10;
    let _ = scene.add_wire((0..l).map(|e| Coord::new(-1, 0, e)).collect()).unwrap();
    let _ = scene.add_wire((0..l).map(|e| Coord::new(1, 0, e)).collect()).unwrap();
    let _ = scene.add_wire((0..l).map(|e| Coord::new(0, 0, l + 3 + e)).collect()).unwrap();
    let _clock_a = scene.add_block(Block::Clock(VoxelClock {
        rate: 5,
        start_tick: 0,
        powered: false,
    }), Coord::new(-1, 0, -1), Default::default()).unwrap();
    let _clock_b = scene.add_block(Block::Clock(VoxelClock {
        rate: 3,
        start_tick: 0,
        powered: false,
    }), Coord::new(1, 0, -1), Default::default()).unwrap();
    let _gate = scene.add_block(Block::ANDGate(VoxelPowered {
        powered: false,
    }), Coord::new(0, 0, l + 1), Default::default()).unwrap();

    scene.save_debug_circuit(&Path::new("./result-initial.dot"));

    for i in 1..=20u32 {
        let _deltas = scene.simulate_tick();

        scene.save_debug_circuit(&Path::new(&format!("./result-tick-{}.dot", i)));
    }
}

#[ignore]
#[test]
pub fn scene_wire_test()
{
    let mut scene = Scene::default();

    let _ = scene.add_wire((0..10).map(|e| Coord::new(0, 0, e)).collect()).unwrap();
    let _ = scene.add_wire((0..10).map(|e| Coord::new(-5 + e, 1, 0)).collect()).unwrap();
    let _toggle = scene.add_block(Block::Toggle(VoxelPowered {
        powered: false,
    }), Coord::new(0, 2, 0), Default::default()).unwrap();
    let _clock = scene.add_block(Block::Clock(VoxelClock {
        rate: 3,
        start_tick: 2,
        powered: false,
    }), Coord::new(-2, 2, 0), Default::default()).unwrap();

    scene.save_debug_circuit(&Path::new("./generated/result-initial.dot"));

    for i in 1..=20u32 {
        let deltas = scene.simulate_tick();
        dbg!(&deltas);

        #[cfg(feature = "")]
        if let Block::Toggle(b) = &mut scene.blocks.get_mut(&toggle).unwrap().2 {
            b.powered = i % 2 == 0;
        }

        scene.save_debug_circuit(&Path::new(&format!("./generated/result-tick-{}.dot", i)));
    }
}