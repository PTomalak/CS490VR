use std::collections::HashMap;
use std::path::Path;
use std::process::Command;

use cgmath::Array;
use petgraph::{Graph, Undirected};
use petgraph::dot::Dot;
use petgraph::graph::NodeIndex;
use serde::{Deserialize, Serialize};

use crate::block::{Block, is_circuit_voxel, Orient, VoxelID};
use crate::grid::{Coord, Grid};

pub type InstanceID = u32;
pub type WireDummy = u32;

pub type NodeIDType = u32;
pub type NodeID = NodeIndex<NodeIDType>;

pub const BLANK: WireDummy = 0;

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct Scene
{
    blocks: HashMap<InstanceID, (Coord, Orient, Block)>,
    circuit: Graph<(InstanceID, VoxelID, Coord), WireDummy, Undirected, NodeIDType>,
    space: Grid<(InstanceID, VoxelID, Option<NodeID>)>,
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
            let (terminal_voxel_parent_id, _, _) = self.circuit.node_weight(terminal_node_id).unwrap();

            let terminal_voxel_global_location = self.get_voxel_location(*terminal_voxel_parent_id, terminal_voxel_id);

            // Iterate over adjacent voxels
            for (_, (_, _, neighbor_node_id))
            in self.space.get_adjacent(terminal_voxel_global_location) {
                // Add connection to circuit
                self.circuit.update_edge(terminal_node_id, neighbor_node_id.unwrap(), BLANK);
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